﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Mails;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	public void SendShowMailBox(ObjectGuid guid)
	{
		NPCInteractionOpenResult npcInteraction = new();
		npcInteraction.Npc = guid;
		npcInteraction.InteractionType = PlayerInteractionType.MailInfo;
		npcInteraction.Success = true;
		SendPacket(npcInteraction);
	}

	bool CanOpenMailBox(ObjectGuid guid)
	{
		if (guid == Player.GUID)
		{
			if (!HasPermission(RBACPermissions.CommandMailbox))
			{
				Log.outWarn(LogFilter.ChatSystem, "{0} attempt open mailbox in cheating way.", Player.GetName());

				return false;
			}
		}
		else if (guid.IsGameObject)
		{
			if (!Player.GetGameObjectIfCanInteractWith(guid, GameObjectTypes.Mailbox))
				return false;
		}
		else if (guid.IsAnyTypeCreature)
		{
			if (!Player.GetNPCIfCanInteractWith(guid, NPCFlags.Mailbox, NPCFlags2.None))
				return false;
		}
		else
		{
			return false;
		}

		return true;
	}

	[WorldPacketHandler(ClientOpcodes.SendMail)]
	void HandleSendMail(SendMail sendMail)
	{
		if (sendMail.Info.Attachments.Count > SharedConst.MaxClientMailItems) // client limit
		{
			Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.TooManyAttachments);

			return;
		}

		if (!CanOpenMailBox(sendMail.Info.Mailbox))
			return;

		if (string.IsNullOrEmpty(sendMail.Info.Target))
			return;

		if (!ValidateHyperlinksAndMaybeKick(sendMail.Info.Subject) || !ValidateHyperlinksAndMaybeKick(sendMail.Info.Body))
			return;

		var player = Player;

		if (player.Level < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
		{
			SendNotification(CypherStrings.MailSenderReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));

			return;
		}

		var receiverGuid = ObjectGuid.Empty;

		if (ObjectManager.NormalizePlayerName(ref sendMail.Info.Target))
			receiverGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(sendMail.Info.Target);

		if (receiverGuid.IsEmpty)
		{
			Log.outInfo(LogFilter.Network,
						"Player {0} is sending mail to {1} (GUID: not existed!) with subject {2}" +
						"and body {3} includes {4} items, {5} copper and {6} COD copper with StationeryID = {7}",
						GetPlayerInfo(),
						sendMail.Info.Target,
						sendMail.Info.Subject,
						sendMail.Info.Body,
						sendMail.Info.Attachments.Count,
						sendMail.Info.SendMoney,
						sendMail.Info.Cod,
						sendMail.Info.StationeryID);

			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientNotFound);

			return;
		}

		if (sendMail.Info.SendMoney < 0)
		{
			Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

			Log.outWarn(LogFilter.Server,
						"Player {0} attempted to send mail to {1} ({2}) with negative money value (SendMoney: {3})",
						GetPlayerInfo(),
						sendMail.Info.Target,
						receiverGuid.ToString(),
						sendMail.Info.SendMoney);

			return;
		}

		if (sendMail.Info.Cod < 0)
		{
			Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

			Log.outWarn(LogFilter.Server,
						"Player {0} attempted to send mail to {1} ({2}) with negative COD value (Cod: {3})",
						GetPlayerInfo(),
						sendMail.Info.Target,
						receiverGuid.ToString(),
						sendMail.Info.Cod);

			return;
		}

		Log.outInfo(LogFilter.Network,
					"Player {0} is sending mail to {1} ({2}) with subject {3} and body {4}" +
					"includes {5} items, {6} copper and {7} COD copper with StationeryID = {8}",
					GetPlayerInfo(),
					sendMail.Info.Target,
					receiverGuid.ToString(),
					sendMail.Info.Subject,
					sendMail.Info.Body,
					sendMail.Info.Attachments.Count,
					sendMail.Info.SendMoney,
					sendMail.Info.Cod,
					sendMail.Info.StationeryID);

		if (player.GUID == receiverGuid)
		{
			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CannotSendToSelf);

			return;
		}

		var cost = (uint)(!sendMail.Info.Attachments.Empty() ? 30 * sendMail.Info.Attachments.Count : 30); // price hardcoded in client

		var reqmoney = cost + sendMail.Info.SendMoney;

		// Check for overflow
		if (reqmoney < sendMail.Info.SendMoney)
		{
			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

			return;
		}

		if (!player.HasEnoughMoney(reqmoney) && !player.IsGameMaster)
		{
			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

			return;
		}

		void mailCountCheckContinuation(TeamFaction receiverTeam, ulong mailsCount, uint receiverLevel, uint receiverAccountId, uint receiverBnetAccountId)
		{
			if (_player != player)
				return;

			// do not allow to have more than 100 mails in mailbox.. mails count is in opcode uint8!!! - so max can be 255..
			if (mailsCount > 100)
			{
				player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientCapReached);

				return;
			}

			// test the receiver's Faction... or all items are account bound
			var accountBound = !sendMail.Info.Attachments.Empty();

			foreach (var att in sendMail.Info.Attachments)
			{
				var item = player.GetItemByGuid(att.ItemGUID);

				if (item != null)
				{
					var itemProto = item.Template;

					if (itemProto == null || !itemProto.HasFlag(ItemFlags.IsBoundToAccount))
					{
						accountBound = false;

						break;
					}
				}
			}

			if (!accountBound && player.Team != receiverTeam && !HasPermission(RBACPermissions.TwoSideInteractionMail))
			{
				player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotYourTeam);

				return;
			}

			if (receiverLevel < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
			{
				SendNotification(CypherStrings.MailReceiverReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));

				return;
			}

			List<Item> items = new();

			foreach (var att in sendMail.Info.Attachments)
			{
				if (att.ItemGUID.IsEmpty)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

					return;
				}

				var item = player.GetItemByGuid(att.ItemGUID);

				// prevent sending bag with items (cheat: can be placed in bag after adding equipped empty bag to mail)
				if (item == null)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

					return;
				}

				if (!item.CanBeTraded(true))
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

					return;
				}

				if (item.IsBoundAccountWide && item.IsSoulBound && player.Session.AccountId != receiverAccountId)
					if (!item.IsBattlenetAccountBound || player.Session.BattlenetAccountId == 0 || player.Session.BattlenetAccountId != receiverBnetAccountId)
					{
						player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.NotSameAccount);

						return;
					}

				if (item.Template.HasFlag(ItemFlags.Conjured) || item.ItemData.Expiration != 0)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

					return;
				}

				if (sendMail.Info.Cod != 0 && item.IsWrapped)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CantSendWrappedCod);

					return;
				}

				if (item.IsNotEmptyBag)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.DestroyNonemptyBag);

					return;
				}

				items.Add(item);
			}

			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.Ok);

			player.ModifyMoney(-reqmoney);
			player.UpdateCriteria(CriteriaType.MoneySpentOnPostage, cost);

			var needItemDelay = false;

			MailDraft draft = new(sendMail.Info.Subject, sendMail.Info.Body);

			var trans = new SQLTransaction();

			if (!sendMail.Info.Attachments.Empty() || sendMail.Info.SendMoney > 0)
			{
				var log = HasPermission(RBACPermissions.LogGmTrade);

				if (!sendMail.Info.Attachments.Empty())
				{
					foreach (var item in items)
					{
						if (log)
							Log.outCommand(AccountId,
											$"GM {PlayerName} ({_player.GUID}) (Account: {AccountId}) mail item: {item.Template.GetName()} " +
											$"(Entry: {item.Entry} Count: {item.Count}) to: {sendMail.Info.Target} ({receiverGuid}) (Account: {receiverAccountId})");

						item.SetNotRefundable(Player); // makes the item no longer refundable
						player.MoveItemFromInventory(item.BagSlot, item.Slot, true);

						item.DeleteFromInventoryDB(trans); // deletes item from character's inventory
						item.SetOwnerGUID(receiverGuid);
						item.SetState(ItemUpdateState.Changed);
						item.SaveToDB(trans); // recursive and not have transaction guard into self, item not in inventory and can be save standalone

						draft.AddItem(item);
					}

					// if item send to character at another account, then apply item delivery delay
					needItemDelay = player.Session.AccountId != receiverAccountId;
				}

				if (log && sendMail.Info.SendMoney > 0)
					Log.outCommand(AccountId, $"GM {PlayerName} ({_player.GUID}) (Account: {AccountId}) mail money: {sendMail.Info.SendMoney} to: {sendMail.Info.Target} ({receiverGuid}) (Account: {receiverAccountId})");
			}

			// If theres is an item, there is a one hour delivery delay if sent to another account's character.
			var deliver_delay = needItemDelay ? WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay) : 0;

			// Mail sent between guild members arrives instantly
			var guild = Global.GuildMgr.GetGuildById(player.GuildId);

			if (guild != null)
				if (guild.IsMember(receiverGuid))
					deliver_delay = 0;

			// don't ask for COD if there are no items
			if (sendMail.Info.Attachments.Empty())
				sendMail.Info.Cod = 0;

			// will delete item or place to receiver mail list
			draft.AddMoney((ulong)sendMail.Info.SendMoney)
				.AddCOD((uint)sendMail.Info.Cod)
				.SendMailTo(trans, new MailReceiver(Global.ObjAccessor.FindConnectedPlayer(receiverGuid), receiverGuid.Counter), new MailSender(player), sendMail.Info.Body.IsEmpty() ? MailCheckMask.Copied : MailCheckMask.HasBody, deliver_delay);

			player.SaveInventoryAndGoldToDB(trans);
			DB.Characters.CommitTransaction(trans);
		}

		var receiver = Global.ObjAccessor.FindPlayer(receiverGuid);

		if (receiver != null)
		{
			mailCountCheckContinuation(receiver.Team, receiver.MailSize, receiver.Level, receiver.Session.AccountId, receiver.Session.BattlenetAccountId);
		}
		else
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT);
			stmt.AddValue(0, receiverGuid.Counter);

			QueryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt)
										.WithChainingCallback((queryCallback, mailCountResult) =>
										{
											var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(receiverGuid);

											if (characterInfo != null)
												queryCallback.WithCallback(bnetAccountResult =>
															{
																mailCountCheckContinuation(Player.TeamForRace(characterInfo.RaceId),
																							!mailCountResult.IsEmpty() ? mailCountResult.Read<ulong>(0) : 0,
																							characterInfo.Level,
																							characterInfo.AccountId,
																							!bnetAccountResult.IsEmpty() ? bnetAccountResult.Read<uint>(0) : 0);
															})
															.SetNextQuery(Global.BNetAccountMgr.GetIdByGameAccountAsync(characterInfo.AccountId));
										}));
		}
	}

	//called when mail is read
	[WorldPacketHandler(ClientOpcodes.MailMarkAsRead)]
	void HandleMailMarkAsRead(MailMarkAsRead markAsRead)
	{
		if (!CanOpenMailBox(markAsRead.Mailbox))
			return;

		var player = Player;
		var m = player.GetMail(markAsRead.MailID);

		if (m != null && m.state != MailState.Deleted)
		{
			if (player.UnReadMails != 0)
				--player.UnReadMails;

			m.checkMask = m.checkMask | MailCheckMask.Read;
			player.MailsUpdated = true;
			m.state = MailState.Changed;
		}
	}

	//called when client deletes mail
	[WorldPacketHandler(ClientOpcodes.MailDelete)]
	void HandleMailDelete(MailDelete mailDelete)
	{
		var m = Player.GetMail(mailDelete.MailID);
		var player = Player;
		player.MailsUpdated = true;

		if (m != null)
		{
			// delete shouldn't show up for COD mails
			if (m.COD != 0)
			{
				player.SendMailResult(mailDelete.MailID, MailResponseType.Deleted, MailResponseResult.InternalError);

				return;
			}

			m.state = MailState.Deleted;
		}

		player.SendMailResult(mailDelete.MailID, MailResponseType.Deleted, MailResponseResult.Ok);
	}

	[WorldPacketHandler(ClientOpcodes.MailReturnToSender)]
	void HandleMailReturnToSender(MailReturnToSender returnToSender)
	{
		if (!CanOpenMailBox(_player.PlayerTalkClass.GetInteractionData().SourceGuid))
			return;

		var player = Player;
		var m = player.GetMail(returnToSender.MailID);

		if (m == null || m.state == MailState.Deleted || m.deliver_time > GameTime.GetGameTime() || m.sender != returnToSender.SenderGUID.Counter)
		{
			player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.InternalError);

			return;
		}

		//we can return mail now, so firstly delete the old one
		SQLTransaction trans = new();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
		stmt.AddValue(0, returnToSender.MailID);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
		stmt.AddValue(0, returnToSender.MailID);
		trans.Append(stmt);

		player.RemoveMail(returnToSender.MailID);

		// only return mail if the player exists (and delete if not existing)
		if (m.messageType == MailMessageType.Normal && m.sender != 0)
		{
			MailDraft draft = new(m.subject, m.body);

			if (m.mailTemplateId != 0)
				draft = new MailDraft(m.mailTemplateId, false); // items already included

			if (m.HasItems())
				foreach (var itemInfo in m.items)
				{
					var item = player.GetMItem(itemInfo.item_guid);

					if (item)
						draft.AddItem(item);

					player.RemoveMItem(itemInfo.item_guid);
				}

			draft.AddMoney(m.money).SendReturnToSender(AccountId, m.receiver, m.sender, trans);
		}

		DB.Characters.CommitTransaction(trans);

		player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.Ok);
	}

	//called when player takes item attached in mail
	[WorldPacketHandler(ClientOpcodes.MailTakeItem)]
	void HandleMailTakeItem(MailTakeItem takeItem)
	{
		var AttachID = takeItem.AttachID;

		if (!CanOpenMailBox(takeItem.Mailbox))
			return;

		var player = Player;

		var m = player.GetMail(takeItem.MailID);

		if (m == null || m.state == MailState.Deleted || m.deliver_time > GameTime.GetGameTime())
		{
			player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);

			return;
		}

		// verify that the mail has the item to avoid cheaters taking COD items without paying
		if (!m.items.Any(p => p.item_guid == AttachID))
		{
			player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);

			return;
		}

		// prevent cheating with skip client money check
		if (!player.HasEnoughMoney(m.COD))
		{
			player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.NotEnoughMoney);

			return;
		}

		var it = player.GetMItem(takeItem.AttachID);

		List<ItemPosCount> dest = new();
		var msg = Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, it, false);

		if (msg == InventoryResult.Ok)
		{
			SQLTransaction trans = new();
			m.RemoveItem(takeItem.AttachID);
			m.removedItems.Add(takeItem.AttachID);

			if (m.COD > 0) //if there is COD, take COD money from player and send them to sender by mail
			{
				var sender_guid = ObjectGuid.Create(HighGuid.Player, m.sender);
				var receiver = Global.ObjAccessor.FindPlayer(sender_guid);

				uint sender_accId = 0;

				if (HasPermission(RBACPermissions.LogGmTrade))
				{
					string sender_name;

					if (receiver)
					{
						sender_accId = receiver.Session.AccountId;
						sender_name = receiver.GetName();
					}
					else
					{
						// can be calculated early
						sender_accId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(sender_guid);

						if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(sender_guid, out sender_name))
							sender_name = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
					}

					Log.outCommand(AccountId,
									"GM {0} (Account: {1}) receiver mail item: {2} (Entry: {3} Count: {4}) and send COD money: {5} to player: {6} (Account: {7})",
									PlayerName,
									AccountId,
									it.Template.GetName(),
									it.Entry,
									it.Count,
									m.COD,
									sender_name,
									sender_accId);
				}
				else if (!receiver)
				{
					sender_accId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(sender_guid);
				}

				// check player existence
				if (receiver || sender_accId != 0)
					new MailDraft(m.subject, "")
						.AddMoney(m.COD)
						.SendMailTo(trans, new MailReceiver(receiver, m.sender), new MailSender(MailMessageType.Normal, m.receiver), MailCheckMask.CodPayment);

				player.ModifyMoney(-(long)m.COD);
			}

			m.COD = 0;
			m.state = MailState.Changed;
			player.MailsUpdated = true;
			player.RemoveMItem(it.GUID.Counter);

			var count = it.Count;                   // save counts before store and possible merge with deleting
			it.SetState(ItemUpdateState.Unchanged); // need to set this state, otherwise item cannot be removed later, if neccessary
			player.MoveItemToInventory(dest, it, true);

			player.SaveInventoryAndGoldToDB(trans);
			player._SaveMail(trans);
			DB.Characters.CommitTransaction(trans);

			player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.Ok, 0, takeItem.AttachID, count);
		}
		else
		{
			player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.EquipError, msg);
		}
	}

	[WorldPacketHandler(ClientOpcodes.MailTakeMoney)]
	void HandleMailTakeMoney(MailTakeMoney takeMoney)
	{
		if (!CanOpenMailBox(takeMoney.Mailbox))
			return;

		var player = Player;

		var m = player.GetMail(takeMoney.MailID);

		if ((m == null || m.state == MailState.Deleted || m.deliver_time > GameTime.GetGameTime()) ||
			(takeMoney.Money > 0 && m.money != (ulong)takeMoney.Money))
		{
			player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.InternalError);

			return;
		}

		if (!player.ModifyMoney((long)m.money, false))
		{
			player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.EquipError, InventoryResult.TooMuchGold);

			return;
		}

		m.money = 0;
		m.state = MailState.Changed;
		player.MailsUpdated = true;

		player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.Ok);

		// save money and mail to prevent cheating
		SQLTransaction trans = new();
		player.SaveGoldToDB(trans);
		player._SaveMail(trans);
		DB.Characters.CommitTransaction(trans);
	}

	//called when player lists his received mails
	[WorldPacketHandler(ClientOpcodes.MailGetList)]
	void HandleGetMailList(MailGetList getList)
	{
		if (!CanOpenMailBox(getList.Mailbox))
			return;

		var player = Player;

		var mails = player.Mails;

		MailListResult response = new();
		var curTime = GameTime.GetGameTime();

		foreach (var m in mails)
		{
			// skip deleted or not delivered (deliver delay not expired) mails
			if (m.state == MailState.Deleted || curTime < m.deliver_time)
				continue;

			// max. 100 mails can be sent
			if (response.Mails.Count < 100)
				response.Mails.Add(new MailListEntry(m, player));
		}

		player.PlayerTalkClass.GetInteractionData().Reset();
		player.PlayerTalkClass.GetInteractionData().SourceGuid = getList.Mailbox;
		SendPacket(response);

		// recalculate m_nextMailDelivereTime and unReadMails
		Player.UpdateNextMailTimeAndUnreads();
	}

	//used when player copies mail body to his inventory
	[WorldPacketHandler(ClientOpcodes.MailCreateTextItem)]
	void HandleMailCreateTextItem(MailCreateTextItem createTextItem)
	{
		if (!CanOpenMailBox(createTextItem.Mailbox))
			return;

		var player = Player;

		var m = player.GetMail(createTextItem.MailID);

		if (m == null || (string.IsNullOrEmpty(m.body) && m.mailTemplateId == 0) || m.state == MailState.Deleted || m.deliver_time > GameTime.GetGameTime())
		{
			player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

			return;
		}

		Item bodyItem = new(); // This is not bag and then can be used new Item.

		if (!bodyItem.Create(Global.ObjectMgr.GetGenerator(HighGuid.Item).Generate(), 8383, ItemContext.None, player))
			return;

		// in mail template case we need create new item text
		if (m.mailTemplateId != 0)
		{
			var mailTemplateEntry = CliDB.MailTemplateStorage.LookupByKey(m.mailTemplateId);

			if (mailTemplateEntry == null)
			{
				player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

				return;
			}

			bodyItem.SetText(mailTemplateEntry.Body[SessionDbcLocale]);
		}
		else
		{
			bodyItem.SetText(m.body);
		}

		if (m.messageType == MailMessageType.Normal)
			bodyItem.SetCreator(ObjectGuid.Create(HighGuid.Player, m.sender));

		bodyItem.SetItemFlag(ItemFieldFlags.Readable);

		Log.outInfo(LogFilter.Network, "HandleMailCreateTextItem mailid={0}", createTextItem.MailID);

		List<ItemPosCount> dest = new();
		var msg = Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, bodyItem, false);

		if (msg == InventoryResult.Ok)
		{
			m.checkMask = m.checkMask | MailCheckMask.Copied;
			m.state = MailState.Changed;
			player.MailsUpdated = true;

			player.StoreItem(dest, bodyItem, true);
			player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.Ok);
		}
		else
		{
			player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.EquipError, msg);
		}
	}

	[WorldPacketHandler(ClientOpcodes.QueryNextMailTime)]
	void HandleQueryNextMailTime(MailQueryNextMailTime queryNextMailTime)
	{
		MailQueryNextTimeResult result = new();

		if (Player.UnReadMails > 0)
		{
			result.NextMailTime = 0.0f;

			var now = GameTime.GetGameTime();
			List<ulong> sentSenders = new();

			foreach (var mail in Player.Mails)
			{
				if (mail.checkMask.HasAnyFlag(MailCheckMask.Read))
					continue;

				// and already delivered
				if (now < mail.deliver_time)
					continue;

				// only send each mail sender once
				if (sentSenders.Any(p => p == mail.sender))
					continue;

				result.Next.Add(new MailQueryNextTimeResult.MailNextTimeEntry(mail));

				sentSenders.Add(mail.sender);

				// do not send more than 2 mails
				if (sentSenders.Count > 2)
					break;
			}
		}
		else
		{
			result.NextMailTime = -Time.Day;
		}

		SendPacket(result);
	}
}