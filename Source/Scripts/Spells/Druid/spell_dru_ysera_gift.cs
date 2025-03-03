﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(145108)]
public class spell_dru_ysera_gift : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandlePeriodic(AuraEffect aurEff)
	{
		var caster = Caster;

		if (caster == null || !caster.IsAlive)
			return;

		var amount = MathFunctions.CalculatePct(caster.MaxHealth, aurEff.BaseAmount);
		var values = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
		values.AddSpellMod(SpellValueMod.MaxTargets, 1);
		values.AddSpellMod(SpellValueMod.BasePoint0, (int)amount);

		if (caster.IsFullHealth)
			caster.CastSpell(caster, DruidSpells.YSERA_GIFT_RAID_HEAL, values);
		else
			caster.CastSpell(caster, DruidSpells.YSERA_GIFT_CASTER_HEAL, values);
	}
}