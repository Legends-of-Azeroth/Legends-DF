﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;

namespace Game.Spells;

public class SpellInfo
{
	public SpellPowerRecord[] PowerCosts = new SpellPowerRecord[SpellConst.MaxPowersPerSpell];
	public uint[] Totem = new uint[SpellConst.MaxTotems];
	public uint[] TotemCategory = new uint[SpellConst.MaxTotems];
	public int[] Reagent = new int[SpellConst.MaxReagents];
	public uint[] ReagentCount = new uint[SpellConst.MaxReagents];
	public List<SpellReagentsCurrencyRecord> ReagentsCurrency = new();
	public uint ChargeCategoryId;
	public List<uint> Labels = new();

	// SpellScalingEntry
	public ScalingInfo Scaling;

	readonly List<SpellProcsPerMinuteModRecord> _procPpmMods = new();

	readonly List<SpellEffectInfo> _effects = new();
	readonly List<SpellXSpellVisualRecord> _visuals = new();
	SpellSpecificType _spellSpecific;
	AuraStateType _auraState;

	SpellDiminishInfo _diminishInfo;
	ulong _allowedMechanicMask;

	public uint Category => CategoryId;

	public List<SpellEffectInfo> Effects => _effects;

	public List<SpellXSpellVisualRecord> SpellVisuals => _visuals;


	public uint Id { get; set; }
	public Difficulty Difficulty { get; set; }
	public uint CategoryId { get; set; }
	public DispelType Dispel { get; set; }
	public Mechanics Mechanic { get; set; }
	public SpellAttr0 Attributes { get; set; }
	public SpellAttr1 AttributesEx { get; set; }
	public SpellAttr2 AttributesEx2 { get; set; }
	public SpellAttr3 AttributesEx3 { get; set; }
	public SpellAttr4 AttributesEx4 { get; set; }
	public SpellAttr5 AttributesEx5 { get; set; }
	public SpellAttr6 AttributesEx6 { get; set; }
	public SpellAttr7 AttributesEx7 { get; set; }
	public SpellAttr8 AttributesEx8 { get; set; }
	public SpellAttr9 AttributesEx9 { get; set; }
	public SpellAttr10 AttributesEx10 { get; set; }
	public SpellAttr11 AttributesEx11 { get; set; }
	public SpellAttr12 AttributesEx12 { get; set; }
	public SpellAttr13 AttributesEx13 { get; set; }
	public SpellAttr14 AttributesEx14 { get; set; }
	public SpellCustomAttributes AttributesCu { get; set; }
	public HashSet<int> NegativeEffects { get; set; }
	public ulong Stances { get; set; }
	public ulong StancesNot { get; set; }
	public SpellCastTargetFlags Targets { get; set; }
	public uint TargetCreatureType { get; set; }
	public uint RequiresSpellFocus { get; set; }
	public uint FacingCasterFlags { get; set; }
	public AuraStateType CasterAuraState { get; set; }
	public AuraStateType TargetAuraState { get; set; }
	public AuraStateType ExcludeCasterAuraState { get; set; }
	public AuraStateType ExcludeTargetAuraState { get; set; }
	public uint CasterAuraSpell { get; set; }
	public uint TargetAuraSpell { get; set; }
	public uint ExcludeCasterAuraSpell { get; set; }
	public uint ExcludeTargetAuraSpell { get; set; }
	public AuraType CasterAuraType { get; set; }
	public AuraType TargetAuraType { get; set; }
	public AuraType ExcludeCasterAuraType { get; set; }
	public AuraType ExcludeTargetAuraType { get; set; }
	public SpellCastTimesRecord CastTimeEntry { get; set; }
	public uint RecoveryTime { get; set; }
	public uint CategoryRecoveryTime { get; set; }
	public uint StartRecoveryCategory { get; set; }
	public uint StartRecoveryTime { get; set; }
	public uint CooldownAuraSpellId { get; set; }
	public SpellInterruptFlags InterruptFlags { get; set; }
	public SpellAuraInterruptFlags AuraInterruptFlags { get; set; }
	public SpellAuraInterruptFlags2 AuraInterruptFlags2 { get; set; }
	public SpellAuraInterruptFlags ChannelInterruptFlags { get; set; }
	public SpellAuraInterruptFlags2 ChannelInterruptFlags2 { get; set; }
	public ProcFlagsInit ProcFlags { get; set; }
	public uint ProcChance { get; set; }
	public uint ProcCharges { get; set; }
	public uint ProcCooldown { get; set; }
	public float ProcBasePpm { get; set; }
	public uint MaxLevel { get; set; }
	public uint BaseLevel { get; set; }
	public uint SpellLevel { get; set; }
	public SpellDurationRecord DurationEntry { get; set; }
	public SpellRangeRecord RangeEntry { get; set; }
	public float Speed { get; set; }
	public float LaunchDelay { get; set; }
	public uint StackAmount { get; set; }
	public ItemClass EquippedItemClass { get; set; }
	public int EquippedItemSubClassMask { get; set; }
	public int EquippedItemInventoryTypeMask { get; set; }
	public uint IconFileDataId { get; set; }
	public uint ActiveIconFileDataId { get; set; }
	public uint ContentTuningId { get; set; }
	public uint ShowFutureSpellPlayerConditionId { get; set; }
	public LocalizedString SpellName { get; set; }
	public float ConeAngle { get; set; }
	public float Width { get; set; }
	public uint MaxTargetLevel { get; set; }
	public uint MaxAffectedTargets { get; set; }
	public SpellFamilyNames SpellFamilyName { get; set; }
	public FlagArray128 SpellFamilyFlags { get; set; }
	public SpellDmgClass DmgClass { get; set; }
	public SpellPreventionType PreventionType { get; set; }
	public int RequiredAreasId { get; set; }
	public SpellSchoolMask SchoolMask { get; set; }
	public Dictionary<byte, SpellEmpowerStageRecord> EmpowerStages { get; set; } = new();
	public SpellCastTargetFlags ExplicitTargetMask { get; set; }
	public SpellChainNode ChainEntry { get; set; }

	public bool IsPassiveStackableWithRanks => IsPassive && !HasEffect(SpellEffectName.ApplyAura);

	public bool IsMultiSlotAura => IsPassive || Id == 55849 || Id == 40075 || Id == 44413; // Power Spark, Fel Flak Fire, Incanter's Absorption

	public bool IsStackableOnOneSlotWithDifferentCasters =>
		// TODO: Re-verify meaning of SPELL_ATTR3_STACK_FOR_DIFF_CASTERS and update conditions here
		StackAmount > 1 && !IsChanneled && !HasAttribute(SpellAttr3.DotStackingRule);

	public bool IsDeathPersistent => HasAttribute(SpellAttr3.AllowAuraWhileDead);

	public bool IsRequiringDeadTarget => HasAttribute(SpellAttr3.OnlyOnGhosts);

	public bool CanBeUsedInCombat => !HasAttribute(SpellAttr0.NotInCombatOnlyPeaceful);

	public bool IsPositive => NegativeEffects.Count == 0;

	public bool IsChanneled => HasAttribute(SpellAttr1.IsChannelled | SpellAttr1.IsSelfChannelled);

	public bool IsMoveAllowedChannel => IsChanneled && !ChannelInterruptFlags.HasFlag(SpellAuraInterruptFlags.Moving | SpellAuraInterruptFlags.Turning);

	public bool NeedsComboPoints => HasAttribute(SpellAttr1.FinishingMoveDamage | SpellAttr1.FinishingMoveDuration);

	public bool IsNextMeleeSwingSpell => HasAttribute(SpellAttr0.OnNextSwingNoDamage | SpellAttr0.OnNextSwing);

	public bool IsAutoRepeatRangedSpell => HasAttribute(SpellAttr2.AutoRepeat);

	public bool HasInitialAggro => !(HasAttribute(SpellAttr1.NoThreat) || HasAttribute(SpellAttr2.NoInitialThreat) || HasAttribute(SpellAttr4.NoHarmfulThreat));

	public bool HasHitDelay => Speed > 0.0f || LaunchDelay > 0.0f;

	private bool IsAffectedBySpellMods => !HasAttribute(SpellAttr3.IgnoreCasterModifiers);

	public bool HasAreaAuraEffect
	{
		get
		{
			foreach (var effectInfo in _effects)
				if (effectInfo.IsAreaAuraEffect)
					return true;

			return false;
		}
	}

	public bool HasOnlyDamageEffects
	{
		get
		{
			foreach (var effectInfo in _effects)
				switch (effectInfo.Effect)
				{
					case SpellEffectName.WeaponDamage:
					case SpellEffectName.WeaponDamageNoSchool:
					case SpellEffectName.NormalizedWeaponDmg:
					case SpellEffectName.WeaponPercentDamage:
					case SpellEffectName.SchoolDamage:
					case SpellEffectName.EnvironmentalDamage:
					case SpellEffectName.HealthLeech:
					case SpellEffectName.DamageFromMaxHealthPCT:
						continue;
					default:
						return false;
				}

			return true;
		}
	}

	public bool IsExplicitDiscovery
	{
		get
		{
			if (Effects.Count < 2)
				return false;

			return ((GetEffect(0).Effect == SpellEffectName.CreateRandomItem || GetEffect(0).Effect == SpellEffectName.CreateLoot) && GetEffect(1).Effect == SpellEffectName.ScriptEffect) || Id == 64323;
		}
	}

	public bool IsLootCrafting => HasEffect(SpellEffectName.CreateRandomItem) || HasEffect(SpellEffectName.CreateLoot);

	public bool IsProfession
	{
		get
		{
			foreach (var effectInfo in _effects)
				if (effectInfo.IsEffect(SpellEffectName.Skill))
				{
					var skill = (uint)effectInfo.MiscValue;

					if (Global.SpellMgr.IsProfessionSkill(skill))
						return true;
				}

			return false;
		}
	}

	public bool IsPrimaryProfession
	{
		get
		{
			foreach (var effectInfo in _effects)
				if (effectInfo.IsEffect(SpellEffectName.Skill) && Global.SpellMgr.IsPrimaryProfessionSkill((uint)effectInfo.MiscValue))
					return true;

			return false;
		}
	}

	public bool IsPrimaryProfessionFirstRank => IsPrimaryProfession && Rank == 1;

	public bool IsAffectingArea
	{
		get
		{
			foreach (var effectInfo in _effects)
				if (effectInfo.IsEffect() && (effectInfo.IsTargetingArea || effectInfo.IsEffect(SpellEffectName.PersistentAreaAura) || effectInfo.IsAreaAuraEffect))
					return true;

			return false;
		}
	}

	// checks if spell targets are selected from area, doesn't include spell effects in check (like area wide auras for example)
	public bool IsTargetingArea
	{
		get
		{
			foreach (var effectInfo in _effects)
				if (effectInfo.IsEffect() && effectInfo.IsTargetingArea)
					return true;

			return false;
		}
	}

	public bool NeedsExplicitUnitTarget => Convert.ToBoolean(GetExplicitTargetMask() & SpellCastTargetFlags.UnitMask);

	public bool IsPassive => HasAttribute(SpellAttr0.Passive);

	public bool IsAutocastable
	{
		get
		{
			if (IsPassive)
				return false;

			if (HasAttribute(SpellAttr1.NoAutocastAi))
				return false;

			return true;
		}
	}

	public bool IsStackableWithRanks
	{
		get
		{
			if (IsPassive)
				return false;

			// All stance spells. if any better way, change it.
			foreach (var effectInfo in _effects)
				switch (SpellFamilyName)
				{
					case SpellFamilyNames.Paladin:
						// Paladin aura Spell
						if (effectInfo.Effect == SpellEffectName.ApplyAreaAuraRaid)
							return false;

						break;
					case SpellFamilyNames.Druid:
						// Druid form Spell
						if (effectInfo.Effect == SpellEffectName.ApplyAura &&
							effectInfo.ApplyAuraName == AuraType.ModShapeshift)
							return false;

						break;
				}

			return true;
		}
	}

	public bool IsCooldownStartedOnEvent
	{
		get
		{
			if (HasAttribute(SpellAttr0.CooldownOnEvent))
				return true;

			var category = CliDB.SpellCategoryStorage.LookupByKey(CategoryId);

			return category != null && category.Flags.HasAnyFlag(SpellCategoryFlags.CooldownStartsOnEvent);
		}
	}

	public bool IsAllowingDeadTarget
	{
		get
		{
			if (HasAttribute(SpellAttr2.AllowDeadTarget) || Targets.HasAnyFlag(SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.CorpseEnemy | SpellCastTargetFlags.UnitDead))
				return true;

			foreach (var effectInfo in _effects)
				if (effectInfo.TargetA.ObjectType == SpellTargetObjectTypes.Corpse || effectInfo.TargetB.ObjectType == SpellTargetObjectTypes.Corpse)
					return true;

			return false;
		}
	}

	public bool IsGroupBuff
	{
		get
		{
			foreach (var effectInfo in _effects)
				switch (effectInfo.TargetA.CheckType)
				{
					case SpellTargetCheckTypes.Party:
					case SpellTargetCheckTypes.Raid:
					case SpellTargetCheckTypes.RaidClass:
						return true;
				}

			return false;
		}
	}

	public bool IsRangedWeaponSpell => (SpellFamilyName == SpellFamilyNames.Hunter && !SpellFamilyFlags[1].HasAnyFlag(0x10000000u)) // for 53352, cannot find better way
										||
										Convert.ToBoolean(EquippedItemSubClassMask & (int)ItemSubClassWeapon.MaskRanged) ||
										Attributes.HasAnyFlag(SpellAttr0.UsesRangedSlot);

	public DiminishingGroup DiminishingReturnsGroupForSpell => _diminishInfo.DiminishGroup;

	public DiminishingReturnsType DiminishingReturnsGroupType => _diminishInfo.DiminishReturnType;

	public DiminishingLevels DiminishingReturnsMaxLevel => _diminishInfo.DiminishMaxLevel;

	public int DiminishingReturnsLimitDuration => _diminishInfo.DiminishDurationLimit;

	public ulong AllowedMechanicMask => _allowedMechanicMask;

	public int Duration
	{
		get
		{
			if (DurationEntry == null)
				return IsPassive ? -1 : 0;

			return (DurationEntry.Duration == -1) ? -1 : Math.Abs(DurationEntry.Duration);
		}
	}

	public int MaxDuration
	{
		get
		{
			if (DurationEntry == null)
				return IsPassive ? -1 : 0;

			return (DurationEntry.MaxDuration == -1) ? -1 : Math.Abs(DurationEntry.MaxDuration);
		}
	}

	public uint MaxTicks
	{
		get
		{
			uint totalTicks = 0;
			var DotDuration = Duration;

			foreach (var effectInfo in Effects)
			{
				if (!effectInfo.IsEffect(SpellEffectName.ApplyAura))
					continue;

				switch (effectInfo.ApplyAuraName)
				{
					case AuraType.PeriodicDamage:
					case AuraType.PeriodicDamagePercent:
					case AuraType.PeriodicHeal:
					case AuraType.ObsModHealth:
					case AuraType.ObsModPower:
					case AuraType.PeriodicTriggerSpellFromClient:
					case AuraType.PowerBurn:
					case AuraType.PeriodicLeech:
					case AuraType.PeriodicManaLeech:
					case AuraType.PeriodicEnergize:
					case AuraType.PeriodicDummy:
					case AuraType.PeriodicTriggerSpell:
					case AuraType.PeriodicTriggerSpellWithValue:
					case AuraType.PeriodicHealthFunnel:
						// skip infinite periodics
						if (effectInfo.ApplyAuraPeriod > 0 && DotDuration > 0)
						{
							totalTicks = (uint)DotDuration / effectInfo.ApplyAuraPeriod;

							if (HasAttribute(SpellAttr5.ExtraInitialPeriod))
								++totalTicks;
						}

						break;
				}
			}

			return totalTicks;
		}
	}

	public uint RecoveryTime1 => RecoveryTime > CategoryRecoveryTime ? RecoveryTime : CategoryRecoveryTime;

	public bool IsRanked => ChainEntry != null;

	public byte Rank
	{
		get
		{
			if (ChainEntry == null)
				return 1;

			return ChainEntry.Rank;
		}
	}

	public bool HasAnyAuraInterruptFlag => AuraInterruptFlags != SpellAuraInterruptFlags.None || AuraInterruptFlags2 != SpellAuraInterruptFlags2.None;

	public SpellInfo(SpellNameRecord spellName, Difficulty difficulty, SpellInfoLoadHelper data)
	{
		Id = spellName.Id;
		Difficulty = difficulty;

		foreach (var spellEffect in data.Effects)
		{
			_effects.EnsureWritableListIndex(spellEffect.Key, new SpellEffectInfo(this));
			_effects[spellEffect.Key] = new SpellEffectInfo(this, spellEffect.Value);
		}

		// Correct EffectIndex for blank effects
		for (var i = 0; i < _effects.Count; ++i)
			_effects[i].EffectIndex = i;

		NegativeEffects = new HashSet<int>();

		SpellName = spellName.Name;

		var _misc = data.Misc;

		if (_misc != null)
		{
			Attributes = (SpellAttr0)_misc.Attributes[0];
			AttributesEx = (SpellAttr1)_misc.Attributes[1];
			AttributesEx2 = (SpellAttr2)_misc.Attributes[2];
			AttributesEx3 = (SpellAttr3)_misc.Attributes[3];
			AttributesEx4 = (SpellAttr4)_misc.Attributes[4];
			AttributesEx5 = (SpellAttr5)_misc.Attributes[5];
			AttributesEx6 = (SpellAttr6)_misc.Attributes[6];
			AttributesEx7 = (SpellAttr7)_misc.Attributes[7];
			AttributesEx8 = (SpellAttr8)_misc.Attributes[8];
			AttributesEx9 = (SpellAttr9)_misc.Attributes[9];
			AttributesEx10 = (SpellAttr10)_misc.Attributes[10];
			AttributesEx11 = (SpellAttr11)_misc.Attributes[11];
			AttributesEx12 = (SpellAttr12)_misc.Attributes[12];
			AttributesEx13 = (SpellAttr13)_misc.Attributes[13];
			AttributesEx14 = (SpellAttr14)_misc.Attributes[14];
			CastTimeEntry = CliDB.SpellCastTimesStorage.LookupByKey(_misc.CastingTimeIndex);
			DurationEntry = CliDB.SpellDurationStorage.LookupByKey(_misc.DurationIndex);
			RangeEntry = CliDB.SpellRangeStorage.LookupByKey(_misc.RangeIndex);
			Speed = _misc.Speed;
			LaunchDelay = _misc.LaunchDelay;
			SchoolMask = (SpellSchoolMask)_misc.SchoolMask;
			IconFileDataId = _misc.SpellIconFileDataID;
			ActiveIconFileDataId = _misc.ActiveIconFileDataID;
			ContentTuningId = _misc.ContentTuningID;
			ShowFutureSpellPlayerConditionId = (uint)_misc.ShowFutureSpellPlayerConditionID;
		}

		// SpellScalingEntry
		var scaling = data.Scaling;

		if (scaling != null)
		{
			Scaling.MinScalingLevel = scaling.MinScalingLevel;
			Scaling.MaxScalingLevel = scaling.MaxScalingLevel;
			Scaling.ScalesFromItemLevel = scaling.ScalesFromItemLevel;
		}

		// SpellAuraOptionsEntry
		var options = data.AuraOptions;

		if (options != null)
		{
			ProcFlags = new ProcFlagsInit(options.ProcTypeMask);
			ProcChance = options.ProcChance;
			ProcCharges = (uint)options.ProcCharges;
			ProcCooldown = options.ProcCategoryRecovery;
			StackAmount = options.CumulativeAura;

			var _ppm = CliDB.SpellProcsPerMinuteStorage.LookupByKey(options.SpellProcsPerMinuteID);

			if (_ppm != null)
			{
				ProcBasePpm = _ppm.BaseProcRate;
				_procPpmMods = Global.DB2Mgr.GetSpellProcsPerMinuteMods(_ppm.Id);
			}
		}

		// SpellAuraRestrictionsEntry
		var aura = data.AuraRestrictions;

		if (aura != null)
		{
			CasterAuraState = (AuraStateType)aura.CasterAuraState;
			TargetAuraState = (AuraStateType)aura.TargetAuraState;
			ExcludeCasterAuraState = (AuraStateType)aura.ExcludeCasterAuraState;
			ExcludeTargetAuraState = (AuraStateType)aura.ExcludeTargetAuraState;
			CasterAuraSpell = aura.CasterAuraSpell;
			TargetAuraSpell = aura.TargetAuraSpell;
			ExcludeCasterAuraSpell = aura.ExcludeCasterAuraSpell;
			ExcludeTargetAuraSpell = aura.ExcludeTargetAuraSpell;
			CasterAuraType = (AuraType)aura.CasterAuraType;
			TargetAuraType = (AuraType)aura.TargetAuraType;
			ExcludeCasterAuraType = (AuraType)aura.ExcludeCasterAuraType;
			ExcludeTargetAuraType = (AuraType)aura.ExcludeTargetAuraType;
		}

		RequiredAreasId = -1;
		// SpellCastingRequirementsEntry
		var castreq = data.CastingRequirements;

		if (castreq != null)
		{
			RequiresSpellFocus = castreq.RequiresSpellFocus;
			FacingCasterFlags = castreq.FacingCasterFlags;
			RequiredAreasId = castreq.RequiredAreasID;
		}

		// SpellCategoriesEntry
		var categories = data.Categories;

		if (categories != null)
		{
			CategoryId = categories.Category;
			Dispel = (DispelType)categories.DispelType;
			Mechanic = (Mechanics)categories.Mechanic;
			StartRecoveryCategory = categories.StartRecoveryCategory;
			DmgClass = (SpellDmgClass)categories.DefenseType;
			PreventionType = (SpellPreventionType)categories.PreventionType;
			ChargeCategoryId = categories.ChargeCategory;
		}

		// SpellClassOptionsEntry
		SpellFamilyFlags = new FlagArray128();
		var classOptions = data.ClassOptions;

		if (classOptions != null)
		{
			SpellFamilyName = (SpellFamilyNames)classOptions.SpellClassSet;
			SpellFamilyFlags = classOptions.SpellClassMask;
		}

		// SpellCooldownsEntry
		var cooldowns = data.Cooldowns;

		if (cooldowns != null)
		{
			RecoveryTime = cooldowns.RecoveryTime;
			CategoryRecoveryTime = cooldowns.CategoryRecoveryTime;
			StartRecoveryTime = cooldowns.StartRecoveryTime;
			CooldownAuraSpellId = cooldowns.AuraSpellID;
		}

		EquippedItemClass = ItemClass.None;
		EquippedItemSubClassMask = 0;
		EquippedItemInventoryTypeMask = 0;
		// SpellEquippedItemsEntry
		var equipped = data.EquippedItems;

		if (equipped != null)
		{
			EquippedItemClass = (ItemClass)equipped.EquippedItemClass;
			EquippedItemSubClassMask = equipped.EquippedItemSubclass;
			EquippedItemInventoryTypeMask = equipped.EquippedItemInvTypes;
		}

		// SpellInterruptsEntry
		var interrupt = data.Interrupts;

		if (interrupt != null)
		{
			InterruptFlags = (SpellInterruptFlags)interrupt.InterruptFlags;
			AuraInterruptFlags = (SpellAuraInterruptFlags)interrupt.AuraInterruptFlags[0];
			AuraInterruptFlags2 = (SpellAuraInterruptFlags2)interrupt.AuraInterruptFlags[1];
			ChannelInterruptFlags = (SpellAuraInterruptFlags)interrupt.ChannelInterruptFlags[0];
			ChannelInterruptFlags2 = (SpellAuraInterruptFlags2)interrupt.ChannelInterruptFlags[1];
		}

		foreach (var label in data.Labels)
			Labels.Add(label.LabelID);

		// SpellLevelsEntry
		var levels = data.Levels;

		if (levels != null)
		{
			MaxLevel = levels.MaxLevel;
			BaseLevel = levels.BaseLevel;
			SpellLevel = levels.SpellLevel;
		}

		// SpellPowerEntry
		PowerCosts = data.Powers;

		// SpellReagentsEntry
		var reagents = data.Reagents;

		if (reagents != null)
			for (var i = 0; i < SpellConst.MaxReagents; ++i)
			{
				Reagent[i] = reagents.Reagent[i];
				ReagentCount[i] = reagents.ReagentCount[i];
			}

		ReagentsCurrency = data.ReagentsCurrency;

		// SpellShapeshiftEntry
		var shapeshift = data.Shapeshift;

		if (shapeshift != null)
		{
			Stances = MathFunctions.MakePair64(shapeshift.ShapeshiftMask[0], shapeshift.ShapeshiftMask[1]);
			StancesNot = MathFunctions.MakePair64(shapeshift.ShapeshiftExclude[0], shapeshift.ShapeshiftExclude[1]);
		}

		// SpellTargetRestrictionsEntry
		var target = data.TargetRestrictions;

		if (target != null)
		{
			Targets = (SpellCastTargetFlags)target.Targets;
			ConeAngle = target.ConeDegrees;
			Width = target.Width;
			TargetCreatureType = target.TargetCreatureType;
			MaxAffectedTargets = target.MaxTargets;
			MaxTargetLevel = target.MaxTargetLevel;
		}

		// SpellTotemsEntry
		var totem = data.Totems;

		if (totem != null)
			for (var i = 0; i < 2; ++i)
			{
				TotemCategory[i] = totem.RequiredTotemCategoryID[i];
				Totem[i] = totem.Totem[i];
			}

		_visuals = data.Visuals;

		_spellSpecific = SpellSpecificType.Normal;
		_auraState = AuraStateType.None;

		EmpowerStages = data.EmpowerStages.ToDictionary(a => a.Stage);
	}

	public SpellInfo(SpellNameRecord spellName, Difficulty difficulty, List<SpellEffectRecord> effects)
	{
		Id = spellName.Id;
		Difficulty = difficulty;
		SpellName = spellName.Name;

		foreach (var spellEffect in effects)
		{
			_effects.EnsureWritableListIndex(spellEffect.EffectIndex, new SpellEffectInfo(this));
			_effects[spellEffect.EffectIndex] = new SpellEffectInfo(this, spellEffect);
		}

		// Correct EffectIndex for blank effects
		for (var i = 0; i < _effects.Count; ++i)
			_effects[i].EffectIndex = i;

		NegativeEffects = new HashSet<int>();
	}

	public bool HasEffect(SpellEffectName effect)
	{
		foreach (var effectInfo in _effects)
			if (effectInfo.IsEffect(effect))
				return true;

		return false;
	}

	public bool HasAura(AuraType aura)
	{
		foreach (var effectInfo in _effects)
			if (effectInfo.IsAura(aura))
				return true;

		return false;
	}

	public bool IsAbilityOfSkillType(SkillType skillType)
	{
		var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(Id);

		foreach (var spellIdx in bounds)
			if (spellIdx.SkillLine == (uint)skillType)
				return true;

		return false;
	}

	public bool NeedsToBeTriggeredByCaster(SpellInfo triggeringSpell)
	{
		if (NeedsExplicitUnitTarget)
			return true;

		if (triggeringSpell.IsChanneled)
		{
			SpellCastTargetFlags mask = 0;

			foreach (var effectInfo in _effects)
				if (effectInfo.TargetA.Target != Framework.Constants.Targets.UnitCaster && effectInfo.TargetA.Target != Framework.Constants.Targets.DestCaster && effectInfo.TargetB.Target != Framework.Constants.Targets.UnitCaster && effectInfo.TargetB.Target != Framework.Constants.Targets.DestCaster)
					mask |= effectInfo.ProvidedTargetMask;

			if (mask.HasAnyFlag(SpellCastTargetFlags.UnitMask))
				return true;
		}

		return false;
	}

	public bool IsPositiveEffect(int effIndex)
	{
		return !NegativeEffects.Contains(effIndex);
	}

	public WeaponAttackType GetAttackType()
	{
		WeaponAttackType result;

		switch (DmgClass)
		{
			case SpellDmgClass.Melee:
				if (HasAttribute(SpellAttr3.RequiresOffHandWeapon))
					result = WeaponAttackType.OffAttack;
				else
					result = WeaponAttackType.BaseAttack;

				break;
			case SpellDmgClass.Ranged:
				result = IsRangedWeaponSpell ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack;

				break;
			default:
				// Wands
				if (IsAutoRepeatRangedSpell)
					result = WeaponAttackType.RangedAttack;
				else
					result = WeaponAttackType.BaseAttack;

				break;
		}

		return result;
	}

	public bool IsItemFitToSpellRequirements(Item item)
	{
		// item neutral spell
		if (EquippedItemClass == ItemClass.None)
			return true;

		// item dependent spell
		if (item && item.IsFitToSpellRequirements(this))
			return true;

		return false;
	}

	public bool IsAffected(SpellFamilyNames familyName, FlagArray128 familyFlags)
	{
		if (familyName == 0)
			return true;

		if (familyName != SpellFamilyName)
			return false;

		if (familyFlags && !(familyFlags & SpellFamilyFlags))
			return false;

		return true;
	}

	public bool IsAffectedBySpellMod(SpellModifier mod)
	{
		if (!IsAffectedBySpellMods)
			return false;

		var affectSpell = Global.SpellMgr.GetSpellInfo(mod.SpellId, Difficulty);

		if (affectSpell == null)
			return false;

		switch (mod.Type)
		{
			case SpellModType.Flat:
			case SpellModType.Pct:
				// TEMP: dont use IsAffected - !familyName and !familyFlags are not valid options for spell mods
				// TODO: investigate if the !familyName and !familyFlags conditions are even valid for all other (nonmod) uses of SpellInfo::IsAffected
				return affectSpell.SpellFamilyName == SpellFamilyName && (mod as SpellModifierByClassMask).Mask & SpellFamilyFlags;
			case SpellModType.LabelFlat:
				return HasLabel((uint)(mod as SpellFlatModifierByLabel).Value.LabelID);
			case SpellModType.LabelPct:
				return HasLabel((uint)(mod as SpellPctModifierByLabel).Value.LabelID);
			default:
				break;
		}

		return false;
	}

	public bool CanPierceImmuneAura(SpellInfo auraSpellInfo)
	{
		// aura can't be pierced
		if (auraSpellInfo == null || auraSpellInfo.HasAttribute(SpellAttr0.NoImmunities))
			return false;

		// these spells pierce all avalible spells (Resurrection Sickness for example)
		if (HasAttribute(SpellAttr0.NoImmunities))
			return true;

		// these spells (Cyclone for example) can pierce all...
		if (HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects) || HasAttribute(SpellAttr2.NoSchoolImmunities))
			// ...but not these (Divine shield, Ice block, Cyclone and Banish for example)
			if (auraSpellInfo.Mechanic != Mechanics.ImmuneShield &&
				auraSpellInfo.Mechanic != Mechanics.Invulnerability &&
				(auraSpellInfo.Mechanic != Mechanics.Banish || (IsRankOf(auraSpellInfo) && auraSpellInfo.Dispel != DispelType.None))) // Banish shouldn't be immune to itself, but Cyclone should
				return true;

		// Dispels other auras on immunity, check if this spell makes the unit immune to aura
		if (HasAttribute(SpellAttr1.ImmunityPurgesEffect) && CanSpellProvideImmunityAgainstAura(auraSpellInfo))
			return true;

		return false;
	}

	public bool CanDispelAura(SpellInfo auraSpellInfo)
	{
		// These auras (like Divine Shield) can't be dispelled
		if (auraSpellInfo.HasAttribute(SpellAttr0.NoImmunities))
			return false;

		// These spells (like Mass Dispel) can dispel all auras
		if (HasAttribute(SpellAttr0.NoImmunities))
			return true;

		// These auras (Cyclone for example) are not dispelable
		if ((auraSpellInfo.HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects) && auraSpellInfo.Mechanic != Mechanics.None) || auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
			return false;

		return true;
	}

	public bool IsSingleTarget()
	{
		// all other single target spells have if it has AttributesEx5
		if (HasAttribute(SpellAttr5.LimitN))
			return true;

		return false;
	}

	public bool IsAuraExclusiveBySpecificWith(SpellInfo spellInfo)
	{
		var spellSpec1 = GetSpellSpecific();
		var spellSpec2 = spellInfo.GetSpellSpecific();

		switch (spellSpec1)
		{
			case SpellSpecificType.WarlockArmor:
			case SpellSpecificType.MageArmor:
			case SpellSpecificType.ElementalShield:
			case SpellSpecificType.MagePolymorph:
			case SpellSpecificType.Presence:
			case SpellSpecificType.Charm:
			case SpellSpecificType.Scroll:
			case SpellSpecificType.WarriorEnrage:
			case SpellSpecificType.MageArcaneBrillance:
			case SpellSpecificType.PriestDivineSpirit:
				return spellSpec1 == spellSpec2;
			case SpellSpecificType.Food:
				return spellSpec2 == SpellSpecificType.Food || spellSpec2 == SpellSpecificType.FoodAndDrink;
			case SpellSpecificType.Drink:
				return spellSpec2 == SpellSpecificType.Drink || spellSpec2 == SpellSpecificType.FoodAndDrink;
			case SpellSpecificType.FoodAndDrink:
				return spellSpec2 == SpellSpecificType.Food || spellSpec2 == SpellSpecificType.Drink || spellSpec2 == SpellSpecificType.FoodAndDrink;
			default:
				return false;
		}
	}

	public bool IsAuraExclusiveBySpecificPerCasterWith(SpellInfo spellInfo)
	{
		var spellSpec = GetSpellSpecific();

		switch (spellSpec)
		{
			case SpellSpecificType.Seal:
			case SpellSpecificType.Hand:
			case SpellSpecificType.Aura:
			case SpellSpecificType.Sting:
			case SpellSpecificType.Curse:
			case SpellSpecificType.Bane:
			case SpellSpecificType.Aspect:
			case SpellSpecificType.WarlockCorruption:
				return spellSpec == spellInfo.GetSpellSpecific();
			default:
				return false;
		}
	}

	public SpellCastResult CheckShapeshift(ShapeShiftForm form)
	{
		// talents that learn spells can have stance requirements that need ignore
		// (this requirement only for client-side stance show in talent description)
		/* TODO: 6.x fix this in proper way (probably spell flags/attributes?)
		if (CliDB.GetTalentSpellCost(Id) > 0 && HasEffect(SpellEffects.LearnSpell))
		return SpellCastResult.SpellCastOk;
		*/

		//if (HasAttribute(SPELL_ATTR13_ACTIVATES_REQUIRED_SHAPESHIFT))
		//    return SPELL_CAST_OK;

		var stanceMask = (form != 0 ? 1ul << ((int)form - 1) : 0);

		if (Convert.ToBoolean(stanceMask & StancesNot)) // can explicitly not be casted in this stance
			return SpellCastResult.NotShapeshift;

		if (Convert.ToBoolean(stanceMask & Stances)) // can explicitly be casted in this stance
			return SpellCastResult.SpellCastOk;

		var actAsShifted = false;
		SpellShapeshiftFormRecord shapeInfo = null;

		if (form > 0)
		{
			shapeInfo = CliDB.SpellShapeshiftFormStorage.LookupByKey(form);

			if (shapeInfo == null)
			{
				Log.outError(LogFilter.Spells, "GetErrorAtShapeshiftedCast: unknown shapeshift {0}", form);

				return SpellCastResult.SpellCastOk;
			}

			actAsShifted = !shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance);
		}

		if (actAsShifted)
		{
			if (HasAttribute(SpellAttr0.NotShapeshifted) || (shapeInfo != null && shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.CanOnlyCastShapeshiftSpells))) // not while shapeshifted
				return SpellCastResult.NotShapeshift;
			else if (Stances != 0) // needs other shapeshift
				return SpellCastResult.OnlyShapeshift;
		}
		else
		{
			// needs shapeshift
			if (!HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm) && Stances != 0)
				return SpellCastResult.OnlyShapeshift;
		}

		return SpellCastResult.SpellCastOk;
	}

	public SpellCastResult CheckLocation(uint map_id, uint zone_id, uint area_id, Player player)
	{
		// normal case
		if (RequiredAreasId > 0)
		{
			var found = false;
			var areaGroupMembers = Global.DB2Mgr.GetAreasForGroup((uint)RequiredAreasId);

			foreach (var areaId in areaGroupMembers)
				if (areaId == zone_id || areaId == area_id)
				{
					found = true;

					break;
				}

			if (!found)
				return SpellCastResult.IncorrectArea;
		}

		// continent limitation (virtual continent)
		if (HasAttribute(SpellAttr4.OnlyFlyingAreas))
		{
			uint mountFlags = 0;

			if (player && player.HasAuraType(AuraType.MountRestrictions))
			{
				foreach (var auraEffect in player.GetAuraEffectsByType(AuraType.MountRestrictions))
					mountFlags |= (uint)auraEffect.MiscValue;
			}
			else
			{
				var areaTable = CliDB.AreaTableStorage.LookupByKey(area_id);

				if (areaTable != null)
					mountFlags = areaTable.MountFlags;
			}

			if (!Convert.ToBoolean(mountFlags & (uint)AreaMountFlags.FlyingAllowed))
				return SpellCastResult.IncorrectArea;

			if (player)
			{
				var mapToCheck = map_id;
				var mapEntry1 = CliDB.MapStorage.LookupByKey(map_id);

				if (mapEntry1 != null)
					mapToCheck = (uint)mapEntry1.CosmeticParentMapID;

				if ((mapToCheck == 1116 || mapToCheck == 1464) && !player.HasSpell(191645)) // Draenor Pathfinder
					return SpellCastResult.IncorrectArea;
				else if (mapToCheck == 1220 && !player.HasSpell(233368)) // Broken Isles Pathfinder
					return SpellCastResult.IncorrectArea;
				else if ((mapToCheck == 1642 || mapToCheck == 1643) && !player.HasSpell(278833)) // Battle for Azeroth Pathfinder
					return SpellCastResult.IncorrectArea;
			}
		}

		var mapEntry = CliDB.MapStorage.LookupByKey(map_id);

		// raid instance limitation
		if (HasAttribute(SpellAttr6.NotInRaidInstances))
			if (mapEntry == null || mapEntry.IsRaid())
				return SpellCastResult.NotInRaidInstance;

		// DB base check (if non empty then must fit at least single for allow)
		var saBounds = Global.SpellMgr.GetSpellAreaMapBounds(Id);

		if (!saBounds.Empty())
		{
			foreach (var bound in saBounds)
				if (bound.IsFitToRequirements(player, zone_id, area_id))
					return SpellCastResult.SpellCastOk;

			return SpellCastResult.IncorrectArea;
		}

		// bg spell checks
		switch (Id)
		{
			case 23333: // Warsong Flag
			case 23335: // Silverwing Flag
				return map_id == 489 && player != null && player.InBattleground ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
			case 34976: // Netherstorm Flag
				return map_id == 566 && player != null && player.InBattleground ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
			case 2584:  // Waiting to Resurrect
			case 22011: // Spirit Heal Channel
			case 22012: // Spirit Heal
			case 42792: // Recently Dropped Flag
			case 43681: // Inactive
			case 44535: // Spirit Heal (mana)
				if (mapEntry == null)
					return SpellCastResult.IncorrectArea;

				return zone_id == (uint)AreaId.Wintergrasp || (mapEntry.IsBattleground() && player != null && player.InBattleground) ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
			case 44521: // Preparation
			{
				if (player == null)
					return SpellCastResult.RequiresArea;

				if (mapEntry == null)
					return SpellCastResult.IncorrectArea;

				if (!mapEntry.IsBattleground())
					return SpellCastResult.RequiresArea;

				var bg = player.Battleground;

				return bg && bg.GetStatus() == BattlegroundStatus.WaitJoin ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
			}
			case 32724: // Gold Team (Alliance)
			case 32725: // Green Team (Alliance)
			case 35774: // Gold Team (Horde)
			case 35775: // Green Team (Horde)
				if (mapEntry == null)
					return SpellCastResult.IncorrectArea;

				return mapEntry.IsBattleArena() && player != null && player.InBattleground ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
			case 32727: // Arena Preparation
			{
				if (player == null)
					return SpellCastResult.RequiresArea;

				if (mapEntry == null)
					return SpellCastResult.IncorrectArea;

				if (!mapEntry.IsBattleArena())
					return SpellCastResult.RequiresArea;

				var bg = player.Battleground;

				return bg && bg.GetStatus() == BattlegroundStatus.WaitJoin ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
			}
		}

		// aura limitations
		if (player)
			foreach (var effectInfo in _effects)
			{
				if (!effectInfo.IsAura())
					continue;

				switch (effectInfo.ApplyAuraName)
				{
					case AuraType.ModShapeshift:
					{
						var spellShapeshiftForm = CliDB.SpellShapeshiftFormStorage.LookupByKey(effectInfo.MiscValue);

						if (spellShapeshiftForm != null)
						{
							uint mountType = spellShapeshiftForm.MountTypeID;

							if (mountType != 0)
								if (player.GetMountCapability(mountType) == null)
									return SpellCastResult.NotHere;
						}

						break;
					}
					case AuraType.Mounted:
					{
						var mountType = (uint)effectInfo.MiscValueB;
						var mountEntry = Global.DB2Mgr.GetMount(Id);

						if (mountEntry != null)
							mountType = mountEntry.MountTypeID;

						if (mountType != 0 && player.GetMountCapability(mountType) == null)
							return SpellCastResult.NotHere;

						break;
					}
				}
			}

		return SpellCastResult.SpellCastOk;
	}

	public SpellCastResult CheckTarget(WorldObject caster, WorldObject target, bool Implicit = true)
	{
		if (HasAttribute(SpellAttr1.ExcludeCaster) && caster == target)
			return SpellCastResult.BadTargets;

		// check visibility - ignore stealth for implicit (area) targets
		if (!HasAttribute(SpellAttr6.IgnorePhaseShift) && !caster.CanSeeOrDetect(target, Implicit))
			return SpellCastResult.BadTargets;

		var unitTarget = target.AsUnit;

		// creature/player specific target checks
		if (unitTarget != null)
		{
			// spells cannot be cast if target has a pet in combat either
			if (HasAttribute(SpellAttr1.OnlyPeacefulTargets) && (unitTarget.IsInCombat || unitTarget.HasUnitFlag(UnitFlags.PetInCombat)))
				return SpellCastResult.TargetAffectingCombat;

			// only spells with SPELL_ATTR3_ONLY_TARGET_GHOSTS can target ghosts
			if (HasAttribute(SpellAttr3.OnlyOnGhosts) != unitTarget.HasAuraType(AuraType.Ghost))
			{
				if (HasAttribute(SpellAttr3.OnlyOnGhosts))
					return SpellCastResult.TargetNotGhost;
				else
					return SpellCastResult.BadTargets;
			}

			if (caster != unitTarget)
				if (caster.IsTypeId(TypeId.Player))
				{
					// Do not allow these spells to target creatures not tapped by us (Banish, Polymorph, many quest spells)
					if (HasAttribute(SpellAttr2.CannotCastOnTapped))
					{
						var targetCreature = unitTarget.AsCreature;

						if (targetCreature != null)
							if (targetCreature.HasLootRecipient && !targetCreature.IsTappedBy(caster.AsPlayer))
								return SpellCastResult.CantCastOnTapped;
					}

					if (HasAttribute(SpellCustomAttributes.PickPocket))
					{
						var targetCreature = unitTarget.AsCreature;

						if (targetCreature == null)
							return SpellCastResult.BadTargets;

						if (!targetCreature.CanHaveLoot || !Loots.LootStorage.Pickpocketing.HaveLootFor(targetCreature.Template.PickPocketId))
							return SpellCastResult.TargetNoPockets;
					}

					// Not allow disarm unarmed player
					if (Mechanic == Mechanics.Disarm)
					{
						if (unitTarget.IsTypeId(TypeId.Player))
						{
							var player = unitTarget.AsPlayer;

							if (player.GetWeaponForAttack(WeaponAttackType.BaseAttack) == null || !player.IsUseEquipedWeapon(true))
								return SpellCastResult.TargetNoWeapons;
						}
						else if (unitTarget.GetVirtualItemId(0) == 0)
						{
							return SpellCastResult.TargetNoWeapons;
						}
					}
				}
		}
		// corpse specific target checks
		else if (target.IsTypeId(TypeId.Corpse))
		{
			var corpseTarget = target.AsCorpse;

			// cannot target bare bones
			if (corpseTarget.GetCorpseType() == CorpseType.Bones)
				return SpellCastResult.BadTargets;

			// we have to use owner for some checks (aura preventing resurrection for example)
			var owner = Global.ObjAccessor.FindPlayer(corpseTarget.OwnerGUID);

			if (owner != null)
				unitTarget = owner;
			// we're not interested in corpses without owner
			else
				return SpellCastResult.BadTargets;
		}
		// other types of objects - always valid
		else
		{
			return SpellCastResult.SpellCastOk;
		}

		// corpseOwner and unit specific target checks
		if (!unitTarget.IsPlayer)
		{
			if (HasAttribute(SpellAttr3.OnlyOnPlayer))
				return SpellCastResult.TargetNotPlayer;

			if (HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && unitTarget.IsControlledByPlayer)
				return SpellCastResult.TargetIsPlayerControlled;
		}
		else if (HasAttribute(SpellAttr5.NotOnPlayer))
		{
			return SpellCastResult.TargetIsPlayer;
		}

		if (!IsAllowingDeadTarget && !unitTarget.IsAlive)
			return SpellCastResult.TargetsDead;

		// check this flag only for implicit targets (chain and area), allow to explicitly target units for spells like Shield of Righteousness
		if (Implicit && HasAttribute(SpellAttr6.DoNotChainToCrowdControlledTargets) && !unitTarget.CanFreeMove())
			return SpellCastResult.BadTargets;

		if (!CheckTargetCreatureType(unitTarget))
		{
			if (target.IsTypeId(TypeId.Player))
				return SpellCastResult.TargetIsPlayer;
			else
				return SpellCastResult.BadTargets;
		}

		// check GM mode and GM invisibility - only for player casts (npc casts are controlled by AI) and negative spells
		if (unitTarget != caster && (caster.AffectingPlayer != null || !IsPositive) && unitTarget.IsTypeId(TypeId.Player))
		{
			if (!unitTarget.AsPlayer.IsVisible())
				return SpellCastResult.BmOrInvisgod;

			if (unitTarget.AsPlayer.IsGameMaster)
				return SpellCastResult.BmOrInvisgod;
		}

		// not allow casting on flying player
		if (unitTarget.HasUnitState(UnitState.InFlight) && !HasAttribute(SpellCustomAttributes.AllowInflightTarget))
			return SpellCastResult.BadTargets;

		/* TARGET_UNIT_MASTER gets blocked here for passengers, because the whole idea of this check is to
		not allow passengers to be implicitly hit by spells, however this target type should be an exception,
		if this is left it kills spells that award kill credit from vehicle to master (few spells),
		the use of these 2 covers passenger target check, logically, if vehicle cast this to master it should always hit
		him, because it would be it's passenger, there's no such case where this gets to fail legitimacy, this problem
		cannot be solved from within the check in other way since target type cannot be called for the spell currently
		Spell examples: [ID - 52864 Devour Water, ID - 52862 Devour Wind, ID - 49370 Wyrmrest Defender: Destabilize Azure Dragonshrine Effect] */
		var unitCaster = caster.AsUnit;

		if (unitCaster != null)
			if (!unitCaster.IsVehicle && unitCaster.CharmerOrOwner != target)
			{
				if (TargetAuraState != 0 && !unitTarget.HasAuraState(TargetAuraState, this, unitCaster))
					return SpellCastResult.TargetAurastate;

				if (ExcludeTargetAuraState != 0 && unitTarget.HasAuraState(ExcludeTargetAuraState, this, unitCaster))
					return SpellCastResult.TargetAurastate;
			}

		if (TargetAuraSpell != 0 && !unitTarget.HasAura(TargetAuraSpell))
			return SpellCastResult.TargetAurastate;

		if (ExcludeTargetAuraSpell != 0 && unitTarget.HasAura(ExcludeTargetAuraSpell))
			return SpellCastResult.TargetAurastate;

		if (unitTarget.HasAuraType(AuraType.PreventResurrection) && !HasAttribute(SpellAttr7.BypassNoResurrectAura))
			if (HasEffect(SpellEffectName.SelfResurrect) || HasEffect(SpellEffectName.Resurrect))
				return SpellCastResult.TargetCannotBeResurrected;

		if (HasAttribute(SpellAttr8.BattleResurrection))
		{
			var map = caster.Map;

			if (map)
			{
				var iMap = map.ToInstanceMap;

				if (iMap)
				{
					var instance = iMap.InstanceScript;

					if (instance != null)
						if (instance.GetCombatResurrectionCharges() == 0 && instance.IsEncounterInProgress())
							return SpellCastResult.TargetCannotBeResurrected;
				}
			}
		}

		return SpellCastResult.SpellCastOk;
	}

	public SpellCastResult CheckExplicitTarget(WorldObject caster, WorldObject target, Item itemTarget = null)
	{
		var neededTargets = GetExplicitTargetMask();

		if (target == null)
		{
			if (Convert.ToBoolean(neededTargets & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.GameobjectMask | SpellCastTargetFlags.CorpseMask)))
				if (!Convert.ToBoolean(neededTargets & SpellCastTargetFlags.GameobjectItem) || itemTarget == null)
					return SpellCastResult.BadTargets;

			return SpellCastResult.SpellCastOk;
		}

		var unitTarget = target.AsUnit;

		if (unitTarget != null)
			if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.UnitAlly | SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitMinipet | SpellCastTargetFlags.UnitPassenger))
			{
				var unitCaster = caster.AsUnit;

				if (neededTargets.HasFlag(SpellCastTargetFlags.UnitEnemy))
					if (caster.IsValidAttackTarget(unitTarget, this))
						return SpellCastResult.SpellCastOk;

				if (neededTargets.HasFlag(SpellCastTargetFlags.UnitAlly) || (neededTargets.HasFlag(SpellCastTargetFlags.UnitParty) && unitCaster != null && unitCaster.IsInPartyWith(unitTarget)) || (neededTargets.HasFlag(SpellCastTargetFlags.UnitRaid) && unitCaster != null && unitCaster.IsInRaidWith(unitTarget)))
					if (caster.IsValidAssistTarget(unitTarget, this))
						return SpellCastResult.SpellCastOk;

				if (neededTargets.HasFlag(SpellCastTargetFlags.UnitMinipet) && unitCaster != null)
					if (unitTarget.GUID == unitCaster.CritterGUID)
						return SpellCastResult.SpellCastOk;

				if (neededTargets.HasFlag(SpellCastTargetFlags.UnitPassenger) && unitCaster != null)
					if (unitTarget.IsOnVehicle(unitCaster))
						return SpellCastResult.SpellCastOk;

				return SpellCastResult.BadTargets;
			}

		return SpellCastResult.SpellCastOk;
	}

	public SpellCastResult CheckVehicle(Unit caster)
	{
		// All creatures should be able to cast as passengers freely, restriction and attribute are only for players
		if (!caster.IsTypeId(TypeId.Player))
			return SpellCastResult.SpellCastOk;

		var vehicle = caster.Vehicle1;

		if (vehicle)
		{
			VehicleSeatFlags checkMask = 0;

			foreach (var effectInfo in _effects)
				if (effectInfo.IsAura(AuraType.ModShapeshift))
				{
					var shapeShiftFromEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)effectInfo.MiscValue);

					if (shapeShiftFromEntry != null && !shapeShiftFromEntry.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance))
						checkMask |= VehicleSeatFlags.Uncontrolled;

					break;
				}

			if (HasAura(AuraType.Mounted))
				checkMask |= VehicleSeatFlags.CanCastMountSpell;

			if (checkMask == 0)
				checkMask = VehicleSeatFlags.CanAttack;

			var vehicleSeat = vehicle.GetSeatForPassenger(caster);

			if (!HasAttribute(SpellAttr6.AllowWhileRidingVehicle) && !HasAttribute(SpellAttr0.AllowWhileMounted) && (vehicleSeat.Flags & (int)checkMask) != (int)checkMask)
				return SpellCastResult.CantDoThatRightNow;

			// Can only summon uncontrolled minions/guardians when on controlled vehicle
			if (vehicleSeat.HasFlag(VehicleSeatFlags.CanControl | VehicleSeatFlags.Unk2))
				foreach (var effectInfo in _effects)
				{
					if (!effectInfo.IsEffect(SpellEffectName.Summon))
						continue;

					var props = CliDB.SummonPropertiesStorage.LookupByKey(effectInfo.MiscValueB);

					if (props != null && props.Control != SummonCategory.Wild)
						return SpellCastResult.CantDoThatRightNow;
				}
		}

		return SpellCastResult.SpellCastOk;
	}

	public bool CheckTargetCreatureType(Unit target)
	{
		// Curse of Doom & Exorcism: not find another way to fix spell target check :/
		if (SpellFamilyName == SpellFamilyNames.Warlock && Category == 1179)
		{
			// not allow cast at player
			if (target.IsTypeId(TypeId.Player))
				return false;
			else
				return true;
		}

		// if target is magnet (i.e Grounding Totem) the check is skipped
		if (target.IsMagnet)
			return true;


		var creatureType = target.CreatureTypeMask;

		return TargetCreatureType == 0 || creatureType == 0 || Convert.ToBoolean(creatureType & TargetCreatureType);
	}

	public SpellSchoolMask GetSchoolMask()
	{
		return SchoolMask;
	}

	public ulong GetAllEffectsMechanicMask()
	{
		ulong mask = 0;

		if (Mechanic != 0)
			mask |= 1ul << (int)Mechanic;

		foreach (var effectInfo in _effects)
			if (effectInfo.IsEffect() && effectInfo.Mechanic != 0)
				mask |= 1ul << (int)effectInfo.Mechanic;

		return mask;
	}

	public ulong GetEffectMechanicMask(int effIndex)
	{
		ulong mask = 0;

		if (Mechanic != 0)
			mask |= 1ul << (int)Mechanic;

		if (GetEffect(effIndex).IsEffect() && GetEffect(effIndex).Mechanic != 0)
			mask |= 1ul << (int)GetEffect(effIndex).Mechanic;

		return mask;
	}

	public ulong GetSpellMechanicMaskByEffectMask(HashSet<int> effectMask)
	{
		ulong mask = 0;

		if (Mechanic != 0)
			mask |= 1ul << (int)Mechanic;

		foreach (var effectInfo in _effects)
			if (effectMask.Contains(effectInfo.EffectIndex) && effectInfo.Mechanic != 0)
				mask |= 1ul << (int)effectInfo.Mechanic;

		return mask;
	}

	public Mechanics GetEffectMechanic(int effIndex)
	{
		if (GetEffect(effIndex).IsEffect() && GetEffect(effIndex).Mechanic != 0)
			return GetEffect(effIndex).Mechanic;

		if (Mechanic != 0)
			return Mechanic;

		return Mechanics.None;
	}

	public uint GetDispelMask()
	{
		return GetDispelMask(Dispel);
	}

	public static uint GetDispelMask(DispelType type)
	{
		// If dispel all
		if (type == DispelType.ALL)
			return (uint)DispelType.AllMask;
		else
			return (uint)(1 << (int)type);
	}

	public SpellCastTargetFlags GetExplicitTargetMask()
	{
		return ExplicitTargetMask;
	}

	public AuraStateType GetAuraState()
	{
		return _auraState;
	}

	public void _LoadAuraState()
	{
		_auraState = AuraStateType.None;

		// Faerie Fire
		if (Category == 1133)
			_auraState = AuraStateType.FaerieFire;

		// Swiftmend state on Regrowth, Rejuvenation, Wild Growth
		if (SpellFamilyName == SpellFamilyNames.Druid && (SpellFamilyFlags[0].HasAnyFlag(0x50u) || SpellFamilyFlags[1].HasAnyFlag(0x4000000u)))
			_auraState = AuraStateType.DruidPeriodicHeal;

		// Deadly poison aura state
		if (SpellFamilyName == SpellFamilyNames.Rogue && SpellFamilyFlags[0].HasAnyFlag(0x10000u))
			_auraState = AuraStateType.RoguePoisoned;

		// Enrage aura state
		if (Dispel == DispelType.Enrage)
			_auraState = AuraStateType.Enraged;

		// Bleeding aura state
		if (Convert.ToBoolean(GetAllEffectsMechanicMask() & (1 << (int)Mechanics.Bleed)))
			_auraState = AuraStateType.Bleed;

		if (Convert.ToBoolean(GetSchoolMask() & SpellSchoolMask.Frost))
			foreach (var effectInfo in _effects)
				if (effectInfo.IsAura(AuraType.ModStun) || effectInfo.IsAura(AuraType.ModRoot) || effectInfo.IsAura(AuraType.ModRoot2))
					_auraState = AuraStateType.Frozen;

		switch (Id)
		{
			case 1064: // Dazed
				_auraState = AuraStateType.Dazed;

				break;
			case 32216: // Victorious
				_auraState = AuraStateType.Victorious;

				break;
			case 71465: // Divine Surge
			case 50241: // Evasive Charges
				_auraState = AuraStateType.RaidEncounter;

				break;
			case 6950:   // Faerie Fire
			case 9806:   // Phantom Strike
			case 9991:   // Touch of Zanzil
			case 13424:  // Faerie Fire
			case 13752:  // Faerie Fire
			case 16432:  // Plague Mist
			case 20656:  // Faerie Fire
			case 25602:  // Faerie Fire
			case 32129:  // Faerie Fire
			case 35325:  // Glowing Blood
			case 35328:  // Lambent Blood
			case 35329:  // Vibrant Blood
			case 35331:  // Black Blood
			case 49163:  // Perpetual Instability
			case 65863:  // Faerie Fire
			case 79559:  // Luxscale Light
			case 82855:  // Dazzling
			case 102953: // In the Rumpus
			case 127907: // Phosphorescence
			case 127913: // Phosphorescence
			case 129007: // Zijin Sting
			case 130159: // Fae Touch
			case 142537: // Spotter Smoke
			case 168455: // Spotted!
			case 176905: // Super Sticky Glitter Bomb
			case 189502: // Marked
			case 201785: // Intruder Alert!
			case 201786: // Intruder Alert!
			case 201935: // Spotted!
			case 239233: // Smoke Bomb
			case 319400: // Glitter Burst
			case 321470: // Dimensional Shifter Mishap
			case 331134: // Spotted
				_auraState = AuraStateType.FaerieFire;

				break;
			default:
				break;
		}
	}

	public SpellSpecificType GetSpellSpecific()
	{
		return _spellSpecific;
	}

	public void _LoadSpellSpecific()
	{
		_spellSpecific = SpellSpecificType.Normal;

		switch (SpellFamilyName)
		{
			case SpellFamilyNames.Generic:
			{
				// Food / Drinks (mostly)
				if (HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing))
				{
					var food = false;
					var drink = false;

					foreach (var effectInfo in _effects)
					{
						if (!effectInfo.IsAura())
							continue;

						switch (effectInfo.ApplyAuraName)
						{
							// Food
							case AuraType.ModRegen:
							case AuraType.ObsModHealth:
								food = true;

								break;
							// Drink
							case AuraType.ModPowerRegen:
							case AuraType.ObsModPower:
								drink = true;

								break;
							default:
								break;
						}
					}

					if (food && drink)
						_spellSpecific = SpellSpecificType.FoodAndDrink;
					else if (food)
						_spellSpecific = SpellSpecificType.Food;
					else if (drink)
						_spellSpecific = SpellSpecificType.Drink;
				}
				// scrolls effects
				else
				{
					var firstRankSpellInfo = GetFirstRankSpell();

					switch (firstRankSpellInfo.Id)
					{
						case 8118: // Strength
						case 8099: // Stamina
						case 8112: // Spirit
						case 8096: // Intellect
						case 8115: // Agility
						case 8091: // Armor
							_spellSpecific = SpellSpecificType.Scroll;

							break;
					}
				}

				break;
			}
			case SpellFamilyNames.Mage:
			{
				// family flags 18(Molten), 25(Frost/Ice), 28(Mage)
				if (SpellFamilyFlags[0].HasAnyFlag(0x12040000u))
					_spellSpecific = SpellSpecificType.MageArmor;

				// Arcane brillance and Arcane intelect (normal check fails because of flags difference)
				if (SpellFamilyFlags[0].HasAnyFlag(0x400u))
					_spellSpecific = SpellSpecificType.MageArcaneBrillance;

				if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u) && GetEffect(0).IsAura(AuraType.ModConfuse))
					_spellSpecific = SpellSpecificType.MagePolymorph;

				break;
			}
			case SpellFamilyNames.Warrior:
			{
				if (Id == 12292) // Death Wish
					_spellSpecific = SpellSpecificType.WarriorEnrage;

				break;
			}
			case SpellFamilyNames.Warlock:
			{
				// Warlock (Bane of Doom | Bane of Agony | Bane of Havoc)
				if (Id == 603 || Id == 980 || Id == 80240)
					_spellSpecific = SpellSpecificType.Bane;

				// only warlock curses have this
				if (Dispel == DispelType.Curse)
					_spellSpecific = SpellSpecificType.Curse;

				// Warlock (Demon Armor | Demon Skin | Fel Armor)
				if (SpellFamilyFlags[1].HasAnyFlag(0x20000020u) || SpellFamilyFlags[2].HasAnyFlag(0x00000010u))
					_spellSpecific = SpellSpecificType.WarlockArmor;

				//seed of corruption and corruption
				if (SpellFamilyFlags[1].HasAnyFlag(0x10u) || SpellFamilyFlags[0].HasAnyFlag(0x2u))
					_spellSpecific = SpellSpecificType.WarlockCorruption;

				break;
			}
			case SpellFamilyNames.Priest:
			{
				// Divine Spirit and Prayer of Spirit
				if (SpellFamilyFlags[0].HasAnyFlag(0x20u))
					_spellSpecific = SpellSpecificType.PriestDivineSpirit;

				break;
			}
			case SpellFamilyNames.Hunter:
			{
				// only hunter stings have this
				if (Dispel == DispelType.Poison)
					_spellSpecific = SpellSpecificType.Sting;

				// only hunter aspects have this (but not all aspects in hunter family)
				if (SpellFamilyFlags & new FlagArray128(0x00200000, 0x00000000, 0x00001010, 0x00000000))
					_spellSpecific = SpellSpecificType.Aspect;

				break;
			}
			case SpellFamilyNames.Paladin:
			{
				// Collection of all the seal family flags. No other paladin spell has any of those.
				if (SpellFamilyFlags[1].HasAnyFlag(0xA2000800))
					_spellSpecific = SpellSpecificType.Seal;

				if (SpellFamilyFlags[0].HasAnyFlag(0x00002190u))
					_spellSpecific = SpellSpecificType.Hand;

				// only paladin auras have this (for palaldin class family)
				switch (Id)
				{
					case 465:    // Devotion Aura
					case 32223:  // Crusader Aura
					case 183435: // Retribution Aura
					case 317920: // Concentration Aura
						_spellSpecific = SpellSpecificType.Aura;

						break;
					default:
						break;
				}

				break;
			}
			case SpellFamilyNames.Shaman:
			{
				// family flags 10 (Lightning), 42 (Earth), 37 (Water), proc shield from T2 8 pieces bonus
				if (SpellFamilyFlags[1].HasAnyFlag(0x420u) || SpellFamilyFlags[0].HasAnyFlag(0x00000400u) || Id == 23552)
					_spellSpecific = SpellSpecificType.ElementalShield;

				break;
			}
			case SpellFamilyNames.Deathknight:
				if (Id == 48266 || Id == 48263 || Id == 48265)
					_spellSpecific = SpellSpecificType.Presence;

				break;
		}

		foreach (var effectInfo in _effects)
			if (effectInfo.IsEffect(SpellEffectName.ApplyAura))
				switch (effectInfo.ApplyAuraName)
				{
					case AuraType.ModCharm:
					case AuraType.ModPossessPet:
					case AuraType.ModPossess:
					case AuraType.AoeCharm:
						_spellSpecific = SpellSpecificType.Charm;

						break;
					case AuraType.TrackCreatures:
						// @workaround For non-stacking tracking spells (We need generic solution)
						if (Id == 30645) // Gas Cloud Tracking
							_spellSpecific = SpellSpecificType.Normal;

						break;
					case AuraType.TrackResources:
					case AuraType.TrackStealthed:
						_spellSpecific = SpellSpecificType.Tracker;

						break;
				}
	}

	public void _LoadSpellDiminishInfo()
	{
		SpellDiminishInfo diminishInfo = new();
		diminishInfo.DiminishGroup = DiminishingGroupCompute();
		diminishInfo.DiminishReturnType = DiminishingTypeCompute(diminishInfo.DiminishGroup);
		diminishInfo.DiminishMaxLevel = DiminishingMaxLevelCompute(diminishInfo.DiminishGroup);
		diminishInfo.DiminishDurationLimit = DiminishingLimitDurationCompute();

		_diminishInfo = diminishInfo;
	}

	public void _LoadImmunityInfo()
	{
		foreach (var effect in _effects)
		{
			uint schoolImmunityMask = 0;
			uint applyHarmfulAuraImmunityMask = 0;
			ulong mechanicImmunityMask = 0;
			uint dispelImmunity = 0;
			uint damageImmunityMask = 0;

			var miscVal = effect.MiscValue;
			var amount = effect.CalcValue();

			var immuneInfo = effect.ImmunityInfo;

			switch (effect.ApplyAuraName)
			{
				case AuraType.MechanicImmunityMask:
				{
					switch (miscVal)
					{
						case 96: // Free Friend, Uncontrollable Frenzy, Warlord's Presence
						{
							mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

							immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
							immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
							immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
							immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
							immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
							immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);

							break;
						}
						case 1615: // Incite Rage, Wolf Spirit, Overload, Lightning Tendrils
						{
							switch (Id)
							{
								case 43292: // Incite Rage
								case 49172: // Wolf Spirit
									mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

									immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
									immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
									immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
									immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
									immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
									immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
									goto case 61869;
								// no break intended
								case 61869: // Overload
								case 63481:
								case 61887: // Lightning Tendrils
								case 63486:
									mechanicImmunityMask |= (1 << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence);

									immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBack);
									immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBackDest);

									break;
								default:
									break;
							}

							break;
						}
						case 679: // Mind Control, Avenging Fury
						{
							if (Id == 57742) // Avenging Fury
							{
								mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

								immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
							}

							break;
						}
						case 1557: // Startling Roar, Warlord Roar, Break Bonds, Stormshield
						{
							if (Id == 64187) // Stormshield
							{
								mechanicImmunityMask |= 1 << (int)Mechanics.Stun;
								immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
							}
							else
							{
								mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

								immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
							}

							break;
						}
						case 1614: // Fixate
						case 1694: // Fixated, Lightning Tendrils
						{
							immuneInfo.SpellEffectImmune.Add(SpellEffectName.AttackMe);
							immuneInfo.AuraTypeImmune.Add(AuraType.ModTaunt);

							break;
						}
						case 1630: // Fervor, Berserk
						{
							if (Id == 64112) // Berserk
							{
								immuneInfo.SpellEffectImmune.Add(SpellEffectName.AttackMe);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModTaunt);
							}
							else
							{
								mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

								immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
							}

							break;
						}
						case 477:  // Bladestorm
						case 1733: // Bladestorm, Killing Spree
						{
							if (amount == 0)
							{
								mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

								immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBack);
								immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBackDest);

								immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
							}

							break;
						}
						case 878: // Whirlwind, Fog of Corruption, Determination
						{
							if (Id == 66092) // Determination
							{
								mechanicImmunityMask |= (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Stun) | (1 << (int)Mechanics.Disoriented) | (1 << (int)Mechanics.Freeze);

								immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
								immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
							}

							break;
						}
						default:
							break;
					}

					if (immuneInfo.AuraTypeImmune.Empty())
					{
						if (miscVal.HasAnyFlag(1 << 10))
							immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);

						if (miscVal.HasAnyFlag(1 << 1))
							immuneInfo.AuraTypeImmune.Add(AuraType.Transform);

						// These flag can be recognized wrong:
						if (miscVal.HasAnyFlag(1 << 6))
							immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);

						if (miscVal.HasAnyFlag(1 << 0))
						{
							immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
							immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
						}

						if (miscVal.HasAnyFlag(1 << 2))
							immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);

						if (miscVal.HasAnyFlag(1 << 9))
							immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);

						if (miscVal.HasAnyFlag(1 << 7))
							immuneInfo.AuraTypeImmune.Add(AuraType.ModDisarm);
					}

					break;
				}
				case AuraType.MechanicImmunity:
				{
					switch (Id)
					{
						case 42292: // PvP trinket
						case 59752: // Every Man for Himself
							mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;
							immuneInfo.AuraTypeImmune.Add(AuraType.UseNormalMovementSpeed);

							break;
						case 34471:  // The Beast Within
						case 19574:  // Bestial Wrath
						case 46227:  // Medallion of Immunity
						case 53490:  // Bullheaded
						case 65547:  // PvP Trinket
						case 134946: // Supremacy of the Alliance
						case 134956: // Supremacy of the Horde
						case 195710: // Honorable Medallion
						case 208683: // Gladiator's Medallion
							mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

							break;
						case 54508: // Demonic Empowerment
							mechanicImmunityMask |= (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Stun);

							break;
						default:
							if (miscVal < 1)
								break;

							mechanicImmunityMask |= 1ul << miscVal;

							break;
					}

					break;
				}
				case AuraType.EffectImmunity:
				{
					immuneInfo.SpellEffectImmune.Add((SpellEffectName)miscVal);

					break;
				}
				case AuraType.StateImmunity:
				{
					immuneInfo.AuraTypeImmune.Add((AuraType)miscVal);

					break;
				}
				case AuraType.SchoolImmunity:
				{
					schoolImmunityMask |= (uint)miscVal;

					break;
				}
				case AuraType.ModImmuneAuraApplySchool:
				{
					applyHarmfulAuraImmunityMask |= (uint)miscVal;

					break;
				}
				case AuraType.DamageImmunity:
				{
					damageImmunityMask |= (uint)miscVal;

					break;
				}
				case AuraType.DispelImmunity:
				{
					dispelImmunity = (uint)miscVal;

					break;
				}
				default:
					break;
			}

			immuneInfo.SchoolImmuneMask = schoolImmunityMask;
			immuneInfo.ApplyHarmfulAuraImmuneMask = applyHarmfulAuraImmunityMask;
			immuneInfo.MechanicImmuneMask = mechanicImmunityMask;
			immuneInfo.DispelImmune = dispelImmunity;
			immuneInfo.DamageSchoolMask = damageImmunityMask;

			_allowedMechanicMask |= immuneInfo.MechanicImmuneMask;
		}

		if (HasAttribute(SpellAttr5.AllowWhileStunned))
			switch (Id)
			{
				case 22812: // Barkskin
				case 47585: // Dispersion
					_allowedMechanicMask |=
						(1 << (int)Mechanics.Stun) |
						(1 << (int)Mechanics.Freeze) |
						(1 << (int)Mechanics.Knockout) |
						(1 << (int)Mechanics.Sleep);

					break;
				case 49039: // Lichborne, don't allow normal stuns
					break;
				default:
					_allowedMechanicMask |= (1 << (int)Mechanics.Stun);

					break;
			}

		if (HasAttribute(SpellAttr5.AllowWhileConfused))
			_allowedMechanicMask |= (1 << (int)Mechanics.Disoriented);

		if (HasAttribute(SpellAttr5.AllowWhileFleeing))
			switch (Id)
			{
				case 22812: // Barkskin
				case 47585: // Dispersion
					_allowedMechanicMask |= (1 << (int)Mechanics.Fear) | (1 << (int)Mechanics.Horror);

					break;
				default:
					_allowedMechanicMask |= (1 << (int)Mechanics.Fear);

					break;
			}
	}

	public void ApplyAllSpellImmunitiesTo(Unit target, SpellEffectInfo spellEffectInfo, bool apply)
	{
		var immuneInfo = spellEffectInfo.ImmunityInfo;

		var schoolImmunity = immuneInfo.SchoolImmuneMask;

		if (schoolImmunity != 0)
		{
			target.ApplySpellImmune(Id, SpellImmunity.School, schoolImmunity, apply);

			if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
				target.RemoveAppliedAuras(aurApp =>
				{
					var auraSpellInfo = aurApp.Base.SpellInfo;

					return (((uint)auraSpellInfo.GetSchoolMask() & schoolImmunity) != 0 && // Check for school mask
							CanDispelAura(auraSpellInfo) &&
							(IsPositive != aurApp.IsPositive) && // Check spell vs aura possitivity
							!auraSpellInfo.IsPassive &&          // Don't remove passive auras
							auraSpellInfo.Id != Id);             // Don't remove self
				});

			if (apply && (schoolImmunity & (uint)SpellSchoolMask.Normal) != 0)
				target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.InvulnerabilityBuff);
		}

		var mechanicImmunity = immuneInfo.MechanicImmuneMask;

		if (mechanicImmunity != 0)
		{
			for (uint i = 0; i < (int)Mechanics.Max; ++i)
				if (Convert.ToBoolean(mechanicImmunity & (1ul << (int)i)))
					target.ApplySpellImmune(Id, SpellImmunity.Mechanic, i, apply);

			if (HasAttribute(SpellAttr1.ImmunityPurgesEffect))
			{
				// exception for purely snare mechanic (eg. hands of freedom)!
				if (apply)
				{
					target.RemoveAurasWithMechanic(mechanicImmunity, AuraRemoveMode.Default, Id);
				}
				else
				{
					List<Aura> aurasToUpdateTargets = new();

					target.RemoveAppliedAuras(aurApp =>
					{
						var aura = aurApp.Base;

						if ((aura.SpellInfo.GetAllEffectsMechanicMask() & mechanicImmunity) != 0)
							aurasToUpdateTargets.Add(aura);

						// only update targets, don't remove anything
						return false;
					});

					foreach (var aura in aurasToUpdateTargets)
						aura.UpdateTargetMap(aura.Caster);
				}
			}
		}

		var dispelImmunity = immuneInfo.DispelImmune;

		if (dispelImmunity != 0)
		{
			target.ApplySpellImmune(Id, SpellImmunity.Dispel, dispelImmunity, apply);

			if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
				target.RemoveAppliedAuras(aurApp =>
				{
					var spellInfo = aurApp.Base.SpellInfo;

					if ((uint)spellInfo.Dispel == dispelImmunity)
						return true;

					return false;
				});
		}

		var damageImmunity = immuneInfo.DamageSchoolMask;

		if (damageImmunity != 0)
		{
			target.ApplySpellImmune(Id, SpellImmunity.Damage, damageImmunity, apply);

			if (apply && (damageImmunity & (uint)SpellSchoolMask.Normal) != 0)
				target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.InvulnerabilityBuff);
		}

		foreach (var auraType in immuneInfo.AuraTypeImmune)
		{
			target.ApplySpellImmune(Id, SpellImmunity.State, auraType, apply);

			if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
				target.RemoveAurasByType(auraType,
										aurApp =>
										{
											// if the aura has SPELL_ATTR0_NO_IMMUNITIES, then it cannot be removed by immunity
											return !aurApp.Base.SpellInfo.HasAttribute(SpellAttr0.NoImmunities);
										});
		}

		foreach (var effectType in immuneInfo.SpellEffectImmune)
			target.ApplySpellImmune(Id, SpellImmunity.Effect, effectType, apply);
	}

	public bool SpellCancelsAuraEffect(AuraEffect aurEff)
	{
		if (!HasAttribute(SpellAttr1.ImmunityPurgesEffect))
			return false;

		if (aurEff.SpellInfo.HasAttribute(SpellAttr0.NoImmunities))
			return false;

		foreach (var effectInfo in Effects)
		{
			if (!effectInfo.IsEffect(SpellEffectName.ApplyAura))
				continue;

			var miscValue = (uint)effectInfo.MiscValue;

			switch (effectInfo.ApplyAuraName)
			{
				case AuraType.StateImmunity:
					if (miscValue != (uint)aurEff.AuraType)
						continue;

					break;
				case AuraType.SchoolImmunity:
				case AuraType.ModImmuneAuraApplySchool:
					if (aurEff.SpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities) || !Convert.ToBoolean((uint)aurEff.SpellInfo.SchoolMask & miscValue))
						continue;

					break;
				case AuraType.DispelImmunity:
					if (miscValue != (uint)aurEff.SpellInfo.Dispel)
						continue;

					break;
				case AuraType.MechanicImmunity:
					if (miscValue != (uint)aurEff.SpellInfo.Mechanic)
						if (miscValue != (uint)aurEff.GetSpellEffectInfo().Mechanic)
							continue;

					break;
				default:
					continue;
			}

			return true;
		}

		return false;
	}

	public ulong GetMechanicImmunityMask(Unit caster)
	{
		var casterMechanicImmunityMask = caster.MechanicImmunityMask;
		ulong mechanicImmunityMask = 0;

		if (CanBeInterrupted(null, caster, true))
		{
			if ((casterMechanicImmunityMask & (1 << (int)Mechanics.Silence)) != 0)
				mechanicImmunityMask |= (1 << (int)Mechanics.Silence);

			if ((casterMechanicImmunityMask & (1 << (int)Mechanics.Interrupt)) != 0)
				mechanicImmunityMask |= (1 << (int)Mechanics.Interrupt);
		}

		return mechanicImmunityMask;
	}

	public float GetMinRange(bool positive = false)
	{
		if (RangeEntry == null)
			return 0.0f;

		return RangeEntry.RangeMin[positive ? 1 : 0];
	}

	public float GetMaxRange(bool positive = false, WorldObject caster = null, Spell spell = null)
	{
		if (RangeEntry == null)
			return 0.0f;

		var range = RangeEntry.RangeMax[positive ? 1 : 0];

		if (caster != null)
		{
			var modOwner = caster.SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(this, SpellModOp.Range, ref range, spell);
		}

		return range;
	}

	public int CalcDuration(WorldObject caster = null)
	{
		var duration = Duration;

		if (caster)
		{
			var modOwner = caster.SpellModOwner;

			if (modOwner)
				modOwner.ApplySpellMod(this, SpellModOp.Duration, ref duration);
		}

		return duration;
	}

	public int CalcCastTime(Spell spell = null)
	{
		var castTime = 0;

		if (CastTimeEntry != null)
			castTime = Math.Max(CastTimeEntry.Base, CastTimeEntry.Minimum);

		if (castTime <= 0)
			return 0;

		if (spell != null)
			spell.Caster.ModSpellCastTime(this, ref castTime, spell);

		if (HasAttribute(SpellAttr0.UsesRangedSlot) && (!IsAutoRepeatRangedSpell) && !HasAttribute(SpellAttr9.AimedShot))
			castTime += 500;

		return (castTime > 0) ? castTime : 0;
	}

	public SpellPowerCost CalcPowerCost(PowerType powerType, bool optionalCost, WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
	{
		// gameobject casts don't use power
		var unitCaster = caster.AsUnit;

		if (unitCaster == null)
			return null;

		var spellPowerRecord = PowerCosts.FirstOrDefault(spellPowerEntry => spellPowerEntry?.PowerType == powerType);

		if (spellPowerRecord == null)
			return null;

		return CalcPowerCost(spellPowerRecord, optionalCost, caster, schoolMask, spell);
	}

	public SpellPowerCost CalcPowerCost(SpellPowerRecord power, bool optionalCost, WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
	{
		// gameobject casts don't use power
		var unitCaster = caster.AsUnit;

		if (!unitCaster)
			return null;

		if (power.RequiredAuraSpellID != 0 && !unitCaster.HasAura(power.RequiredAuraSpellID))
			return null;

		SpellPowerCost cost = new();

		// Spell drain all exist power on cast (Only paladin lay of Hands)
		if (HasAttribute(SpellAttr1.UseAllMana))
		{
			// If power type - health drain all
			if (power.PowerType == PowerType.Health)
			{
				cost.Power = PowerType.Health;
				cost.Amount = (int)unitCaster.Health;

				return cost;
			}

			// Else drain all power
			if (power.PowerType < PowerType.Max)
			{
				cost.Power = power.PowerType;
				cost.Amount = unitCaster.GetPower(cost.Power);

				return cost;
			}

			Log.outError(LogFilter.Spells, $"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");

			return default;
		}

		// Base powerCost
		double powerCost = 0;

		if (!optionalCost)
		{
			powerCost = power.ManaCost;

			// PCT cost from total amount
			if (power.PowerCostPct != 0)
				switch (power.PowerType)
				{
					// health as power used
					case PowerType.Health:
						if (MathFunctions.fuzzyEq(power.PowerCostPct, 0.0f))
							powerCost += (int)MathFunctions.CalculatePct(unitCaster.MaxHealth, power.PowerCostMaxPct);
						else
							powerCost += (int)MathFunctions.CalculatePct(unitCaster.MaxHealth, power.PowerCostPct);

						break;
					case PowerType.Mana:
						powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetCreateMana(), power.PowerCostPct);

						break;
					case PowerType.AlternatePower:
						Log.outError(LogFilter.Spells, $"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");

						return null;
					default:
					{
						var powerTypeEntry = Global.DB2Mgr.GetPowerTypeEntry(power.PowerType);

						if (powerTypeEntry != null)
						{
							powerCost += MathFunctions.CalculatePct(powerTypeEntry.MaxBasePower, power.PowerCostPct);

							break;
						}

						Log.outError(LogFilter.Spells, $"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");

						return null;
					}
				}
		}
		else
		{
			powerCost = power.OptionalCost;

			if (power.OptionalCostPct != 0)
				switch (power.PowerType)
				{
					// health as power used
					case PowerType.Health:
						powerCost += (int)MathFunctions.CalculatePct(unitCaster.MaxHealth, power.OptionalCostPct);

						break;
					case PowerType.Mana:
						powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetCreateMana(), power.OptionalCostPct);

						break;
					case PowerType.AlternatePower:
						Log.outError(LogFilter.Spells, $"SpellInfo::CalcPowerCost: Unsupported power type POWER_ALTERNATE_POWER in spell {Id} for optional cost percent");

						return null;
					default:
					{
						var powerTypeEntry = Global.DB2Mgr.GetPowerTypeEntry(power.PowerType);

						if (powerTypeEntry != null)
						{
							powerCost += (int)MathFunctions.CalculatePct(powerTypeEntry.MaxBasePower, power.OptionalCostPct);

							break;
						}

						Log.outError(LogFilter.Spells, $"SpellInfo::CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id} for optional cost percent");

						return null;
					}
				}

			powerCost += unitCaster.GetTotalAuraModifier(AuraType.ModAdditionalPowerCost, aurEff => { return aurEff.MiscValue == (int)power.PowerType && aurEff.IsAffectingSpell(this); });
		}

		var initiallyNegative = powerCost < 0;

		// Shiv - costs 20 + weaponSpeed*10 energy (apply only to non-triggered spell with energy cost)
		if (HasAttribute(SpellAttr4.WeaponSpeedCostScaling))
		{
			uint speed = 0;
			var ss = CliDB.SpellShapeshiftFormStorage.LookupByKey(unitCaster.ShapeshiftForm);

			if (ss != null)
			{
				speed = ss.CombatRoundTime;
			}
			else
			{
				var slot = WeaponAttackType.BaseAttack;

				if (!HasAttribute(SpellAttr3.RequiresMainHandWeapon) && HasAttribute(SpellAttr3.RequiresOffHandWeapon))
					slot = WeaponAttackType.OffAttack;

				speed = unitCaster.GetBaseAttackTime(slot);
			}

			powerCost += speed / 100;
		}

		if (power.PowerType != PowerType.Health)
		{
			if (!optionalCost)
				// Flat mod from caster auras by spell school and power type
				foreach (var aura in unitCaster.GetAuraEffectsByType(AuraType.ModPowerCostSchool))
				{
					if ((aura.MiscValue & (int)schoolMask) == 0)
						continue;

					if ((aura.MiscValueB & (1 << (int)power.PowerType)) == 0)
						continue;

					powerCost += aura.Amount;
				}

			// PCT mod from user auras by spell school and power type
			foreach (var schoolCostPct in unitCaster.GetAuraEffectsByType(AuraType.ModPowerCostSchoolPct))
			{
				if ((schoolCostPct.MiscValue & (int)schoolMask) == 0)
					continue;

				if ((schoolCostPct.MiscValueB & (1 << (int)power.PowerType)) == 0)
					continue;

				powerCost += MathFunctions.CalculatePct(powerCost, schoolCostPct.Amount);
			}
		}

		// Apply cost mod by spell
		var modOwner = unitCaster.SpellModOwner;

		if (modOwner != null)
		{
			var mod = SpellModOp.Max;

			switch (power.OrderIndex)
			{
				case 0:
					mod = SpellModOp.PowerCost0;

					break;
				case 1:
					mod = SpellModOp.PowerCost1;

					break;
				case 2:
					mod = SpellModOp.PowerCost2;

					break;
				default:
					break;
			}

			if (mod != SpellModOp.Max)
			{
				if (!optionalCost)
				{
					modOwner.ApplySpellMod(this, mod, ref powerCost, spell);
				}
				else
				{
					// optional cost ignores flat modifiers
					double flatMod = 0;
					double pctMod = 1.0f;
					modOwner.GetSpellModValues(this, mod, spell, powerCost, ref flatMod, ref pctMod);
					powerCost = (powerCost * pctMod);
				}
			}
		}

		if (!unitCaster.IsControlledByPlayer && MathFunctions.fuzzyEq(power.PowerCostPct, 0.0f) && SpellLevel != 0 && power.PowerType == PowerType.Mana)
			if (HasAttribute(SpellAttr0.ScalesWithCreatureLevel))
			{
				var spellScaler = CliDB.NpcManaCostScalerGameTable.GetRow(SpellLevel);
				var casterScaler = CliDB.NpcManaCostScalerGameTable.GetRow(unitCaster.Level);

				if (spellScaler != null && casterScaler != null)
					powerCost *= (int)(casterScaler.Scaler / spellScaler.Scaler);
			}

		if (power.PowerType == PowerType.Mana)
			powerCost = (int)((double)powerCost * (1.0f + unitCaster.UnitData.ManaCostMultiplier));

		// power cost cannot become negative if initially positive
		if (initiallyNegative != (powerCost < 0))
			powerCost = 0;

		cost.Power = power.PowerType;
		cost.Amount = (int)powerCost;

		return cost;
	}

	public List<SpellPowerCost> CalcPowerCost(WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
	{
		List<SpellPowerCost> costs = new();

		if (caster.IsUnit)
		{
			SpellPowerCost getOrCreatePowerCost(PowerType powerType)
			{
				var itr = costs.Find(cost => cost.Power == powerType);

				if (itr != null)
					return itr;

				SpellPowerCost cost = new();
				cost.Power = powerType;
				cost.Amount = 0;
				costs.Add(cost);

				return costs.Last();
			}

			foreach (var power in PowerCosts)
			{
				if (power == null)
					continue;

				var cost = CalcPowerCost(power, false, caster, schoolMask, spell);

				if (cost != null)
					getOrCreatePowerCost(cost.Power).Amount += cost.Amount;

				var optionalCost = CalcPowerCost(power, true, caster, schoolMask, spell);

				if (optionalCost != null)
				{
					var cost1 = getOrCreatePowerCost(optionalCost.Power);
					var remainingPower = caster.AsUnit.GetPower(optionalCost.Power) - cost1.Amount;

					if (remainingPower > 0)
						cost1.Amount += Math.Min(optionalCost.Amount, remainingPower);
				}
			}
		}

		return costs;
	}

	public double CalcProcPPM(Unit caster, int itemLevel)
	{
		double ppm = ProcBasePpm;

		if (!caster)
			return ppm;

		foreach (var mod in _procPpmMods)
			switch (mod.Type)
			{
				case SpellProcsPerMinuteModType.Haste:
				{
					ppm *= 1.0f + CalcPPMHasteMod(mod, caster);

					break;
				}
				case SpellProcsPerMinuteModType.Crit:
				{
					ppm *= 1.0f + CalcPPMCritMod(mod, caster);

					break;
				}
				case SpellProcsPerMinuteModType.Class:
				{
					if (caster.ClassMask.HasAnyFlag((uint)mod.Param))
						ppm *= 1.0f + mod.Coeff;

					break;
				}
				case SpellProcsPerMinuteModType.Spec:
				{
					var plrCaster = caster.AsPlayer;

					if (plrCaster)
						if (plrCaster.GetPrimarySpecialization() == mod.Param)
							ppm *= 1.0f + mod.Coeff;

					break;
				}
				case SpellProcsPerMinuteModType.Race:
				{
					if (SharedConst.GetMaskForRace(caster.Race).HasAnyFlag((int)mod.Param))
						ppm *= 1.0f + mod.Coeff;

					break;
				}
				case SpellProcsPerMinuteModType.ItemLevel:
				{
					ppm *= 1.0f + CalcPPMItemLevelMod(mod, itemLevel);

					break;
				}
				case SpellProcsPerMinuteModType.Battleground:
				{
					if (caster.Map.IsBattlegroundOrArena)
						ppm *= 1.0f + mod.Coeff;

					break;
				}
				default:
					break;
			}

		return ppm;
	}

	public SpellInfo GetFirstRankSpell()
	{
		if (ChainEntry == null)
			return this;

		return ChainEntry.First;
	}

	public SpellInfo GetNextRankSpell()
	{
		if (ChainEntry == null)
			return null;

		return ChainEntry.Next;
	}

	public SpellInfo GetAuraRankForLevel(uint level)
	{
		// ignore passive spells
		if (IsPassive)
			return this;

		// Client ignores spell with these attributes (sub_53D9D0)
		if (HasAttribute(SpellAttr0.AuraIsDebuff) || HasAttribute(SpellAttr2.AllowLowLevelBuff) || HasAttribute(SpellAttr3.OnlyProcOnCaster))
			return this;

		var needRankSelection = false;

		foreach (var effectInfo in Effects)
			if (IsPositiveEffect(effectInfo.EffectIndex) &&
				(effectInfo.IsEffect(SpellEffectName.ApplyAura) ||
				effectInfo.IsEffect(SpellEffectName.ApplyAreaAuraParty) ||
				effectInfo.IsEffect(SpellEffectName.ApplyAreaAuraRaid)) &&
				effectInfo.Scaling.Coefficient != 0)
			{
				needRankSelection = true;

				break;
			}

		// not required
		if (!needRankSelection)
			return this;

		for (var nextSpellInfo = this; nextSpellInfo != null; nextSpellInfo = nextSpellInfo.GetPrevRankSpell())
			// if found appropriate level
			if ((level + 10) >= nextSpellInfo.SpellLevel)
				return nextSpellInfo;

		// one rank less then
		// not found
		return null;
	}

	public bool IsRankOf(SpellInfo spellInfo)
	{
		return GetFirstRankSpell() == spellInfo.GetFirstRankSpell();
	}

	public bool IsDifferentRankOf(SpellInfo spellInfo)
	{
		if (Id == spellInfo.Id)
			return false;

		return IsRankOf(spellInfo);
	}

	public bool IsHighRankOf(SpellInfo spellInfo)
	{
		if (ChainEntry != null && spellInfo.ChainEntry != null)
			if (ChainEntry.First == spellInfo.ChainEntry.First)
				if (ChainEntry.Rank > spellInfo.ChainEntry.Rank)
					return true;

		return false;
	}

	public uint GetSpellXSpellVisualId(WorldObject caster = null, WorldObject viewer = null)
	{
		foreach (var visual in _visuals)
		{
			var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(visual.CasterPlayerConditionID);

			if (playerCondition != null)
				if (!caster || !caster.IsPlayer || !ConditionManager.IsPlayerMeetingCondition(caster.AsPlayer, playerCondition))
					continue;

			var unitCondition = CliDB.UnitConditionStorage.LookupByKey(visual.CasterUnitConditionID);

			if (unitCondition != null)
				if (!caster || !caster.IsUnit || !ConditionManager.IsUnitMeetingCondition(caster.AsUnit, viewer?.AsUnit, unitCondition))
					continue;

			return visual.Id;
		}

		return 0;
	}

	public uint GetSpellVisual(WorldObject caster = null, WorldObject viewer = null)
	{
		var visual = CliDB.SpellXSpellVisualStorage.LookupByKey(GetSpellXSpellVisualId(caster, viewer));

		if (visual != null)
			//if (visual.LowViolenceSpellVisualID && forPlayer.GetViolenceLevel() operator 2)
			//    return visual.LowViolenceSpellVisualID;
			return visual.SpellVisualID;

		return 0;
	}

	public void InitializeExplicitTargetMask()
	{
		var srcSet = false;
		var dstSet = false;
		var targetMask = Targets;

		// prepare target mask using effect target entries
		foreach (var effectInfo in Effects)
		{
			if (!effectInfo.IsEffect())
				continue;

			targetMask |= effectInfo.TargetA.GetExplicitTargetMask(ref srcSet, ref dstSet);
			targetMask |= effectInfo.TargetB.GetExplicitTargetMask(ref srcSet, ref dstSet);

			// add explicit target flags based on spell effects which have SpellEffectImplicitTargetTypes.Explicit and no valid target provided
			if (effectInfo.ImplicitTargetType != SpellEffectImplicitTargetTypes.Explicit)
				continue;

			// extend explicit target mask only if valid targets for effect could not be provided by target types
			var effectTargetMask = effectInfo.GetMissingTargetMask(srcSet, dstSet, targetMask);

			// don't add explicit object/dest flags when spell has no max range
			if (GetMaxRange(true) == 0.0f && GetMaxRange(false) == 0.0f)
				effectTargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.CorpseMask | SpellCastTargetFlags.DestLocation);

			targetMask |= effectTargetMask;
		}

		ExplicitTargetMask = targetMask;
	}

	public bool IsPositiveTarget(SpellEffectInfo effect)
	{
		if (!effect.IsEffect())
			return true;

		return effect.TargetA.CheckType != SpellTargetCheckTypes.Enemy &&
				effect.TargetB.CheckType != SpellTargetCheckTypes.Enemy;
	}

	public void InitializeSpellPositivity()
	{
		List<Tuple<SpellInfo, int>> visited = new();

		foreach (var effect in Effects)
			if (!IsPositiveEffectImpl(this, effect, visited))
				NegativeEffects.Add(effect.EffectIndex);


		// additional checks after effects marked
		foreach (var spellEffectInfo in Effects)
		{
			if (!spellEffectInfo.IsEffect() || !IsPositiveEffect(spellEffectInfo.EffectIndex))
				continue;

			switch (spellEffectInfo.ApplyAuraName)
			{
				// has other non positive effect?
				// then it should be marked negative if has same target as negative effect (ex 8510, 8511, 8893, 10267)
				case AuraType.Dummy:
				case AuraType.ModStun:
				case AuraType.ModFear:
				case AuraType.ModTaunt:
				case AuraType.Transform:
				case AuraType.ModAttackspeed:
				case AuraType.ModDecreaseSpeed:
				{
					for (var j = spellEffectInfo.EffectIndex + 1; j < Effects.Count; ++j)
						if (!IsPositiveEffect(j) && spellEffectInfo.TargetA.Target == GetEffect(j).TargetA.Target && spellEffectInfo.TargetB.Target == GetEffect(j).TargetB.Target)
							NegativeEffects.Add(spellEffectInfo.EffectIndex);

					break;
				}
				default:
					break;
			}
		}
	}

	public void UnloadImplicitTargetConditionLists()
	{
		// find the same instances of ConditionList and delete them.
		foreach (var effectInfo in _effects)
		{
			var cur = effectInfo.ImplicitTargetConditions;

			if (cur == null)
				continue;

			for (var j = effectInfo.EffectIndex; j < _effects.Count; ++j)
			{
				var eff = _effects[j];

				if (eff.ImplicitTargetConditions == cur)
					eff.ImplicitTargetConditions = null;
			}
		}
	}

	public bool MeetsFutureSpellPlayerCondition(Player player)
	{
		if (ShowFutureSpellPlayerConditionId == 0)
			return false;

		var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(ShowFutureSpellPlayerConditionId);

		return playerCondition == null || ConditionManager.IsPlayerMeetingCondition(player, playerCondition);
	}

	public bool HasLabel(uint labelId)
	{
		return Labels.Contains(labelId);
	}

	public static SpellCastTargetFlags GetTargetFlagMask(SpellTargetObjectTypes objType)
	{
		switch (objType)
		{
			case SpellTargetObjectTypes.Dest:
				return SpellCastTargetFlags.DestLocation;
			case SpellTargetObjectTypes.UnitAndDest:
				return SpellCastTargetFlags.DestLocation | SpellCastTargetFlags.Unit;
			case SpellTargetObjectTypes.CorpseAlly:
				return SpellCastTargetFlags.CorpseAlly;
			case SpellTargetObjectTypes.CorpseEnemy:
				return SpellCastTargetFlags.CorpseEnemy;
			case SpellTargetObjectTypes.Corpse:
				return SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.CorpseEnemy;
			case SpellTargetObjectTypes.Unit:
				return SpellCastTargetFlags.Unit;
			case SpellTargetObjectTypes.Gobj:
				return SpellCastTargetFlags.Gameobject;
			case SpellTargetObjectTypes.GobjItem:
				return SpellCastTargetFlags.GameobjectItem;
			case SpellTargetObjectTypes.Item:
				return SpellCastTargetFlags.Item;
			case SpellTargetObjectTypes.Src:
				return SpellCastTargetFlags.SourceLocation;
			default:
				return SpellCastTargetFlags.None;
		}
	}

	public SpellEffectInfo GetEffect(int index)
	{
		return _effects[index];
	}

	public bool TryGetEffect(int index, out SpellEffectInfo spellEffectInfo)
	{
		spellEffectInfo = null;

		if (_effects.Count < index)
			return false;

		spellEffectInfo = _effects[index];

		return spellEffectInfo != null;
	}

	public bool HasTargetType(Targets target)
	{
		foreach (var effectInfo in _effects)
			if (effectInfo.TargetA.Target == target || effectInfo.TargetB.Target == target)
				return true;

		return false;
	}

	public bool HasAttribute(SpellAttr0 attribute)
	{
		return Convert.ToBoolean(Attributes & attribute);
	}

	public bool HasAttribute(SpellAttr1 attribute)
	{
		return Convert.ToBoolean(AttributesEx & attribute);
	}

	public bool HasAttribute(SpellAttr2 attribute)
	{
		return Convert.ToBoolean(AttributesEx2 & attribute);
	}

	public bool HasAttribute(SpellAttr3 attribute)
	{
		return Convert.ToBoolean(AttributesEx3 & attribute);
	}

	public bool HasAttribute(SpellAttr4 attribute)
	{
		return Convert.ToBoolean(AttributesEx4 & attribute);
	}

	public bool HasAttribute(SpellAttr5 attribute)
	{
		return Convert.ToBoolean(AttributesEx5 & attribute);
	}

	public bool HasAttribute(SpellAttr6 attribute)
	{
		return Convert.ToBoolean(AttributesEx6 & attribute);
	}

	public bool HasAttribute(SpellAttr7 attribute)
	{
		return Convert.ToBoolean(AttributesEx7 & attribute);
	}

	public bool HasAttribute(SpellAttr8 attribute)
	{
		return Convert.ToBoolean(AttributesEx8 & attribute);
	}

	public bool HasAttribute(SpellAttr9 attribute)
	{
		return Convert.ToBoolean(AttributesEx9 & attribute);
	}

	public bool HasAttribute(SpellAttr10 attribute)
	{
		return Convert.ToBoolean(AttributesEx10 & attribute);
	}

	public bool HasAttribute(SpellAttr11 attribute)
	{
		return Convert.ToBoolean(AttributesEx11 & attribute);
	}

	public bool HasAttribute(SpellAttr12 attribute)
	{
		return Convert.ToBoolean(AttributesEx12 & attribute);
	}

	public bool HasAttribute(SpellAttr13 attribute)
	{
		return Convert.ToBoolean(AttributesEx13 & attribute);
	}

	public bool HasAttribute(SpellAttr14 attribute)
	{
		return Convert.ToBoolean(AttributesEx14 & attribute);
	}

	public bool HasAttribute(SpellCustomAttributes attribute)
	{
		return Convert.ToBoolean(AttributesCu & attribute);
	}

	public bool CanBeInterrupted(WorldObject interruptCaster, Unit interruptTarget, bool ignoreImmunity = false)
	{
		return HasAttribute(SpellAttr7.CanAlwaysBeInterrupted) || HasChannelInterruptFlag(SpellAuraInterruptFlags.Damage | SpellAuraInterruptFlags.EnteringCombat) || (interruptTarget.IsPlayer && InterruptFlags.HasFlag(SpellInterruptFlags.DamageCancelsPlayerOnly)) || InterruptFlags.HasFlag(SpellInterruptFlags.DamageCancels) || (interruptCaster != null && interruptCaster.IsUnit && interruptCaster.AsUnit.HasAuraTypeWithMiscvalue(AuraType.AllowInterruptSpell, (int)Id)) || (((interruptTarget.MechanicImmunityMask & (1 << (int)Mechanics.Interrupt)) == 0 || ignoreImmunity) && !interruptTarget.HasAuraTypeWithAffectMask(AuraType.PreventInterrupt, this) && PreventionType.HasAnyFlag(SpellPreventionType.Silence));
	}

	public bool HasAuraInterruptFlag(SpellAuraInterruptFlags flag)
	{
		return AuraInterruptFlags.HasAnyFlag(flag);
	}

	public bool HasAuraInterruptFlag(SpellAuraInterruptFlags2 flag)
	{
		return AuraInterruptFlags2.HasAnyFlag(flag);
	}

	public bool HasChannelInterruptFlag(SpellAuraInterruptFlags flag)
	{
		return ChannelInterruptFlags.HasAnyFlag(flag);
	}

	public bool HasChannelInterruptFlag(SpellAuraInterruptFlags2 flag)
	{
		return ChannelInterruptFlags2.HasAnyFlag(flag);
	}

	DiminishingGroup DiminishingGroupCompute()
	{
		if (IsPositive)
			return DiminishingGroup.None;

		if (HasAura(AuraType.ModTaunt))
			return DiminishingGroup.Taunt;

		switch (Id)
		{
			case 20549:  // War Stomp (Racial - Tauren)
			case 24394:  // Intimidation
			case 118345: // Pulverize (Primal Earth Elemental)
			case 118905: // Static Charge (Capacitor Totem)
				return DiminishingGroup.Stun;
			case 107079: // Quaking Palm
				return DiminishingGroup.Incapacitate;
			case 155145: // Arcane Torrent (Racial - Blood Elf)
				return DiminishingGroup.Silence;
			case 108199: // Gorefiend's Grasp
			case 191244: // Sticky Bomb
				return DiminishingGroup.AOEKnockback;
			default:
				break;
		}

		// Explicit Diminishing Groups
		switch (SpellFamilyName)
		{
			case SpellFamilyNames.Generic:
				// Frost Tomb
				if (Id == 48400)
					return DiminishingGroup.None;
				// Gnaw
				else if (Id == 47481)
					return DiminishingGroup.Stun;
				// ToC Icehowl Arctic Breath
				else if (Id == 66689)
					return DiminishingGroup.None;
				// Black Plague
				else if (Id == 64155)
					return DiminishingGroup.None;
				// Screams of the Dead (King Ymiron)
				else if (Id == 51750)
					return DiminishingGroup.None;
				// Crystallize (Keristrasza heroic)
				else if (Id == 48179)
					return DiminishingGroup.None;

				break;
			case SpellFamilyNames.Mage:
			{
				// Frost Nova -- 122
				if (SpellFamilyFlags[0].HasAnyFlag(0x40u))
					return DiminishingGroup.Root;

				// Freeze (Water Elemental) -- 33395
				if (SpellFamilyFlags[2].HasAnyFlag(0x200u))
					return DiminishingGroup.Root;

				// Dragon's Breath -- 31661
				if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
					return DiminishingGroup.Incapacitate;

				// Polymorph -- 118
				if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u))
					return DiminishingGroup.Incapacitate;

				// Ring of Frost -- 82691
				if (SpellFamilyFlags[2].HasAnyFlag(0x40u))
					return DiminishingGroup.Incapacitate;

				// Ice Nova -- 157997
				if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
					return DiminishingGroup.Incapacitate;

				break;
			}
			case SpellFamilyNames.Warrior:
			{
				// Shockwave -- 132168
				if (SpellFamilyFlags[1].HasAnyFlag(0x8000u))
					return DiminishingGroup.Stun;

				// Storm Bolt -- 132169
				if (SpellFamilyFlags[2].HasAnyFlag(0x1000u))
					return DiminishingGroup.Stun;

				// Intimidating Shout -- 5246
				if (SpellFamilyFlags[0].HasAnyFlag(0x40000u))
					return DiminishingGroup.Disorient;

				break;
			}
			case SpellFamilyNames.Warlock:
			{
				// Mortal Coil -- 6789
				if (SpellFamilyFlags[0].HasAnyFlag(0x80000u))
					return DiminishingGroup.Incapacitate;

				// Banish -- 710
				if (SpellFamilyFlags[1].HasAnyFlag(0x8000000u))
					return DiminishingGroup.Incapacitate;

				// Fear -- 118699
				if (SpellFamilyFlags[1].HasAnyFlag(0x400u))
					return DiminishingGroup.Disorient;

				// Howl of Terror -- 5484
				if (SpellFamilyFlags[1].HasAnyFlag(0x8u))
					return DiminishingGroup.Disorient;

				// Shadowfury -- 30283
				if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
					return DiminishingGroup.Stun;

				// Summon Infernal -- 22703
				if (SpellFamilyFlags[0].HasAnyFlag(0x1000u))
					return DiminishingGroup.Stun;

				// 170995 -- Cripple
				if (Id == 170995)
					return DiminishingGroup.LimitOnly;

				break;
			}
			case SpellFamilyNames.WarlockPet:
			{
				// Fellash -- 115770
				// Whiplash -- 6360
				if (SpellFamilyFlags[0].HasAnyFlag(0x8000000u))
					return DiminishingGroup.AOEKnockback;

				// Mesmerize (Shivarra pet) -- 115268
				// Seduction (Succubus pet) -- 6358
				if (SpellFamilyFlags[0].HasAnyFlag(0x2000000u))
					return DiminishingGroup.Disorient;

				// Axe Toss (Felguard pet) -- 89766
				if (SpellFamilyFlags[1].HasAnyFlag(0x4u))
					return DiminishingGroup.Stun;

				break;
			}
			case SpellFamilyNames.Druid:
			{
				// Maim -- 22570
				if (SpellFamilyFlags[1].HasAnyFlag(0x80u))
					return DiminishingGroup.Stun;

				// Mighty Bash -- 5211
				if (SpellFamilyFlags[0].HasAnyFlag(0x2000u))
					return DiminishingGroup.Stun;

				// Rake -- 163505 -- no flags on the stun
				if (Id == 163505)
					return DiminishingGroup.Stun;

				// Incapacitating Roar -- 99, no flags on the stun, 14
				if (SpellFamilyFlags[1].HasAnyFlag(0x1u))
					return DiminishingGroup.Incapacitate;

				// Cyclone -- 33786
				if (SpellFamilyFlags[1].HasAnyFlag(0x20u))
					return DiminishingGroup.Disorient;

				// Solar Beam -- 81261
				if (Id == 81261)
					return DiminishingGroup.Silence;

				// Typhoon -- 61391
				if (SpellFamilyFlags[1].HasAnyFlag(0x1000000u))
					return DiminishingGroup.AOEKnockback;

				// Ursol's Vortex -- 118283, no family flags
				if (Id == 118283)
					return DiminishingGroup.AOEKnockback;

				// Entangling Roots -- 339
				if (SpellFamilyFlags[0].HasAnyFlag(0x200u))
					return DiminishingGroup.Root;

				// Mass Entanglement -- 102359
				if (SpellFamilyFlags[2].HasAnyFlag(0x4u))
					return DiminishingGroup.Root;

				break;
			}
			case SpellFamilyNames.Rogue:
			{
				// Between the Eyes -- 199804
				if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
					return DiminishingGroup.Stun;

				// Cheap Shot -- 1833
				if (SpellFamilyFlags[0].HasAnyFlag(0x400u))
					return DiminishingGroup.Stun;

				// Kidney Shot -- 408
				if (SpellFamilyFlags[0].HasAnyFlag(0x200000u))
					return DiminishingGroup.Stun;

				// Gouge -- 1776
				if (SpellFamilyFlags[0].HasAnyFlag(0x8u))
					return DiminishingGroup.Incapacitate;

				// Sap -- 6770
				if (SpellFamilyFlags[0].HasAnyFlag(0x80u))
					return DiminishingGroup.Incapacitate;

				// Blind -- 2094
				if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u))
					return DiminishingGroup.Disorient;

				// Garrote -- 1330
				if (SpellFamilyFlags[1].HasAnyFlag(0x20000000u))
					return DiminishingGroup.Silence;

				break;
			}
			case SpellFamilyNames.Hunter:
			{
				// Charge (Tenacity pet) -- 53148, no flags
				if (Id == 53148)
					return DiminishingGroup.Root;

				// Ranger's Net -- 200108
				// Tracker's Net -- 212638
				if (Id == 200108 || Id == 212638)
					return DiminishingGroup.Root;

				// Binding Shot -- 117526, no flags
				if (Id == 117526)
					return DiminishingGroup.Stun;

				// Freezing Trap -- 3355
				if (SpellFamilyFlags[0].HasAnyFlag(0x8u))
					return DiminishingGroup.Incapacitate;

				// Wyvern Sting -- 19386
				if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
					return DiminishingGroup.Incapacitate;

				// Bursting Shot -- 224729
				if (SpellFamilyFlags[2].HasAnyFlag(0x40u))
					return DiminishingGroup.Disorient;

				// Scatter Shot -- 213691
				if (SpellFamilyFlags[2].HasAnyFlag(0x8000u))
					return DiminishingGroup.Disorient;

				// Spider Sting -- 202933
				if (Id == 202933)
					return DiminishingGroup.Silence;

				break;
			}
			case SpellFamilyNames.Paladin:
			{
				// Repentance -- 20066
				if (SpellFamilyFlags[0].HasAnyFlag(0x4u))
					return DiminishingGroup.Incapacitate;

				// Blinding Light -- 105421
				if (Id == 105421)
					return DiminishingGroup.Disorient;

				// Avenger's Shield -- 31935
				if (SpellFamilyFlags[0].HasAnyFlag(0x4000u))
					return DiminishingGroup.Silence;

				// Hammer of Justice -- 853
				if (SpellFamilyFlags[0].HasAnyFlag(0x800u))
					return DiminishingGroup.Stun;

				break;
			}
			case SpellFamilyNames.Shaman:
			{
				// Hex -- 51514
				// Hex -- 196942 (Voodoo Totem)
				if (SpellFamilyFlags[1].HasAnyFlag(0x8000u))
					return DiminishingGroup.Incapacitate;

				// Thunderstorm -- 51490
				if (SpellFamilyFlags[1].HasAnyFlag(0x2000u))
					return DiminishingGroup.AOEKnockback;

				// Earthgrab Totem -- 64695
				if (SpellFamilyFlags[2].HasAnyFlag(0x4000u))
					return DiminishingGroup.Root;

				// Lightning Lasso -- 204437
				if (SpellFamilyFlags[3].HasAnyFlag(0x2000000u))
					return DiminishingGroup.Stun;

				break;
			}
			case SpellFamilyNames.Deathknight:
			{
				// Chains of Ice -- 96294
				if (Id == 96294)
					return DiminishingGroup.Root;

				// Blinding Sleet -- 207167
				if (Id == 207167)
					return DiminishingGroup.Disorient;

				// Strangulate -- 47476
				if (SpellFamilyFlags[0].HasAnyFlag(0x200u))
					return DiminishingGroup.Silence;

				// Asphyxiate -- 108194
				if (SpellFamilyFlags[2].HasAnyFlag(0x100000u))
					return DiminishingGroup.Stun;

				// Gnaw (Ghoul) -- 91800, no flags
				if (Id == 91800)
					return DiminishingGroup.Stun;

				// Monstrous Blow (Ghoul w/ Dark Transformation active) -- 91797
				if (Id == 91797)
					return DiminishingGroup.Stun;

				// Winter is Coming -- 207171
				if (Id == 207171)
					return DiminishingGroup.Stun;

				break;
			}
			case SpellFamilyNames.Priest:
			{
				// Holy Word: Chastise -- 200200
				if (SpellFamilyFlags[2].HasAnyFlag(0x20u) && GetSpellVisual() == 52021)
					return DiminishingGroup.Stun;

				// Mind Bomb -- 226943
				if (Id == 226943)
					return DiminishingGroup.Stun;

				// Mind Control -- 605
				if (SpellFamilyFlags[0].HasAnyFlag(0x20000u) && GetSpellVisual() == 39068)
					return DiminishingGroup.Incapacitate;

				// Holy Word: Chastise -- 200196
				if (SpellFamilyFlags[2].HasAnyFlag(0x20u) && GetSpellVisual() == 52019)
					return DiminishingGroup.Incapacitate;

				// Psychic Scream -- 8122
				if (SpellFamilyFlags[0].HasAnyFlag(0x10000u))
					return DiminishingGroup.Disorient;

				// Silence -- 15487
				if (SpellFamilyFlags[1].HasAnyFlag(0x200000u) && GetSpellVisual() == 39025)
					return DiminishingGroup.Silence;

				// Shining Force -- 204263
				if (Id == 204263)
					return DiminishingGroup.AOEKnockback;

				break;
			}
			case SpellFamilyNames.Monk:
			{
				// Disable -- 116706, no flags
				if (Id == 116706)
					return DiminishingGroup.Root;

				// Fists of Fury -- 120086
				if (SpellFamilyFlags[1].HasAnyFlag(0x800000u) && !SpellFamilyFlags[2].HasAnyFlag(0x8u))
					return DiminishingGroup.Stun;

				// Leg Sweep -- 119381
				if (SpellFamilyFlags[1].HasAnyFlag(0x200u))
					return DiminishingGroup.Stun;

				// Incendiary Breath (honor talent) -- 202274, no flags
				if (Id == 202274)
					return DiminishingGroup.Incapacitate;

				// Paralysis -- 115078
				if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
					return DiminishingGroup.Incapacitate;

				// Song of Chi-Ji -- 198909
				if (Id == 198909)
					return DiminishingGroup.Disorient;

				break;
			}
			case SpellFamilyNames.DemonHunter:
				switch (Id)
				{
					case 179057: // Chaos Nova
					case 211881: // Fel Eruption
					case 200166: // Metamorphosis
					case 205630: // Illidan's Grasp
						return DiminishingGroup.Stun;
					case 217832: // Imprison
					case 221527: // Imprison
						return DiminishingGroup.Incapacitate;
					default:
						break;
				}

				break;
			default:
				break;
		}

		return DiminishingGroup.None;
	}

	DiminishingReturnsType DiminishingTypeCompute(DiminishingGroup group)
	{
		switch (group)
		{
			case DiminishingGroup.Taunt:
			case DiminishingGroup.Stun:
				return DiminishingReturnsType.All;
			case DiminishingGroup.LimitOnly:
			case DiminishingGroup.None:
				return DiminishingReturnsType.None;
			default:
				return DiminishingReturnsType.Player;
		}
	}

	DiminishingLevels DiminishingMaxLevelCompute(DiminishingGroup group)
	{
		switch (group)
		{
			case DiminishingGroup.Taunt:
				return DiminishingLevels.TauntImmune;
			case DiminishingGroup.AOEKnockback:
				return DiminishingLevels.Level2;
			default:
				return DiminishingLevels.Immune;
		}
	}

	int DiminishingLimitDurationCompute()
	{
		// Explicit diminishing duration
		switch (SpellFamilyName)
		{
			case SpellFamilyNames.Mage:
				// Dragon's Breath - 3 seconds in PvP
				if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
					return 3 * Time.InMilliseconds;

				break;
			case SpellFamilyNames.Warlock:
				// Cripple - 4 seconds in PvP
				if (Id == 170995)
					return 4 * Time.InMilliseconds;

				break;
			case SpellFamilyNames.Hunter:
				// Binding Shot - 3 seconds in PvP
				if (Id == 117526)
					return 3 * Time.InMilliseconds;

				// Wyvern Sting - 6 seconds in PvP
				if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
					return 6 * Time.InMilliseconds;

				break;
			case SpellFamilyNames.Monk:
				// Paralysis - 4 seconds in PvP regardless of if they are facing you
				if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
					return 4 * Time.InMilliseconds;

				break;
			case SpellFamilyNames.DemonHunter:
				switch (Id)
				{
					case 217832: // Imprison
					case 221527: // Imprison
						return 4 * Time.InMilliseconds;
					default:
						break;
				}

				break;
			default:
				break;
		}

		return 8 * Time.InMilliseconds;
	}

	bool CanSpellProvideImmunityAgainstAura(SpellInfo auraSpellInfo)
	{
		if (auraSpellInfo == null)
			return false;

		foreach (var effectInfo in _effects)
		{
			if (!effectInfo.IsEffect())
				continue;

			var immuneInfo = effectInfo.ImmunityInfo;

			if (!auraSpellInfo.HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects) && !auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
			{
				var schoolImmunity = immuneInfo.SchoolImmuneMask;

				if (schoolImmunity != 0)
					if (((uint)auraSpellInfo.SchoolMask & schoolImmunity) != 0)
						return true;
			}

			var mechanicImmunity = immuneInfo.MechanicImmuneMask;

			if (mechanicImmunity != 0)
				if ((mechanicImmunity & (1ul << (int)auraSpellInfo.Mechanic)) != 0)
					return true;

			var dispelImmunity = immuneInfo.DispelImmune;

			if (dispelImmunity != 0)
				if ((uint)auraSpellInfo.Dispel == dispelImmunity)
					return true;

			var immuneToAllEffects = true;

			foreach (var auraSpellEffectInfo in auraSpellInfo.Effects)
			{
				if (!auraSpellEffectInfo.IsEffect())
					continue;

				if (!immuneInfo.SpellEffectImmune.Contains(auraSpellEffectInfo.Effect))
				{
					immuneToAllEffects = false;

					break;
				}

				var mechanic = (uint)auraSpellEffectInfo.Mechanic;

				if (mechanic != 0)
					if (!Convert.ToBoolean(immuneInfo.MechanicImmuneMask & (1ul << (int)mechanic)))
					{
						immuneToAllEffects = false;

						break;
					}

				if (!auraSpellInfo.HasAttribute(SpellAttr3.AlwaysHit))
				{
					var auraName = auraSpellEffectInfo.ApplyAuraName;

					if (auraName != 0)
					{
						var isImmuneToAuraEffectApply = false;

						if (!immuneInfo.AuraTypeImmune.Contains(auraName))
							isImmuneToAuraEffectApply = true;

						if (!isImmuneToAuraEffectApply && !auraSpellInfo.IsPositiveEffect(auraSpellEffectInfo.EffectIndex) && !auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
						{
							var applyHarmfulAuraImmunityMask = immuneInfo.ApplyHarmfulAuraImmuneMask;

							if (applyHarmfulAuraImmunityMask != 0)
								if (((uint)auraSpellInfo.GetSchoolMask() & applyHarmfulAuraImmunityMask) != 0)
									isImmuneToAuraEffectApply = true;
						}

						if (!isImmuneToAuraEffectApply)
						{
							immuneToAllEffects = false;

							break;
						}
					}
				}
			}

			if (immuneToAllEffects)
				return true;
		}

		return false;
	}

	double CalcPPMHasteMod(SpellProcsPerMinuteModRecord mod, Unit caster)
	{
		double haste = caster.UnitData.ModHaste;
		double rangedHaste = caster.UnitData.ModRangedHaste;
		double spellHaste = caster.UnitData.ModSpellHaste;
		double regenHaste = caster.UnitData.ModHasteRegen;

		switch (mod.Param)
		{
			case 1:
				return (1.0f / haste - 1.0f) * mod.Coeff;
			case 2:
				return (1.0f / rangedHaste - 1.0f) * mod.Coeff;
			case 3:
				return (1.0f / spellHaste - 1.0f) * mod.Coeff;
			case 4:
				return (1.0f / regenHaste - 1.0f) * mod.Coeff;
			case 5:
				return (1.0f / Math.Min(Math.Min(Math.Min(haste, rangedHaste), spellHaste), regenHaste) - 1.0f) * mod.Coeff;
			default:
				break;
		}

		return 0.0f;
	}

	double CalcPPMCritMod(SpellProcsPerMinuteModRecord mod, Unit caster)
	{
		var player = caster.AsPlayer;

		if (player == null)
			return 0.0f;

		double crit = player.ActivePlayerData.CritPercentage;
		double rangedCrit = player.ActivePlayerData.RangedCritPercentage;
		double spellCrit = player.ActivePlayerData.SpellCritPercentage;

		switch (mod.Param)
		{
			case 1:
				return crit * mod.Coeff * 0.01f;
			case 2:
				return rangedCrit * mod.Coeff * 0.01f;
			case 3:
				return spellCrit * mod.Coeff * 0.01f;
			case 4:
				return Math.Min(Math.Min(crit, rangedCrit), spellCrit) * mod.Coeff * 0.01f;
			default:
				break;
		}

		return 0.0f;
	}

	double CalcPPMItemLevelMod(SpellProcsPerMinuteModRecord mod, int itemLevel)
	{
		if (itemLevel == mod.Param)
			return 0.0f;

		double itemLevelPoints = ItemEnchantmentManager.GetRandomPropertyPoints((uint)itemLevel, ItemQuality.Rare, InventoryType.Chest, 0);
		double basePoints = ItemEnchantmentManager.GetRandomPropertyPoints(mod.Param, ItemQuality.Rare, InventoryType.Chest, 0);

		if (itemLevelPoints == basePoints)
			return 0.0f;

		return ((itemLevelPoints / basePoints) - 1.0f) * mod.Coeff;
	}

	SpellInfo GetLastRankSpell()
	{
		if (ChainEntry == null)
			return null;

		return ChainEntry.Last;
	}

	SpellInfo GetPrevRankSpell()
	{
		if (ChainEntry == null)
			return null;

		return ChainEntry.Prev;
	}

	bool IsPositiveEffectImpl(SpellInfo spellInfo, SpellEffectInfo effect, List<Tuple<SpellInfo, int>> visited)
	{
		if (!effect.IsEffect())
			return true;

		// attribute may be already set in DB
		if (!spellInfo.IsPositiveEffect(effect.EffectIndex))
			return false;

		// passive auras like talents are all positive
		if (spellInfo.IsPassive)
			return true;

		// not found a single positive spell with this attribute
		if (spellInfo.HasAttribute(SpellAttr0.AuraIsDebuff))
			return false;

		if (spellInfo.HasAttribute(SpellAttr4.AuraIsBuff))
			return true;

		visited.Add(Tuple.Create(spellInfo, effect.EffectIndex));

		//We need scaling level info for some auras that compute bp 0 or positive but should be debuffs
		var bpScalePerLevel = effect.RealPointsPerLevel;
		var bp = effect.CalcValue();

		switch (spellInfo.SpellFamilyName)
		{
			case SpellFamilyNames.Generic:
				switch (spellInfo.Id)
				{
					case 40268: // Spiritual Vengeance, Teron Gorefiend, Black Temple
					case 61987: // Avenging Wrath Marker
					case 61988: // Divine Shield exclude aura
					case 64412: // Phase Punch, Algalon the Observer, Ulduar
					case 72410: // Rune of Blood, Saurfang, Icecrown Citadel
					case 71204: // Touch of Insignificance, Lady Deathwhisper, Icecrown Citadel
						return false;
					case 24732: // Bat Costume
					case 30877: // Tag Murloc
					case 61716: // Rabbit Costume
					case 61734: // Noblegarden Bunny
					case 62344: // Fists of Stone
					case 50344: // Dream Funnel
					case 61819: // Manabonked! (item)
					case 61834: // Manabonked! (minigob)
					case 73523: // Rigor Mortis
						return true;
					default:
						break;
				}

				break;
			case SpellFamilyNames.Rogue:
				switch (spellInfo.Id)
				{
					// Envenom must be considered as a positive effect even though it deals damage
					case 32645: // Envenom
						return true;
					case 40251: // Shadow of Death, Teron Gorefiend, Black Temple
						return false;
					default:
						break;
				}

				break;
			case SpellFamilyNames.Warrior:
				// Slam, Execute
				if ((spellInfo.SpellFamilyFlags[0] & 0x20200000) != 0)
					return false;

				break;
			default:
				break;
		}

		switch (spellInfo.Mechanic)
		{
			case Mechanics.ImmuneShield:
				return true;
			default:
				break;
		}

		// Special case: effects which determine positivity of whole spell
		if (spellInfo.HasAttribute(SpellAttr1.AuraUnique))
			// check for targets, there seems to be an assortment of dummy triggering spells that should be negative
			foreach (var otherEffect in spellInfo.Effects)
				if (!IsPositiveTarget(otherEffect))
					return false;

		foreach (var otherEffect in spellInfo.Effects)
		{
			switch (otherEffect.Effect)
			{
				case SpellEffectName.Heal:
				case SpellEffectName.LearnSpell:
				case SpellEffectName.SkillStep:
				case SpellEffectName.HealPct:
					return true;
				case SpellEffectName.Instakill:
					if (otherEffect.EffectIndex != effect.EffectIndex && // for spells like 38044: instakill effect is negative but auras on target must count as buff
						otherEffect.TargetA.Target == effect.TargetA.Target &&
						otherEffect.TargetB.Target == effect.TargetB.Target)
						return false;

					break;
				default:
					break;
			}

			if (otherEffect.IsAura())
				switch (otherEffect.ApplyAuraName)
				{
					case AuraType.ModStealth:
					case AuraType.ModUnattackable:
						return true;
					case AuraType.SchoolHealAbsorb:
					case AuraType.Empathy:
					case AuraType.ModSpellDamageFromCaster:
					case AuraType.PreventsFleeing:
						return false;
					default:
						break;
				}
		}

		switch (effect.Effect)
		{
			case SpellEffectName.WeaponDamage:
			case SpellEffectName.WeaponDamageNoSchool:
			case SpellEffectName.NormalizedWeaponDmg:
			case SpellEffectName.WeaponPercentDamage:
			case SpellEffectName.SchoolDamage:
			case SpellEffectName.EnvironmentalDamage:
			case SpellEffectName.HealthLeech:
			case SpellEffectName.Instakill:
			case SpellEffectName.PowerDrain:
			case SpellEffectName.StealBeneficialBuff:
			case SpellEffectName.InterruptCast:
			case SpellEffectName.Pickpocket:
			case SpellEffectName.GameObjectDamage:
			case SpellEffectName.DurabilityDamage:
			case SpellEffectName.DurabilityDamagePct:
			case SpellEffectName.ApplyAreaAuraEnemy:
			case SpellEffectName.Tamecreature:
			case SpellEffectName.Distract:
				return false;
			case SpellEffectName.Energize:
			case SpellEffectName.EnergizePct:
			case SpellEffectName.HealPct:
			case SpellEffectName.HealMaxHealth:
			case SpellEffectName.HealMechanical:
				return true;
			case SpellEffectName.KnockBack:
			case SpellEffectName.Charge:
			case SpellEffectName.PersistentAreaAura:
			case SpellEffectName.AttackMe:
			case SpellEffectName.PowerBurn:
				// check targets
				if (!IsPositiveTarget(effect))
					return false;

				break;
			case SpellEffectName.Dispel:
				// non-positive dispel
				switch ((DispelType)effect.MiscValue)
				{
					case DispelType.Stealth:
					case DispelType.Invisibility:
					case DispelType.Enrage:
						return false;
					default:
						break;
				}

				// also check targets
				if (!IsPositiveTarget(effect))
					return false;

				break;
			case SpellEffectName.DispelMechanic:
				if (!IsPositiveTarget(effect))
					// non-positive mechanic dispel on negative target
					switch ((Mechanics)effect.MiscValue)
					{
						case Mechanics.Bandage:
						case Mechanics.Shield:
						case Mechanics.Mount:
						case Mechanics.Invulnerability:
							return false;
						default:
							break;
					}

				break;
			case SpellEffectName.Threat:
			case SpellEffectName.ModifyThreatPercent:
				// check targets AND basepoints
				if (!IsPositiveTarget(effect) && bp > 0)
					return false;

				break;
			default:
				break;
		}

		if (effect.IsAura())
			// non-positive aura use
			switch (effect.ApplyAuraName)
			{
				case AuraType.ModStat: // dependent from basepoint sign (negative -> negative)
				case AuraType.ModSkill:
				case AuraType.ModSkill2:
				case AuraType.ModDodgePercent:
				case AuraType.ModHealingDone:
				case AuraType.ModDamageDoneCreature:
				case AuraType.ObsModHealth:
				case AuraType.ObsModPower:
				case AuraType.ModCritPct:
				case AuraType.ModHitChance:
				case AuraType.ModSpellHitChance:
				case AuraType.ModSpellCritChance:
				case AuraType.ModRangedHaste:
				case AuraType.ModMeleeRangedHaste:
				case AuraType.ModCastingSpeedNotStack:
				case AuraType.HasteSpells:
				case AuraType.ModRecoveryRateBySpellLabel:
				case AuraType.ModDetectRange:
				case AuraType.ModIncreaseHealthPercent:
				case AuraType.ModTotalStatPercentage:
				case AuraType.ModIncreaseSwimSpeed:
				case AuraType.ModPercentStat:
				case AuraType.ModIncreaseHealth:
				case AuraType.ModSpeedAlways:
					if (bp < 0 || bpScalePerLevel < 0) //TODO: What if both are 0? Should it be a buff or debuff?
						return false;

					break;
				case AuraType.ModAttackspeed: // some buffs have negative bp, check both target and bp
				case AuraType.ModMeleeHaste:
				case AuraType.ModDamageDone:
				case AuraType.ModResistance:
				case AuraType.ModResistancePct:
				case AuraType.ModRating:
				case AuraType.ModAttackPower:
				case AuraType.ModRangedAttackPower:
				case AuraType.ModDamagePercentDone:
				case AuraType.ModSpeedSlowAll:
				case AuraType.MeleeSlow:
				case AuraType.ModAttackPowerPct:
				case AuraType.ModHealingDonePercent:
				case AuraType.ModHealingPct:
					if (!IsPositiveTarget(effect) || bp < 0)
						return false;

					break;
				case AuraType.ModDamageTaken: // dependent from basepoint sign (positive . negative)
				case AuraType.ModMeleeDamageTaken:
				case AuraType.ModMeleeDamageTakenPct:
				case AuraType.ModPowerCostSchool:
				case AuraType.ModPowerCostSchoolPct:
				case AuraType.ModMechanicDamageTakenPercent:
					if (bp > 0)
						return false;

					break;
				case AuraType.ModDamagePercentTaken: // check targets and basepoints (ex Recklessness)
					if (!IsPositiveTarget(effect) && bp > 0)
						return false;

					break;
				case AuraType.ModHealthRegenPercent: // check targets and basepoints (target enemy and negative bp -> negative)
					if (!IsPositiveTarget(effect) && bp < 0)
						return false;

					break;
				case AuraType.AddTargetTrigger:
					return true;
				case AuraType.PeriodicTriggerSpellWithValue:
				case AuraType.PeriodicTriggerSpellFromClient:
					var spellTriggeredProto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell, spellInfo.Difficulty);

					if (spellTriggeredProto != null)
						// negative targets of main spell return early
						foreach (var spellTriggeredEffect in spellTriggeredProto.Effects)
						{
							// already seen this
							if (visited.Contains(Tuple.Create(spellTriggeredProto, spellTriggeredEffect.EffectIndex)))
								continue;

							if (!spellTriggeredEffect.IsEffect())
								continue;

							// if non-positive trigger cast targeted to positive target this main cast is non-positive
							// this will place this spell auras as debuffs
							if (IsPositiveTarget(spellTriggeredEffect) && !IsPositiveEffectImpl(spellTriggeredProto, spellTriggeredEffect, visited))
								return false;
						}

					break;
				case AuraType.PeriodicTriggerSpell:
				case AuraType.ModStun:
				case AuraType.Transform:
				case AuraType.ModDecreaseSpeed:
				case AuraType.ModFear:
				case AuraType.ModTaunt:
				// special auras: they may have non negative target but still need to be marked as debuff
				// checked again after all effects (SpellInfo::_InitializeSpellPositivity)
				case AuraType.ModPacify:
				case AuraType.ModPacifySilence:
				case AuraType.ModDisarm:
				case AuraType.ModDisarmOffhand:
				case AuraType.ModDisarmRanged:
				case AuraType.ModCharm:
				case AuraType.AoeCharm:
				case AuraType.ModPossess:
				case AuraType.ModLanguage:
				case AuraType.DamageShield:
				case AuraType.ProcTriggerSpell:
				case AuraType.ModAttackerMeleeHitChance:
				case AuraType.ModAttackerRangedHitChance:
				case AuraType.ModAttackerSpellHitChance:
				case AuraType.ModAttackerMeleeCritChance:
				case AuraType.ModAttackerRangedCritChance:
				case AuraType.ModAttackerSpellAndWeaponCritChance:
				case AuraType.Dummy:
				case AuraType.PeriodicDummy:
				case AuraType.ModHealing:
				case AuraType.ModWeaponCritPercent:
				case AuraType.PowerBurn:
				case AuraType.ModCooldown:
				case AuraType.ModChargeCooldown:
				case AuraType.ModIncreaseSpeed:
				case AuraType.ModParryPercent:
				case AuraType.SetVehicleId:
				case AuraType.PeriodicEnergize:
				case AuraType.EffectImmunity:
				case AuraType.OverrideClassScripts:
				case AuraType.ModShapeshift:
				case AuraType.ModThreat:
				case AuraType.ProcTriggerSpellWithValue:
					// check target for positive and negative spells
					if (!IsPositiveTarget(effect))
						return false;

					break;
				case AuraType.ModConfuse:
				case AuraType.ChannelDeathItem:
				case AuraType.ModRoot:
				case AuraType.ModRoot2:
				case AuraType.ModSilence:
				case AuraType.ModDetaunt:
				case AuraType.Ghost:
				case AuraType.ModLeech:
				case AuraType.PeriodicManaLeech:
				case AuraType.ModStalked:
				case AuraType.PreventResurrection:
				case AuraType.PeriodicDamage:
				case AuraType.PeriodicWeaponPercentDamage:
				case AuraType.PeriodicDamagePercent:
				case AuraType.MeleeAttackPowerAttackerBonus:
				case AuraType.RangedAttackPowerAttackerBonus:
					return false;
				case AuraType.MechanicImmunity:
				{
					// non-positive immunities
					switch ((Mechanics)effect.MiscValue)
					{
						case Mechanics.Bandage:
						case Mechanics.Shield:
						case Mechanics.Mount:
						case Mechanics.Invulnerability:
							return false;
						default:
							break;
					}

					break;
				}
				case AuraType.AddFlatModifier: // mods
				case AuraType.AddPctModifier:
				case AuraType.AddFlatModifierBySpellLabel:
				case AuraType.AddPctModifierBySpellLabel:
				{
					switch ((SpellModOp)effect.MiscValue)
					{
						case SpellModOp.ChangeCastTime: // dependent from basepoint sign (positive . negative)
						case SpellModOp.Period:
						case SpellModOp.PowerCostOnMiss:
						case SpellModOp.StartCooldown:
							if (bp > 0)
								return false;

							break;
						case SpellModOp.Cooldown:
						case SpellModOp.PowerCost0:
						case SpellModOp.PowerCost1:
						case SpellModOp.PowerCost2:
							if (!spellInfo.IsPositive && bp > 0) // dependent on prev effects too (ex Arcane Power)
								return false;

							break;
						case SpellModOp.PointsIndex0: // always positive
						case SpellModOp.PointsIndex1:
						case SpellModOp.PointsIndex2:
						case SpellModOp.PointsIndex3:
						case SpellModOp.PointsIndex4:
						case SpellModOp.Points:
						case SpellModOp.Hate:
						case SpellModOp.ChainAmplitude:
						case SpellModOp.Amplitude:
							return true;
						case SpellModOp.Duration:
						case SpellModOp.CritChance:
						case SpellModOp.HealingAndDamage:
						case SpellModOp.ChainTargets:
							if (!spellInfo.IsPositive && bp < 0) // dependent on prev effects too
								return false;

							break;
						default: // dependent from basepoint sign (negative . negative)
							if (bp < 0)
								return false;

							break;
					}

					break;
				}
				default:
					break;
			}

		// negative spell if triggered spell is negative
		if (effect.ApplyAuraName == 0 && effect.TriggerSpell != 0)
		{
			var spellTriggeredProto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell, spellInfo.Difficulty);

			if (spellTriggeredProto != null)
				// spells with at least one negative effect are considered negative
				// some self-applied spells have negative effects but in self casting case negative check ignored.
				foreach (var spellTriggeredEffect in spellTriggeredProto.Effects)
				{
					// already seen this
					if (visited.Contains(Tuple.Create(spellTriggeredProto, spellTriggeredEffect.EffectIndex)))
						continue;

					if (!spellTriggeredEffect.IsEffect())
						continue;

					if (!IsPositiveEffectImpl(spellTriggeredProto, spellTriggeredEffect, visited))
						return false;
				}
		}

		// ok, positive
		return true;
	}


	public struct ScalingInfo
	{
		public uint MinScalingLevel;
		public uint MaxScalingLevel;
		public uint ScalesFromItemLevel;
	}
}