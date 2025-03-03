﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 40112 Knockdown Fel Cannon: The Aggro Check
internal class spell_q11010_q11102_q11023_aggro_check : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var playerTarget = HitPlayer;

		if (playerTarget)
			// Check if found player Target is on fly Mount or using flying form
			if (playerTarget.HasAuraType(AuraType.Fly) ||
				playerTarget.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
				playerTarget.CastSpell(playerTarget, QuestSpellIds.FlakCannonTrigger, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle));
	}
}