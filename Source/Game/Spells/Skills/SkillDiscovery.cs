﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;

namespace Game.Spells;

public class SkillDiscovery
{
	static readonly MultiMap<int, SkillDiscoveryEntry> SkillDiscoveryStorage = new();

	public static void LoadSkillDiscoveryTable()
	{
		var oldMsTime = Time.MSTime;

		SkillDiscoveryStorage.Clear(); // need for reload

		//                                                0        1         2              3
		var result = DB.World.Query("SELECT spellId, reqSpell, reqSkillValue, chance FROM skill_discovery_template");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 skill discovery definitions. DB table `skill_discovery_template` is empty.");

			return;
		}

		uint count = 0;

		StringBuilder ssNonDiscoverableEntries = new();
		List<uint> reportedReqSpells = new();

		do
		{
			var spellId = result.Read<uint>(0);
			var reqSkillOrSpell = result.Read<int>(1);
			var reqSkillValue = result.Read<uint>(2);
			var chance = result.Read<double>(3);

			if (chance <= 0) // chance
			{
				ssNonDiscoverableEntries.AppendFormat("spellId = {0} reqSkillOrSpell = {1} reqSkillValue = {2} chance = {3} (chance problem)\n", spellId, reqSkillOrSpell, reqSkillValue, chance);

				continue;
			}

			if (reqSkillOrSpell > 0) // spell case
			{
				var absReqSkillOrSpell = (uint)reqSkillOrSpell;
				var reqSpellInfo = Global.SpellMgr.GetSpellInfo(absReqSkillOrSpell, Difficulty.None);

				if (reqSpellInfo == null)
				{
					if (!reportedReqSpells.Contains(absReqSkillOrSpell))
					{
						Log.outError(LogFilter.Sql, "Spell (ID: {0}) have not existed spell (ID: {1}) in `reqSpell` field in `skill_discovery_template` table", spellId, reqSkillOrSpell);
						reportedReqSpells.Add(absReqSkillOrSpell);
					}

					continue;
				}

				// mechanic discovery
				if (reqSpellInfo.Mechanic != Mechanics.Discovery &&
					// explicit discovery ability
					!reqSpellInfo.IsExplicitDiscovery)
				{
					if (!reportedReqSpells.Contains(absReqSkillOrSpell))
					{
						Log.outError(LogFilter.Sql,
									"Spell (ID: {0}) not have MECHANIC_DISCOVERY (28) value in Mechanic field in spell.dbc" +
									" and not 100%% chance random discovery ability but listed for spellId {1} (and maybe more) in `skill_discovery_template` table",
									absReqSkillOrSpell,
									spellId);

						reportedReqSpells.Add(absReqSkillOrSpell);
					}

					continue;
				}

				SkillDiscoveryStorage.Add(reqSkillOrSpell, new SkillDiscoveryEntry(spellId, reqSkillValue, chance));
			}
			else if (reqSkillOrSpell == 0) // skill case
			{
				var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);

				if (bounds.Empty())
				{
					Log.outError(LogFilter.Sql, "Spell (ID: {0}) not listed in `SkillLineAbility.dbc` but listed with `reqSpell`=0 in `skill_discovery_template` table", spellId);

					continue;
				}

				foreach (var _spell_idx in bounds)
					SkillDiscoveryStorage.Add(-(int)_spell_idx.SkillLine, new SkillDiscoveryEntry(spellId, reqSkillValue, chance));
			}
			else
			{
				Log.outError(LogFilter.Sql, "Spell (ID: {0}) have negative value in `reqSpell` field in `skill_discovery_template` table", spellId);

				continue;
			}

			++count;
		} while (result.NextRow());

		if (ssNonDiscoverableEntries.Length != 0)
			Log.outError(LogFilter.Sql, "Some items can't be successfully discovered: have in chance field value < 0.000001 in `skill_discovery_template` DB table . List:\n{0}", ssNonDiscoverableEntries.ToString());

		// report about empty data for explicit discovery spells
		foreach (var spellNameEntry in CliDB.SpellNameStorage.Values)
		{
			var spellEntry = Global.SpellMgr.GetSpellInfo(spellNameEntry.Id, Difficulty.None);

			if (spellEntry == null)
				continue;

			// skip not explicit discovery spells
			if (!spellEntry.IsExplicitDiscovery)
				continue;

			if (!SkillDiscoveryStorage.ContainsKey((int)spellEntry.Id))
				Log.outError(LogFilter.Sql, "Spell (ID: {0}) is 100% chance random discovery ability but not have data in `skill_discovery_template` table", spellEntry.Id);
		}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} skill discovery definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMsTime));
	}

	public static uint GetExplicitDiscoverySpell(uint spellId, Player player)
	{
		// explicit discovery spell chances (always success if case exist)
		// in this case we have both skill and spell
		var tab = SkillDiscoveryStorage.LookupByKey((int)spellId);

		if (tab.Empty())
			return 0;

		var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);
		var skillvalue = !bounds.Empty() ? (uint)player.GetSkillValue((SkillType)bounds.FirstOrDefault().SkillLine) : 0;

		double full_chance = 0;

		foreach (var item_iter in tab)
			if (item_iter.ReqSkillValue <= skillvalue)
				if (!player.HasSpell(item_iter.SpellId))
					full_chance += item_iter.Chance;

		var rate = full_chance / 100.0f;
		var roll = (double)RandomHelper.randChance() * rate; // roll now in range 0..full_chance

		foreach (var item_iter in tab)
		{
			if (item_iter.ReqSkillValue > skillvalue)
				continue;

			if (player.HasSpell(item_iter.SpellId))
				continue;

			if (item_iter.Chance > roll)
				return item_iter.SpellId;

			roll -= item_iter.Chance;
		}

		return 0;
	}

	public static bool HasDiscoveredAllSpells(uint spellId, Player player)
	{
		var tab = SkillDiscoveryStorage.LookupByKey((int)spellId);

		if (tab.Empty())
			return true;

		foreach (var item_iter in tab)
			if (!player.HasSpell(item_iter.SpellId))
				return false;

		return true;
	}

	public static bool HasDiscoveredAnySpell(uint spellId, Player player)
	{
		var tab = SkillDiscoveryStorage.LookupByKey((int)spellId);

		if (tab.Empty())
			return false;

		foreach (var item_iter in tab)
			if (player.HasSpell(item_iter.SpellId))
				return true;

		return false;
	}

	public static uint GetSkillDiscoverySpell(uint skillId, uint spellId, Player player)
	{
		var skillvalue = skillId != 0 ? (uint)player.GetSkillValue((SkillType)skillId) : 0;

		// check spell case
		var tab = SkillDiscoveryStorage.LookupByKey((int)spellId);

		if (!tab.Empty())
		{
			foreach (var item_iter in tab)
				if (RandomHelper.randChance(item_iter.Chance * WorldConfig.GetFloatValue(WorldCfg.RateSkillDiscovery)) &&
					item_iter.ReqSkillValue <= skillvalue &&
					!player.HasSpell(item_iter.SpellId))
					return item_iter.SpellId;

			return 0;
		}

		if (skillId == 0)
			return 0;

		// check skill line case
		tab = SkillDiscoveryStorage.LookupByKey(-(int)skillId);

		if (!tab.Empty())
		{
			foreach (var item_iter in tab)
				if (RandomHelper.randChance(item_iter.Chance * WorldConfig.GetFloatValue(WorldCfg.RateSkillDiscovery)) &&
					item_iter.ReqSkillValue <= skillvalue &&
					!player.HasSpell(item_iter.SpellId))
					return item_iter.SpellId;

			return 0;
		}

		return 0;
	}
}