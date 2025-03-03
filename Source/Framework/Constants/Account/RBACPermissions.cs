﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum RBACPermissions
{
	InstantLogout = 1,
	SkipQueue = 2,
	JoinNormalBg = 3,
	JoinRandomBg = 4,
	JoinArenas = 5,
	JoinDungeonFinder = 6,
	IgnoreIdleConnection = 7,
	CannotEarnAchievements = 8,
	CannotEarnRealmFirstAchievements = 9,
	UseCharacterTemplates = 10,
	LogGmTrade = 11,
	SkipCheckCharacterCreationDemonHunter = 12,
	SkipCheckInstanceRequiredBosses = 13,
	SkipCheckCharacterCreationTeammask = 14,
	SkipCheckCharacterCreationClassmask = 15,
	SkipCheckCharacterCreationRacemask = 16,
	SkipCheckCharacterCreationReservedname = 17,
	SkipCheckCharacterCreationDeathKnight = 18, // Deprecated Since Draenor Don'T Reuse
	SkipCheckChatChannelReq = 19,
	SkipCheckDisableMap = 20,
	SkipCheckMoreTalentsThanAllowed = 21,
	SkipCheckChatSpam = 22,
	SkipCheckOverspeedPing = 23,
	TwoSideCharacterCreation = 24,
	TwoSideInteractionChat = 25,
	TwoSideInteractionChannel = 26,
	TwoSideInteractionMail = 27,
	TwoSideWhoList = 28,
	TwoSideAddFriend = 29,
	CommandsSaveWithoutDelay = 30,
	CommandsUseUnstuckWithArgs = 31,
	CommandsBeAssignedTicket = 32,
	CommandsNotifyCommandNotFoundError = 33,
	CommandsAppearInGmList = 34,
	WhoSeeAllSecLevels = 35,
	CanFilterWhispers = 36,
	ChatUseStaffBadge = 37,
	ResurrectWithFullHps = 38,
	RestoreSavedGmState = 39,
	AllowGmFriend = 40,
	UseStartGmLevel = 41,
	OpcodeWorldTeleport = 42,
	OpcodeWhois = 43,
	ReceiveGlobalGmTextmessage = 44,
	SilentlyJoinChannel = 45,
	ChangeChannelNotModerator = 46,
	CheckForLowerSecurity = 47,
	CommandsPinfoCheckPersonalData = 48,
	EmailConfirmForPassChange = 49,
	MayCheckOwnEmail = 50,
	AllowTwoSideTrade = 51,

	// Free Space For Core Permissions (Till 149)
	// Roles (Permissions With Delegated Permissions) Use 199 And Descending
	RoleAdministrator = 196,
	RoleGamemaster = 197,
	RoleModerator = 198,
	RolePlayer = 199,

	// 200 previously used, do not reuse
	// 201 previously used, do not reuse
	CommandRbacAccPermList = 202,
	CommandRbacAccPermGrant = 203,
	CommandRbacAccPermDeny = 204,
	CommandRbacAccPermRevoke = 205,
	CommandRbacList = 206,
	CommandBnetAccount = 207,
	CommandBnetAccountCreate = 208,
	CommandBnetAccountLockCountry = 209,
	CommandBnetAccountLockIp = 210,
	CommandBnetAccountPassword = 211,

	// 212 previously used, do not reuse
	CommandBnetAccountSetPassword = 213,
	CommandBnetAccountLink = 214,
	CommandBnetAccountUnlink = 215,
	CommandBnetAccountCreateGame = 216,
	CommandAccount = 217,
	CommandAccountAddon = 218,
	CommandAccountCreate = 219,
	CommandAccountDelete = 220,
	CommandAccountLock = 221,
	CommandAccountLockCountry = 222,
	CommandAccountLockIp = 223,
	CommandAccountOnlineList = 224,
	CommandAccountPassword = 225,
	CommandAccountSet = 226,
	CommandAccountSetAddon = 227,
	CommandAccountSetSecLevel = 228,
	CommandAccountSetPassword = 229,

	// 230 previously used, do not reuse
	CommandAchievementAdd = 231,

	// 232 previously used, do not reuse
	CommandArenaCaptain = 233,
	CommandArenaCreate = 234,
	CommandArenaDisband = 235,
	CommandArenaInfo = 236,
	CommandArenaLookup = 237,
	CommandArenaRename = 238,

	// 239 previously used, do not reuse
	CommandBanAccount = 240,
	CommandBanCharacter = 241,
	CommandBanIp = 242,
	CommandBanPlayeraccount = 243,

	// 244 previously used, do not reuse
	CommandBaninfoAccount = 245,
	CommandBaninfoCharacter = 246,
	CommandBaninfoIp = 247,

	// 248 previously used, do not reuse
	CommandBanlistAccount = 249,
	CommandBanlistCharacter = 250,
	CommandBanlistIp = 251,

	// 252 previously used, do not reuse
	CommandUnbanAccount = 253,
	CommandUnbanCharacter = 254,
	CommandUnbanIp = 255,
	CommandUnbanPlayeraccount = 256,

	// 257 previously used, do not reuse
	CommandBfStart = 258,
	CommandBfStop = 259,
	CommandBfSwitch = 260,
	CommandBfTimer = 261,
	CommandBfEnable = 262,
	CommandAccountEmail = 263,

	// 264 previously used, do not reuse
	CommandAccountSetSecEmail = 265,
	CommandAccountSetSecRegmail = 266,
	CommandCast = 267,
	CommandCastBack = 268,
	CommandCastDist = 269,
	CommandCastSelf = 270,
	CommandCastTarget = 271,
	CommandCastDest = 272,

	// 273 previously used, do not reuse
	CommandCharacterCustomize = 274,
	CommandCharacterChangefaction = 275,
	CommandCharacterChangerace = 276,

	// 277 previously used, do not reuse
	CommandCharacterDeletedDelete = 278,
	CommandCharacterDeletedList = 279,
	CommandCharacterDeletedRestore = 280,
	CommandCharacterDeletedOld = 281,
	CommandCharacterErase = 282,
	CommandCharacterLevel = 283,
	CommandCharacterRename = 284,
	CommandCharacterReputation = 285,
	CommandCharacterTitles = 286,
	CommandLevelup = 287,

	// 288 previously used, do not reuse
	CommandPdumpLoad = 289,
	CommandPdumpWrite = 290,

	// 291 previously used, do not reuse
	CommandCheatCasttime = 292,
	CommandCheatCooldown = 293,
	CommandCheatExplore = 294,
	CommandCheatGod = 295,
	CommandCheatPower = 296,
	CommandCheatStatus = 297,
	CommandCheatTaxi = 298,
	CommandCheatWaterwalk = 299,
	CommandDebug = 300,

	// 301-342 previously used, do not reuse
	CommandDeserterBgAdd = 343,
	CommandDeserterBgRemove = 344,

	// 345 previously used, do not reuse
	CommandDeserterInstanceAdd = 346,
	CommandDeserterInstanceRemove = 347,

	// 348-349 previously used, do not reuse
	CommandDisableAddCriteria = 350,
	CommandDisableAddBattleground = 351,
	CommandDisableAddMap = 352,
	CommandDisableAddMmap = 353,
	CommandDisableAddOutdoorpvp = 354,
	CommandDisableAddQuest = 355,
	CommandDisableAddSpell = 356,
	CommandDisableAddVmap = 357,

	// 358 previously used, do not reuse
	CommandDisableRemoveCriteria = 359,
	CommandDisableRemoveBattleground = 360,
	CommandDisableRemoveMap = 361,
	CommandDisableRemoveMmap = 362,
	CommandDisableRemoveOutdoorpvp = 363,
	CommandDisableRemoveQuest = 364,
	CommandDisableRemoveSpell = 365,
	CommandDisableRemoveVmap = 366,
	CommandEventInfo = 367,
	CommandEventActivelist = 368,
	CommandEventStart = 369,
	CommandEventStop = 370,
	CommandGm = 371,
	CommandGmChat = 372,
	CommandGmFly = 373,
	CommandGmIngame = 374,
	CommandGmList = 375,
	CommandGmVisible = 376,
	CommandGo = 377,
	CommandAccount2Fa = 378,
	CommandAccount2FaSetup = 379,
	CommandAccount2FaRemove = 380,
	CommandAccountSet2Fa = 381,

	//                                                       = 382, // DEPRECATED: DON'T REUSE
	//                                                       = 383, // DEPRECATED: DON'T REUSE
	//                                                       = 384, // DEPRECATED: DON'T REUSE
	//                                                       = 385, // DEPRECATED: DON'T REUSE
	//                                                       = 386, // DEPRECATED: DON'T REUSE
	// 387 previously used, do not reuse
	CommandGobjectActivate = 388,
	CommandGobjectAdd = 389,
	CommandGobjectAddTemp = 390,
	CommandGobjectDelete = 391,
	CommandGobjectInfo = 392,
	CommandGobjectMove = 393,
	CommandGobjectNear = 394,

	// 395 previously used, do not reuse
	CommandGobjectSetPhase = 396,
	CommandGobjectSetState = 397,
	CommandGobjectTarget = 398,
	CommandGobjectTurn = 399,

	// 400 previously used, do not reuse
	CommandGuild = 401,
	CommandGuildCreate = 402,
	CommandGuildDelete = 403,
	CommandGuildInvite = 404,
	CommandGuildUninvite = 405,
	CommandGuildRank = 406,
	CommandGuildRename = 407,

	// 408 previously used, do not reuse
	CommandHonorAdd = 409,
	CommandHonorAddKill = 410,
	CommandHonorUpdate = 411,

	// 412 previously used, do not reuse
	CommandInstanceListbinds = 413,
	CommandInstanceUnbind = 414,
	CommandInstanceStats = 415,

	// 416 previously used, do not reuse
	CommandLearn = 417,

	// 418 previously used, do not reuse
	CommandLearnAllMy = 419,
	CommandLearnAllMyClass = 420,
	CommandLearnMyPetTalents = 421,
	CommandLearnAllMySpells = 422,
	CommandLearnAllTalents = 423,
	CommandLearnAllGm = 424,
	CommandLearnAllCrafts = 425,
	CommandLearnAllDefault = 426,
	CommandLearnAllLang = 427,
	CommandLearnAllRecipes = 428,
	CommandUnlearn = 429,

	// 430 previously used, do not reuse
	CommandLfgPlayer = 431,
	CommandLfgGroup = 432,
	CommandLfgQueue = 433,
	CommandLfgClean = 434,
	CommandLfgOptions = 435,

	// 436 previously used, do not reuse
	CommandListCreature = 437,
	CommandListItem = 438,
	CommandListObject = 439,
	CommandListAuras = 440,
	CommandListMail = 441,
	CommandLookup = 442,
	CommandLookupArea = 443,
	CommandLookupCreature = 444,
	CommandLookupEvent = 445,
	CommandLookupFaction = 446,
	CommandLookupItem = 447,
	CommandLookupItemset = 448,
	CommandLookupObject = 449,
	CommandLookupQuest = 450,
	CommandLookupPlayer = 451,
	CommandLookupPlayerIp = 452,
	CommandLookupPlayerAccount = 453,
	CommandLookupPlayerEmail = 454,
	CommandLookupSkill = 455,
	CommandLookupSpell = 456,
	CommandLookupSpellId = 457,
	CommandLookupTaxinode = 458,
	CommandLookupTele = 459,
	CommandLookupTitle = 460,
	CommandLookupMap = 461,
	CommandAnnounce = 462,
	CommandChannel = 463,
	CommandChannelSet = 464,
	CommandChannelSetOwnership = 465,
	CommandGmannounce = 466,
	CommandGmnameannounce = 467,
	CommandGmnotify = 468,
	CommandNameannounce = 469,
	CommandNotify = 470,
	CommandWhispers = 471,
	CommandGroup = 472,
	CommandGroupLeader = 473,
	CommandGroupDisband = 474,
	CommandGroupRemove = 475,
	CommandGroupJoin = 476,
	CommandGroupList = 477,
	CommandGroupSummon = 478,
	CommandPet = 479,
	CommandPetCreate = 480,
	CommandPetLearn = 481,
	CommandPetUnlearn = 482,
	CommandSend = 483,
	CommandSendItems = 484,
	CommandSendMail = 485,
	CommandSendMessage = 486,
	CommandSendMoney = 487,
	CommandAdditem = 488,
	CommandAdditemset = 489,
	CommandAppear = 490,
	CommandAura = 491,
	CommandBank = 492,
	CommandBindsight = 493,
	CommandCombatstop = 494,
	CommandCometome = 495,
	CommandCommands = 496,
	CommandCooldown = 497,
	CommandDamage = 498,
	CommandDev = 499,
	CommandDie = 500,
	CommandDismount = 501,
	CommandDistance = 502,
	CommandFlusharenapoints = 503,
	CommandFreeze = 504,
	CommandGps = 505,
	CommandGuid = 506,
	CommandHelp = 507,
	CommandHidearea = 508,
	CommandItemmove = 509,
	CommandKick = 510,
	CommandLinkgrave = 511,
	CommandListfreeze = 512,
	CommandMaxskill = 513,
	CommandMovegens = 514,
	CommandMute = 515,
	CommandNeargrave = 516,
	CommandPinfo = 517,
	CommandPlayall = 518,
	CommandPossess = 519,
	CommandRecall = 520,
	CommandRepairitems = 521,
	CommandRespawn = 522,
	CommandRevive = 523,
	CommandSaveall = 524,
	CommandSave = 525,
	CommandSetskill = 526,
	CommandShowarea = 527,
	CommandSummon = 528,
	CommandUnaura = 529,
	CommandUnbindsight = 530,
	CommandUnfreeze = 531,
	CommandUnmute = 532,
	CommandUnpossess = 533,
	CommandUnstuck = 534,
	CommandWchange = 535,
	CommandMmap = 536,
	CommandMmapLoadedtiles = 537,
	CommandMmapLoc = 538,
	CommandMmapPath = 539,
	CommandMmapStats = 540,
	CommandMmapTestarea = 541,
	CommandMorph = 542,
	CommandDemorph = 543,
	CommandModify = 544,
	CommandModifyArenapoints = 545,
	CommandModifyBit = 546, // DEPRECATED: DON'T REUSE
	CommandModifyDrunk = 547,
	CommandModifyEnergy = 548,
	CommandModifyFaction = 549,
	CommandModifyGender = 550,
	CommandModifyHonor = 551,
	CommandModifyHp = 552,
	CommandModifyMana = 553,
	CommandModifyMoney = 554,
	CommandModifyMount = 555,
	CommandModifyPhase = 556,
	CommandModifyRage = 557,
	CommandModifyReputation = 558,
	CommandModifyRunicpower = 559,
	CommandModifyScale = 560,
	CommandModifySpeed = 561,
	CommandModifySpeedAll = 562,
	CommandModifySpeedBackwalk = 563,
	CommandModifySpeedFly = 564,
	CommandModifySpeedWalk = 565,
	CommandModifySpeedSwim = 566,
	CommandModifySpell = 567,
	CommandModifyStandstate = 568,
	CommandModifyTalentpoints = 569,

	// 570 previously used, do not reuse
	CommandNpcAdd = 571,
	CommandNpcAddFormation = 572,
	CommandNpcAddItem = 573,
	CommandNpcAddMove = 574,
	CommandNpcAddTemp = 575,
	CommandNpcDelete = 576,
	CommandNpcDeleteItem = 577,
	CommandNpcFollow = 578,
	CommandNpcFollowStop = 579,
	CommandNpcSet = 580,
	CommandNpcSetAllowmove = 581,
	CommandNpcSetEntry = 582,
	CommandNpcSetFactionid = 583,
	CommandNpcSetFlag = 584,
	CommandNpcSetLevel = 585,
	CommandNpcSetLink = 586,
	CommandNpcSetModel = 587,
	CommandNpcSetMovetype = 588,
	CommandNpcSetPhase = 589,
	CommandNpcSetSpawndist = 590,
	CommandNpcSetSpawntime = 591,
	CommandNpcSetData = 592,
	CommandNpcInfo = 593,
	CommandNpcNear = 594,
	CommandNpcMove = 595,
	CommandNpcPlayemote = 596,
	CommandNpcSay = 597,
	CommandNpcTextemote = 598,
	CommandNpcWhisper = 599,
	CommandNpcYell = 600,
	CommandNpcTame = 601,
	CommandQuest = 602,
	CommandQuestAdd = 603,
	CommandQuestComplete = 604,
	CommandQuestRemove = 605,
	CommandQuestReward = 606,
	CommandReload = 607,
	CommandReloadAccessRequirement = 608,
	CommandReloadCriteriaData = 609,
	CommandReloadAchievementReward = 610,
	CommandReloadAll = 611,
	CommandReloadAllAchievement = 612,
	CommandReloadAllArea = 613,
	CommandReloadBroadcastText = 614,
	CommandReloadAllGossip = 615,
	CommandReloadAllItem = 616,
	CommandReloadAllLocales = 617,
	CommandReloadAllLoot = 618,
	CommandReloadAllNpc = 619,
	CommandReloadAllQuest = 620,
	CommandReloadAllScripts = 621,
	CommandReloadAllSpell = 622,
	CommandReloadAreatriggerInvolvedrelation = 623,
	CommandReloadAreatriggerTavern = 624,
	CommandReloadAreatriggerTeleport = 625,
	CommandReloadAuctions = 626,
	CommandReloadAutobroadcast = 627,

	// 628 previously used, do not reuse
	CommandReloadConditions = 629,
	CommandReloadConfig = 630,
	CommandReloadBattlegroundTemplate = 631,
	CommandMutehistory = 632,
	CommandReloadCreatureLinkedRespawn = 633,
	CommandReloadCreatureLootTemplate = 634,
	CommandReloadCreatureOnkillReputation = 635,
	CommandReloadCreatureQuestender = 636,
	CommandReloadCreatureQueststarter = 637,
	CommandReloadCreatureSummonGroups = 638,
	CommandReloadCreatureTemplate = 639,
	CommandReloadCreatureText = 640,
	CommandReloadDisables = 641,
	CommandReloadDisenchantLootTemplate = 642,
	CommandReloadEventScripts = 643,
	CommandReloadFishingLootTemplate = 644,
	CommandReloadGraveyardZone = 645,
	CommandReloadGameTele = 646,
	CommandReloadGameobjectQuestender = 647,
	CommandReloadGameobjectQuestLootTemplate = 648,
	CommandReloadGameobjectQueststarter = 649,
	CommandReloadSupportSystem = 650,
	CommandReloadGossipMenu = 651,
	CommandReloadGossipMenuOption = 652,
	CommandReloadItemRandomBonusListTemplate = 653,
	CommandReloadItemLootTemplate = 654,
	CommandReloadItemSetNames = 655,
	CommandReloadLfgDungeonRewards = 656,
	CommandReloadAchievementRewardLocale = 657,
	CommandReloadCreatureTemplateLocale = 658,
	CommandReloadCreatureTextLocale = 659,
	CommandReloadGameobjectTemplateLocale = 660,
	CommandReloadGossipMenuOptionLocale = 661,
	CommandReloadItemTemplateLocale = 662, // Deprecated Since Draenor Don'T Reus
	CommandReloadItemSetNameLocale = 663,
	CommandReloadNpcTextLocale = 664, // Deprecated Since Draenor Don'T Reuse
	CommandReloadPageTextLocale = 665,
	CommandReloadPointsOfInterestLocale = 666,
	CommandReloadQuestTemplateLocale = 667,
	CommandReloadMailLevelReward = 668,
	CommandReloadMailLootTemplate = 669,
	CommandReloadMillingLootTemplate = 670,
	CommandReloadNpcSpellclickSpells = 671,
	CommandReloadTrainer = 672,
	CommandReloadNpcVendor = 673,
	CommandReloadPageText = 674,
	CommandReloadPickpocketingLootTemplate = 675,
	CommandReloadPointsOfInterest = 676,
	CommandReloadProspectingLootTemplate = 677,
	CommandReloadQuestPoi = 678,
	CommandReloadQuestTemplate = 679,
	CommandReloadRbac = 680,
	CommandReloadReferenceLootTemplate = 681,
	CommandReloadReservedName = 682,
	CommandReloadReputationRewardRate = 683,
	CommandReloadSpilloverTemplate = 684,
	CommandReloadSkillDiscoveryTemplate = 685,
	CommandReloadSkillExtraItemTemplate = 686,
	CommandReloadSkillFishingBaseLevel = 687,
	CommandReloadSkinningLootTemplate = 688,
	CommandReloadSmartScripts = 689,
	CommandReloadSpellRequired = 690,
	CommandReloadSpellArea = 691,
	CommandReloadSpellBonusData = 692, // Deprecated Since Draenor Don'T Reuse
	CommandReloadSpellGroup = 693,
	CommandReloadSpellLearnSpell = 694,
	CommandReloadSpellLootTemplate = 695,
	CommandReloadSpellLinkedSpell = 696,
	CommandReloadSpellPetAuras = 697,
	CommandCharacterChangeaccount = 698,
	CommandReloadSpellProc = 699,
	CommandReloadSpellScripts = 700,
	CommandReloadSpellTargetPosition = 701,
	CommandReloadSpellThreats = 702,
	CommandReloadSpellGroupStackRules = 703,
	CommandReloadCypherString = 704,

	// 705 previously used, do not reuse
	CommandReloadWaypointScripts = 706,
	CommandReloadWaypointData = 707,
	CommandReloadVehicleAccesory = 708,
	CommandReloadVehicleTemplateAccessory = 709,
	CommandReset = 710,
	CommandResetAchievements = 711,
	CommandResetHonor = 712,
	CommandResetLevel = 713,
	CommandResetSpells = 714,
	CommandResetStats = 715,
	CommandResetTalents = 716,
	CommandResetAll = 717,
	CommandServer = 718,
	CommandServerCorpses = 719,
	CommandServerExit = 720,
	CommandServerIdlerestart = 721,
	CommandServerIdlerestartCancel = 722,
	CommandServerIdleshutdown = 723,
	CommandServerIdleshutdownCancel = 724,
	CommandServerInfo = 725,
	CommandServerPlimit = 726,
	CommandServerRestart = 727,
	CommandServerRestartCancel = 728,
	CommandServerSet = 729,
	CommandServerSetClosed = 730,
	CommandServerSetDifftime = 731,
	CommandServerSetLoglevel = 732,
	CommandServerSetMotd = 733,
	CommandServerShutdown = 734,
	CommandServerShutdownCancel = 735,
	CommandServerMotd = 736,
	CommandTele = 737,
	CommandTeleAdd = 738,
	CommandTeleDel = 739,
	CommandTeleName = 740,
	CommandTeleGroup = 741,
	CommandTicket = 742,
	CommandTicketAssign = 743,        // Deprecated Since Draenor Don'T Reuse
	CommandTicketClose = 744,         // Deprecated Since Draenor Don'T Reuse
	CommandTicketClosedlist = 745,    // Deprecated Since Draenor Don'T Reuse
	CommandTicketComment = 746,       // Deprecated Since Draenor Don'T Reuse
	CommandTicketComplete = 747,      // Deprecated Since Draenor Don'T Reuse
	CommandTicketDelete = 748,        // Deprecated Since Draenor Don'T Reuse
	CommandTicketEscalate = 749,      // Deprecated Since Draenor Don'T Reuse
	CommandTicketEscalatedlist = 750, // Deprecated Since Draenor Don'T Reuse
	CommandTicketList = 751,          // Deprecated Since Draenor Don'T Reuse
	CommandTicketOnlinelist = 752,    // Deprecated Since Draenor Don'T Reuse
	CommandTicketReset = 753,
	CommandTicketResponse = 754,         // Deprecated Since Draenor Don'T Reuse
	CommandTicketResponseAppend = 755,   // Deprecated Since Draenor Don'T Reuse
	CommandTicketResponseAppendln = 756, // Deprecated Since Draenor Don'T Reuse
	CommandTicketTogglesystem = 757,
	CommandTicketUnassign = 758, // Deprecated Since Draenor Don'T Reuse
	CommandTicketViewid = 759,   // Deprecated Since Draenor Don'T Reuse
	CommandTicketViewname = 760, // Deprecated Since Draenor Don'T Reuse

	// 761 previously used, do not reuse
	CommandTitlesAdd = 762,
	CommandTitlesCurrent = 763,
	CommandTitlesRemove = 764,

	// 765 previously used, do not reuse
	CommandTitlesSetMask = 766,
	CommandWp = 767,
	CommandWpAdd = 768,
	CommandWpEvent = 769,
	CommandWpLoad = 770,
	CommandWpModify = 771,
	CommandWpUnload = 772,
	CommandWpReload = 773,
	CommandWpShow = 774,
	CommandModifyCurrency = 775,

	// 776 previously used, do not reuse
	CommandMailbox = 777,

	// 778 previously used, do not reuse
	CommandAhbotItems = 779,
	CommandAhbotItemsGray = 780,
	CommandAhbotItemsWhite = 781,
	CommandAhbotItemsGreen = 782,
	CommandAhbotItemsBlue = 783,
	CommandAhbotItemsPurple = 784,
	CommandAhbotItemsOrange = 785,
	CommandAhbotItemsYellow = 786,
	CommandAhbotRatio = 787,
	CommandAhbotRatioAlliance = 788,
	CommandAhbotRatioHorde = 789,
	CommandAhbotRatioNeutral = 790,
	CommandAhbotRebuild = 791,
	CommandAhbotReload = 792,
	CommandAhbotStatus = 793,
	CommandGuildInfo = 794,
	CommandInstanceSetBossState = 795,
	CommandInstanceGetBossState = 796,
	CommandPvpstats = 797,
	CommandModifyXp = 798,

	//                                                       = 799, // DEPRECATED: DON'T REUSE
	//                                                       = 800, // DEPRECATED: DON'T REUSE
	//                                                       = 801, // DEPRECATED: DON'T REUSE
	CommandTicketBug = 802,
	CommandTicketComplaint = 803,
	CommandTicketSuggestion = 804,
	CommandTicketBugAssign = 805,
	CommandTicketBugClose = 806,
	CommandTicketBugClosedlist = 807,
	CommandTicketBugComment = 808,
	CommandTicketBugDelete = 809,
	CommandTicketBugList = 810,
	CommandTicketBugUnassign = 811,
	CommandTicketBugView = 812,
	CommandTicketComplaintAssign = 813,
	CommandTicketComplaintClose = 814,
	CommandTicketComplaintClosedlist = 815,
	CommandTicketComplaintComment = 816,
	CommandTicketComplaintDelete = 817,
	CommandTicketComplaintList = 818,
	CommandTicketComplaintUnassign = 819,
	CommandTicketComplaintView = 820,
	CommandTicketSuggestionAssign = 821,
	CommandTicketSuggestionClose = 822,
	CommandTicketSuggestionClosedlist = 823,
	CommandTicketSuggestionComment = 824,
	CommandTicketSuggestionDelete = 825,
	CommandTicketSuggestionList = 826,
	CommandTicketSuggestionUnassign = 827,
	CommandTicketSuggestionView = 828,
	CommandTicketResetAll = 829,
	CommandBnetAccountListGameAccounts = 830,
	CommandTicketResetBug = 831,
	CommandTicketResetComplaint = 832,
	CommandTicketResetSuggestion = 833,

	//                                                       = 834, // DEPRECATED: DON'T REUSE
	// 835-836 previously used, do not reuse
	CommandNpcEvade = 837,
	CommandPetLevel = 838,
	CommandServerShutdownForce = 839,
	CommandServerRestartForce = 840,
	CommandNearGraveyard = 841,
	CommandReloadCharacterTemplate = 842,
	CommandReloadQuestGreeting = 843,
	CommandScene = 844,
	CommandSceneDebug = 845,
	CommandScenePlay = 846,
	CommandScenePlayPackage = 847,
	CommandSceneCancel = 848,
	CommandListScenes = 849,
	CommandReloadSceneTemplate = 850,
	CommandReloadAreatriggerTemplate = 851,

	// 852 previously used, do not reuse
	CommandReloadConversationTemplate = 853,

	// 854-855 previously used, do not reuse
	CommandNpcSpawngroup = 856,
	CommandNpcDespawngroup = 857,
	CommandGobjectSpawngroup = 858,
	CommandGobjectDespawngroup = 859,
	CommandListRespawns = 860,
	CommandGroupSet = 861,
	CommandGroupAssistant = 862,
	CommandGroupMaintank = 863,
	CommandGroupMainassist = 864,
	CommandNpcShowloot = 865,
	CommandListSpawnpoints = 866,
	CommandReloadQuestGreetingLocale = 867, // Reserved
	CommandModifyPower = 868,

	// 869 previously used, do not reuse
	// 870-871 previously used, do not reuse
	CommandServerDebug = 872,
	CommandReloadCreatureMovementOverride = 873,

	// 874 previously used, do not reuse
	CommandLookupMapId = 875,
	CommandLookupItemId = 876,
	CommandLookupQuestId = 877,

	// 878-879 previously used, do not reuse
	CommandPdumpCopy = 880,
	CommandReloadVehicleTemplate = 881,
	CommandReloadSpellScriptNames = 882,
	CommandQuestObjectiveComplete = 883,

	// Custom Permissions 1000+
	Max
}