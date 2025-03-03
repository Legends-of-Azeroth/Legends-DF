﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102383)]
public class spell_dru_wild_charge_moonkin : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (Caster)
		{
			if (!Caster.IsInCombat)
				return SpellCastResult.DontReport;
		}
		else
		{
			return SpellCastResult.DontReport;
		}

		return SpellCastResult.SpellCastOk;
	}
}