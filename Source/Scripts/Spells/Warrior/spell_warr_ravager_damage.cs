﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// Ravager Damage - 156287
	[SpellScript(156287)]
	public class spell_warr_ravager_damage : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleOnHitTarget(int UnnamedParameter)
		{
			if (!_alreadyProc)
			{
				GetCaster().CastSpell(GetCaster(), WarriorSpells.RAVAGER_ENERGIZE, true);
				_alreadyProc = true;
			}

			if (GetCaster().HasAura(262304))                       // Deep Wounds
				GetCaster().CastSpell(GetHitUnit(), 262115, true); // Deep Wounds
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}

		private bool _alreadyProc = false;
	}
}