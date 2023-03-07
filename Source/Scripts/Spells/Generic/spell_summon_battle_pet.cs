﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 118301 - Summon Battle Pet
internal class spell_summon_battle_pet : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Summon, SpellScriptHookType.EffectHit));
	}

	private void HandleSummon(int effIndex)
	{
		var creatureId = (uint)GetSpellValue().EffectBasePoints[effIndex];

		if (Global.ObjectMgr.GetCreatureTemplate(creatureId) != null)
		{
			PreventHitDefaultEffect(effIndex);

			var caster     = GetCaster();
			var properties = CliDB.SummonPropertiesStorage.LookupByKey((uint)GetEffectInfo().MiscValueB);
			var duration   = (uint)GetSpellInfo().CalcDuration(caster);
			var pos        = GetHitDest();

			Creature summon = caster.GetMap().SummonCreature(creatureId, pos, properties, duration, caster, GetSpellInfo().Id);

			summon?.SetImmuneToAll(true);
		}
	}
}