﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(new uint[]
{
	47758, 47757
})]
public class spell_pri_penance_triggered : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(ApplyEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(RemoveEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void ApplyEffect(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = Caster;

		if (caster != null)
			if (caster.HasAura(PriestSpells.POWER_OF_THE_DARK_SIDE_AURA))
			{
				caster.RemoveAura(PriestSpells.POWER_OF_THE_DARK_SIDE_AURA);
				caster.CastSpell(caster, PriestSpells.POWER_OF_THE_DARK_SIDE_MARKER, true);
			}
	}

	private void RemoveEffect(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = Caster;

		if (caster != null) // Penance has travel time we need to delay the aura remove a little bit...
			caster.Events.AddEventAtOffset(new DelayedAuraRemoveEvent(caster, (uint)PriestSpells.POWER_OF_THE_DARK_SIDE_MARKER), TimeSpan.FromSeconds(1));
	}
}