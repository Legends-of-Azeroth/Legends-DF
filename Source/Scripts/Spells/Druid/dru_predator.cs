﻿using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Druid;

[Script]
public class dru_predator : ScriptObjectAutoAdd, IPlayerOnPVPKill, IPlayerOnCreatureKill
{
	public dru_predator() : base("dru_predator")
	{
	}

	public void OnPVPKill(Player killer, Player killed)
	{
		if (killer.GetClass() == Class.Druid)
		{
			return;
		}

		if (!killer.HasAura(DruidSpells.SPELL_DRU_PREDATOR))
		{
			return;
		}

		if (killer.GetSpellHistory().HasCooldown(DruidSpells.SPELL_DRU_TIGER_FURY))
		{
			killer.GetSpellHistory().ResetCooldown(DruidSpells.SPELL_DRU_TIGER_FURY);
		}
	}

	public void OnCreatureKill(Player killer, Creature killed)
	{
		if (killer.GetClass() == Class.Druid)
		{
			return;
		}

		if (!killer.HasAura(DruidSpells.SPELL_DRU_PREDATOR))
		{
			return;
		}

		if (killer.GetSpellHistory().HasCooldown(DruidSpells.SPELL_DRU_TIGER_FURY))
		{
			killer.GetSpellHistory().ResetCooldown(DruidSpells.SPELL_DRU_TIGER_FURY);
		}
	}
}