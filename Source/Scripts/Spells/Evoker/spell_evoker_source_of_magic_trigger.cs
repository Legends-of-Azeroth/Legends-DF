﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

// all empower spells
[SpellScript(EvokerSpells.GREEN_DREAM_BREATH,
			EvokerSpells.GREEN_DREAM_BREATH_2,
			EvokerSpells.BLUE_ETERNITY_SURGE,
			EvokerSpells.BLUE_ETERNITY_SURGE_2,
			EvokerSpells.RED_FIRE_BREATH,
			EvokerSpells.RED_FIRE_BREATH,
			EvokerSpells.GREEN_SPIRITBLOOM,
			EvokerSpells.GREEN_SPIRITBLOOM_2)]
public class spell_evoker_source_of_magic_trigger : SpellScript, ISpellOnEpowerSpellEnd
{
    public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
		if (Caster.TryGetAura(EvokerSpells.BLUE_SOURCE_OF_MAGIC_AURA, out var aura))
		{
			Unit target = null;

			aura.ForEachAuraScript<IAuraScriptValues>(a => { if (a.ScriptValues.TryGetValue("target", out var targetObj)) { target = (Unit)targetObj; } });

			if (target == null)
				return;

			var amount = SpellManager.Instance.GetSpellInfo(EvokerSpells.BLUE_SOURCE_OF_MAGIC_ENERGIZE).GetEffect(0).BasePoints * (stage.Stage + 1);
			Caster.CastSpell(target, EvokerSpells.BLUE_SOURCE_OF_MAGIC_ENERGIZE, amount, true);
		}
    }
}