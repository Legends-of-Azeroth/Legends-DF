﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Networking.Packets;

class CancelAura : ClientPacket
{
	public ObjectGuid CasterGUID;
	public uint SpellID;
	public CancelAura(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellID = _worldPacket.ReadUInt32();
		CasterGUID = _worldPacket.ReadPackedGuid();
	}
}

class CancelAutoRepeatSpell : ClientPacket
{
	public CancelAutoRepeatSpell(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class CancelChannelling : ClientPacket
{
	public int ChannelSpell;
	public int Reason; // 40 = /run SpellStopCasting(), 16 = movement/AURA_INTERRUPT_FLAG_MOVE, 41 = turning/AURA_INTERRUPT_FLAG_TURNING
	public CancelChannelling(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ChannelSpell = _worldPacket.ReadInt32();
		Reason = _worldPacket.ReadInt32();
	}
	// does not match SpellCastResult enum
}

class CancelGrowthAura : ClientPacket
{
	public CancelGrowthAura(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class CancelMountAura : ClientPacket
{
	public CancelMountAura(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class RequestCategoryCooldowns : ClientPacket
{
	public RequestCategoryCooldowns(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class SpellCategoryCooldown : ServerPacket
{
	public List<CategoryCooldownInfo> CategoryCooldowns = new();

	public SpellCategoryCooldown() : base(ServerOpcodes.CategoryCooldown, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(CategoryCooldowns.Count);

		foreach (var cooldown in CategoryCooldowns)
		{
			_worldPacket.WriteUInt32(cooldown.Category);
			_worldPacket.WriteInt32(cooldown.ModCooldown);
		}
	}

	public class CategoryCooldownInfo
	{
		public uint Category;   // SpellCategory Id
		public int ModCooldown; // Reduced Cooldown in ms

		public CategoryCooldownInfo(uint category, int cooldown)
		{
			Category = category;
			ModCooldown = cooldown;
		}
	}
}

public class SendKnownSpells : ServerPacket
{
	public bool InitialLogin;
	public List<uint> KnownSpells = new();
	public List<uint> FavoriteSpells = new(); // tradeskill recipes
	public SendKnownSpells() : base(ServerOpcodes.SendKnownSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(InitialLogin);
		_worldPacket.WriteInt32(KnownSpells.Count);
		_worldPacket.WriteInt32(FavoriteSpells.Count);

		foreach (var spellId in KnownSpells)
			_worldPacket.WriteUInt32(spellId);

		foreach (var spellId in FavoriteSpells)
			_worldPacket.WriteUInt32(spellId);
	}
}

public class UpdateActionButtons : ServerPacket
{
	public ulong[] ActionButtons = new ulong[PlayerConst.MaxActionButtons];
	public byte Reason;

	public UpdateActionButtons() : base(ServerOpcodes.UpdateActionButtons, ConnectionType.Instance) { }

	public override void Write()
	{
		for (var i = 0; i < PlayerConst.MaxActionButtons; ++i)
			_worldPacket.WriteUInt64(ActionButtons[i]);

		_worldPacket.WriteUInt8(Reason);
	}
}

public class SetActionButton : ClientPacket
{
	public ulong Action; // two packed values (action and type)
	public byte Index;
	public SetActionButton(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Action = _worldPacket.ReadUInt64();
		Index = _worldPacket.ReadUInt8();
	}

	public uint GetButtonAction()
	{
		return (uint)(Action & 0x00FFFFFFFFFFFFFF);
	}

	public uint GetButtonType()
	{
		return (uint)((Action & 0xFF00000000000000) >> 56);
	}
}

public class SendUnlearnSpells : ServerPacket
{
	readonly List<uint> Spells = new();

	public SendUnlearnSpells() : base(ServerOpcodes.SendUnlearnSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Spells.Count);

		foreach (var spell in Spells)
			_worldPacket.WriteUInt32(spell);
	}
}

public class AuraUpdate : ServerPacket
{
	public bool UpdateAll;
	public ObjectGuid UnitGUID;
	public List<AuraInfo> Auras = new();
	public AuraUpdate() : base(ServerOpcodes.AuraUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(UpdateAll);
		_worldPacket.WriteBits(Auras.Count, 9);

		foreach (var aura in Auras)
			aura.Write(_worldPacket);

		_worldPacket.WritePackedGuid(UnitGUID);
	}
}

public class CastSpell : ClientPacket
{
	public SpellCastRequest Cast;

	public CastSpell(WorldPacket packet) : base(packet)
	{
		Cast = new SpellCastRequest();
	}

	public override void Read()
	{
		Cast.Read(_worldPacket);
	}
}

public class PetCastSpell : ClientPacket
{
	public ObjectGuid PetGUID;
	public SpellCastRequest Cast;

	public PetCastSpell(WorldPacket packet) : base(packet)
	{
		Cast = new SpellCastRequest();
	}

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();
		Cast.Read(_worldPacket);
	}
}

public class UseItem : ClientPacket
{
	public byte PackSlot;
	public byte Slot;
	public ObjectGuid CastItem;
	public SpellCastRequest Cast;

	public UseItem(WorldPacket packet) : base(packet)
	{
		Cast = new SpellCastRequest();
	}

	public override void Read()
	{
		PackSlot = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
		CastItem = _worldPacket.ReadPackedGuid();
		Cast.Read(_worldPacket);
	}
}

class SpellPrepare : ServerPacket
{
	public ObjectGuid ClientCastID;
	public ObjectGuid ServerCastID;
	public SpellPrepare() : base(ServerOpcodes.SpellPrepare) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ClientCastID);
		_worldPacket.WritePackedGuid(ServerCastID);
	}
}

class SpellGo : CombatLogServerPacket
{
	public SpellCastData Cast = new();
	public SpellGo() : base(ServerOpcodes.SpellGo, ConnectionType.Instance) { }

	public override void Write()
	{
		Cast.Write(_worldPacket);

		WriteLogDataBit();
		FlushBits();

		WriteLogData();
	}
}

public class SpellStart : ServerPacket
{
	public SpellCastData Cast;

	public SpellStart() : base(ServerOpcodes.SpellStart, ConnectionType.Instance)
	{
		Cast = new SpellCastData();
	}

	public override void Write()
	{
		Cast.Write(_worldPacket);
	}
}

public class SupercededSpells : ServerPacket
{
	public List<LearnedSpellInfo> ClientLearnedSpellData = new();
	public SupercededSpells() : base(ServerOpcodes.SupercededSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(ClientLearnedSpellData.Count);

		foreach (var spell in ClientLearnedSpellData)
			spell.Write(_worldPacket);
	}
}

public class LearnedSpells : ServerPacket
{
	public List<LearnedSpellInfo> ClientLearnedSpellData = new();
	public uint SpecializationID;
	public bool SuppressMessaging;
	public LearnedSpells() : base(ServerOpcodes.LearnedSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(ClientLearnedSpellData.Count);
		_worldPacket.WriteUInt32(SpecializationID);
		_worldPacket.WriteBit(SuppressMessaging);
		_worldPacket.FlushBits();

		foreach (var spell in ClientLearnedSpellData)
			spell.Write(_worldPacket);
	}
}

public class SpellFailure : ServerPacket
{
	public ObjectGuid CasterUnit;
	public uint SpellID;
	public SpellCastVisual Visual;
	public ushort Reason;
	public ObjectGuid CastID;
	public SpellFailure() : base(ServerOpcodes.SpellFailure, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterUnit);
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteUInt32(SpellID);

		Visual.Write(_worldPacket);

		_worldPacket.WriteUInt16(Reason);
	}
}

public class SpellFailedOther : ServerPacket
{
	public ObjectGuid CasterUnit;
	public uint SpellID;
	public SpellCastVisual Visual;
	public ushort Reason;
	public ObjectGuid CastID;
	public SpellFailedOther() : base(ServerOpcodes.SpellFailedOther, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterUnit);
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteUInt32(SpellID);

		Visual.Write(_worldPacket);

		_worldPacket.WriteUInt16(Reason);
	}
}

class CastFailedBase : ServerPacket
{
	public ObjectGuid CastID;
	public int SpellID;
	public SpellCastResult Reason;
	public int FailedArg1 = -1;
	public int FailedArg2 = -1;

	public CastFailedBase(ServerOpcodes opcode, ConnectionType connectionType) : base(opcode, connectionType) { }

	public override void Write()
	{
		throw new NotImplementedException();
	}
}

class CastFailed : CastFailedBase
{
	public SpellCastVisual Visual;

	public CastFailed() : base(ServerOpcodes.CastFailed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteInt32(SpellID);

		Visual.Write(_worldPacket);

		_worldPacket.WriteInt32((int)Reason);
		_worldPacket.WriteInt32(FailedArg1);
		_worldPacket.WriteInt32(FailedArg2);
	}
}

class PetCastFailed : CastFailedBase
{
	public PetCastFailed() : base(ServerOpcodes.PetCastFailed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteInt32(SpellID);
		_worldPacket.WriteInt32((int)Reason);
		_worldPacket.WriteInt32(FailedArg1);
		_worldPacket.WriteInt32(FailedArg2);
	}
}

public class SetSpellModifier : ServerPacket
{
	public List<SpellModifierInfo> Modifiers = new();
	public SetSpellModifier(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Modifiers.Count);

		foreach (var spellMod in Modifiers)
			spellMod.Write(_worldPacket);
	}
}

public class UnlearnedSpells : ServerPacket
{
	public List<uint> SpellID = new();
	public bool SuppressMessaging;
	public UnlearnedSpells() : base(ServerOpcodes.UnlearnedSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(SpellID.Count);

		foreach (var spellId in SpellID)
			_worldPacket.WriteUInt32(spellId);

		_worldPacket.WriteBit(SuppressMessaging);
		_worldPacket.FlushBits();
	}
}

public class CooldownEvent : ServerPacket
{
	public bool IsPet;
	public uint SpellID;

	public CooldownEvent(bool isPet, uint spellId) : base(ServerOpcodes.CooldownEvent, ConnectionType.Instance)
	{
		IsPet = isPet;
		SpellID = spellId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}

public class ClearCooldowns : ServerPacket
{
	public List<uint> SpellID = new();
	public bool IsPet;
	public ClearCooldowns() : base(ServerOpcodes.ClearCooldowns, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(SpellID.Count);

		if (!SpellID.Empty())
			SpellID.ForEach(p => _worldPacket.WriteUInt32(p));

		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}

public class ClearCooldown : ServerPacket
{
	public bool IsPet;
	public uint SpellID;
	public bool ClearOnHold;
	public ClearCooldown() : base(ServerOpcodes.ClearCooldown, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(ClearOnHold);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}

public class ModifyCooldown : ServerPacket
{
	public bool IsPet;
	public bool WithoutCategoryCooldown;
	public int DeltaTime;
	public uint SpellID;
	public ModifyCooldown() : base(ServerOpcodes.ModifyCooldown, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(DeltaTime);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.WriteBit(WithoutCategoryCooldown);
		_worldPacket.FlushBits();
	}
}

public class SpellCooldownPkt : ServerPacket
{
	public List<SpellCooldownStruct> SpellCooldowns = new();
	public ObjectGuid Caster;
	public SpellCooldownFlags Flags;
	public SpellCooldownPkt() : base(ServerOpcodes.SpellCooldown, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteUInt8((byte)Flags);
		_worldPacket.WriteInt32(SpellCooldowns.Count);
		SpellCooldowns.ForEach(p => p.Write(_worldPacket));
	}
}

public class SendSpellHistory : ServerPacket
{
	public List<SpellHistoryEntry> Entries = new();
	public SendSpellHistory() : base(ServerOpcodes.SendSpellHistory, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Entries.Count);
		Entries.ForEach(p => p.Write(_worldPacket));
	}
}

public class ClearAllSpellCharges : ServerPacket
{
	public bool IsPet;
	public ClearAllSpellCharges() : base(ServerOpcodes.ClearAllSpellCharges, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}

public class ClearSpellCharges : ServerPacket
{
	public bool IsPet;
	public uint Category;
	public ClearSpellCharges() : base(ServerOpcodes.ClearSpellCharges, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Category);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}

public class SetSpellCharges : ServerPacket
{
	public bool IsPet;
	public uint Category;
	public uint NextRecoveryTime;
	public byte ConsumedCharges;
	public float ChargeModRate = 1.0f;
	public SetSpellCharges() : base(ServerOpcodes.SetSpellCharges) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Category);
		_worldPacket.WriteUInt32(NextRecoveryTime);
		_worldPacket.WriteUInt8(ConsumedCharges);
		_worldPacket.WriteFloat(ChargeModRate);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}

public class SendSpellCharges : ServerPacket
{
	public List<SpellChargeEntry> Entries = new();
	public SendSpellCharges() : base(ServerOpcodes.SendSpellCharges, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Entries.Count);
		Entries.ForEach(p => p.Write(_worldPacket));
	}
}

public class ClearTarget : ServerPacket
{
	public ObjectGuid Guid;
	public ClearTarget() : base(ServerOpcodes.ClearTarget) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
	}
}

public class CancelOrphanSpellVisual : ServerPacket
{
	public uint SpellVisualID;
	public CancelOrphanSpellVisual() : base(ServerOpcodes.CancelOrphanSpellVisual) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellVisualID);
	}
}

public class CancelSpellVisual : ServerPacket
{
	public ObjectGuid Source;
	public uint SpellVisualID;
	public CancelSpellVisual() : base(ServerOpcodes.CancelSpellVisual) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Source);
		_worldPacket.WriteUInt32(SpellVisualID);
	}
}

class CancelSpellVisualKit : ServerPacket
{
	public ObjectGuid Source;
	public uint SpellVisualKitID;
	public bool MountedVisual;
	public CancelSpellVisualKit() : base(ServerOpcodes.CancelSpellVisualKit) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Source);
		_worldPacket.WriteUInt32(SpellVisualKitID);
		_worldPacket.WriteBit(MountedVisual);
		_worldPacket.FlushBits();
	}
}

class PlayOrphanSpellVisual : ServerPacket
{
	public ObjectGuid Target; // Exclusive with TargetLocation
	public Position SourceLocation;
	public uint SpellVisualID;
	public bool SpeedAsTime;
	public float TravelSpeed;
	public float LaunchDelay; // Always zero
	public float MinDuration;
	public Vector3 SourceRotation; // Vector of rotations, Orientation is z
	public Vector3 TargetLocation; // Exclusive with Target
	public PlayOrphanSpellVisual() : base(ServerOpcodes.PlayOrphanSpellVisual) { }

	public override void Write()
	{
		_worldPacket.WriteXYZ(SourceLocation);
		_worldPacket.WriteVector3(SourceRotation);
		_worldPacket.WriteVector3(TargetLocation);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WriteUInt32(SpellVisualID);
		_worldPacket.WriteFloat(TravelSpeed);
		_worldPacket.WriteFloat(LaunchDelay);
		_worldPacket.WriteFloat(MinDuration);
		_worldPacket.WriteBit(SpeedAsTime);
		_worldPacket.FlushBits();
	}
}

class PlaySpellVisual : ServerPacket
{
	public ObjectGuid Source;
	public ObjectGuid Target;
	public ObjectGuid Transport;   // Used when Target = Empty && (SpellVisual::Flags & 0x400) == 0
	public Vector3 TargetPosition; // Overrides missile destination for SpellVisual::SpellVisualMissileSetID
	public uint SpellVisualID;
	public float TravelSpeed;
	public ushort HitReason;
	public ushort MissReason;
	public ushort ReflectStatus;
	public float LaunchDelay;
	public float MinDuration;
	public bool SpeedAsTime;
	public PlaySpellVisual() : base(ServerOpcodes.PlaySpellVisual) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Source);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(Transport);
		_worldPacket.WriteVector3(TargetPosition);
		_worldPacket.WriteUInt32(SpellVisualID);
		_worldPacket.WriteFloat(TravelSpeed);
		_worldPacket.WriteUInt16(HitReason);
		_worldPacket.WriteUInt16(MissReason);
		_worldPacket.WriteUInt16(ReflectStatus);
		_worldPacket.WriteFloat(LaunchDelay);
		_worldPacket.WriteFloat(MinDuration);
		_worldPacket.WriteBit(SpeedAsTime);
		_worldPacket.FlushBits();
	}
}

class PlaySpellVisualKit : ServerPacket
{
	public ObjectGuid Unit;
	public uint KitRecID;
	public uint KitType;
	public uint Duration;
	public bool MountedVisual;
	public PlaySpellVisualKit() : base(ServerOpcodes.PlaySpellVisualKit) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt32(KitRecID);
		_worldPacket.WriteUInt32(KitType);
		_worldPacket.WriteUInt32(Duration);
		_worldPacket.WriteBit(MountedVisual);
		_worldPacket.FlushBits();
	}
}

class SpellVisualLoadScreen : ServerPacket
{
	public int SpellVisualKitID;
	public int Delay;

	public SpellVisualLoadScreen(int spellVisualKitId, int delay) : base(ServerOpcodes.SpellVisualLoadScreen, ConnectionType.Instance)
	{
		SpellVisualKitID = spellVisualKitId;
		Delay = delay;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(SpellVisualKitID);
		_worldPacket.WriteInt32(Delay);
	}
}

public class CancelCast : ClientPacket
{
	public uint SpellID;
	public ObjectGuid CastID;
	public CancelCast(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CastID = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
	}
}

public class OpenItem : ClientPacket
{
	public byte Slot;
	public byte PackSlot;
	public OpenItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Slot = _worldPacket.ReadUInt8();
		PackSlot = _worldPacket.ReadUInt8();
	}
}

public class SpellChannelStart : ServerPacket
{
	public int SpellID;
	public SpellCastVisual Visual;
	public SpellChannelStartInterruptImmunities? InterruptImmunities;
	public ObjectGuid CasterGUID;
	public SpellTargetedHealPrediction? HealPrediction;
	public uint ChannelDuration;
	public SpellChannelStart() : base(ServerOpcodes.SpellChannelStart, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WriteInt32(SpellID);

		Visual.Write(_worldPacket);

		_worldPacket.WriteUInt32(ChannelDuration);
		_worldPacket.WriteBit(InterruptImmunities.HasValue);
		_worldPacket.WriteBit(HealPrediction.HasValue);
		_worldPacket.FlushBits();

		if (InterruptImmunities.HasValue)
			InterruptImmunities.Value.Write(_worldPacket);

		if (HealPrediction.HasValue)
			HealPrediction.Value.Write(_worldPacket);
	}
}

public class SpellChannelUpdate : ServerPacket
{
	public ObjectGuid CasterGUID;
	public int TimeRemaining;
	public SpellChannelUpdate() : base(ServerOpcodes.SpellChannelUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WriteInt32(TimeRemaining);
	}
}

class ResurrectRequest : ServerPacket
{
	public ObjectGuid ResurrectOffererGUID;
	public uint ResurrectOffererVirtualRealmAddress;
	public uint PetNumber;
	public uint SpellID;
	public bool UseTimer;
	public bool Sickness;
	public string Name;
	public ResurrectRequest() : base(ServerOpcodes.ResurrectRequest) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ResurrectOffererGUID);
		_worldPacket.WriteUInt32(ResurrectOffererVirtualRealmAddress);
		_worldPacket.WriteUInt32(PetNumber);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBits(Name.GetByteCount(), 11);
		_worldPacket.WriteBit(UseTimer);
		_worldPacket.WriteBit(Sickness);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(Name);
	}
}

class UnlearnSkill : ClientPacket
{
	public uint SkillLine;
	public UnlearnSkill(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SkillLine = _worldPacket.ReadUInt32();
	}
}

class SelfRes : ClientPacket
{
	public uint SpellId;
	public SelfRes(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellId = _worldPacket.ReadUInt32();
	}
}

class GetMirrorImageData : ClientPacket
{
	public ObjectGuid UnitGUID;
	public GetMirrorImageData(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		UnitGUID = _worldPacket.ReadPackedGuid();
	}
}

class MirrorImageComponentedData : ServerPacket
{
	public ObjectGuid UnitGUID;
	public int DisplayID;
	public int SpellVisualKitID;
	public byte RaceID;
	public byte Gender;
	public byte ClassID;
	public List<ChrCustomizationChoice> Customizations = new();
	public ObjectGuid GuildGUID;

	public List<int> ItemDisplayID = new();
	public MirrorImageComponentedData() : base(ServerOpcodes.MirrorImageComponentedData) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteInt32(DisplayID);
		_worldPacket.WriteInt32(SpellVisualKitID);
		_worldPacket.WriteUInt8(RaceID);
		_worldPacket.WriteUInt8(Gender);
		_worldPacket.WriteUInt8(ClassID);
		_worldPacket.WriteInt32(Customizations.Count);
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteInt32(ItemDisplayID.Count);

		foreach (var customization in Customizations)
		{
			_worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
			_worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
		}

		foreach (var itemDisplayId in ItemDisplayID)
			_worldPacket.WriteInt32(itemDisplayId);
	}
}

class MirrorImageCreatureData : ServerPacket
{
	public ObjectGuid UnitGUID;
	public int DisplayID;
	public int SpellVisualKitID;
	public MirrorImageCreatureData() : base(ServerOpcodes.MirrorImageCreatureData) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteInt32(DisplayID);
		_worldPacket.WriteInt32(SpellVisualKitID);
	}
}

class SpellClick : ClientPacket
{
	public ObjectGuid SpellClickUnitGuid;
	public bool TryAutoDismount;
	public bool IsSoftInteract;
	public SpellClick(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellClickUnitGuid = _worldPacket.ReadPackedGuid();
		TryAutoDismount = _worldPacket.HasBit();
		IsSoftInteract = _worldPacket.HasBit();
	}
}

class ResyncRunes : ServerPacket
{
	public RuneData Runes = new();
	public ResyncRunes() : base(ServerOpcodes.ResyncRunes) { }

	public override void Write()
	{
		Runes.Write(_worldPacket);
	}
}

class AddRunePower : ServerPacket
{
	public uint AddedRunesMask;
	public AddRunePower() : base(ServerOpcodes.AddRunePower, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(AddedRunesMask);
	}
}

class MissileTrajectoryCollision : ClientPacket
{
	public ObjectGuid Target;
	public uint SpellID;
	public ObjectGuid CastID;
	public Vector3 CollisionPos;
	public MissileTrajectoryCollision(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Target = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
		CastID = _worldPacket.ReadPackedGuid();
		CollisionPos = _worldPacket.ReadVector3();
	}
}

class NotifyMissileTrajectoryCollision : ServerPacket
{
	public ObjectGuid Caster;
	public ObjectGuid CastID;
	public Vector3 CollisionPos;
	public NotifyMissileTrajectoryCollision() : base(ServerOpcodes.NotifyMissileTrajectoryCollision) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteVector3(CollisionPos);
	}
}

class UpdateMissileTrajectory : ClientPacket
{
	public ObjectGuid Guid;
	public ObjectGuid CastID;
	public ushort MoveMsgID;
	public uint SpellID;
	public float Pitch;
	public float Speed;
	public Vector3 FirePos;
	public Vector3 ImpactPos;
	public MovementInfo Status;
	public UpdateMissileTrajectory(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		CastID = _worldPacket.ReadPackedGuid();
		MoveMsgID = _worldPacket.ReadUInt16();
		SpellID = _worldPacket.ReadUInt32();
		Pitch = _worldPacket.ReadFloat();
		Speed = _worldPacket.ReadFloat();
		FirePos = _worldPacket.ReadVector3();
		ImpactPos = _worldPacket.ReadVector3();
		var hasStatus = _worldPacket.HasBit();

		_worldPacket.ResetBitPos();

		if (hasStatus)
			Status = MovementExtensions.ReadMovementInfo(_worldPacket);
	}
}

class SpellEmpowerMinHold : ClientPacket
{
	public float HoldPct;
	public SpellEmpowerMinHold(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		HoldPct = _worldPacket.ReadFloat();
	}
}

class SpellEmpowerRelease : ClientPacket
{
	public uint SpellID;
	public SpellEmpowerRelease(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellID = _worldPacket.ReadUInt32();
	}
}

public class SpellEmpowerStageUpdate : ServerPacket
{
	public ObjectGuid CastID;
	public ObjectGuid Caster;
	public int TimeRemaining;
	public bool Unk;
	public List<uint> RemainingStageDurations = new();
	public SpellEmpowerStageUpdate() : base(ServerOpcodes.SpellEmpowerUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.Write(TimeRemaining);
		_worldPacket.Write((uint)RemainingStageDurations.Count);
		_worldPacket.Write(Unk);

		foreach (var stageDuration in RemainingStageDurations)
			_worldPacket.Write(stageDuration);
	}
}

public class SpellEmpowerSetStage : ServerPacket
{
	public ObjectGuid CastID;
	public ObjectGuid Caster;
	public uint Stage;
	public SpellEmpowerSetStage() : base(ServerOpcodes.SpellEmpowerSetStage, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.Write(Stage);
	}
}

public class SpellEmpowerStart : ServerPacket
{
	public uint SpellID;
	public ObjectGuid CastID;
	public ObjectGuid Caster;
	public SpellCastVisual Visual;
	public uint Duration;
	public uint FirstStageDuration;
	public uint FinalStageDuration;
	public List<ObjectGuid> Targets = new();
	public Dictionary<byte, uint> StageDurations = new();
	public SpellChannelStartInterruptImmunities? Immunities;
	public SpellHealPrediction? HealPrediction;
	public SpellEmpowerStart() : base(ServerOpcodes.SpellEmpowerStart, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteUInt32((uint)Targets.Count);
		_worldPacket.Write(SpellID);
		Visual.Write(_worldPacket);
		_worldPacket.Write(Duration);
		_worldPacket.Write(FirstStageDuration);
		_worldPacket.Write(FinalStageDuration);
		_worldPacket.WriteUInt32((uint)StageDurations.Count);

		foreach (var target in Targets)
			_worldPacket.Write(target);

		foreach (var val in StageDurations.Values)
			_worldPacket.Write(val);

		_worldPacket.Write(Immunities.HasValue);
		_worldPacket.Write(HealPrediction.HasValue);

		if (Immunities.HasValue)
			Immunities.Value.Write(_worldPacket);

		if (HealPrediction.HasValue)
			HealPrediction.Value.Write(_worldPacket);
	}
}

public class SpellDelayed : ServerPacket
{
	public ObjectGuid Caster;
	public int ActualDelay;
	public SpellDelayed() : base(ServerOpcodes.SpellDelayed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteInt32(ActualDelay);
	}
}

class DispelFailed : ServerPacket
{
	public ObjectGuid CasterGUID;
	public ObjectGuid VictimGUID;
	public uint SpellID;
	public List<uint> FailedSpells = new();
	public DispelFailed() : base(ServerOpcodes.DispelFailed) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WritePackedGuid(VictimGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(FailedSpells.Count);

		FailedSpells.ForEach(FailedSpellID => _worldPacket.WriteUInt32(FailedSpellID));
	}
}

class CustomLoadScreen : ServerPacket
{
	readonly uint TeleportSpellID;
	readonly uint LoadingScreenID;

	public CustomLoadScreen(uint teleportSpellId, uint loadingScreenId) : base(ServerOpcodes.CustomLoadScreen)
	{
		TeleportSpellID = teleportSpellId;
		LoadingScreenID = loadingScreenId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(TeleportSpellID);
		_worldPacket.WriteUInt32(LoadingScreenID);
	}
}

class MountResultPacket : ServerPacket
{
	public uint Result;
	public MountResultPacket() : base(ServerOpcodes.MountResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Result);
	}
}

class MissileCancel : ServerPacket
{
	public ObjectGuid OwnerGUID;
	public bool Reverse;
	public uint SpellID;
	public MissileCancel() : base(ServerOpcodes.MissileCancel) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(OwnerGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(Reverse);
		_worldPacket.FlushBits();
	}
}

class TradeSkillSetFavorite : ClientPacket
{
	public uint RecipeID;
	public bool IsFavorite;

	public TradeSkillSetFavorite(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RecipeID = _worldPacket.ReadUInt32();
		IsFavorite = _worldPacket.HasBit();
	}
}

class KeyboundOverride : ClientPacket
{
	public ushort OverrideID;
	public KeyboundOverride(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		OverrideID = _worldPacket.ReadUInt16();
	}
}

//Structs
public struct SpellLogPowerData
{
	public SpellLogPowerData(int powerType, int amount, int cost)
	{
		PowerType = powerType;
		Amount = amount;
		Cost = cost;
	}

	public int PowerType;
	public int Amount;
	public int Cost;
}

public class SpellCastLogData
{
	readonly List<SpellLogPowerData> PowerData = new();

	long Health;
	double AttackPower;
	double SpellPower;
	uint Armor;

	public void Initialize(Unit unit)
	{
		Health = unit.GetHealth();
		AttackPower = unit.GetTotalAttackPowerValue(unit.Class == Class.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
		SpellPower = unit.SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
		Armor = unit.GetArmor();
		PowerData.Add(new SpellLogPowerData((int)unit.GetPowerType(), unit.GetPower(unit.GetPowerType()), 0));
	}

	public void Initialize(Spell spell)
	{
		var unitCaster = spell.Caster.AsUnit;

		if (unitCaster != null)
		{
			Health = unitCaster.GetHealth();
			AttackPower = unitCaster.GetTotalAttackPowerValue(unitCaster.Class == Class.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
			SpellPower = unitCaster.SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
			Armor = unitCaster.GetArmor();
			var primaryPowerType = unitCaster.GetPowerType();
			var primaryPowerAdded = false;

			foreach (var cost in spell.PowerCost)
			{
				PowerData.Add(new SpellLogPowerData((int)cost.Power, unitCaster.GetPower(cost.Power), (int)cost.Amount));

				if (cost.Power == primaryPowerType)
					primaryPowerAdded = true;
			}

			if (!primaryPowerAdded)
				PowerData.Insert(0, new SpellLogPowerData((int)primaryPowerType, unitCaster.GetPower(primaryPowerType), 0));
		}
	}

	public void Write(WorldPacket data)
	{
		data.WriteInt64(Health);
		data.WriteInt32((int)AttackPower);
		data.WriteInt32((int)SpellPower);
		data.WriteUInt32(Armor);
		data.WriteBits(PowerData.Count, 9);
		data.FlushBits();

		foreach (var powerData in PowerData)
		{
			data.WriteInt32(powerData.PowerType);
			data.WriteInt32(powerData.Amount);
			data.WriteInt32(powerData.Cost);
		}
	}
}

class ContentTuningParams
{
	public enum ContentTuningType
	{
		CreatureToPlayerDamage = 1,
		PlayerToCreatureDamage = 2,
		CreatureToCreatureDamage = 4,
		PlayerToSandboxScaling = 7, // NYI
		PlayerToPlayerExpectedStat = 8
	}

	public enum ContentTuningFlags
	{
		NoLevelScaling = 0x1,
		NoItemLevelScaling = 0x2
	}

	public ContentTuningType TuningType;
	public short PlayerLevelDelta;
	public float PlayerItemLevel;
	public float TargetItemLevel;
	public ushort ScalingHealthItemLevelCurveID;
	public byte TargetLevel;
	public byte Expansion;
	public sbyte TargetScalingLevelDelta;
	public ContentTuningFlags Flags = ContentTuningFlags.NoLevelScaling | ContentTuningFlags.NoItemLevelScaling;
	public uint PlayerContentTuningID;
	public uint TargetContentTuningID;
	public int Unused927;

	public bool GenerateDataForUnits(Unit attacker, Unit target)
	{
		var playerAttacker = attacker.AsPlayer;
		var creatureAttacker = attacker.AsCreature;

		if (playerAttacker)
		{
			var playerTarget = target.AsPlayer;
			var creatureTarget = target.AsCreature;

			if (playerTarget)
				return GenerateDataPlayerToPlayer(playerAttacker, playerTarget);
			else if (creatureTarget)
				if (creatureTarget.HasScalableLevels)
					return GenerateDataPlayerToCreature(playerAttacker, creatureTarget);
		}
		else if (creatureAttacker)
		{
			var playerTarget = target.AsPlayer;
			var creatureTarget = target.AsCreature;

			if (playerTarget)
			{
				if (creatureAttacker.HasScalableLevels)
					return GenerateDataCreatureToPlayer(creatureAttacker, playerTarget);
			}
			else if (creatureTarget)
			{
				if (creatureAttacker.HasScalableLevels || creatureTarget.HasScalableLevels)
					return GenerateDataCreatureToCreature(creatureAttacker, creatureTarget);
			}
		}

		return false;
	}

	public void Write(WorldPacket data)
	{
		data.WriteFloat(PlayerItemLevel);
		data.WriteFloat(TargetItemLevel);
		data.WriteInt16(PlayerLevelDelta);
		data.WriteUInt16(ScalingHealthItemLevelCurveID);
		data.WriteUInt8(TargetLevel);
		data.WriteUInt8(Expansion);
		data.WriteInt8(TargetScalingLevelDelta);
		data.WriteUInt32((uint)Flags);
		data.WriteUInt32(PlayerContentTuningID);
		data.WriteUInt32(TargetContentTuningID);
		data.WriteInt32(Unused927);
		data.WriteBits(TuningType, 4);
		data.FlushBits();
	}

	bool GenerateDataPlayerToPlayer(Player attacker, Player target)
	{
		return false;
	}

	bool GenerateDataCreatureToPlayer(Creature attacker, Player target)
	{
		var creatureTemplate = attacker.CreatureTemplate;
		var creatureScaling = creatureTemplate.GetLevelScaling(attacker.Map.GetDifficultyID());

		TuningType = ContentTuningType.CreatureToPlayerDamage;
		PlayerLevelDelta = (short)target.ActivePlayerData.ScalingPlayerLevelDelta;
		PlayerItemLevel = (ushort)target.GetAverageItemLevel();
		ScalingHealthItemLevelCurveID = (ushort)target.UnitData.ScalingHealthItemLevelCurveID;
		TargetLevel = (byte)target.Level;
		Expansion = (byte)creatureTemplate.HealthScalingExpansion;
		TargetScalingLevelDelta = (sbyte)attacker.UnitData.ScalingLevelDelta;
		TargetContentTuningID = creatureScaling.ContentTuningId;

		return true;
	}

	bool GenerateDataPlayerToCreature(Player attacker, Creature target)
	{
		var creatureTemplate = target.CreatureTemplate;
		var creatureScaling = creatureTemplate.GetLevelScaling(target.Map.GetDifficultyID());

		TuningType = ContentTuningType.PlayerToCreatureDamage;
		PlayerLevelDelta = (short)attacker.ActivePlayerData.ScalingPlayerLevelDelta;
		PlayerItemLevel = (ushort)attacker.GetAverageItemLevel();
		ScalingHealthItemLevelCurveID = (ushort)target.UnitData.ScalingHealthItemLevelCurveID;
		TargetLevel = (byte)target.Level;
		Expansion = (byte)creatureTemplate.HealthScalingExpansion;
		TargetScalingLevelDelta = (sbyte)target.UnitData.ScalingLevelDelta;
		TargetContentTuningID = creatureScaling.ContentTuningId;

		return true;
	}

	bool GenerateDataCreatureToCreature(Creature attacker, Creature target)
	{
		var accessor = target.HasScalableLevels ? target : attacker;
		var creatureTemplate = accessor.CreatureTemplate;
		var creatureScaling = creatureTemplate.GetLevelScaling(accessor.Map.GetDifficultyID());

		TuningType = ContentTuningType.CreatureToCreatureDamage;
		PlayerLevelDelta = 0;
		PlayerItemLevel = 0;
		TargetLevel = (byte)target.Level;
		Expansion = (byte)creatureTemplate.HealthScalingExpansion;
		TargetScalingLevelDelta = (sbyte)accessor.UnitData.ScalingLevelDelta;
		TargetContentTuningID = creatureScaling.ContentTuningId;

		return true;
	}
}

public struct SpellCastVisual
{
	public uint SpellXSpellVisualID;
	public uint ScriptVisualID;

	public SpellCastVisual(uint spellXSpellVisualID, uint scriptVisualID)
	{
		SpellXSpellVisualID = spellXSpellVisualID;
		ScriptVisualID = scriptVisualID;
	}

	public void Read(WorldPacket data)
	{
		SpellXSpellVisualID = data.ReadUInt32();
		ScriptVisualID = data.ReadUInt32();
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellXSpellVisualID);
		data.WriteUInt32(ScriptVisualID);
	}

	public static implicit operator SpellCastVisualField(SpellCastVisual spellCastVisual)
	{
		SpellCastVisualField visual = new();
		visual.SpellXSpellVisualID = spellCastVisual.SpellXSpellVisualID;
		visual.ScriptVisualID = spellCastVisual.ScriptVisualID;

		return visual;
	}
}

public class AuraDataInfo
{
	public ObjectGuid CastID;
	public int SpellID;
	public SpellCastVisual Visual;
	public AuraFlags Flags;
	public uint ActiveFlags;
	public ushort CastLevel = 1;
	public byte Applications = 1;
	public int ContentTuningID;
	public ObjectGuid? CastUnit;
	public int? Duration;
	public int? Remaining;
	public List<double> Points = new();
	public List<double> EstimatedPoints = new();
	readonly ContentTuningParams ContentTuning;
	readonly float? TimeMod;

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(CastID);
		data.WriteInt32(SpellID);

		Visual.Write(data);

		data.WriteUInt16((ushort)Flags);
		data.WriteUInt32(ActiveFlags);
		data.WriteUInt16(CastLevel);
		data.WriteUInt8(Applications);
		data.WriteInt32(ContentTuningID);
		data.WriteBit(CastUnit.HasValue);
		data.WriteBit(Duration.HasValue);
		data.WriteBit(Remaining.HasValue);
		data.WriteBit(TimeMod.HasValue);
		data.WriteBits(Points.Count, 6);
		data.WriteBits(EstimatedPoints.Count, 6);
		data.WriteBit(ContentTuning != null);

		if (ContentTuning != null)
			ContentTuning.Write(data);

		if (CastUnit.HasValue)
			data.WritePackedGuid(CastUnit.Value);

		if (Duration.HasValue)
			data.WriteInt32(Duration.Value);

		if (Remaining.HasValue)
			data.WriteInt32(Remaining.Value);

		if (TimeMod.HasValue)
			data.WriteFloat(TimeMod.Value);

		foreach (var point in Points)
			data.WriteFloat((float)point);

		foreach (var point in EstimatedPoints)
			data.WriteFloat((float)point);
	}
}

public struct AuraInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt8(Slot);
		data.WriteBit(AuraData != null);
		data.FlushBits();

		if (AuraData != null)
			AuraData.Write(data);
	}

	public byte Slot;
	public AuraDataInfo AuraData;
}

public class TargetLocation
{
	public ObjectGuid Transport;
	public Position Location;

	public void Read(WorldPacket data)
	{
		Transport = data.ReadPackedGuid();
		Location = new Position();
		Location.X = data.ReadFloat();
		Location.Y = data.ReadFloat();
		Location.Z = data.ReadFloat();
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Transport);
		data.WriteFloat(Location.X);
		data.WriteFloat(Location.Y);
		data.WriteFloat(Location.Z);
	}
}

public class SpellTargetData
{
	public SpellCastTargetFlags Flags;
	public ObjectGuid Unit;
	public ObjectGuid Item;
	public TargetLocation SrcLocation;
	public TargetLocation DstLocation;
	public float? Orientation;
	public int? MapID;
	public string Name = "";

	public void Read(WorldPacket data)
	{
		data.ResetBitPos();
		Flags = (SpellCastTargetFlags)data.ReadBits<uint>(28);

		if (data.HasBit())
			SrcLocation = new TargetLocation();

		if (data.HasBit())
			DstLocation = new TargetLocation();

		var hasOrientation = data.HasBit();
		var hasMapId = data.HasBit();

		var nameLength = data.ReadBits<uint>(7);

		Unit = data.ReadPackedGuid();
		Item = data.ReadPackedGuid();

		if (SrcLocation != null)
			SrcLocation.Read(data);

		if (DstLocation != null)
			DstLocation.Read(data);

		if (hasOrientation)
			Orientation = data.ReadFloat();

		if (hasMapId)
			MapID = data.ReadInt32();

		Name = data.ReadString(nameLength);
	}

	public void Write(WorldPacket data)
	{
		data.WriteBits((uint)Flags, 28);
		data.WriteBit(SrcLocation != null);
		data.WriteBit(DstLocation != null);
		data.WriteBit(Orientation.HasValue);
		data.WriteBit(MapID.HasValue);
		data.WriteBits(Name.GetByteCount(), 7);
		data.FlushBits();

		data.WritePackedGuid(Unit);
		data.WritePackedGuid(Item);

		if (SrcLocation != null)
			SrcLocation.Write(data);

		if (DstLocation != null)
			DstLocation.Write(data);

		if (Orientation.HasValue)
			data.WriteFloat(Orientation.Value);

		if (MapID.HasValue)
			data.WriteInt32(MapID.Value);

		data.WriteString(Name);
	}
}

public struct MissileTrajectoryRequest
{
	public float Pitch;
	public float Speed;

	public void Read(WorldPacket data)
	{
		Pitch = data.ReadFloat();
		Speed = data.ReadFloat();
	}
}

public struct SpellWeight
{
	public uint Type;
	public int ID;
	public uint Quantity;
}

public struct SpellCraftingReagent
{
	public int ItemID;
	public int DataSlotIndex;
	public int Quantity;
	public byte? Unknown_1000;

	public void Read(WorldPacket data)
	{
		ItemID = data.ReadInt32();
		DataSlotIndex = data.ReadInt32();
		Quantity = data.ReadInt32();

		if (data.HasBit())
			Unknown_1000 = data.ReadUInt8();
	}
}

public struct SpellExtraCurrencyCost
{
	public int CurrencyID;
	public int Count;

	public void Read(WorldPacket data)
	{
		CurrencyID = data.ReadInt32();
		Count = data.ReadInt32();
	}
}

public class SpellCastRequest
{
	public ObjectGuid CastID;
	public uint SpellID;
	public SpellCastVisual Visual;
	public uint SendCastFlags;
	public SpellTargetData Target = new();
	public MissileTrajectoryRequest MissileTrajectory;
	public MovementInfo MoveUpdate;
	public List<SpellWeight> Weight = new();
	public Array<SpellCraftingReagent> OptionalReagents = new(3);
	public Array<SpellExtraCurrencyCost> OptionalCurrencies = new(5 /*MAX_ITEM_EXT_COST_CURRENCIES*/);
	public ulong? CraftingOrderID;
	public ObjectGuid CraftingNPC;
	public uint[] Misc = new uint[2];

	public void Read(WorldPacket data)
	{
		CastID = data.ReadPackedGuid();
		Misc[0] = data.ReadUInt32();
		Misc[1] = data.ReadUInt32();
		SpellID = data.ReadUInt32();

		Visual.Read(data);

		MissileTrajectory.Read(data);
		CraftingNPC = data.ReadPackedGuid();

		var optionalCurrencies = data.ReadUInt32();
		var optionalReagents = data.ReadUInt32();

		for (var i = 0; i < optionalCurrencies; ++i)
			OptionalCurrencies[i].Read(data);

		SendCastFlags = data.ReadBits<uint>(5);
		var hasMoveUpdate = data.HasBit();
		var weightCount = data.ReadBits<uint>(2);
		var hasCraftingOrderID = data.HasBit();

		Target.Read(data);

		if (hasCraftingOrderID)
			CraftingOrderID = data.ReadUInt64();

		for (var i = 0; i < optionalReagents; ++i)
			OptionalReagents[i].Read(data);

		if (hasMoveUpdate)
			MoveUpdate = MovementExtensions.ReadMovementInfo(data);

		for (var i = 0; i < weightCount; ++i)
		{
			data.ResetBitPos();
			SpellWeight weight;
			weight.Type = data.ReadBits<uint>(2);
			weight.ID = data.ReadInt32();
			weight.Quantity = data.ReadUInt32();
			Weight.Add(weight);
		}
	}
}

public struct SpellHitStatus
{
	public SpellHitStatus(SpellMissInfo reason)
	{
		Reason = reason;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt8((byte)Reason);
	}

	public SpellMissInfo Reason;
}

public struct SpellMissStatus
{
	public SpellMissStatus(SpellMissInfo reason, SpellMissInfo reflectStatus)
	{
		Reason = reason;
		ReflectStatus = reflectStatus;
	}

	public void Write(WorldPacket data)
	{
		data.WriteBits((byte)Reason, 4);

		if (Reason == SpellMissInfo.Reflect)
			data.WriteBits(ReflectStatus, 4);

		data.FlushBits();
	}

	public SpellMissInfo Reason;
	public SpellMissInfo ReflectStatus;
}

public struct SpellPowerData
{
	public int Cost;
	public PowerType Type;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Cost);
		data.WriteInt8((sbyte)Type);
	}
}

public class RuneData
{
	public byte Start;
	public byte Count;
	public List<byte> Cooldowns = new();

	public void Write(WorldPacket data)
	{
		data.WriteUInt8(Start);
		data.WriteUInt8(Count);
		data.WriteInt32(Cooldowns.Count);

		foreach (var cd in Cooldowns)
			data.WriteUInt8(cd);
	}
}

public struct MissileTrajectoryResult
{
	public uint TravelTime;
	public float Pitch;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(TravelTime);
		data.WriteFloat(Pitch);
	}
}

public struct SpellAmmo
{
	public int DisplayID;
	public sbyte InventoryType;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(DisplayID);
		data.WriteInt8(InventoryType);
	}
}

public struct CreatureImmunities
{
	public uint School;
	public uint Value;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(School);
		data.WriteUInt32(Value);
	}
}

public struct SpellHealPrediction
{
	public ObjectGuid BeaconGUID;
	public uint Points;
	public byte Type;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Points);
		data.WriteUInt8(Type);
		data.WritePackedGuid(BeaconGUID);
	}
}

public class SpellCastData
{
	public ObjectGuid CasterGUID;
	public ObjectGuid CasterUnit;
	public ObjectGuid CastID;
	public ObjectGuid OriginalCastID;
	public int SpellID;
	public SpellCastVisual Visual;
	public SpellCastFlags CastFlags;
	public SpellCastFlagsEx CastFlagsEx;
	public uint CastTime;
	public List<ObjectGuid> HitTargets = new();
	public List<ObjectGuid> MissTargets = new();
	public List<SpellHitStatus> HitStatus = new();
	public List<SpellMissStatus> MissStatus = new();
	public SpellTargetData Target = new();
	public List<SpellPowerData> RemainingPower = new();
	public RuneData RemainingRunes;
	public MissileTrajectoryResult MissileTrajectory;
	public SpellAmmo Ammo;
	public byte DestLocSpellCastIndex;
	public List<TargetLocation> TargetPoints = new();
	public CreatureImmunities Immunities;
	public SpellHealPrediction Predict;

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(CasterGUID);
		data.WritePackedGuid(CasterUnit);
		data.WritePackedGuid(CastID);
		data.WritePackedGuid(OriginalCastID);
		data.WriteInt32(SpellID);

		Visual.Write(data);

		data.WriteUInt32((uint)CastFlags);
		data.WriteUInt32((uint)CastFlagsEx);
		data.WriteUInt32(CastTime);

		MissileTrajectory.Write(data);

		data.WriteInt32(Ammo.DisplayID);
		data.WriteUInt8(DestLocSpellCastIndex);

		Immunities.Write(data);
		Predict.Write(data);

		data.WriteBits(HitTargets.Count, 16);
		data.WriteBits(MissTargets.Count, 16);
		data.WriteBits(HitStatus.Count, 16);
		data.WriteBits(MissStatus.Count, 16);
		data.WriteBits(RemainingPower.Count, 9);
		data.WriteBit(RemainingRunes != null);
		data.WriteBits(TargetPoints.Count, 16);
		data.FlushBits();

		foreach (var missStatus in MissStatus)
			missStatus.Write(data);

		Target.Write(data);

		foreach (var hitTarget in HitTargets)
			data.WritePackedGuid(hitTarget);

		foreach (var missTarget in MissTargets)
			data.WritePackedGuid(missTarget);

		foreach (var hitStatus in HitStatus)
			hitStatus.Write(data);

		foreach (var power in RemainingPower)
			power.Write(data);

		if (RemainingRunes != null)
			RemainingRunes.Write(data);

		foreach (var targetLoc in TargetPoints)
			targetLoc.Write(data);
	}
}

public struct LearnedSpellInfo
{
	public uint SpellID;
	public bool IsFavorite;
	public int? field_8;
	public int? Superceded;
	public int? TraitDefinitionID;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellID);
		data.WriteBit(IsFavorite);
		data.WriteBit(field_8.HasValue);
		data.WriteBit(Superceded.HasValue);
		data.WriteBit(TraitDefinitionID.HasValue);
		data.FlushBits();

		if (field_8.HasValue)
			data.WriteInt32(field_8.Value);

		if (Superceded.HasValue)
			data.WriteInt32(Superceded.Value);

		if (TraitDefinitionID.HasValue)
			data.WriteInt32(TraitDefinitionID.Value);
	}
}

public struct SpellModifierData
{
	public double ModifierValue;
	public byte ClassIndex;

	public void Write(WorldPacket data)
	{
		data.WriteFloat((float)ModifierValue);
		data.WriteUInt8(ClassIndex);
	}
}

public class SpellModifierInfo
{
	public byte ModIndex;
	public List<SpellModifierData> ModifierData = new();

	public void Write(WorldPacket data)
	{
		data.WriteUInt8(ModIndex);
		data.WriteInt32(ModifierData.Count);

		foreach (var modData in ModifierData)
			modData.Write(data);
	}
}

public class SpellCooldownStruct
{
	public uint SrecID;
	public uint ForcedCooldown;
	public float ModRate = 1.0f;

	public SpellCooldownStruct(uint spellId, uint forcedCooldown)
	{
		SrecID = spellId;
		ForcedCooldown = forcedCooldown;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SrecID);
		data.WriteUInt32(ForcedCooldown);
		data.WriteFloat(ModRate);
	}
}

public class SpellHistoryEntry
{
	public uint SpellID;
	public uint ItemID;
	public uint Category;
	public int RecoveryTime;
	public int CategoryRecoveryTime;
	public float ModRate = 1.0f;
	public bool OnHold;
	readonly uint? unused622_1; // This field is not used for anything in the client in 6.2.2.20444
	readonly uint? unused622_2; // This field is not used for anything in the client in 6.2.2.20444

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellID);
		data.WriteUInt32(ItemID);
		data.WriteUInt32(Category);
		data.WriteInt32(RecoveryTime);
		data.WriteInt32(CategoryRecoveryTime);
		data.WriteFloat(ModRate);
		data.WriteBit(unused622_1.HasValue);
		data.WriteBit(unused622_2.HasValue);
		data.WriteBit(OnHold);
		data.FlushBits();

		if (unused622_1.HasValue)
			data.WriteUInt32(unused622_1.Value);

		if (unused622_2.HasValue)
			data.WriteUInt32(unused622_2.Value);
	}
}

public class SpellChargeEntry
{
	public uint Category;
	public uint NextRecoveryTime;
	public float ChargeModRate = 1.0f;
	public byte ConsumedCharges;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Category);
		data.WriteUInt32(NextRecoveryTime);
		data.WriteFloat(ChargeModRate);
		data.WriteUInt8(ConsumedCharges);
	}
}

public struct SpellChannelStartInterruptImmunities
{
	public void Write(WorldPacket data)
	{
		data.WriteInt32(SchoolImmunities);
		data.WriteInt32(Immunities);
	}

	public int SchoolImmunities;
	public int Immunities;
}

public struct SpellTargetedHealPrediction
{
	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(TargetGUID);
		Predict.Write(data);
	}

	public ObjectGuid TargetGUID;
	public SpellHealPrediction Predict;
}