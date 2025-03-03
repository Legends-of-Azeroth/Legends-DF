﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

//202192 - Resonance totem
[SpellScript(202192)]
public class spell_sha_resonance_effect : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicEnergize));
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (caster.OwnerUnit)
			caster.OwnerUnit.ModifyPower(PowerType.Maelstrom, +1);
	}
}