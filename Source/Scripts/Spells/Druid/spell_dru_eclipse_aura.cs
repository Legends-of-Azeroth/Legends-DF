﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script] // 48517 Eclipse (Solar) + 48518 Eclipse (Lunar)
internal class spell_dru_eclipse_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemoved, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var auraEffDummy = Target.GetAuraEffect(DruidSpellIds.EclipseDummy, 0);

		if (auraEffDummy == null)
			return;

		var spellId = SpellInfo.Id == DruidSpellIds.EclipseSolarAura ? DruidSpellIds.EclipseLunarSpellCnt : DruidSpellIds.EclipseSolarSpellCnt;
		spell_dru_eclipse_common.SetSpellCount(Target, spellId, (uint)auraEffDummy.Amount);
	}
}