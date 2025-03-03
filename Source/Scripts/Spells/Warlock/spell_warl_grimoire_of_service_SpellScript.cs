﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Grimoire of Service summons - 111859, 111895, 111896, 111897, 111898
[SpellScript(new uint[]
{
	111859, 111895, 111896, 111897, 111898
})]
public class spell_warl_grimoire_of_service_SpellScript : SpellScript, ISpellOnSummon
{
	public enum eServiceSpells
	{
		IMP_SINGE_MAGIC = 89808,
		VOIDWALKER_SUFFERING = 17735,
		SUCCUBUS_SEDUCTION = 6358,
		FELHUNTER_LOCK = 19647,
		FELGUARD_AXE_TOSS = 89766
	}

	public void OnSummon(Creature creature)
	{
		var caster = Caster;
		var target = ExplTargetUnit;

		if (caster == null ||
			creature == null ||
			target == null)
			return;

		switch (SpellInfo.Id)
		{
			case WarlockSpells.GRIMOIRE_IMP: // Imp
				creature.CastSpell(caster, (uint)eServiceSpells.IMP_SINGE_MAGIC, true);

				break;
			case WarlockSpells.GRIMOIRE_VOIDWALKER: // Voidwalker
				creature.CastSpell(target, (uint)eServiceSpells.VOIDWALKER_SUFFERING, true);

				break;
			case WarlockSpells.GRIMOIRE_SUCCUBUS: // Succubus
				creature.CastSpell(target, (uint)eServiceSpells.SUCCUBUS_SEDUCTION, true);

				break;
			case WarlockSpells.GRIMOIRE_FELHUNTER: // Felhunter
				creature.CastSpell(target, (uint)eServiceSpells.FELHUNTER_LOCK, true);

				break;
			case WarlockSpells.GRIMOIRE_FELGUARD: // Felguard
				creature.CastSpell(target, (uint)eServiceSpells.FELGUARD_AXE_TOSS, true);

				break;
		}
	}
}