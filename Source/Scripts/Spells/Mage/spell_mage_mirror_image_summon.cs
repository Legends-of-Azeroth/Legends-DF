﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(55342)]
public class spell_mage_mirror_image_summon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;

		if (caster != null)
		{
			caster.CastSpell(caster, MageSpells.MIRROR_IMAGE_LEFT, true);
			caster.CastSpell(caster, MageSpells.MIRROR_IMAGE_FRONT, true);
			caster.CastSpell(caster, MageSpells.MIRROR_IMAGE_RIGHT, true);
		}
	}
}