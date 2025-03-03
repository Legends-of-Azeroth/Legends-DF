﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(236189)]
public class spell_dh_demonic_infusion : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;

		if (caster == null)
			return;

		caster.SpellHistory.ResetCharges(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.DEMON_SPIKES, Difficulty.None).ChargeCategoryId);
		caster.CastSpell(caster, DemonHunterSpells.DEMON_SPIKES, true);
		caster.SpellHistory.ResetCharges(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.DEMON_SPIKES, Difficulty.None).ChargeCategoryId);
	}
}