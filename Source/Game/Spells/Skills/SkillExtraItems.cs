﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Entities;

namespace Game.Spells
{
	public class SkillExtraItems
	{
		private static Dictionary<uint, SkillExtraItemEntry> SkillExtraItemStorage = new();

		// loads the extra Item creation info from DB
		public static void LoadSkillExtraItemTable()
		{
			uint oldMSTime = Time.GetMSTime();

			SkillExtraItemStorage.Clear(); // need for reload

			//                                             0               1                       2                    3
			SQLResult result = DB.World.Query("SELECT spellId, requiredSpecialization, additionalCreateChance, additionalMaxNum FROM skill_extra_item_template");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell specialization definitions. DB table `skill_extra_item_template` is empty.");

				return;
			}

			uint count = 0;

			do
			{
				uint spellId = result.Read<uint>(0);

				if (!Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
				{
					Log.outError(LogFilter.Sql, "Skill specialization {0} has non-existent spell Id in `skill_extra_item_template`!", spellId);

					continue;
				}

				uint requiredSpecialization = result.Read<uint>(1);

				if (!Global.SpellMgr.HasSpellInfo(requiredSpecialization, Difficulty.None))
				{
					Log.outError(LogFilter.Sql, "Skill specialization {0} have not existed required specialization spell Id {1} in `skill_extra_item_template`!", spellId, requiredSpecialization);

					continue;
				}

				float additionalCreateChance = result.Read<float>(2);

				if (additionalCreateChance <= 0.0f)
				{
					Log.outError(LogFilter.Sql, "Skill specialization {0} has too low additional create chance in `skill_extra_item_template`!", spellId);

					continue;
				}

				byte additionalMaxNum = result.Read<byte>(3);

				if (additionalMaxNum == 0)
				{
					Log.outError(LogFilter.Sql, "Skill specialization {0} has 0 max number of extra items in `skill_extra_item_template`!", spellId);

					continue;
				}

				SkillExtraItemEntry skillExtraItemEntry = new();
				skillExtraItemEntry.requiredSpecialization = requiredSpecialization;
				skillExtraItemEntry.additionalCreateChance = additionalCreateChance;
				skillExtraItemEntry.additionalMaxNum       = additionalMaxNum;

				SkillExtraItemStorage[spellId] = skillExtraItemEntry;
				++count;
			} while (result.NextRow());

			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell specialization definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}

		public static bool CanCreateExtraItems(Player player, uint spellId, ref float additionalChance, ref byte additionalMax)
		{
			// get the info for the specified spell
			var specEntry = SkillExtraItemStorage.LookupByKey(spellId);

			if (specEntry == null)
				return false;

			// the player doesn't have the required specialization, return false
			if (!player.HasSpell(specEntry.requiredSpecialization))
				return false;

			// set the arguments to the appropriate values
			additionalChance = specEntry.additionalCreateChance;
			additionalMax    = specEntry.additionalMaxNum;

			// enable extra Item creation
			return true;
		}
	}

	internal class SkillExtraItemEntry
	{
		// the chance to create one additional Item
		public float additionalCreateChance;

		// maximum number of extra items created per crafting
		public byte additionalMaxNum;

		// the spell Id of the specialization required to create extra items
		public uint requiredSpecialization;

		public SkillExtraItemEntry(uint rS = 0, float aCC = 0f, byte aMN = 0)
		{
			requiredSpecialization = rS;
			additionalCreateChance = aCC;
			additionalMaxNum       = aMN;
		}
	}

	public class SkillPerfectItems
	{
		private static Dictionary<uint, SkillPerfectItemEntry> SkillPerfectItemStorage = new();

		// loads the perfection proc info from DB
		public static void LoadSkillPerfectItemTable()
		{
			uint oldMSTime = Time.GetMSTime();

			SkillPerfectItemStorage.Clear(); // reload capability

			//                                                  0               1                      2                  3
			SQLResult result = DB.World.Query("SELECT spellId, requiredSpecialization, perfectCreateChance, perfectItemType FROM skill_perfect_item_template");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell perfection definitions. DB table `skill_perfect_item_template` is empty.");

				return;
			}

			uint count = 0;

			do
			{
				uint spellId = result.Read<uint>(0);

				if (!Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
				{
					Log.outError(LogFilter.Sql, "Skill perfection _data for spell {0} has non-existent spell Id in `skill_perfect_item_template`!", spellId);

					continue;
				}

				uint requiredSpecialization = result.Read<uint>(1);

				if (!Global.SpellMgr.HasSpellInfo(requiredSpecialization, Difficulty.None))
				{
					Log.outError(LogFilter.Sql, "Skill perfection _data for spell {0} has non-existent required specialization spell Id {1} in `skill_perfect_item_template`!", spellId, requiredSpecialization);

					continue;
				}

				float perfectCreateChance = result.Read<float>(2);

				if (perfectCreateChance <= 0.0f)
				{
					Log.outError(LogFilter.Sql, "Skill perfection _data for spell {0} has impossibly low proc chance in `skill_perfect_item_template`!", spellId);

					continue;
				}

				uint perfectItemType = result.Read<uint>(3);

				if (Global.ObjectMgr.GetItemTemplate(perfectItemType) == null)
				{
					Log.outError(LogFilter.Sql, "Skill perfection _data for spell {0} references non-existent perfect Item Id {1} in `skill_perfect_item_template`!", spellId, perfectItemType);

					continue;
				}

				SkillPerfectItemStorage[spellId] = new SkillPerfectItemEntry(requiredSpecialization, perfectCreateChance, perfectItemType);

				++count;
			} while (result.NextRow());

			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell perfection definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}

		public static bool CanCreatePerfectItem(Player player, uint spellId, ref float perfectCreateChance, ref uint perfectItemType)
		{
			var entry = SkillPerfectItemStorage.LookupByKey(spellId);

			// no entry in DB means no perfection proc possible
			if (entry == null)
				return false;

			// if you don't have the spell needed, then no procs for you
			if (!player.HasSpell(entry.requiredSpecialization))
				return false;

			// set values as appropriate
			perfectCreateChance = entry.perfectCreateChance;
			perfectItemType     = entry.perfectItemType;

			// and tell the caller to start rolling the dice
			return true;
		}
	}

	// struct to store information about perfection procs
	// one entry per spell
	internal class SkillPerfectItemEntry
	{
		// perfection proc chance
		public float perfectCreateChance;

		// itemid of the resulting perfect Item
		public uint perfectItemType;

		// the spell Id of the spell required - it's named "specialization" to conform with SkillExtraItemEntry
		public uint requiredSpecialization;

		public SkillPerfectItemEntry(uint rS = 0, float pCC = 0f, uint pIT = 0)
		{
			requiredSpecialization = rS;
			perfectCreateChance    = pCC;
			perfectItemType        = pIT;
		}
	}
}