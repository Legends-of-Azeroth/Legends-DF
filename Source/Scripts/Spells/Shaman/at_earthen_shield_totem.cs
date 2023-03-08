﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman;

//AT ID : 5760
//Spell ID : 198839
[Script]
public class at_earthen_shield_totem : AreaTriggerAI
{
	public int timeInterval;

	public at_earthen_shield_totem(AreaTrigger areatrigger) : base(areatrigger)
	{
		timeInterval = 200;
	}

	public override void OnCreate()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		foreach (var itr in at.InsideUnits)
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (caster.IsFriendlyTo(target) || target == caster.OwnerUnit)
				if (!target.IsTotem)
					caster.CastSpell(target, SpellsUsed.EARTHEN_SHIELD_ABSORB, true);
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.IsTotem)
			return;

		if (caster.IsFriendlyTo(unit) || unit == caster.OwnerUnit)
			caster.CastSpell(unit, SpellsUsed.EARTHEN_SHIELD_ABSORB, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.IsTotem)
			return;

		if (unit.HasAura(SpellsUsed.EARTHEN_SHIELD_ABSORB) && unit.GetAura(SpellsUsed.EARTHEN_SHIELD_ABSORB).GetCaster() == caster)
			unit.RemoveAura(SpellsUsed.EARTHEN_SHIELD_ABSORB);
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		foreach (var itr in at.InsideUnits)
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (target != null)
				if (!target.IsTotem)
					if (target.HasAura(SpellsUsed.EARTHEN_SHIELD_ABSORB) && target.GetAura(SpellsUsed.EARTHEN_SHIELD_ABSORB).GetCaster() == caster)
						target.RemoveAura(SpellsUsed.EARTHEN_SHIELD_ABSORB);
		}
	}

	public struct SpellsUsed
	{
		public const uint EARTHEN_SHIELD_ABSORB = 201633;
	}
}