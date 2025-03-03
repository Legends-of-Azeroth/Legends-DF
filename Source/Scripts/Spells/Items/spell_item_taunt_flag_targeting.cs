﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 51640 - Taunt Flag Targeting
internal class spell_item_taunt_flag_targeting : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.CorpseSrcAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveAll(obj => !obj.IsTypeId(TypeId.Player) && !obj.IsTypeId(TypeId.Corpse));

		if (targets.Empty())
		{
			FinishCast(SpellCastResult.NoValidTargets);

			return;
		}

		targets.RandomResize(1);
	}

	private void HandleDummy(int effIndex)
	{
		// we *really* want the unit implementation here
		// it sends a packet like seen on sniff
		Caster.TextEmote(TextIds.EmotePlantsFlag, HitUnit, false);

		Caster.CastSpell(HitUnit, ItemSpellIds.TauntFlag, true);
	}
}