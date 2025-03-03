﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(202360)]
public class spell_dru_blessing_of_the_ancients : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}


	private void HandleDummy(int effIndex)
	{
		var removeAura = Caster.HasAura(DruidSpells.BLESSING_OF_ELUNE) ? (uint)DruidSpells.BLESSING_OF_ELUNE : (uint)DruidSpells.BLESSING_OF_ANSHE;
		var addAura = Caster.HasAura(DruidSpells.BLESSING_OF_ELUNE) ? (uint)DruidSpells.BLESSING_OF_ANSHE : (uint)DruidSpells.BLESSING_OF_ELUNE;

		Caster.RemoveAura(removeAura);
		Caster.CastSpell(null, addAura, true);
	}
}