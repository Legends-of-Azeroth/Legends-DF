﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_av_drekthar_presence : AuraScript, IAuraCheckAreaTarget
{
	public bool CheckAreaTarget(Unit target)
	{
		return (target.Entry) switch
		{
			// alliance
			// Dun Baldar North Marshal
			14762 or 14763 or 14764 or 14765 or 11948 or 14772 or 14776 or 14773 or 14777 or 11946 => true,
			_                                                                                      => false
		};
	}
}