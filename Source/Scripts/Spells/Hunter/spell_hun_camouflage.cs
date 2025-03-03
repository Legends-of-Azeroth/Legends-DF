﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(199483)]
public class spell_hun_camouflage : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModStealth, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}


	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (Caster && Caster.IsPlayer)
		{
			Unit pet = Caster.GetGuardianPet();

			if (pet != null)
				pet.CastSpell(pet, HunterSpells.CAMOUFLAGE, true);
		}
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (Caster && Caster.IsPlayer)
		{
			Unit pet = Caster.GetGuardianPet();

			if (pet != null)
				pet.RemoveAura(HunterSpells.CAMOUFLAGE);
		}
	}
}