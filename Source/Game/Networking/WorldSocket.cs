﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net.Sockets;
using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Framework.IO;
using Framework.Networking;
using Game.Networking.Packets;

namespace Game.Networking;

public class WorldSocket : SocketBase
{
	static readonly string ClientConnectionInitialize = "WORLD OF WARCRAFT CONNECTION - CLIENT TO SERVER - V2";
	static readonly string ServerConnectionInitialize = "WORLD OF WARCRAFT CONNECTION - SERVER TO CLIENT - V2";

	static readonly byte[] AuthCheckSeed =
	{
		0xC5, 0xC6, 0x98, 0x95, 0x76, 0x3F, 0x1D, 0xCD, 0xB6, 0xA1, 0x37, 0x28, 0xB3, 0x12, 0xFF, 0x8A
	};

	static readonly byte[] SessionKeySeed =
	{
		0x58, 0xCB, 0xCF, 0x40, 0xFE, 0x2E, 0xCE, 0xA6, 0x5A, 0x90, 0xB8, 0x01, 0x68, 0x6C, 0x28, 0x0B
	};

	static readonly byte[] ContinuedSessionSeed =
	{
		0x16, 0xAD, 0x0C, 0xD4, 0x46, 0xF9, 0x4F, 0xB2, 0xEF, 0x7D, 0xEA, 0x2A, 0x17, 0x66, 0x4D, 0x2F
	};

	static readonly byte[] EncryptionKeySeed =
	{
		0xE9, 0x75, 0x3C, 0x50, 0x90, 0x93, 0x61, 0xDA, 0x3B, 0x07, 0xEE, 0xFA, 0xFF, 0x9D, 0x41, 0xB8
	};

	static readonly int HeaderSize = 16;
	readonly SocketBuffer _headerBuffer;
	readonly SocketBuffer _packetBuffer;
	readonly WorldCrypt _worldCrypt;
	readonly byte[] _encryptKey;
	readonly object _worldSessionLock = new();

	ConnectionType _connectType;
	ulong _key;

	byte[] _serverChallenge;
	byte[] _sessionKey;

	long _LastPingTime;
	uint _OverSpeedPings;
	WorldSession _worldSession;

	ZLib.z_stream _compressionStream;

	AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
	string _ipCountry;
	readonly object _sendlock = new();

	public WorldSocket(Socket socket) : base(socket)
	{
		_connectType = ConnectionType.Realm;
		_serverChallenge = Array.Empty<byte>().GenerateRandomKey(16);
		_worldCrypt = new WorldCrypt();

		_encryptKey = new byte[16];

		_headerBuffer = new SocketBuffer(HeaderSize);
		_packetBuffer = new SocketBuffer(0);
	}

	public override void Dispose()
	{
		_worldSession = null;
		_serverChallenge = null;
		_queryProcessor = null;
		_sessionKey = null;
		_compressionStream = null;

		base.Dispose();
	}

	public override void Accept()
	{
		var ip_address = GetRemoteIpAddress().ToString();

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SelIpInfo);
		stmt.AddValue(0, ip_address);
		stmt.AddValue(1, BitConverter.ToUInt32(GetRemoteIpAddress().Address.GetAddressBytes(), 0));

		_queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(CheckIpCallback));
	}

	public override void ReadHandler(SocketAsyncEventArgs args)
	{
		if (!IsOpen())
			return;

		var currentReadIndex = 0;

		while (currentReadIndex < args.BytesTransferred)
		{
			if (_headerBuffer.GetRemainingSpace() > 0)
			{
				// need to receive the header
				var readHeaderSize = Math.Min(args.BytesTransferred - currentReadIndex, _headerBuffer.GetRemainingSpace());
				_headerBuffer.Write(args.Buffer, currentReadIndex, readHeaderSize);
				currentReadIndex += readHeaderSize;

				if (_headerBuffer.GetRemainingSpace() > 0)
					break; // Couldn't receive the whole header this time.

				// We just received nice new header
				if (!ReadHeader())
				{
					CloseSocket();

					return;
				}
			}

			// We have full read header, now check the data payload
			if (_packetBuffer.GetRemainingSpace() > 0)
			{
				// need more data in the payload
				var readDataSize = Math.Min(args.BytesTransferred - currentReadIndex, _packetBuffer.GetRemainingSpace());
				_packetBuffer.Write(args.Buffer, currentReadIndex, readDataSize);
				currentReadIndex += readDataSize;

				if (_packetBuffer.GetRemainingSpace() > 0)
					break; // Couldn't receive the whole data this time.
			}

			// just received fresh new payload
			var result = ReadData();
			_headerBuffer.Reset();

			if (result != ReadDataHandlerResult.Ok)
			{
				if (result != ReadDataHandlerResult.WaitingForQuery)
					CloseSocket();

				return;
			}
		}

		AsyncRead();
	}

	public void SendPacket(ServerPacket packet)
	{
		if (!IsOpen() || _serverChallenge == null)
			return;

		// SendPacket may be called from multiple threads.
		lock (_sendlock)
		{
			try
			{
				packet.LogPacket(_worldSession);
				packet.WritePacketData();
				Log.outTrace(LogFilter.Network, "Received opcode: {0} ({1})", (ServerOpcodes)packet.GetOpcode(), (uint)packet.GetOpcode());

				var data = packet.GetData();
				var opcode = packet.GetOpcode();
				PacketLog.Write(data, (uint)opcode, GetRemoteIpAddress(), _connectType, false);

				ByteBuffer buffer = new();

				var packetSize = data.Length;

				if (packetSize > 0x400 && _worldCrypt.IsInitialized)
				{
					buffer.WriteInt32(packetSize + 2);
					buffer.WriteUInt32(ZLib.adler32(ZLib.adler32(0x9827D8F1, BitConverter.GetBytes((ushort)opcode), 2), data, (uint)packetSize));

					var compressedSize = CompressPacket(data, opcode, out var compressedData);
					buffer.WriteUInt32(ZLib.adler32(0x9827D8F1, compressedData, compressedSize));
					buffer.WriteBytes(compressedData, compressedSize);

					packetSize = (int)(compressedSize + 12);
					opcode = ServerOpcodes.CompressedPacket;

					data = buffer.GetData();
				}

				buffer = new ByteBuffer();
				buffer.WriteUInt16((ushort)opcode);
				buffer.WriteBytes(data);
				packetSize += 2 /*opcode*/;

				data = buffer.GetData();

				PacketHeader header = new();
				header.Size = packetSize;
				_worldCrypt.Encrypt(ref data, ref header.Tag);

				ByteBuffer byteBuffer = new();
				header.Write(byteBuffer);
				byteBuffer.WriteBytes(data);

				AsyncWrite(byteBuffer.GetData()); // LIES not async.
			}
			catch (Exception ex)
			{
				Log.outException(ex);
			}
		}
	}

	public void SetWorldSession(WorldSession session)
	{
		lock (_worldSessionLock)
		{
			_worldSession = session;
		}
	}

	public uint CompressPacket(byte[] data, ServerOpcodes opcode, out byte[] outData)
	{
		var uncompressedData = BitConverter.GetBytes((ushort)opcode).Combine(data);

		var bufferSize = ZLib.deflateBound(_compressionStream, (uint)data.Length);
		outData = new byte[bufferSize];

		_compressionStream.next_out = 0;
		_compressionStream.avail_out = bufferSize;
		_compressionStream.out_buf = outData;

		_compressionStream.next_in = 0;
		_compressionStream.avail_in = (uint)uncompressedData.Length;
		_compressionStream.in_buf = uncompressedData;

		var z_res = ZLib.deflate(_compressionStream, 2);

		if (z_res != 0)
		{
			Log.outError(LogFilter.Network, "Can't compress packet data (zlib: deflate) Error code: {0} msg: {1}", z_res, _compressionStream.msg);

			return 0;
		}

		return bufferSize - _compressionStream.avail_out;
	}

	public override bool Update()
	{
		if (!base.Update())
			return false;

		_queryProcessor.ProcessReadyCallbacks();

		return true;
	}

	public override void OnClose()
	{
		lock (_worldSessionLock)
		{
			_worldSession = null;
		}

		base.OnClose();
	}

	public void SendAuthResponseError(BattlenetRpcErrorCode code)
	{
		AuthResponse response = new();
		response.SuccessInfo = null;
		response.WaitInfo = null;
		response.Result = code;
		SendPacket(response);
	}

	void CheckIpCallback(SQLResult result)
	{
		if (!result.IsEmpty())
		{
			var banned = false;

			do
			{
				if (result.Read<ulong>(0) != 0)
					banned = true;

				_ipCountry = result.Read<string>(1);
			} while (result.NextRow());

			if (banned)
			{
				Log.outError(LogFilter.Network, "WorldSocket.Connect: Sent Auth Response (IP {0} banned).", GetRemoteIpAddress().ToString());
				CloseSocket();

				return;
			}
		}

		_packetBuffer.Resize(ClientConnectionInitialize.Length + 1);

		AsyncReadWithCallback(InitializeHandler);

		ByteBuffer packet = new();
		packet.WriteString(ServerConnectionInitialize);
		packet.WriteString("\n");
		AsyncWrite(packet.GetData());
	}

	void InitializeHandler(SocketAsyncEventArgs args)
	{
		if (args.SocketError != SocketError.Success)
		{
			CloseSocket();

			return;
		}

		if (args.BytesTransferred > 0)
			if (_packetBuffer.GetRemainingSpace() > 0)
			{
				// need to receive the header
				var readHeaderSize = Math.Min(args.BytesTransferred, _packetBuffer.GetRemainingSpace());
				_packetBuffer.Write(args.Buffer, 0, readHeaderSize);

				if (_packetBuffer.GetRemainingSpace() > 0)
				{
					// Couldn't receive the whole header this time.
					AsyncReadWithCallback(InitializeHandler);

					return;
				}

				ByteBuffer buffer = new(_packetBuffer.GetData());
				var initializer = buffer.ReadString((uint)ClientConnectionInitialize.Length);

				if (initializer != ClientConnectionInitialize)
				{
					CloseSocket();

					return;
				}

				var terminator = buffer.ReadUInt8();

				if (terminator != '\n')
				{
					CloseSocket();

					return;
				}

				// Initialize the zlib stream
				_compressionStream = new ZLib.z_stream();

				// Initialize the deflate algo...
				var z_res1 = ZLib.deflateInit2(_compressionStream, 1, 8, -15, 8, 0);

				if (z_res1 != 0)
				{
					CloseSocket();
					Log.outError(LogFilter.Network, "Can't initialize packet compression (zlib: deflateInit2_) Error code: {0}", z_res1);

					return;
				}

				_packetBuffer.Resize(0);
				_packetBuffer.Reset();
				HandleSendAuthSession();
				AsyncRead();

				return;
			}
	}

	bool ReadHeader()
	{
		PacketHeader header = new();
		header.Read(_headerBuffer.GetData());

		_packetBuffer.Resize(header.Size);

		return true;
	}

	ReadDataHandlerResult ReadData()
	{
		PacketHeader header = new();
		header.Read(_headerBuffer.GetData());

		if (!_worldCrypt.Decrypt(_packetBuffer.GetData(), header.Tag))
		{
			Log.outError(LogFilter.Network, $"WorldSocket.ReadData(): client {GetRemoteIpAddress()} failed to decrypt packet (size: {header.Size})");

			return ReadDataHandlerResult.Error;
		}

		WorldPacket packet = new(_packetBuffer.GetData());
		_packetBuffer.Reset();

		if (packet.GetOpcode() >= (int)ClientOpcodes.Max)
		{
			Log.outError(LogFilter.Network, $"WorldSocket.ReadData(): client {GetRemoteIpAddress()} sent wrong opcode (opcode: {packet.GetOpcode()})");
			Log.outError(LogFilter.Network, $"Header: {_headerBuffer.GetData().ToHexString()} Data: {_packetBuffer.GetData().ToHexString()}");

			return ReadDataHandlerResult.Error;
		}

		PacketLog.Write(packet.GetData(), packet.GetOpcode(), GetRemoteIpAddress(), _connectType, true);

		var opcode = (ClientOpcodes)packet.GetOpcode();

		if (opcode != ClientOpcodes.HotfixRequest && !header.IsValidSize())
		{
			Log.outError(LogFilter.Network, $"WorldSocket.ReadHeaderHandler(): client {GetRemoteIpAddress()} sent malformed packet (size: {header.Size})");

			return ReadDataHandlerResult.Error;
		}

		switch (opcode)
		{
			case ClientOpcodes.Ping:
				Ping ping = new(packet);
				ping.Read();

				if (!HandlePing(ping))
					return ReadDataHandlerResult.Error;

				break;
			case ClientOpcodes.AuthSession:
				if (_worldSession != null)
				{
					Log.outError(LogFilter.Network, $"WorldSocket.ReadData(): received duplicate CMSG_AUTH_SESSION from {_worldSession.GetPlayerInfo()}");

					return ReadDataHandlerResult.Error;
				}

				AuthSession authSession = new(packet);
				authSession.Read();
				HandleAuthSession(authSession);

				return ReadDataHandlerResult.WaitingForQuery;
			case ClientOpcodes.AuthContinuedSession:
				if (_worldSession != null)
				{
					Log.outError(LogFilter.Network, $"WorldSocket.ReadData(): received duplicate CMSG_AUTH_CONTINUED_SESSION from {_worldSession.GetPlayerInfo()}");

					return ReadDataHandlerResult.Error;
				}

				AuthContinuedSession authContinuedSession = new(packet);
				authContinuedSession.Read();
				HandleAuthContinuedSession(authContinuedSession);

				return ReadDataHandlerResult.WaitingForQuery;
			case ClientOpcodes.KeepAlive:
				if (_worldSession != null)
				{
					_worldSession.ResetTimeOutTime(true);

					return ReadDataHandlerResult.Ok;
				}

				Log.outError(LogFilter.Network, $"WorldSocket::ReadDataHandler: client {GetRemoteIpAddress()} sent CMSG_KEEP_ALIVE without being authenticated");

				return ReadDataHandlerResult.Error;
			case ClientOpcodes.LogDisconnect:
				break;
			case ClientOpcodes.EnableNagle:
				SetNoDelay(false);

				break;
			case ClientOpcodes.ConnectToFailed:
				ConnectToFailed connectToFailed = new(packet);
				connectToFailed.Read();
				HandleConnectToFailed(connectToFailed);

				break;
			case ClientOpcodes.EnterEncryptedModeAck:
				HandleEnterEncryptedModeAck();

				break;
			default:
				lock (_worldSessionLock)
				{
					if (_worldSession == null)
					{
						Log.outError(LogFilter.Network, $"ProcessIncoming: Client not authed opcode = {opcode}");

						return ReadDataHandlerResult.Error;
					}

					Log.outTrace(LogFilter.Network, "Received opcode: {0} ({1})", (ClientOpcodes)packet.GetOpcode(), packet.GetOpcode());

					if (!PacketManager.ContainsHandler(opcode))
					{
						Log.outError(LogFilter.Network, $"No defined handler for opcode {opcode} ({packet.GetOpcode()}) sent by {_worldSession.GetPlayerInfo()}");

						break;
					}

					if (opcode == ClientOpcodes.TimeSyncResponse)
						packet.SetReceiveTime(DateTime.Now);

					// Our Idle timer will reset on any non PING opcodes on login screen, allowing us to catch people idling.
					_worldSession.ResetTimeOutTime(false);

					// Copy the packet to the heap before enqueuing
					_worldSession.QueuePacket(packet);
				}

				break;
		}

		return ReadDataHandlerResult.Ok;
	}

	void HandleSendAuthSession()
	{
		AuthChallenge challenge = new();
		challenge.Challenge = _serverChallenge;
		challenge.DosChallenge = new byte[32].GenerateRandomKey(32);
		challenge.DosZeroBits = 1;

		SendPacket(challenge);
	}

	void HandleAuthSession(AuthSession authSession)
	{
		// Get the account information from the realmd database
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_INFO_BY_NAME);
		stmt.AddValue(0, Global.WorldMgr.Realm.Id.Index);
		stmt.AddValue(1, authSession.RealmJoinTicket);

		_queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(HandleAuthSessionCallback, authSession));
	}

	void HandleAuthSessionCallback(AuthSession authSession, SQLResult result)
	{
		// Stop if the account is not found
		if (result.IsEmpty())
		{
			Log.outError(LogFilter.Network, "HandleAuthSession: Sent Auth Response (unknown account).");
			CloseSocket();

			return;
		}

		var buildInfo = Global.RealmMgr.GetBuildInfo(Global.WorldMgr.Realm.Build);

		if (buildInfo == null)
		{
			SendAuthResponseError(BattlenetRpcErrorCode.BadVersion);
			Log.outError(LogFilter.Network, $"WorldSocket.HandleAuthSessionCallback: Missing auth seed for realm build {Global.WorldMgr.Realm.Build} ({GetRemoteIpAddress()}).");
			CloseSocket();

			return;
		}

		AccountInfo account = new(result.GetFields());

		// For hook purposes, we get Remoteaddress at this point.
		var address = GetRemoteIpAddress();

		Sha256 digestKeyHash = new();
		digestKeyHash.Process(account.game.SessionKey, account.game.SessionKey.Length);

		if (account.game.OS == "Wn64")
		{
			digestKeyHash.Finish(buildInfo.Win64AuthSeed);
		}
		else if (account.game.OS == "Mc64")
		{
			digestKeyHash.Finish(buildInfo.Mac64AuthSeed);
		}
		else
		{
			Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: Authentication failed for account: {0} ('{1}') address: {2}", account.game.Id, authSession.RealmJoinTicket, address);
			CloseSocket();

			return;
		}

		HmacSha256 hmac = new(digestKeyHash.Digest);
		hmac.Process(authSession.LocalChallenge, authSession.LocalChallenge.Count);
		hmac.Process(_serverChallenge, 16);
		hmac.Finish(AuthCheckSeed, 16);

		// Check that Key and account name are the same on client and server
		if (!hmac.Digest.Compare(authSession.Digest))
		{
			Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: Authentication failed for account: {0} ('{1}') address: {2}", account.game.Id, authSession.RealmJoinTicket, address);
			CloseSocket();

			return;
		}

		Sha256 keyData = new();
		keyData.Finish(account.game.SessionKey);

		HmacSha256 sessionKeyHmac = new(keyData.Digest);
		sessionKeyHmac.Process(_serverChallenge, 16);
		sessionKeyHmac.Process(authSession.LocalChallenge, authSession.LocalChallenge.Count);
		sessionKeyHmac.Finish(SessionKeySeed, 16);

		_sessionKey = new byte[40];
		var sessionKeyGenerator = new SessionKeyGenerator256(sessionKeyHmac.Digest, 32);
		sessionKeyGenerator.Generate(_sessionKey, 40);

		HmacSha256 encryptKeyGen = new(_sessionKey);
		encryptKeyGen.Process(authSession.LocalChallenge, authSession.LocalChallenge.Count);
		encryptKeyGen.Process(_serverChallenge, 16);
		encryptKeyGen.Finish(EncryptionKeySeed, 16);

		// only first 16 bytes of the hmac are used
		Buffer.BlockCopy(encryptKeyGen.Digest, 0, _encryptKey, 0, 16);

		PreparedStatement stmt = null;

		if (WorldConfig.GetBoolValue(WorldCfg.AllowLogginIpAddressesInDatabase))
		{
			// As we don't know if attempted login process by ip works, we update last_attempt_ip right away
			stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_LAST_ATTEMPT_IP);
			stmt.AddValue(0, address.Address.ToString());
			stmt.AddValue(1, authSession.RealmJoinTicket);
			DB.Login.Execute(stmt);
			// This also allows to check for possible "hack" attempts on account
		}

		stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_INFO_CONTINUED_SESSION);
		stmt.AddValue(0, _sessionKey);
		stmt.AddValue(1, account.game.Id);
		DB.Login.Execute(stmt);

		// First reject the connection if packet contains invalid data or realm state doesn't allow logging in
		if (Global.WorldMgr.IsClosed)
		{
			SendAuthResponseError(BattlenetRpcErrorCode.Denied);
			Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: World closed, denying client ({0}).", GetRemoteIpAddress());
			CloseSocket();

			return;
		}

		if (authSession.RealmID != Global.WorldMgr.Realm.Id.Index)
		{
			SendAuthResponseError(BattlenetRpcErrorCode.Denied);

			Log.outError(LogFilter.Network,
						"WorldSocket.HandleAuthSession: Client {0} requested connecting with realm id {1} but this realm has id {2} set in config.",
						GetRemoteIpAddress().ToString(),
						authSession.RealmID,
						Global.WorldMgr.Realm.Id.Index);

			CloseSocket();

			return;
		}

		// Must be done before WorldSession is created
		var wardenActive = WorldConfig.GetBoolValue(WorldCfg.WardenEnabled);

		if (wardenActive && account.game.OS != "Win" && account.game.OS != "Wn64" && account.game.OS != "Mc64")
		{
			SendAuthResponseError(BattlenetRpcErrorCode.Denied);
			Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: Client {0} attempted to log in using invalid client OS ({1}).", address, account.game.OS);
			CloseSocket();

			return;
		}

		//Re-check ip locking (same check as in auth).
		if (account.battleNet.IsLockedToIP) // if ip is locked
		{
			if (account.battleNet.LastIP != address.Address.ToString())
			{
				SendAuthResponseError(BattlenetRpcErrorCode.RiskAccountLocked);
				Log.outDebug(LogFilter.Network, "HandleAuthSession: Sent Auth Response (Account IP differs).");
				CloseSocket();

				return;
			}
		}
		else if (!account.battleNet.LockCountry.IsEmpty() && account.battleNet.LockCountry != "00" && !_ipCountry.IsEmpty())
		{
			if (account.battleNet.LockCountry != _ipCountry)
			{
				SendAuthResponseError(BattlenetRpcErrorCode.RiskAccountLocked);
				Log.outDebug(LogFilter.Network, "WorldSocket.HandleAuthSession: Sent Auth Response (Account country differs. Original country: {0}, new country: {1}).", account.battleNet.LockCountry, _ipCountry);
				CloseSocket();

				return;
			}
		}

		var mutetime = account.game.MuteTime;

		//! Negative mutetime indicates amount of seconds to be muted effective on next login - which is now.
		if (mutetime < 0)
		{
			mutetime = GameTime.GetGameTime() + mutetime;

			stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME_LOGIN);
			stmt.AddValue(0, mutetime);
			stmt.AddValue(1, account.game.Id);
			DB.Login.Execute(stmt);
		}

		if (account.IsBanned()) // if account banned
		{
			SendAuthResponseError(BattlenetRpcErrorCode.GameAccountBanned);
			Log.outError(LogFilter.Network, "WorldSocket:HandleAuthSession: Sent Auth Response (Account banned).");
			CloseSocket();

			return;
		}

		// Check locked state for server
		var allowedAccountType = Global.WorldMgr.PlayerSecurityLimit;

		if (allowedAccountType > AccountTypes.Player && account.game.Security < allowedAccountType)
		{
			SendAuthResponseError(BattlenetRpcErrorCode.ServerIsPrivate);
			Log.outInfo(LogFilter.Network, "WorldSocket:HandleAuthSession: User tries to login but his security level is not enough");
			CloseSocket();

			return;
		}

		Log.outDebug(LogFilter.Network, "WorldSocket:HandleAuthSession: Client '{0}' authenticated successfully from {1}.", authSession.RealmJoinTicket, address);

		if (WorldConfig.GetBoolValue(WorldCfg.AllowLogginIpAddressesInDatabase))
		{
			// Update the last_ip in the database
			stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_LAST_IP);
			stmt.AddValue(0, address.Address.ToString());
			stmt.AddValue(1, authSession.RealmJoinTicket);
			DB.Login.Execute(stmt);
		}

		_worldSession = new WorldSession(account.game.Id,
										authSession.RealmJoinTicket,
										account.battleNet.Id,
										this,
										account.game.Security,
										(Expansion)account.game.Expansion,
										mutetime,
										account.game.OS,
										account.battleNet.Locale,
										account.game.Recruiter,
										account.game.IsRectuiter);

		// Initialize Warden system only if it is enabled by config
		//if (wardenActive)
		//_worldSession.InitWarden(_sessionKey);

		_queryProcessor.AddCallback(_worldSession.LoadPermissionsAsync().WithCallback(LoadSessionPermissionsCallback));
		AsyncRead();
	}

	void LoadSessionPermissionsCallback(SQLResult result)
	{
		// RBAC must be loaded before adding session to check for skip queue permission
		// RBAC must be loaded before adding session to check for skip queue permission

		if (_worldSession == null)
		{
			SendAuthResponseError(BattlenetRpcErrorCode.TimedOut);

			return;
		}

		_worldSession.RBACData.LoadFromDBCallback(result);

		SendPacket(new EnterEncryptedMode(_encryptKey, true));
	}

	void HandleAuthContinuedSession(AuthContinuedSession authSession)
	{
		ConnectToKey key = new();
		_key = key.Raw = authSession.Key;

		_connectType = key.connectionType;

		if (_connectType != ConnectionType.Instance)
		{
			SendAuthResponseError(BattlenetRpcErrorCode.Denied);
			CloseSocket();

			return;
		}

		var accountId = key.AccountId;
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_INFO_CONTINUED_SESSION);
		stmt.AddValue(0, accountId);

		_queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(HandleAuthContinuedSessionCallback, authSession));
	}

	void HandleAuthContinuedSessionCallback(AuthContinuedSession authSession, SQLResult result)
	{
		if (result.IsEmpty())
		{
			SendAuthResponseError(BattlenetRpcErrorCode.Denied);
			CloseSocket();

			return;
		}

		ConnectToKey key = new();
		_key = key.Raw = authSession.Key;

		var accountId = key.AccountId;
		var login = result.Read<string>(0);
		_sessionKey = result.Read<byte[]>(1);

		HmacSha256 hmac = new(_sessionKey);
		hmac.Process(BitConverter.GetBytes(authSession.Key), 8);
		hmac.Process(authSession.LocalChallenge, authSession.LocalChallenge.Length);
		hmac.Process(_serverChallenge, 16);
		hmac.Finish(ContinuedSessionSeed, 16);

		if (!hmac.Digest.Compare(authSession.Digest))
		{
			Log.outError(LogFilter.Network, "WorldSocket.HandleAuthContinuedSession: Authentication failed for account: {0} ('{1}') address: {2}", accountId, login, GetRemoteIpAddress());
			CloseSocket();

			return;
		}

		HmacSha256 encryptKeyGen = new(_sessionKey);
		encryptKeyGen.Process(authSession.LocalChallenge, authSession.LocalChallenge.Length);
		encryptKeyGen.Process(_serverChallenge, 16);
		encryptKeyGen.Finish(EncryptionKeySeed, 16);

		// only first 16 bytes of the hmac are used
		Buffer.BlockCopy(encryptKeyGen.Digest, 0, _encryptKey, 0, 16);

		SendPacket(new EnterEncryptedMode(_encryptKey, true));
		AsyncRead();
	}

	void HandleConnectToFailed(ConnectToFailed connectToFailed)
	{
		if (_worldSession != null)
			if (_worldSession.PlayerLoading)
				switch (connectToFailed.Serial)
				{
					case ConnectToSerial.WorldAttempt1:
						_worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt2);

						break;
					case ConnectToSerial.WorldAttempt2:
						_worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt3);

						break;
					case ConnectToSerial.WorldAttempt3:
						_worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt4);

						break;
					case ConnectToSerial.WorldAttempt4:
						_worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt5);

						break;
					case ConnectToSerial.WorldAttempt5:
					{
						Log.outError(LogFilter.Network, "{0} failed to connect 5 times to world socket, aborting login", _worldSession.GetPlayerInfo());
						_worldSession.AbortLogin(LoginFailureReason.NoWorld);

						break;
					}
					default:
						return;
				}
		//else
		//{
		//    transfer_aborted when/if we get map node redirection
		//    SendPacket(*WorldPackets.Auth.ResumeComms());
		//}
	}

	void HandleEnterEncryptedModeAck()
	{
		_worldCrypt.Initialize(_encryptKey);

		if (_connectType == ConnectionType.Realm)
			Global.WorldMgr.AddSession(_worldSession);
		else
			Global.WorldMgr.AddInstanceSocket(this, _key);
	}

	bool HandlePing(Ping ping)
	{
		if (_LastPingTime == 0)
		{
			_LastPingTime = GameTime.GetGameTime(); // for 1st ping
		}
		else
		{
			var now = GameTime.GetGameTime();
			var diff = now - _LastPingTime;
			_LastPingTime = now;

			if (diff < 27)
			{
				++_OverSpeedPings;

				var maxAllowed = WorldConfig.GetUIntValue(WorldCfg.MaxOverspeedPings);

				if (maxAllowed != 0 && _OverSpeedPings > maxAllowed)
					lock (_worldSessionLock)
					{
						if (_worldSession != null && !_worldSession.HasPermission(RBACPermissions.SkipCheckOverspeedPing))
							Log.outError(LogFilter.Network, "WorldSocket:HandlePing: {0} kicked for over-speed pings (address: {1})", _worldSession.GetPlayerInfo(), GetRemoteIpAddress());
						//return ReadDataHandlerResult.Error;
					}
			}
			else
			{
				_OverSpeedPings = 0;
			}
		}

		lock (_worldSessionLock)
		{
			if (_worldSession != null)
			{
				_worldSession.Latency = ping.Latency;
			}
			else
			{
				Log.outError(LogFilter.Network, "WorldSocket:HandlePing: peer sent CMSG_PING, but is not authenticated or got recently kicked, address = {0}", GetRemoteIpAddress());

				return false;
			}
		}

		SendPacket(new Pong(ping.Serial));

		return true;
	}
}