﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//190456 - Ignore Pain
	[SpellScript(190456)]
	public class spell_warr_ignore_pain : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(WarriorSpells.RENEWED_FURY, WarriorSpells.VENGEANCE_FOCUSED_RAGE);
		}

		private void HandleDummy(int UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				if (caster.HasAura(WarriorSpells.RENEWED_FURY))
					caster.CastSpell(caster, WarriorSpells.RENEWED_FURY_EFFECT, true);

				if (caster.HasAura(WarriorSpells.VENGEANCE_AURA))
					caster.CastSpell(caster, WarriorSpells.VENGEANCE_FOCUSED_RAGE, true);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}