﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script]
internal class spell_quest_taming_the_beast : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (!Caster ||
			!Caster.IsAlive ||
			!Target.IsAlive)
			return;

		if (TargetApplication.RemoveMode != AuraRemoveMode.Expire)
			return;

		uint finalSpellId = Id switch
		{
			QuestSpellIds.TameIceClawBear          => QuestSpellIds.TameIceClawBear1,
			QuestSpellIds.TameLargeCragBoar        => QuestSpellIds.TameLargeCragBoar1,
			QuestSpellIds.TameSnowLeopard          => QuestSpellIds.TameSnowLeopard1,
			QuestSpellIds.TameAdultPlainstrider    => QuestSpellIds.TameAdultPlainstrider1,
			QuestSpellIds.TamePrairieStalker       => QuestSpellIds.TamePrairieStalker1,
			QuestSpellIds.TameSwoop                => QuestSpellIds.TameSwoop1,
			QuestSpellIds.TameWebwoodLurker        => QuestSpellIds.TameWebwoodLurker1,
			QuestSpellIds.TameDireMottledBoar      => QuestSpellIds.TameDireMottledBoar1,
			QuestSpellIds.TameSurfCrawler          => QuestSpellIds.TameSurfCrawler1,
			QuestSpellIds.TameArmoredScorpid       => QuestSpellIds.TameArmoredScorpid1,
			QuestSpellIds.TameNightsaberStalker    => QuestSpellIds.TameNightsaberStalker1,
			QuestSpellIds.TameStrigidScreecher     => QuestSpellIds.TameStrigidScreecher1,
			QuestSpellIds.TameBarbedCrawler        => QuestSpellIds.TameBarbedCrawler1,
			QuestSpellIds.TameGreaterTimberstrider => QuestSpellIds.TameGreaterTimberstrider1,
			QuestSpellIds.TameNightstalker         => QuestSpellIds.TameNightstalker1,
			QuestSpellIds.TameCrazedDragonhawk     => QuestSpellIds.TameCrazedDragonhawk1,
			QuestSpellIds.TameElderSpringpaw       => QuestSpellIds.TameElderSpringpaw1,
			QuestSpellIds.TameMistbat              => QuestSpellIds.TameMistbat1,
			_                                      => 0
		};

		if (finalSpellId != 0)
			Caster.CastSpell(Target, finalSpellId, true);
	}
}