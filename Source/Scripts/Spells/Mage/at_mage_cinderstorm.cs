﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_cinderstorm : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (caster.IsValidAttackTarget(unit))
				caster.CastSpell(unit, MageSpells.CINDERSTORM_DMG, true);
	}
}