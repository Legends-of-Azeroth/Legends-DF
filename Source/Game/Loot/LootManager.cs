﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;

namespace Game.Loots;

using LootStoreItemList = List<LootStoreItem>;
using LootTemplateMap = Dictionary<uint, LootTemplate>;

public class LootManager : LootStorage
{
	public static void LoadLootTables()
	{
		Initialize();
		LoadLootTemplates_Creature();
		LoadLootTemplates_Fishing();
		LoadLootTemplates_Gameobject();
		LoadLootTemplates_Item();
		LoadLootTemplates_Mail();
		LoadLootTemplates_Milling();
		LoadLootTemplates_Pickpocketing();
		LoadLootTemplates_Skinning();
		LoadLootTemplates_Disenchant();
		LoadLootTemplates_Prospecting();
		LoadLootTemplates_Spell();

		LoadLootTemplates_Reference();
	}

	public static Dictionary<ObjectGuid, Loot> GenerateDungeonEncounterPersonalLoot(uint dungeonEncounterId, uint lootId, LootStore store,
																					LootType type, WorldObject lootOwner, uint minMoney, uint maxMoney, ushort lootMode, ItemContext context, List<Player> tappers)
	{
		Dictionary<Player, Loot> tempLoot = new();

		foreach (var tapper in tappers)
		{
			if (tapper.IsLockedToDungeonEncounter(dungeonEncounterId))
				continue;

			Loot loot = new(lootOwner.Map, lootOwner.GUID, type, null);
			loot.SetItemContext(context);
			loot.SetDungeonEncounterId(dungeonEncounterId);
			loot.GenerateMoneyLoot(minMoney, maxMoney);

			tempLoot[tapper] = loot;
		}

		var tab = store.GetLootFor(lootId);

		if (tab != null)
			tab.ProcessPersonalLoot(tempLoot, store.IsRatesAllowed(), lootMode);

		Dictionary<ObjectGuid, Loot> personalLoot = new();

		foreach (var (looter, loot) in tempLoot)
		{
			loot.FillNotNormalLootFor(looter);

			if (loot.IsLooted())
				continue;

			personalLoot[looter.GUID] = loot;
		}

		return personalLoot;
	}

	public static void LoadLootTemplates_Creature()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading creature loot templates...");

		var oldMSTime = Time.MSTime;

		List<uint> lootIdSetUsed = new();
		var count = Creature.LoadAndCollectLootIds(out var lootIdSet);

		// Remove real entries and check loot existence
		var ctc = Global.ObjectMgr.GetCreatureTemplates();

		foreach (var pair in ctc)
		{
			var lootid = pair.Value.LootId;

			if (lootid != 0)
			{
				if (!lootIdSet.Contains(lootid))
					Creature.ReportNonExistingId(lootid, pair.Value.Entry);
				else
					lootIdSetUsed.Add(lootid);
			}
		}

		foreach (var id in lootIdSetUsed)
			lootIdSet.Remove(id);

		// 1 means loot for player corpse
		lootIdSet.Remove(SharedConst.PlayerCorpseLootEntry);

		// output error for any still listed (not referenced from appropriate table) ids
		Creature.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creature loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 creature loot templates. DB table `creature_loot_template` is empty");
	}

	public static void LoadLootTemplates_Disenchant()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading disenchanting loot templates...");

		var oldMSTime = Time.MSTime;

		List<uint> lootIdSetUsed = new();
		var count = Disenchant.LoadAndCollectLootIds(out var lootIdSet);

		foreach (var disenchant in CliDB.ItemDisenchantLootStorage.Values)
		{
			var lootid = disenchant.Id;

			if (!lootIdSet.Contains(lootid))
				Disenchant.ReportNonExistingId(lootid, disenchant.Id);
			else
				lootIdSetUsed.Add(lootid);
		}

		foreach (var id in lootIdSetUsed)
			lootIdSet.Remove(id);

		// output error for any still listed (not referenced from appropriate table) ids
		Disenchant.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} disenchanting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 disenchanting loot templates. DB table `disenchant_loot_template` is empty");
	}

	public static void LoadLootTemplates_Fishing()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading fishing loot templates...");

		var oldMSTime = Time.MSTime;

		var count = Fishing.LoadAndCollectLootIds(out var lootIdSet);

		// remove real entries and check existence loot
		foreach (var areaEntry in CliDB.AreaTableStorage.Values)
			if (lootIdSet.Contains(areaEntry.Id))
				lootIdSet.Remove(areaEntry.Id);

		// output error for any still listed (not referenced from appropriate table) ids
		Fishing.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} fishing loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 fishing loot templates. DB table `fishing_loot_template` is empty");
	}

	public static void LoadLootTemplates_Gameobject()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading gameobject loot templates...");

		var oldMSTime = Time.MSTime;

		List<uint> lootIdSetUsed = new();
		var count = Gameobject.LoadAndCollectLootIds(out var lootIdSet);

		void checkLootId(uint lootId, uint gameObjectId)
		{
			if (!lootIdSet.Contains(lootId))
				Gameobject.ReportNonExistingId(lootId, gameObjectId);
			else
				lootIdSetUsed.Add(lootId);
		}

		// remove real entries and check existence loot
		var gotc = Global.ObjectMgr.GetGameObjectTemplates();

		foreach (var (gameObjectId, gameObjectTemplate) in gotc)
		{
			var lootid = gameObjectTemplate.GetLootId();

			if (lootid != 0)
				checkLootId(lootid, gameObjectId);

			if (gameObjectTemplate.type == GameObjectTypes.Chest)
			{
				if (gameObjectTemplate.Chest.chestPersonalLoot != 0)
					checkLootId(gameObjectTemplate.Chest.chestPersonalLoot, gameObjectId);

				if (gameObjectTemplate.Chest.chestPushLoot != 0)
					checkLootId(gameObjectTemplate.Chest.chestPushLoot, gameObjectId);
			}
		}

		foreach (var id in lootIdSetUsed)
			lootIdSet.Remove(id);

		// output error for any still listed (not referenced from appropriate table) ids
		Gameobject.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} gameobject loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 gameobject loot templates. DB table `gameobject_loot_template` is empty");
	}

	public static void LoadLootTemplates_Item()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading item loot templates...");

		var oldMSTime = Time.MSTime;

		var count = Items.LoadAndCollectLootIds(out var lootIdSet);

		// remove real entries and check existence loot
		var its = Global.ObjectMgr.GetItemTemplates();

		foreach (var pair in its)
			if (lootIdSet.Contains(pair.Value.Id) && pair.Value.HasFlag(ItemFlags.HasLoot))
				lootIdSet.Remove(pair.Value.Id);

		// output error for any still listed (not referenced from appropriate table) ids
		Items.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} item loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 item loot templates. DB table `item_loot_template` is empty");
	}

	public static void LoadLootTemplates_Milling()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading milling loot templates...");

		var oldMSTime = Time.MSTime;

		var count = Milling.LoadAndCollectLootIds(out var lootIdSet);

		// remove real entries and check existence loot
		var its = Global.ObjectMgr.GetItemTemplates();

		foreach (var pair in its)
		{
			if (!pair.Value.HasFlag(ItemFlags.IsMillable))
				continue;

			if (lootIdSet.Contains(pair.Value.Id))
				lootIdSet.Remove(pair.Value.Id);
		}

		// output error for any still listed (not referenced from appropriate table) ids
		Milling.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} milling loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 milling loot templates. DB table `milling_loot_template` is empty");
	}

	public static void LoadLootTemplates_Pickpocketing()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading pickpocketing loot templates...");

		var oldMSTime = Time.MSTime;

		List<uint> lootIdSetUsed = new();
		var count = Pickpocketing.LoadAndCollectLootIds(out var lootIdSet);

		// Remove real entries and check loot existence
		var ctc = Global.ObjectMgr.GetCreatureTemplates();

		foreach (var pair in ctc)
		{
			var lootid = pair.Value.PickPocketId;

			if (lootid != 0)
			{
				if (!lootIdSet.Contains(lootid))
					Pickpocketing.ReportNonExistingId(lootid, pair.Value.Entry);
				else
					lootIdSetUsed.Add(lootid);
			}
		}

		foreach (var id in lootIdSetUsed)
			lootIdSet.Remove(id);

		// output error for any still listed (not referenced from appropriate table) ids
		Pickpocketing.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pickpocketing loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 pickpocketing loot templates. DB table `pickpocketing_loot_template` is empty");
	}

	public static void LoadLootTemplates_Prospecting()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading prospecting loot templates...");

		var oldMSTime = Time.MSTime;

		var count = Prospecting.LoadAndCollectLootIds(out var lootIdSet);

		// remove real entries and check existence loot
		var its = Global.ObjectMgr.GetItemTemplates();

		foreach (var pair in its)
		{
			if (!pair.Value.HasFlag(ItemFlags.IsProspectable))
				continue;

			if (lootIdSet.Contains(pair.Value.Id))
				lootIdSet.Remove(pair.Value.Id);
		}

		// output error for any still listed (not referenced from appropriate table) ids
		Prospecting.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} prospecting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 prospecting loot templates. DB table `prospecting_loot_template` is empty");
	}

	public static void LoadLootTemplates_Mail()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading mail loot templates...");

		var oldMSTime = Time.MSTime;

		var count = Mail.LoadAndCollectLootIds(out var lootIdSet);

		// remove real entries and check existence loot
		foreach (var mail in CliDB.MailTemplateStorage.Values)
			if (lootIdSet.Contains(mail.Id))
				lootIdSet.Remove(mail.Id);

		// output error for any still listed (not referenced from appropriate table) ids
		Mail.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} mail loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 mail loot templates. DB table `mail_loot_template` is empty");
	}

	public static void LoadLootTemplates_Skinning()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading skinning loot templates...");

		var oldMSTime = Time.MSTime;

		List<uint> lootIdSetUsed = new();
		var count = Skinning.LoadAndCollectLootIds(out var lootIdSet);

		// remove real entries and check existence loot
		var ctc = Global.ObjectMgr.GetCreatureTemplates();

		foreach (var pair in ctc)
		{
			var lootid = pair.Value.SkinLootId;

			if (lootid != 0)
			{
				if (!lootIdSet.Contains(lootid))
					Skinning.ReportNonExistingId(lootid, pair.Value.Entry);
				else
					lootIdSetUsed.Add(lootid);
			}
		}

		foreach (var id in lootIdSetUsed)
			lootIdSet.Remove(id);

		// output error for any still listed (not referenced from appropriate table) ids
		Skinning.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} skinning loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 skinning loot templates. DB table `skinning_loot_template` is empty");
	}

	public static void LoadLootTemplates_Spell()
	{
		// TODO: change this to use MiscValue from spell effect as id instead of spell id
		Log.outInfo(LogFilter.ServerLoading, "Loading spell loot templates...");

		var oldMSTime = Time.MSTime;

		var count = Spell.LoadAndCollectLootIds(out var lootIdSet);

		// remove real entries and check existence loot
		foreach (var spellNameEntry in CliDB.SpellNameStorage.Values)
		{
			var spellInfo = Global.SpellMgr.GetSpellInfo(spellNameEntry.Id, Difficulty.None);

			if (spellInfo == null)
				continue;

			// possible cases
			if (!spellInfo.IsLootCrafting)
				continue;

			if (!lootIdSet.Contains(spellInfo.Id))
			{
				// not report about not trainable spells (optionally supported by DB)
				// ignore 61756 (Northrend Inscription Research (FAST QA VERSION) for example
				if (!spellInfo.HasAttribute(SpellAttr0.NotShapeshifted) || spellInfo.HasAttribute(SpellAttr0.IsTradeskill))
					Spell.ReportNonExistingId(spellInfo.Id, spellInfo.Id);
			}
			else
			{
				lootIdSet.Remove(spellInfo.Id);
			}
		}

		// output error for any still listed (not referenced from appropriate table) ids
		Spell.ReportUnusedIds(lootIdSet);

		if (count != 0)
			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell loot templates. DB table `spell_loot_template` is empty");
	}

	public static void LoadLootTemplates_Reference()
	{
		Log.outInfo(LogFilter.ServerLoading, "Loading reference loot templates...");

		var oldMSTime = Time.MSTime;

		Reference.LoadAndCollectLootIds(out var lootIdSet);

		// check references and remove used
		Creature.CheckLootRefs(lootIdSet);
		Fishing.CheckLootRefs(lootIdSet);
		Gameobject.CheckLootRefs(lootIdSet);
		Items.CheckLootRefs(lootIdSet);
		Milling.CheckLootRefs(lootIdSet);
		Pickpocketing.CheckLootRefs(lootIdSet);
		Skinning.CheckLootRefs(lootIdSet);
		Disenchant.CheckLootRefs(lootIdSet);
		Prospecting.CheckLootRefs(lootIdSet);
		Mail.CheckLootRefs(lootIdSet);
		Reference.CheckLootRefs(lootIdSet);

		// output error for any still listed ids (not referenced from any loot table)
		Reference.ReportUnusedIds(lootIdSet);

		Log.outInfo(LogFilter.ServerLoading, "Loaded reference loot templates in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
	}

	static void Initialize()
	{
		Creature = new LootStore("creature_loot_template", "creature entry");
		Disenchant = new LootStore("disenchant_loot_template", "item disenchant id");
		Fishing = new LootStore("fishing_loot_template", "area id");
		Gameobject = new LootStore("gameobject_loot_template", "gameobject entry");
		Items = new LootStore("item_loot_template", "item entry");
		Mail = new LootStore("mail_loot_template", "mail template id", false);
		Milling = new LootStore("milling_loot_template", "item entry (herb)");
		Pickpocketing = new LootStore("pickpocketing_loot_template", "creature pickpocket lootid");
		Prospecting = new LootStore("prospecting_loot_template", "item entry (ore)");
		Reference = new LootStore("reference_loot_template", "reference id", false);
		Skinning = new LootStore("skinning_loot_template", "creature skinning id");
		Spell = new LootStore("spell_loot_template", "spell id (random item creating)", false);
	}
}

public class LootStoreItem
{
	public static WorldCfg[] qualityToRate = new WorldCfg[7]
	{
		WorldCfg.RateDropItemPoor,      // ITEM_QUALITY_POOR
		WorldCfg.RateDropItemNormal,    // ITEM_QUALITY_NORMAL
		WorldCfg.RateDropItemUncommon,  // ITEM_QUALITY_UNCOMMON
		WorldCfg.RateDropItemRare,      // ITEM_QUALITY_RARE
		WorldCfg.RateDropItemEpic,      // ITEM_QUALITY_EPIC
		WorldCfg.RateDropItemLegendary, // ITEM_QUALITY_LEGENDARY
		WorldCfg.RateDropItemArtifact,  // ITEM_QUALITY_ARTIFACT
	};

	public uint itemid;    // id of the item
	public uint reference; // referenced TemplateleId
	public float chance;   // chance to drop for both quest and non-quest items, chance to be used for refs
	public ushort lootmode;
	public bool needs_quest; // quest drop (negative ChanceOrQuestChance in DB)
	public byte groupid;
	public byte mincount;              // mincount for drop items
	public byte maxcount;              // max drop count for the item mincount or Ref multiplicator
	public List<Condition> conditions; // additional loot condition

	public LootStoreItem(uint _itemid, uint _reference, float _chance, bool _needs_quest, ushort _lootmode, byte _groupid, byte _mincount, byte _maxcount)
	{
		itemid = _itemid;
		reference = _reference;
		chance = _chance;
		lootmode = _lootmode;
		needs_quest = _needs_quest;
		groupid = _groupid;
		mincount = _mincount;
		maxcount = _maxcount;
		conditions = new List<Condition>();
	}

	public bool Roll(bool rate)
	{
		if (chance >= 100.0f)
			return true;

		if (reference > 0) // reference case
			return RandomHelper.randChance(chance * (rate ? WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferenced) : 1.0f));

		var pProto = Global.ObjectMgr.GetItemTemplate(itemid);

		var qualityModifier = pProto != null && rate ? WorldConfig.GetFloatValue(qualityToRate[(int)pProto.Quality]) : 1.0f;

		return RandomHelper.randChance(chance * qualityModifier);
	}

	public bool IsValid(LootStore store, uint entry)
	{
		if (mincount == 0)
		{
			Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: wrong mincount ({3}) - skipped", store.GetName(), entry, itemid, reference);

			return false;
		}

		if (reference == 0) // item (quest or non-quest) entry, maybe grouped
		{
			var proto = Global.ObjectMgr.GetItemTemplate(itemid);

			if (proto == null)
			{
				if (ConfigMgr.GetDefaultValue("load.autoclean", false))
					DB.World.Execute($"DELETE FROM {store.GetName()} WHERE Entry = {itemid}");
				else
					Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: item does not exist - skipped", store.GetName(), entry, itemid);

				return false;
			}

			if (chance == 0 && groupid == 0) // Zero chance is allowed for grouped entries only
			{
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: equal-chanced grouped entry, but group not defined - skipped", store.GetName(), entry, itemid);

				return false;
			}

			if (chance != 0 && chance < 0.000001f) // loot with low chance
			{
				Log.outError(LogFilter.Sql,
							"Table '{0}' entry {1} item {2}: low chance ({3}) - skipped",
							store.GetName(),
							entry,
							itemid,
							chance);

				return false;
			}

			if (maxcount < mincount) // wrong max count
			{
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: max count ({3}) less that min count ({4}) - skipped", store.GetName(), entry, itemid, maxcount, reference);

				return false;
			}
		}
		else // mincountOrRef < 0
		{
			if (needs_quest)
			{
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: quest chance will be treated as non-quest chance", store.GetName(), entry, itemid);
			}
			else if (chance == 0) // no chance for the reference
			{
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: zero chance is specified for a reference, skipped", store.GetName(), entry, itemid);

				return false;
			}
		}

		return true; // Referenced template existence is checked at whole store level
	}
}

public class LootStore
{
	readonly LootTemplateMap m_LootTemplates = new();
	readonly string m_name;
	readonly string m_entryName;
	readonly bool m_ratesAllowed;

	public LootStore(string name, string entryName, bool ratesAllowed = true)
	{
		m_name = name;
		m_entryName = entryName;
		m_ratesAllowed = ratesAllowed;
	}

	public uint LoadAndCollectLootIds(out List<uint> lootIdSet)
	{
		var count = LoadLootTable();
		lootIdSet = new List<uint>();

		foreach (var tab in m_LootTemplates)
			lootIdSet.Add(tab.Key);

		return count;
	}

	public void CheckLootRefs(List<uint> ref_set = null)
	{
		foreach (var pair in m_LootTemplates)
			pair.Value.CheckLootRefs(m_LootTemplates, ref_set);
	}

	public void ReportUnusedIds(List<uint> lootIdSet)
	{
		// all still listed ids isn't referenced
		foreach (var id in lootIdSet)
			if (ConfigMgr.GetDefaultValue("load.autoclean", false))
				DB.World.Execute($"DELETE FROM {GetName()} WHERE Entry = {id}");
			else
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} isn't {2} and not referenced from loot, and then useless.", GetName(), id, GetEntryName());
	}

	public void ReportNonExistingId(uint lootId, uint ownerId)
	{
		Log.outDebug(LogFilter.Sql, "Table '{0}' Entry {1} does not exist but it is used by {2} {3}", GetName(), lootId, GetEntryName(), ownerId);
	}

	public bool HaveLootFor(uint loot_id)
	{
		return m_LootTemplates.LookupByKey(loot_id) != null;
	}

	public bool HaveQuestLootFor(uint loot_id)
	{
		var lootTemplate = m_LootTemplates.LookupByKey(loot_id);

		if (lootTemplate == null)
			return false;

		// scan loot for quest items
		return lootTemplate.HasQuestDrop(m_LootTemplates);
	}

	public bool HaveQuestLootForPlayer(uint loot_id, Player player)
	{
		var tab = m_LootTemplates.LookupByKey(loot_id);

		if (tab != null)
			if (tab.HasQuestDropForPlayer(m_LootTemplates, player))
				return true;

		return false;
	}

	public LootTemplate GetLootFor(uint loot_id)
	{
		var tab = m_LootTemplates.LookupByKey(loot_id);

		if (tab == null)
			return null;

		return tab;
	}

	public void ResetConditions()
	{
		foreach (var pair in m_LootTemplates)
		{
			List<Condition> empty = new();
			pair.Value.CopyConditions(empty);
		}
	}

	public LootTemplate GetLootForConditionFill(uint loot_id)
	{
		var tab = m_LootTemplates.LookupByKey(loot_id);

		if (tab == null)
			return null;

		return tab;
	}

	public string GetName()
	{
		return m_name;
	}

	public bool IsRatesAllowed()
	{
		return m_ratesAllowed;
	}

	void Verify()
	{
		foreach (var i in m_LootTemplates)
			i.Value.Verify(this, i.Key);
	}

	string GetEntryName()
	{
		return m_entryName;
	}

	uint LoadLootTable()
	{
		// Clearing store (for reloading case)
		Clear();

		//                                            0     1      2        3         4             5          6        7         8
		var result = DB.World.Query("SELECT Entry, Item, Reference, Chance, QuestRequired, LootMode, GroupId, MinCount, MaxCount FROM {0}", GetName());

		if (result.IsEmpty())
			return 0;

		uint count = 0;

		do
		{
			var entry = result.Read<uint>(0);
			var item = result.Read<uint>(1);
			var reference = result.Read<uint>(2);
			var chance = result.Read<float>(3);
			var needsquest = result.Read<bool>(4);
			var lootmode = result.Read<ushort>(5);
			var groupid = result.Read<byte>(6);
			var mincount = result.Read<byte>(7);
			var maxcount = result.Read<byte>(8);

			if (groupid >= 1 << 7) // it stored in 7 bit field
			{
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: group ({3}) must be less {4} - skipped", GetName(), entry, item, groupid, 1 << 7);

				return 0;
			}

			LootStoreItem storeitem = new(item, reference, chance, needsquest, lootmode, groupid, mincount, maxcount);

			if (!storeitem.IsValid(this, entry)) // Validity checks
				continue;

			// Looking for the template of the entry
			// often entries are put together
			if (m_LootTemplates.Empty() || !m_LootTemplates.ContainsKey(entry))
				m_LootTemplates.Add(entry, new LootTemplate());

			// Adds current row to the template
			m_LootTemplates[entry].AddEntry(storeitem);
			++count;
		} while (result.NextRow());

		Verify(); // Checks validity of the loot store

		return count;
	}

	void Clear()
	{
		m_LootTemplates.Clear();
	}
}

public class LootTemplate
{
	readonly LootStoreItemList Entries = new();         // not grouped only
	readonly Dictionary<int, LootGroup> Groups = new(); // groups have own (optimised) processing, grouped entries go there

	public void AddEntry(LootStoreItem item)
	{
		if (item.groupid > 0 && item.reference == 0) // Group
		{
			if (!Groups.ContainsKey(item.groupid - 1))
				Groups[item.groupid - 1] = new LootGroup();

			Groups[item.groupid - 1].AddEntry(item); // Adds new entry to the group
		}
		else // Non-grouped entries and references are stored together
		{
			Entries.Add(item);
		}
	}

	public void Process(Loot loot, bool rate, ushort lootMode, byte groupId, Player personalLooter = null)
	{
		if (groupId != 0) // Group reference uses own processing of the group
		{
			if (groupId > Groups.Count)
				return; // Error message already printed at loading stage

			if (Groups[groupId - 1] == null)
				return;

			Groups[groupId - 1].Process(loot, lootMode, personalLooter);

			return;
		}

		// Rolling non-grouped items
		foreach (var item in Entries)
		{
			if (!Convert.ToBoolean(item.lootmode & lootMode)) // Do not add if mode mismatch
				continue;

			if (!item.Roll(rate))
				continue; // Bad luck for the entry

			if (item.reference > 0) // References processing
			{
				var Referenced = LootStorage.Reference.GetLootFor(item.reference);

				if (Referenced == null)
					continue; // Error message already printed at loading stage

				var maxcount = (uint)(item.maxcount * WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferencedAmount));

				for (uint loop = 0; loop < maxcount; ++loop) // Ref multiplicator
					Referenced.Process(loot, rate, lootMode, item.groupid, personalLooter);
			}
			else
			{
				// Plain entries (not a reference, not grouped)
				// Chance is already checked, just add
				if (personalLooter == null ||
					LootItem.AllowedForPlayer(personalLooter,
											null,
											item.itemid,
											item.needs_quest,
											!item.needs_quest || Global.ObjectMgr.GetItemTemplate(item.itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
											true,
											item.conditions))
					loot.AddItem(item);
			}
		}

		// Now processing groups
		foreach (var group in Groups.Values)
			if (group != null)
				group.Process(loot, lootMode, personalLooter);
	}

	public void ProcessPersonalLoot(Dictionary<Player, Loot> personalLoot, bool rate, ushort lootMode)
	{
		List<Player> getLootersForItem(Func<Player, bool> predicate)
		{
			List<Player> lootersForItem = new();

			foreach (var (looter, loot) in personalLoot)
				if (predicate(looter))
					lootersForItem.Add(looter);

			return lootersForItem;
		}

		// Rolling non-grouped items
		foreach (var item in Entries)
		{
			if ((item.lootmode & lootMode) == 0) // Do not add if mode mismatch
				continue;

			if (!item.Roll(rate))
				continue; // Bad luck for the entry

			if (item.reference > 0) // References processing
			{
				var referenced = LootStorage.Reference.GetLootFor(item.reference);

				if (referenced == null)
					continue; // Error message already printed at loading stage

				var maxcount = (uint)((float)item.maxcount * WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferencedAmount));
				List<Player> gotLoot = new();

				for (uint loop = 0; loop < maxcount; ++loop) // Ref multiplicator
				{
					var lootersForItem = getLootersForItem(looter => referenced.HasDropForPlayer(looter, item.groupid, true));

					// nobody can loot this, skip it
					if (lootersForItem.Empty())
						break;

					var newEnd = lootersForItem.RemoveAll(looter => gotLoot.Contains(looter));

					if (lootersForItem.Count == newEnd)
						// if we run out of looters this means that there are more items dropped than players
						// start a new cycle adding one item to everyone
						gotLoot.Clear();
					else
						lootersForItem.RemoveRange(newEnd, lootersForItem.Count - newEnd);

					var chosenLooter = lootersForItem.SelectRandom();
					referenced.Process(personalLoot[chosenLooter], rate, lootMode, item.groupid, chosenLooter);
					gotLoot.Add(chosenLooter);
				}
			}
			else
			{
				// Plain entries (not a reference, not grouped)
				// Chance is already checked, just add
				var lootersForItem = getLootersForItem(looter =>
				{
					return LootItem.AllowedForPlayer(looter,
													null,
													item.itemid,
													item.needs_quest,
													!item.needs_quest || Global.ObjectMgr.GetItemTemplate(item.itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
													true,
													item.conditions);
				});

				if (!lootersForItem.Empty())
				{
					var chosenLooter = lootersForItem.SelectRandom();
					personalLoot[chosenLooter].AddItem(item);
				}
			}
		}

		// Now processing groups
		foreach (var group in Groups.Values)
			if (group != null)
			{
				var lootersForGroup = getLootersForItem(looter => group.HasDropForPlayer(looter, true));

				if (!lootersForGroup.Empty())
				{
					var chosenLooter = lootersForGroup.SelectRandom();
					group.Process(personalLoot[chosenLooter], lootMode);
				}
			}
	}

	public void CopyConditions(List<Condition> conditions)
	{
		foreach (var i in Entries)
			i.conditions.Clear();

		foreach (var group in Groups.Values)
			group.CopyConditions(conditions);
	}

	public void CopyConditions(LootItem li)
	{
		// Copies the conditions list from a template item to a LootItem
		foreach (var item in Entries)
		{
			if (item.itemid != li.itemid)
				continue;

			li.conditions = item.conditions;

			break;
		}
	}

	public bool HasQuestDrop(LootTemplateMap store, byte groupId = 0)
	{
		if (groupId != 0) // Group reference
		{
			if (groupId > Groups.Count)
				return false; // Error message [should be] already printed at loading stage

			if (Groups[groupId - 1] == null)
				return false;

			return Groups[groupId - 1].HasQuestDrop();
		}

		foreach (var item in Entries)
			if (item.reference > 0) // References
			{
				var Referenced = store.LookupByKey(item.reference);

				if (Referenced == null)
					continue; // Error message [should be] already printed at loading stage

				if (Referenced.HasQuestDrop(store, item.groupid))
					return true;
			}
			else if (item.needs_quest)
			{
				return true; // quest drop found
			}

		// Now processing groups
		foreach (var group in Groups.Values)
			if (group.HasQuestDrop())
				return true;

		return false;
	}

	public bool HasQuestDropForPlayer(LootTemplateMap store, Player player, byte groupId = 0)
	{
		if (groupId != 0) // Group reference
		{
			if (groupId > Groups.Count)
				return false; // Error message already printed at loading stage

			if (Groups[groupId - 1] == null)
				return false;

			return Groups[groupId - 1].HasQuestDropForPlayer(player);
		}

		// Checking non-grouped entries
		foreach (var item in Entries)
			if (item.reference > 0) // References processing
			{
				var Referenced = store.LookupByKey(item.reference);

				if (Referenced == null)
					continue; // Error message already printed at loading stage

				if (Referenced.HasQuestDropForPlayer(store, player, item.groupid))
					return true;
			}
			else if (player.HasQuestForItem(item.itemid))
			{
				return true; // active quest drop found
			}

		// Now checking groups
		foreach (var group in Groups.Values)
			if (group.HasQuestDropForPlayer(player))
				return true;

		return false;
	}

	public void Verify(LootStore lootstore, uint id)
	{
		// Checking group chances
		foreach (var group in Groups)
			group.Value.Verify(lootstore, id, (byte)(group.Key + 1));

		// @todo References validity checks
	}

	public void CheckLootRefs(LootTemplateMap store, List<uint> ref_set)
	{
		foreach (var item in Entries)
			if (item.reference > 0)
			{
				if (LootStorage.Reference.GetLootFor(item.reference) == null)
					LootStorage.Reference.ReportNonExistingId(item.reference, item.itemid);
				else if (ref_set != null)
					ref_set.Remove(item.reference);
			}

		foreach (var group in Groups.Values)
			group.CheckLootRefs(store, ref_set);
	}

	public bool AddConditionItem(Condition cond)
	{
		if (cond == null || !cond.IsLoaded()) //should never happen, checked at loading
		{
			Log.outError(LogFilter.Loot, "LootTemplate.addConditionItem: condition is null");

			return false;
		}

		if (!Entries.Empty())
			foreach (var i in Entries)
				if (i.itemid == cond.SourceEntry)
				{
					i.conditions.Add(cond);

					return true;
				}

		if (!Groups.Empty())
			foreach (var group in Groups.Values)
			{
				if (group == null)
					continue;

				var itemList = group.GetExplicitlyChancedItemList();

				if (!itemList.Empty())
					foreach (var i in itemList)
						if (i.itemid == cond.SourceEntry)
						{
							i.conditions.Add(cond);

							return true;
						}

				itemList = group.GetEqualChancedItemList();

				if (!itemList.Empty())
					foreach (var i in itemList)
						if (i.itemid == cond.SourceEntry)
						{
							i.conditions.Add(cond);

							return true;
						}
			}

		return false;
	}

	public bool IsReference(uint id)
	{
		foreach (var storeItem in Entries)
			if (storeItem.itemid == id && storeItem.reference > 0)
				return true;

		return false; //not found or not reference
	}

	// True if template includes at least 1 drop for the player
	bool HasDropForPlayer(Player player, byte groupId, bool strictUsabilityCheck)
	{
		if (groupId != 0) // Group reference
		{
			if (groupId > Groups.Count)
				return false; // Error message already printed at loading stage

			if (Groups[groupId - 1] == null)
				return false;

			return Groups[groupId - 1].HasDropForPlayer(player, strictUsabilityCheck);
		}

		// Checking non-grouped entries
		foreach (var lootStoreItem in Entries)
			if (lootStoreItem.reference > 0) // References processing
			{
				var referenced = LootStorage.Reference.GetLootFor(lootStoreItem.reference);

				if (referenced == null)
					continue; // Error message already printed at loading stage

				if (referenced.HasDropForPlayer(player, lootStoreItem.groupid, strictUsabilityCheck))
					return true;
			}
			else if (LootItem.AllowedForPlayer(player,
												null,
												lootStoreItem.itemid,
												lootStoreItem.needs_quest,
												!lootStoreItem.needs_quest || Global.ObjectMgr.GetItemTemplate(lootStoreItem.itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
												strictUsabilityCheck,
												lootStoreItem.conditions))
			{
				return true; // active quest drop found
			}

		// Now checking groups
		foreach (var group in Groups.Values)
			if (group != null && group.HasDropForPlayer(player, strictUsabilityCheck))
				return true;

		return false;
	}

	public class LootGroup // A set of loot definitions for items (refs are not allowed)
	{
		readonly LootStoreItemList ExplicitlyChanced = new(); // Entries with chances defined in DB
		readonly LootStoreItemList EqualChanced = new();      // Zero chances - every entry takes the same chance

		public void AddEntry(LootStoreItem item)
		{
			if (item.chance != 0)
				ExplicitlyChanced.Add(item);
			else
				EqualChanced.Add(item);
		}

		public bool HasQuestDrop()
		{
			foreach (var i in ExplicitlyChanced)
				if (i.needs_quest)
					return true;

			foreach (var i in EqualChanced)
				if (i.needs_quest)
					return true;

			return false;
		}

		public bool HasQuestDropForPlayer(Player player)
		{
			foreach (var i in ExplicitlyChanced)
				if (player.HasQuestForItem(i.itemid))
					return true;

			foreach (var i in EqualChanced)
				if (player.HasQuestForItem(i.itemid))
					return true;

			return false;
		}

		public void Process(Loot loot, ushort lootMode, Player personalLooter = null)
		{
			var item = Roll(lootMode, personalLooter);

			if (item != null)
				loot.AddItem(item);
		}

		public void Verify(LootStore lootstore, uint id, byte group_id = 0)
		{
			var chance = RawTotalChance();

			if (chance > 101.0f) // @todo replace with 100% when DBs will be ready
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} group {2} has total chance > 100% ({3})", lootstore.GetName(), id, group_id, chance);

			if (chance >= 100.0f && !EqualChanced.Empty())
				Log.outError(LogFilter.Sql, "Table '{0}' entry {1} group {2} has items with chance=0% but group total chance >= 100% ({3})", lootstore.GetName(), id, group_id, chance);
		}

		public void CheckLootRefs(LootTemplateMap store, List<uint> ref_set)
		{
			foreach (var item in ExplicitlyChanced)
				if (item.reference > 0)
				{
					if (LootStorage.Reference.GetLootFor(item.reference) == null)
						LootStorage.Reference.ReportNonExistingId(item.reference, item.itemid);
					else if (ref_set != null)
						ref_set.Remove(item.reference);
				}

			foreach (var item in EqualChanced)
				if (item.reference > 0)
				{
					if (LootStorage.Reference.GetLootFor(item.reference) == null)
						LootStorage.Reference.ReportNonExistingId(item.reference, item.itemid);
					else if (ref_set != null)
						ref_set.Remove(item.reference);
				}
		}

		public LootStoreItemList GetExplicitlyChancedItemList()
		{
			return ExplicitlyChanced;
		}

		public LootStoreItemList GetEqualChancedItemList()
		{
			return EqualChanced;
		}

		public void CopyConditions(List<Condition> conditions)
		{
			foreach (var i in ExplicitlyChanced)
				i.conditions.Clear();

			foreach (var i in EqualChanced)
				i.conditions.Clear();
		}

		public bool HasDropForPlayer(Player player, bool strictUsabilityCheck)
		{
			foreach (var lootStoreItem in ExplicitlyChanced)
				if (LootItem.AllowedForPlayer(player,
											null,
											lootStoreItem.itemid,
											lootStoreItem.needs_quest,
											!lootStoreItem.needs_quest || Global.ObjectMgr.GetItemTemplate(lootStoreItem.itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
											strictUsabilityCheck,
											lootStoreItem.conditions))
					return true;

			foreach (var lootStoreItem in EqualChanced)
				if (LootItem.AllowedForPlayer(player,
											null,
											lootStoreItem.itemid,
											lootStoreItem.needs_quest,
											!lootStoreItem.needs_quest || Global.ObjectMgr.GetItemTemplate(lootStoreItem.itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
											strictUsabilityCheck,
											lootStoreItem.conditions))
					return true;

			return false;
		}

		float RawTotalChance()
		{
			float result = 0;

			foreach (var i in ExplicitlyChanced)
				if (!i.needs_quest)
					result += i.chance;

			return result;
		}

		float TotalChance()
		{
			var result = RawTotalChance();

			if (!EqualChanced.Empty() && result < 100.0f)
				return 100.0f;

			return result;
		}

		LootStoreItem Roll(ushort lootMode, Player personalLooter = null)
		{
			var possibleLoot = ExplicitlyChanced;
			possibleLoot.RemoveAll(new LootGroupInvalidSelector(lootMode, personalLooter).Check);

			if (!possibleLoot.Empty()) // First explicitly chanced entries are checked
			{
				var roll = (float)RandomHelper.randChance();

				foreach (var item in possibleLoot) // check each explicitly chanced entry in the template and modify its chance based on quality.
				{
					if (item.chance >= 100.0f)
						return item;

					roll -= item.chance;

					if (roll < 0)
						return item;
				}
			}

			possibleLoot = EqualChanced;
			possibleLoot.RemoveAll(new LootGroupInvalidSelector(lootMode, personalLooter).Check);

			if (!possibleLoot.Empty()) // If nothing selected yet - an item is taken from equal-chanced part
				return possibleLoot.SelectRandom();

			return null; // Empty drop from the group
		}
	}
}

public struct LootGroupInvalidSelector
{
	public LootGroupInvalidSelector(ushort lootMode, Player personalLooter)
	{
		_lootMode = lootMode;
		_personalLooter = personalLooter;
	}

	public bool Check(LootStoreItem item)
	{
		if ((item.lootmode & _lootMode) == 0)
			return true;

		if (_personalLooter &&
			!LootItem.AllowedForPlayer(_personalLooter,
										null,
										item.itemid,
										item.needs_quest,
										!item.needs_quest || Global.ObjectMgr.GetItemTemplate(item.itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
										true,
										item.conditions))
			return true;

		return false;
	}

	readonly ushort _lootMode;
	readonly Player _personalLooter;
}