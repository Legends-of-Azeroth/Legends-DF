﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

//Cloudburst - 157503
[SpellScript(157503)]
public class spell_sha_cloudburst : SpellScript, IHasSpellEffects
{
	private byte l_TargetCount;
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		l_TargetCount = 0;

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitDestAreaAlly));
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHeal(int effIndex)
	{
		if (l_TargetCount != 0)
			HitHeal = HitHeal / l_TargetCount;
	}

	private void CountTargets(List<WorldObject> p_Targets)
	{
		l_TargetCount = (byte)p_Targets.Count;
	}
}