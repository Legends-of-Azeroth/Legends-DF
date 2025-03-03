﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Networking;

namespace Game.Entities;

public class Bag : Item
{
	readonly ContainerData m_containerData;
	Item[] m_bagslot = new Item[36];

	public Bag()
	{
		ObjectTypeMask |= TypeMask.Container;
		ObjectTypeId = TypeId.Container;

		m_containerData = new ContainerData();
	}

	public override void Dispose()
	{
		for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
		{
			var item = m_bagslot[i];

			if (item)
			{
				if (item.IsInWorld)
				{
					Log.outFatal(LogFilter.PlayerItems,
								"Item {0} (slot {1}, bag slot {2}) in bag {3} (slot {4}, bag slot {5}, m_bagslot {6}) is to be deleted but is still in world.",
								item.Entry,
								item.Slot,
								item.BagSlot,
								Entry,
								Slot,
								BagSlot,
								i);

					item.RemoveFromWorld();
				}

				m_bagslot[i].Dispose();
			}
		}

		base.Dispose();
	}

	public override void AddToWorld()
	{
		base.AddToWorld();

		for (uint i = 0; i < GetBagSize(); ++i)
			if (m_bagslot[i] != null)
				m_bagslot[i].AddToWorld();
	}

	public override void RemoveFromWorld()
	{
		for (uint i = 0; i < GetBagSize(); ++i)
			if (m_bagslot[i] != null)
				m_bagslot[i].RemoveFromWorld();

		base.RemoveFromWorld();
	}

	public override bool Create(ulong guidlow, uint itemid, ItemContext context, Player owner)
	{
		var itemProto = Global.ObjectMgr.GetItemTemplate(itemid);

		if (itemProto == null || itemProto.ContainerSlots > ItemConst.MaxBagSize)
			return false;

		Create(ObjectGuid.Create(HighGuid.Item, guidlow));

		BonusData = new BonusData(itemProto);

		Entry = itemid;
		ObjectScale = 1.0f;

		if (owner)
		{
			SetOwnerGUID(owner.GUID);
			SetContainedIn(owner.GUID);
		}

		SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.MaxDurability), itemProto.MaxDurability);
		SetDurability(itemProto.MaxDurability);
		SetCount(1);
		SetContext(context);

		// Setting the number of Slots the Container has
		SetBagSize(itemProto.ContainerSlots);

		// Cleaning 20 slots
		for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
			SetSlot(i, ObjectGuid.Empty);

		m_bagslot = new Item[ItemConst.MaxBagSize];

		return true;
	}

	public override bool LoadFromDB(ulong guid, ObjectGuid owner_guid, SQLFields fields, uint entry)
	{
		if (!base.LoadFromDB(guid, owner_guid, fields, entry))
			return false;

		var itemProto = Template; // checked in Item.LoadFromDB
		SetBagSize(itemProto.ContainerSlots);

		// cleanup bag content related item value fields (its will be filled correctly from `character_inventory`)
		for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
		{
			SetSlot(i, ObjectGuid.Empty);
			m_bagslot[i] = null;
		}

		return true;
	}

	public override void DeleteFromDB(SQLTransaction trans)
	{
		for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
			if (m_bagslot[i] != null)
				m_bagslot[i].DeleteFromDB(trans);

		base.DeleteFromDB(trans);
	}

	public uint GetFreeSlots()
	{
		uint slots = 0;

		for (uint i = 0; i < GetBagSize(); ++i)
			if (m_bagslot[i] == null)
				++slots;

		return slots;
	}

	public void RemoveItem(byte slot, bool update)
	{
		if (m_bagslot[slot] != null)
			m_bagslot[slot].SetContainer(null);

		m_bagslot[slot] = null;
		SetSlot(slot, ObjectGuid.Empty);
	}

	public void StoreItem(byte slot, Item pItem, bool update)
	{
		if (pItem != null && pItem.GUID != GUID)
		{
			m_bagslot[slot] = pItem;
			SetSlot(slot, pItem.GUID);
			pItem.SetContainedIn(GUID);
			pItem.SetOwnerGUID(OwnerGUID);
			pItem.SetContainer(this);
			pItem.SetSlot(slot);
		}
	}

	public override void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
	{
		base.BuildCreateUpdateBlockForPlayer(data, target);

		for (var i = 0; i < GetBagSize(); ++i)
			if (m_bagslot[i] != null)
				m_bagslot[i].BuildCreateUpdateBlockForPlayer(data, target);
	}

	public override void BuildValuesCreate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt8((byte)flags);
		ObjectData.WriteCreate(buffer, flags, this, target);
		ItemData.WriteCreate(buffer, flags, this, target);
		m_containerData.WriteCreate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void BuildValuesUpdate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

		if (Values.HasChanged(TypeId.Object))
			ObjectData.WriteUpdate(buffer, flags, this, target);

		if (Values.HasChanged(TypeId.Item))
			ItemData.WriteUpdate(buffer, flags, this, target);

		if (Values.HasChanged(TypeId.Container))
			m_containerData.WriteUpdate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void ClearUpdateMask(bool remove)
	{
		Values.ClearChangesMask(m_containerData);
		base.ClearUpdateMask(remove);
	}

	public bool IsEmpty()
	{
		for (var i = 0; i < GetBagSize(); ++i)
			if (m_bagslot[i] != null)
				return false;

		return true;
	}

	public Item GetItemByPos(byte slot)
	{
		if (slot < GetBagSize())
			return m_bagslot[slot];

		return null;
	}

	public uint GetBagSize()
	{
		return m_containerData.NumSlots;
	}

	void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedContainerMask, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		UpdateMask valuesMask = new((int)TypeId.Max);

		if (requestedObjectMask.IsAnySet())
			valuesMask.Set((int)TypeId.Object);

		ItemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);

		if (requestedItemMask.IsAnySet())
			valuesMask.Set((int)TypeId.Item);

		if (requestedContainerMask.IsAnySet())
			valuesMask.Set((int)TypeId.Container);

		WorldPacket buffer = new();
		buffer.WriteUInt32(valuesMask.GetBlock(0));

		if (valuesMask[(int)TypeId.Object])
			ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

		if (valuesMask[(int)TypeId.Item])
			ItemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

		if (valuesMask[(int)TypeId.Container])
			m_containerData.WriteUpdate(buffer, requestedContainerMask, true, this, target);

		WorldPacket buffer1 = new();
		buffer1.WriteUInt8((byte)UpdateType.Values);
		buffer1.WritePackedGuid(GUID);
		buffer1.WriteUInt32(buffer.GetSize());
		buffer1.WriteBytes(buffer.GetData());

		data.AddUpdateBlock(buffer1);
	}

	byte GetSlotByItemGUID(ObjectGuid guid)
	{
		for (byte i = 0; i < GetBagSize(); ++i)
			if (m_bagslot[i] != null)
				if (m_bagslot[i].GUID == guid)
					return i;

		return ItemConst.NullSlot;
	}

	void SetBagSize(uint numSlots)
	{
		SetUpdateFieldValue(Values.ModifyValue(m_containerData).ModifyValue(m_containerData.NumSlots), numSlots);
	}

	void SetSlot(int slot, ObjectGuid guid)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(m_containerData).ModifyValue(m_containerData.Slots, slot), guid);
	}

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly Bag Owner;
		readonly ObjectFieldData ObjectMask = new();
		readonly ItemData ItemMask = new();
		readonly ContainerData ContainerMask = new();

		public ValuesUpdateForPlayerWithMaskSender(Bag owner)
		{
			Owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(Owner.Location.MapId);

			Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ItemMask.GetUpdateMask(), ContainerMask.GetUpdateMask(), player);

			udata.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}
}