﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102793)]
public class spell_dru_ursols_vortex : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


	private struct Spells
	{
		public static uint SPELL_DRUID_URSOLS_VORTEX_SLOW = 127797;
	}


	private void HandleHit(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			caster.AddAura(Spells.SPELL_DRUID_URSOLS_VORTEX_SLOW, GetHitUnit());
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}