﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_pet_summoned : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var player = Caster.AsPlayer;

		if (player.LastPetNumber != 0)
		{
			var newPetType = (player.Class == PlayerClass.Hunter) ? PetType.Hunter : PetType.Summon;
			Pet newPet = new(player, newPetType);

			if (newPet.LoadPetFromDB(player, 0, player.LastPetNumber, true))
			{
				// revive the pet if it is dead
				if (newPet.DeathState != DeathState.Alive &&
					newPet.DeathState != DeathState.JustRespawned)
					newPet.SetDeathState(DeathState.JustRespawned);

				newPet.SetFullHealth();
				newPet.SetFullPower(newPet.DisplayPowerType);

				var summonScript = Spell.GetSpellScripts<ISpellOnSummon>();

				foreach (ISpellOnSummon summon in summonScript)
					summon.OnSummon(newPet);

				switch (newPet.Entry)
				{
					case CreatureIds.Doomguard:
					case CreatureIds.Infernal:
						newPet.Entry = CreatureIds.Imp;

						break;
					default:
						break;
				}
			}
		}
	}
}