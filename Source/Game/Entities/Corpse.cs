﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Loots;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities
{
	public class Corpse : WorldObject
	{
		private CellCoord _cellCoord; // gride for corpse position for fast search

		public CorpseData _corpseData;
		private long _time;

		private CorpseType _type;

		public Loot loot;
		public Player lootRecipient;

		public Corpse(CorpseType type = CorpseType.Bones) : base(type != CorpseType.Bones)
		{
			_type          =  type;
			ObjectTypeId   =  TypeId.Corpse;
			ObjectTypeMask |= TypeMask.Corpse;

			UpdateFlag.Stationary = true;

			_corpseData = new CorpseData();

			_time = GameTime.GetGameTime();
		}

		public override void AddToWorld()
		{
			// Register the corpse for Guid lookup
			if (!IsInWorld)
				GetMap().GetObjectsStore().Add(GetGUID(), this);

			base.AddToWorld();
		}

		public override void RemoveFromWorld()
		{
			// Remove the corpse from the accessor
			if (IsInWorld)
				GetMap().GetObjectsStore().Remove(GetGUID());

			base.RemoveFromWorld();
		}

		public bool Create(ulong guidlow, Map map)
		{
			_Create(ObjectGuid.Create(HighGuid.Corpse, map.GetId(), 0, guidlow));

			return true;
		}

		public bool Create(ulong guidlow, Player owner)
		{
			Cypher.Assert(owner != null);

			Relocate(owner.GetPositionX(), owner.GetPositionY(), owner.GetPositionZ(), owner.GetOrientation());

			if (!IsPositionValid())
			{
				Log.outError(LogFilter.Player,
				             "Corpse (guidlow {0}, owner {1}) not created. Suggested coordinates isn't valid (X: {2} Y: {3})",
				             guidlow,
				             owner.GetName(),
				             owner.GetPositionX(),
				             owner.GetPositionY());

				return false;
			}

			_Create(ObjectGuid.Create(HighGuid.Corpse, owner.GetMapId(), 0, guidlow));

			SetObjectScale(1);
			SetOwnerGUID(owner.GetGUID());

			_cellCoord = GridDefines.ComputeCellCoord(GetPositionX(), GetPositionY());

			PhasingHandler.InheritPhaseShift(this, owner);

			return true;
		}

		public override void Update(uint diff)
		{
			base.Update(diff);

			loot?.Update();
		}

		public void SaveToDB()
		{
			// prevent DB _data inconsistence problems and duplicates
			SQLTransaction trans = new();
			DeleteFromDB(trans);

			StringBuilder items = new();

			for (var i = 0; i < _corpseData.Items.GetSize(); ++i)
				items.Append($"{_corpseData.Items[i]} ");

			byte              index = 0;
			PreparedStatement stmt  = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE);
			stmt.AddValue(index++, GetOwnerGUID().GetCounter());    // Guid
			stmt.AddValue(index++, GetPositionX());                 // X
			stmt.AddValue(index++, GetPositionY());                 // Y
			stmt.AddValue(index++, GetPositionZ());                 // Z
			stmt.AddValue(index++, GetOrientation());               // orientation
			stmt.AddValue(index++, GetMapId());                     // mapId
			stmt.AddValue(index++, (uint)_corpseData.DisplayID);    // displayId
			stmt.AddValue(index++, items.ToString());               // itemCache
			stmt.AddValue(index++, (byte)_corpseData.RaceID);       // race
			stmt.AddValue(index++, (byte)_corpseData.Class);        // class
			stmt.AddValue(index++, (byte)_corpseData.Sex);          // Gender
			stmt.AddValue(index++, (uint)_corpseData.Flags);        // Flags
			stmt.AddValue(index++, (uint)_corpseData.DynamicFlags); // dynFlags
			stmt.AddValue(index++, (uint)_time);                    // Time
			stmt.AddValue(index++, (uint)GetCorpseType());          // corpseType
			stmt.AddValue(index++, GetInstanceId());                // InstanceId
			trans.Append(stmt);

			foreach (var phaseId in GetPhaseShift().GetPhases().Keys)
			{
				index = 0;
				stmt  = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE_PHASES);
				stmt.AddValue(index++, GetOwnerGUID().GetCounter()); // OwnerGuid
				stmt.AddValue(index++, phaseId);                     // PhaseId
				trans.Append(stmt);
			}

			foreach (var customization in _corpseData.Customizations)
			{
				index = 0;
				stmt  = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE_CUSTOMIZATIONS);
				stmt.AddValue(index++, GetOwnerGUID().GetCounter()); // OwnerGuid
				stmt.AddValue(index++, customization.ChrCustomizationOptionID);
				stmt.AddValue(index++, customization.ChrCustomizationChoiceID);
				trans.Append(stmt);
			}

			DB.Characters.CommitTransaction(trans);
		}

		public void DeleteFromDB(SQLTransaction trans)
		{
			DeleteFromDB(GetOwnerGUID(), trans);
		}

		public static void DeleteFromDB(ObjectGuid ownerGuid, SQLTransaction trans)
		{
			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE);
			stmt.AddValue(0, ownerGuid.GetCounter());
			DB.Characters.ExecuteOrAppend(trans, stmt);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE_PHASES);
			stmt.AddValue(0, ownerGuid.GetCounter());
			DB.Characters.ExecuteOrAppend(trans, stmt);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE_CUSTOMIZATIONS);
			stmt.AddValue(0, ownerGuid.GetCounter());
			DB.Characters.ExecuteOrAppend(trans, stmt);
		}

		public bool LoadCorpseFromDB(ulong guid, SQLFields field)
		{
			//        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
			// SELECT X, Y, Z, orientation, mapId, displayId, itemCache, race, class, Gender, Flags, dynFlags, Time, corpseType, InstanceId, Guid FROM corpse WHERE mapId = ? AND InstanceId = ?

			float  posX  = field.Read<float>(0);
			float  posY  = field.Read<float>(1);
			float  posZ  = field.Read<float>(2);
			float  o     = field.Read<float>(3);
			ushort mapId = field.Read<ushort>(4);

			_Create(ObjectGuid.Create(HighGuid.Corpse, mapId, 0, guid));

			SetObjectScale(1.0f);
			SetDisplayId(field.Read<uint>(5));
			StringArray items = new(field.Read<string>(6), ' ');

			if (items.Length == _corpseData.Items.GetSize())
				for (uint index = 0; index < _corpseData.Items.GetSize(); ++index)
					SetItem(index, uint.Parse(items[(int)index]));

			SetRace(field.Read<byte>(7));
			SetClass(field.Read<byte>(8));
			SetSex(field.Read<byte>(9));
			ReplaceAllFlags((CorpseFlags)field.Read<byte>(10));
			ReplaceAllCorpseDynamicFlags((CorpseDynFlags)field.Read<byte>(11));
			SetOwnerGUID(ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(15)));
			SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(_corpseData.RaceID).FactionID);

			_time = field.Read<uint>(12);

			uint instanceId = field.Read<uint>(14);

			// place
			SetLocationInstanceId(instanceId);
			SetMapId(mapId);
			Relocate(posX, posY, posZ, o);

			if (!IsPositionValid())
			{
				Log.outError(LogFilter.Player,
				             "Corpse ({0}, owner: {1}) is not created, given coordinates are not valid (X: {2}, Y: {3}, Z: {4})",
				             GetGUID().ToString(),
				             GetOwnerGUID().ToString(),
				             posX,
				             posY,
				             posZ);

				return false;
			}

			_cellCoord = GridDefines.ComputeCellCoord(GetPositionX(), GetPositionY());

			return true;
		}

		public bool IsExpired(long t)
		{
			// Deleted character
			if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(GetOwnerGUID()))
				return true;

			if (_type == CorpseType.Bones)
				return _time < t - 60 * Time.Minute;
			else
				return _time < t - 3 * Time.Day;
		}

		public override void BuildValuesCreate(WorldPacket data, Player target)
		{
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			ObjectData.WriteCreate(buffer, flags, this, target);
			_corpseData.WriteCreate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize() + 1);
			data.WriteUInt8((byte)flags);
			data.WriteBytes(buffer);
		}

		public override void BuildValuesUpdate(WorldPacket data, Player target)
		{
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

			if (Values.HasChanged(TypeId.Object))
				ObjectData.WriteUpdate(buffer, flags, this, target);

			if (Values.HasChanged(TypeId.Corpse))
				_corpseData.WriteUpdate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize());
			data.WriteBytes(buffer);
		}

		private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedCorpseMask, Player target)
		{
			UpdateMask valuesMask = new((int)TypeId.Max);

			if (requestedObjectMask.IsAnySet())
				valuesMask.Set((int)TypeId.Object);

			if (requestedCorpseMask.IsAnySet())
				valuesMask.Set((int)TypeId.Corpse);

			WorldPacket buffer = new();
			buffer.WriteUInt32(valuesMask.GetBlock(0));

			if (valuesMask[(int)TypeId.Object])
				ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

			if (valuesMask[(int)TypeId.Corpse])
				_corpseData.WriteUpdate(buffer, requestedCorpseMask, true, this, target);

			WorldPacket buffer1 = new();
			buffer1.WriteUInt8((byte)UpdateType.Values);
			buffer1.WritePackedGuid(GetGUID());
			buffer1.WriteUInt32(buffer.GetSize());
			buffer1.WriteBytes(buffer.GetData());

			data.AddUpdateBlock(buffer1);
		}

		public override void ClearUpdateMask(bool remove)
		{
			Values.ClearChangesMask(_corpseData);
			base.ClearUpdateMask(remove);
		}

		public CorpseDynFlags GetCorpseDynamicFlags()
		{
			return (CorpseDynFlags)(uint)_corpseData.DynamicFlags;
		}

		public bool HasCorpseDynamicFlag(CorpseDynFlags flag)
		{
			return (_corpseData.DynamicFlags & (uint)flag) != 0;
		}

		public void SetCorpseDynamicFlag(CorpseDynFlags dynamicFlags)
		{
			SetUpdateFieldFlagValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.DynamicFlags), (uint)dynamicFlags);
		}

		public void RemoveCorpseDynamicFlag(CorpseDynFlags dynamicFlags)
		{
			RemoveUpdateFieldFlagValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.DynamicFlags), (uint)dynamicFlags);
		}

		public void ReplaceAllCorpseDynamicFlags(CorpseDynFlags dynamicFlags)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.DynamicFlags), (uint)dynamicFlags);
		}

		public override ObjectGuid GetOwnerGUID()
		{
			return _corpseData.Owner;
		}

		public void SetOwnerGUID(ObjectGuid owner)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.Owner), owner);
		}

		public void SetPartyGUID(ObjectGuid partyGuid)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.PartyGUID), partyGuid);
		}

		public void SetGuildGUID(ObjectGuid guildGuid)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.GuildGUID), guildGuid);
		}

		public void SetDisplayId(uint displayId)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.DisplayID), displayId);
		}

		public void SetRace(byte race)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.RaceID), race);
		}

		public void SetClass(byte classId)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.Class), classId);
		}

		public void SetSex(byte sex)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.Sex), sex);
		}

		public void ReplaceAllFlags(CorpseFlags flags)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.Flags), (uint)flags);
		}

		public void SetFactionTemplate(int factionTemplate)
		{
			SetUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.FactionTemplate), factionTemplate);
		}

		public override uint GetFaction()
		{
			return (uint)(int)_corpseData.FactionTemplate;
		}

		public override void SetFaction(uint faction)
		{
			SetFactionTemplate((int)faction);
		}

		public void SetItem(uint slot, uint item)
		{
			SetUpdateFieldValue(ref Values.ModifyValue(_corpseData).ModifyValue(_corpseData.Items, (int)slot), item);
		}

		public void SetCustomizations(List<ChrCustomizationChoice> customizations)
		{
			ClearDynamicUpdateFieldValues(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.Customizations));

			foreach (var customization in customizations)
			{
				var newChoice = new ChrCustomizationChoice();
				newChoice.ChrCustomizationOptionID = customization.ChrCustomizationOptionID;
				newChoice.ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID;
				AddDynamicUpdateFieldValue(Values.ModifyValue(_corpseData).ModifyValue(_corpseData.Customizations), newChoice);
			}
		}

		public long GetGhostTime()
		{
			return _time;
		}

		public void ResetGhostTime()
		{
			_time = GameTime.GetGameTime();
		}

		public CorpseType GetCorpseType()
		{
			return _type;
		}

		public CellCoord GetCellCoord()
		{
			return _cellCoord;
		}

		public void SetCellCoord(CellCoord cellCoord)
		{
			_cellCoord = cellCoord;
		}

		public override Loot GetLootForPlayer(Player player)
		{
			return loot;
		}

		private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
		{
			private CorpseData CorpseMask = new();
			private ObjectFieldData ObjectMask = new();
			private Corpse Owner;

			public ValuesUpdateForPlayerWithMaskSender(Corpse owner)
			{
				Owner = owner;
			}

			public void Invoke(Player player)
			{
				UpdateData udata = new(Owner.GetMapId());

				Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), CorpseMask.GetUpdateMask(), player);

				udata.BuildPacket(out UpdateObject packet);
				player.SendPacket(packet);
			}
		}
	}
}