﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 41404 - Dementia
internal class spell_item_dementia : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicDummy, 0, AuraType.PeriodicDummy));
	}

	private void HandlePeriodicDummy(AuraEffect aurEff)
	{
		PreventDefaultAction();
		Target.CastSpell(Target, RandomHelper.RAND(ItemSpellIds.DementiaPos, ItemSpellIds.DementiaNeg), new CastSpellExtraArgs(aurEff));
	}
}