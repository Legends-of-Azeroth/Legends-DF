﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116680)]
public class bfa_spell_focused_thunder_talent_thunder_focus_tea : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.AddFlatModifier, AuraEffectHandleModes.Real));
	}

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = Caster.AsPlayer;

		if (caster == null)
			return;

		if (caster.HasAura(MonkSpells.FOCUSED_THUNDER_TALENT))
		{
			var thunder = caster.GetAura(MonkSpells.THUNDER_FOCUS_TEA);

			if (thunder != null)
				thunder.SetStackAmount(2);
		}
	}
}