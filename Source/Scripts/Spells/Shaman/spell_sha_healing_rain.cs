﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 73920 - Healing Rain
[SpellScript(73920)]
internal class spell_sha_healing_rain : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var aura = GetHitAura();

		if (aura != null)
		{
			var dest = ExplTargetDest;

			if (dest != null)
			{
				var duration = SpellInfo.CalcDuration(OriginalCaster);
				var summon = Caster.Map.SummonCreature(CreatureIds.HealingRainInvisibleStalker, dest, null, (uint)duration, OriginalCaster);

				if (summon == null)
					return;

				summon.CastSpell(summon, ShamanSpells.HealingRainVisual, true);

				var script = aura.GetScript<spell_sha_healing_rain_AuraScript>();

				script?.SetVisualDummy(summon);
			}
		}
	}
}