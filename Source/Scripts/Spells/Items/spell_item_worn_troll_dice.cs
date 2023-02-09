﻿using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 47776 - Roll 'dem Bones
internal class spell_item_worn_troll_dice : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		if (!CliDB.BroadcastTextStorage.ContainsKey(TextIds.WornTrollDice))
			return false;

		return true;
	}

	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		GetCaster().TextEmote(TextIds.WornTrollDice, GetHitUnit());

		uint minimum = 1;
		uint maximum = 6;

		// roll twice
		GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
		GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
	}
}