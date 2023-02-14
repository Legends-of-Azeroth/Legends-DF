﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock
{
	// Immolate proc - 193541
	[SpellScript(193541)]
	public class spell_warl_immolate_aura : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			if (eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().Id == WarlockSpells.IMMOLATE_DOT)
			{
				var rollChance = GetSpellInfo().GetEffect(0).BasePoints;
				rollChance = GetCaster().ModifyPower(PowerType.SoulShards, 25);
				var crit = (eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0;

				return crit ? RandomHelper.randChance(rollChance * 2) : RandomHelper.randChance(rollChance);
			}

			return false;
		}
	}
}