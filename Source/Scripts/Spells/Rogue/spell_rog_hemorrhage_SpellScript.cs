﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(16511)]
public class spell_rog_hemorrhage_SpellScript : SpellScript, ISpellOnHit, ISpellBeforeHit, ISpellAfterHit
{
	private bool _bleeding;

	public void AfterHit()
	{
		var caster = Caster;
		var cp = caster.GetPower(PowerType.ComboPoints);

		if (cp > 0)
			caster.SetPower(PowerType.ComboPoints, cp - 1);
	}

	public void BeforeHit(SpellMissInfo UnnamedParameter)
	{
		var target = HitUnit;

		if (target != null)
			_bleeding = target.HasAuraState(AuraStateType.Bleed);
	}

	public void OnHit()
	{
		var _player = Caster.AsPlayer;

		if (_player != null)
			if (HitUnit)
				if (_player.HasAura(RogueSpells.GLYPH_OF_HEMORRHAGE))
					if (!_bleeding)
					{
						PreventHitAura();

						return;
					}
	}
}