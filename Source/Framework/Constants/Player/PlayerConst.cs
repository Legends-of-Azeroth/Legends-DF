﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

public struct PlayerConst
{
	public const Expansion CurrentExpansion = Expansion.Dragonflight;

	public const int MaxTalentTiers = 7;
	public const int MaxTalentColumns = 3;
	public const int MaxTalentRank = 5;
	public const int MaxPvpTalentSlots = 4;
	public const int MinSpecializationLevel = 10;
	public const int MaxSpecializations = 5;
	public const int InitialSpecializationIndex = 4;
	public const int MaxMasterySpells = 2;

	public const int ReqPrimaryTreeTalents = 31;
	public const int ExploredZonesSize = 192;
	public const ulong MaxMoneyAmount = 99999999999UL;
	public const int MaxActionButtons = 180;
	public const int MaxActionButtonActionValue = 0x00FFFFFF + 1;

	public const int MaxDailyQuests = 25;

	public static TimeSpan InfinityCooldownDelay = TimeSpan.FromSeconds(Time.Month); // used for set "infinity cooldowns" for spells and check
	public const uint infinityCooldownDelayCheck = Time.Month / 2;
	public const int MaxPlayerSummonDelay = 2 * Time.Minute;

	// corpse reclaim times
	public const int DeathExpireStep = (5 * Time.Minute);
	public const int MaxDeathCount = 3;

	public const int MaxCUFProfiles = 5;

	public static uint[] copseReclaimDelay =
	{
		30, 60, 120
	};

	public const int MaxRunes = 7;
	public const int MaxRechargingRunes = 3;

	public const int ArtifactsAllWeaponsGeneralWeaponEquippedPassive = 197886;

	public const int MaxArtifactTier = 1;

	public const int MaxHonorLevel = 500;
	public const byte LevelMinHonor = 10;
	public const uint SpellPvpRulesEnabled = 134735;

	//Azerite
	public const uint ItemIdHeartOfAzeroth = 158075;
	public const uint MaxAzeriteItemLevel = 129;
	public const uint MaxAzeriteItemKnowledgeLevel = 30;
	public const uint PlayerConditionIdUnlockedAzeriteEssences = 69048;
	public const uint SpellIdHeartEssenceActionBarOverride = 298554;

	//Warmode
	public const uint WarmodeEnlistedSpellOutside = 269083;

	public const uint SpellExperienceEliminated = 206662;

	public const uint CurrencyMaxCapAncientMana = 2000;
}