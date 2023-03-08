﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.DataStorage;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities;

public class Totem : Minion
{
	TotemType _totemType;
	uint _duration;

	public Totem(SummonPropertiesRecord propertiesRecord, Unit owner) : base(propertiesRecord, owner, false)
	{
		UnitTypeMask |= UnitTypeMask.Totem;
		_totemType = TotemType.Passive;
	}

	public override void Update(uint diff)
	{
		if (!GetOwner().IsAlive() || !IsAlive())
		{
			UnSummon(); // remove self

			return;
		}

		if (_duration <= diff)
		{
			UnSummon(); // remove self

			return;
		}
		else
		{
			_duration -= diff;
		}

		base.Update(diff);
	}

	public override void InitStats(uint duration)
	{
		// client requires SMSG_TOTEM_CREATED to be sent before adding to world and before removing old totem
		var owner = GetOwner().ToPlayer();

		if (owner)
		{
			if (SummonPropertiesRecord.Slot >= (int)Framework.Constants.SummonSlot.Totem && SummonPropertiesRecord.Slot < SharedConst.MaxTotemSlot)
			{
				TotemCreated packet = new();
				packet.Totem = GetGUID();
				packet.Slot = (byte)(SummonPropertiesRecord.Slot - (int)Framework.Constants.SummonSlot.Totem);
				packet.Duration = duration;
				packet.SpellID = UnitData.CreatedBySpell;
				owner.ToPlayer().SendPacket(packet);
			}

			// set display id depending on caster's race
			var totemDisplayId = Global.SpellMgr.GetModelForTotem(UnitData.CreatedBySpell, owner.GetRace());

			if (totemDisplayId != 0)
				SetDisplayId(totemDisplayId);
			else
				Log.outDebug(LogFilter.Misc, $"Totem with entry {GetEntry()}, does not have a specialized model for spell {UnitData.CreatedBySpell} and race {owner.GetRace()}. Set to default.");
		}

		base.InitStats(duration);

		// Get spell cast by totem
		var totemSpell = Global.SpellMgr.GetSpellInfo(GetSpell(), GetMap().GetDifficultyID());

		if (totemSpell != null)
			if (totemSpell.CalcCastTime() != 0) // If spell has cast time -> its an active totem
				_totemType = TotemType.Active;

		_duration = duration;
	}

	public override void InitSummon()
	{
		if (_totemType == TotemType.Passive && GetSpell() != 0)
			CastSpell(this, GetSpell(), true);

		// Some totems can have both instant effect and passive spell
		if (GetSpell(1) != 0)
			CastSpell(this, GetSpell(1), true);
	}

	public override void UnSummon()
	{
		UnSummon(TimeSpan.Zero);
	}

	public override void UnSummon(TimeSpan msTime)
	{
		if (msTime != TimeSpan.Zero)
		{
			Events.AddEvent(new ForcedUnsummonDelayEvent(this), Events.CalculateTime(msTime));

			return;
		}

		CombatStop();
		RemoveAurasDueToSpell(GetSpell(), GetGUID());

		// clear owner's totem slot
		for (byte i = (int)Framework.Constants.SummonSlot.Totem; i < SharedConst.MaxTotemSlot; ++i)
			if (GetOwner().SummonSlot[i] == GetGUID())
			{
				GetOwner().SummonSlot[i].Clear();

				break;
			}

		GetOwner().RemoveAurasDueToSpell(GetSpell(), GetGUID());

		// remove aura all party members too
		var owner = GetOwner().ToPlayer();

		if (owner != null)
		{
			owner.SendAutoRepeatCancel(this);

			var spell = Global.SpellMgr.GetSpellInfo(UnitData.CreatedBySpell, GetMap().GetDifficultyID());

			if (spell != null)
				GetSpellHistory().SendCooldownEvent(spell, 0, null, false);

			var group = owner.GetGroup();

			if (group)
				for (var refe = group.GetFirstMember(); refe != null; refe = refe.Next())
				{
					var target = refe.GetSource();

					if (target && target.IsInMap(owner) && group.SameSubGroup(owner, target))
						target.RemoveAurasDueToSpell(GetSpell(), GetGUID());
				}
		}

		AddObjectToRemoveList();
	}

	public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
	{
		// immune to all positive spells, except of stoneclaw totem absorb and sentry totem bind sight
		// totems positive spells have unit_caster target
		if (spellEffectInfo.Effect != SpellEffectName.Dummy &&
		    spellEffectInfo.Effect != SpellEffectName.ScriptEffect &&
		    spellInfo.IsPositive() &&
		    spellEffectInfo.TargetA.GetTarget() != Targets.UnitCaster &&
		    spellEffectInfo.TargetA.GetCheckType() != SpellTargetCheckTypes.Entry)
			return true;

		switch (spellEffectInfo.ApplyAuraName)
		{
			case AuraType.PeriodicDamage:
			case AuraType.PeriodicLeech:
			case AuraType.ModFear:
			case AuraType.Transform:
				return true;
			default:
				break;
		}

		return base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute);
	}

	public uint GetSpell(byte slot = 0)
	{
		return Spells[slot];
	}

	public uint GetTotemDuration()
	{
		return _duration;
	}

	public void SetTotemDuration(uint duration)
	{
		_duration = duration;
	}

	public TotemType GetTotemType()
	{
		return _totemType;
	}

	public override bool UpdateStats(Stats stat)
	{
		return true;
	}

	public override bool UpdateAllStats()
	{
		return true;
	}

	public override void UpdateResistances(SpellSchools school)
	{
	}

	public override void UpdateArmor()
	{
	}

	public override void UpdateMaxHealth()
	{
	}

	public override void UpdateMaxPower(PowerType power)
	{
	}

	public override void UpdateAttackPowerAndDamage(bool ranged = false)
	{
	}

	public override void UpdateDamagePhysical(WeaponAttackType attType)
	{
	}
}