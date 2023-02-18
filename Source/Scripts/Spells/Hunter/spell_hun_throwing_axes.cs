﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(200163)]
public class spell_hun_throwing_axes : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster == null || target == null)
			return;

		var targetGUID = target.GetGUID();
		var throwCount = GetSpellInfo().GetEffect(0).BasePoints;

		for (byte i = 0; i < throwCount; ++i)
			caster.m_Events.AddEventAtOffset(() =>
			                                 {
				                                 if (caster != null)
				                                 {
					                                 Unit target = ObjectAccessor.GetCreature(caster, targetGUID);

					                                 if (target != null)
						                                 caster.CastSpell(target, HunterSpells.THOWING_AXES_DAMAGE, false);
				                                 }
			                                 },
			                                 TimeSpan.FromMilliseconds(500 * i));
	}
}