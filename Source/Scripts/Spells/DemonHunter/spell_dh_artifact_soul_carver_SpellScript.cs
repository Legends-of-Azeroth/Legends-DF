﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207407)]
public class spell_dh_artifact_soul_carver_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.WeaponPercentDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleOnHit(int effIndex)
	{
		var target = HitUnit;

		if (target != null)
		{
			var attackPower = Caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
			var damage = (165.0f / 100.0f) * attackPower + (165.0f / 100.0f) * attackPower;
			var damageOverTime = (107.415f / 100.0f) * attackPower;
			Caster.CastSpell(target, DemonHunterSpells.SOUL_CARVER_DAMAGE, (int)damage);
			Caster.CastSpell(target, DemonHunterSpells.SOUL_CARVER_DAMAGE, (int)damageOverTime);
			// Code for shattering the soul fragments
		}
	}
}