﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class MountResultPacket : ServerPacket
{
	public uint Result;
	public MountResultPacket() : base(ServerOpcodes.MountResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Result);
	}
}