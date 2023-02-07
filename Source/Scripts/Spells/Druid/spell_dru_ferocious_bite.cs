﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 22568 - Ferocious Bite
    internal class spell_dru_ferocious_bite : SpellScript, IHasSpellEffects
    {
        private float _damageMultiplier = 0.0f;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.IncarnationKingOfTheJungle) && Global.SpellMgr.GetSpellInfo(DruidSpellIds.IncarnationKingOfTheJungle, Difficulty.None).GetEffects().Count > 1;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 1, SpellEffectName.PowerBurn, SpellScriptHookType.LaunchTarget));
            SpellEffects.Add(new EffectHandler(HandleHitTargetBurn, 1, SpellEffectName.PowerBurn, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleHitTargetDmg, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHitTargetBurn(uint effIndex)
        {
            int newValue = (int)((float)GetEffectValue() * _damageMultiplier);
            SetEffectValue(newValue);
        }

        private void HandleHitTargetDmg(uint effIndex)
        {
            int newValue = (int)((float)GetHitDamage() * (1.0f + _damageMultiplier));
            SetHitDamage(newValue);
        }

        private void HandleLaunchTarget(uint effIndex)
        {
            Unit caster = GetCaster();

            int maxExtraConsumedPower = GetEffectValue();

            AuraEffect auraEffect = caster.GetAuraEffect(DruidSpellIds.IncarnationKingOfTheJungle, 1);

            if (auraEffect != null)
            {
                float multiplier = 1.0f + (float)auraEffect.GetAmount() / 100.0f;
                maxExtraConsumedPower = (int)((float)maxExtraConsumedPower * multiplier);
                SetEffectValue(maxExtraConsumedPower);
            }

            _damageMultiplier = Math.Min(caster.GetPower(PowerType.Energy), maxExtraConsumedPower) / maxExtraConsumedPower;
        }
    }
}