﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_explosive_trapAI : AreaTriggerAI
{
	public int timeInterval;

	public enum UsedSpells
	{
		EXPLOSIVE_TRAP_DAMAGE = 13812
	}

	public at_hun_explosive_trapAI(AreaTrigger areatrigger) : base(areatrigger)
	{
		timeInterval = 200;
	}

	public override void OnCreate()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (!caster.ToPlayer())
			return;

		foreach (var itr in at.GetInsideUnits())
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (!caster.IsFriendlyTo(target))
			{
				var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(200));

				if (tempSumm != null)
				{
					tempSumm.SetFaction(caster.GetFaction());
					tempSumm.SetSummonerGUID(caster.GetGUID());
					PhasingHandler.InheritPhaseShift(tempSumm, caster);
					caster.CastSpell(tempSumm, UsedSpells.EXPLOSIVE_TRAP_DAMAGE, true);
					at.Remove();
				}
			}
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (!caster.IsFriendlyTo(unit))
		{
			var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(200));

			if (tempSumm != null)
			{
				tempSumm.SetFaction(caster.GetFaction());
				tempSumm.SetSummonerGUID(caster.GetGUID());
				PhasingHandler.InheritPhaseShift(tempSumm, caster);
				caster.CastSpell(tempSumm, UsedSpells.EXPLOSIVE_TRAP_DAMAGE, true);
				at.Remove();
			}
		}
	}
}