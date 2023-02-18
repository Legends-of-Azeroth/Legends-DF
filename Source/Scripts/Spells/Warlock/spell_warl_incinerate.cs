﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Incinerate - 29722
	[SpellScript(29722)]
	public class spell_warl_incinerate : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleOnHitMainTarget(uint UnnamedParameter)
		{
			GetCaster().ModifyPower(PowerType.SoulShards, 20);
		}

		private void HandleOnHitTarget(uint UnnamedParameter)
		{
			var target = GetHitUnit();
			var caster = GetCaster();

            if (target != null)
				if (!caster.HasAura(WarlockSpells.FIRE_AND_BRIMSTONE))
					if (target != GetExplTargetUnit())
					{
						PreventHitDamage();
						return;
					}

            if (caster.HasAura(WarlockSpells.ROARING_BLAZE))
            {
                var aur = target.GetAura(WarlockSpells.IMMOLATE_DOT, caster.GetGUID());
                var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

                if (aur != null && dmgEff != null)
                {
					var dmg = GetHitDamage();
					SetHitDamage(MathFunctions.AddPct(ref dmg, dmgEff.BasePoints));
                }
            }
        }

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHitMainTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}