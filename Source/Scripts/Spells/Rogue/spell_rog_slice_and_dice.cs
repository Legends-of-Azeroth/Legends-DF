﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(315496)]
public class spell_rog_slice_and_dice : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var _player = Caster.AsPlayer;

		if (_player != null)
		{
			var sliceAndDice = _player.GetAura(RogueSpells.SLICE_AND_DICE);

			if (sliceAndDice != null)
			{
				var costs = Spell.PowerCost;
				var c = costs.FirstOrDefault(p => p.Power == PowerType.ComboPoints);

				if (c != null)
				{
					if (c.Amount == 1)
						sliceAndDice.SetDuration(12 * Time.InMilliseconds);
					else if (c.Amount == 2)
						sliceAndDice.SetDuration(18 * Time.InMilliseconds);
					else if (c.Amount == 3)
						sliceAndDice.SetDuration(24 * Time.InMilliseconds);
					else if (c.Amount == 4)
						sliceAndDice.SetDuration(30 * Time.InMilliseconds);
					else if (c.Amount == 5)
						sliceAndDice.SetDuration(36 * Time.InMilliseconds);
				}
			}
		}
	}
}