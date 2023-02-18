﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(596)]
public class spell_pri_prayer_of_healing : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.GetSpellHistory().HasCooldown(PriestSpells.HOLY_WORD_SANCTIFY))
			caster.GetSpellHistory().ModifyCooldown(PriestSpells.HOLY_WORD_SANCTIFY, TimeSpan.FromSeconds(-6 * Time.InMilliseconds));
	}
}