﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants
{
    public struct SkillConst
    {
        public const int MaxPlayerSkills = 256;
        public const uint MaxSkillStep = 15;
    }

    public enum SkillType
    {
        None = 0,

        Swords = 43,
        Axes = 44,
        Bows = 45,
        Guns = 46,
        Maces = 54,
        TwoHandedSwords = 55,
        Defense = 95,
        LanguageCommon = 98,
        RacialDwarf = 101,
        LanguageOrcish = 109,
        LanguageDwarven = 111,
        LanguageDarnassian = 113,
        LanguageTaurahe = 115,
        DualWield = 118,
        RacialTauren = 124,
        RacialOrc = 125,
        RacialNightElf = 126,
        Staves = 136,
        LanguageThalassian = 137,
        LanguageDraconic = 138,
        LanguageDemonTongue = 139,
        LanguageTitan = 140,
        LanguageOldTongue = 141,
        Survival = 142,
        HorseRiding = 148,
        WolfRiding = 149,
        TigerRiding = 150,
        RamRiding = 152,
        Swimming = 155,
        TwoHandedMaces = 160,
        Unarmed = 162,
        Blacksmithing = 164,
        Leatherworking = 165,
        Alchemy = 171,
        TwoHandedAxes = 172,
        Daggers = 173,
        Herbalism = 182,
        GenericDnd = 183,
        Cooking = 185,
        Mining = 186,
        PetImp = 188,
        PetFelhunter = 189,
        Tailoring = 197,
        Engineering = 202,
        PetSpider = 203,
        PetVoidwalker = 204,
        PetSuccubus = 205,
        PetInfernal = 206,
        PetDoomguard = 207,
        PetWolf = 208,
        PetCat = 209,
        PetBear = 210,
        PetBoar = 211,
        PetCrocolisk = 212,
        PetCarrionBird = 213,
        PetCrab = 214,
        PetGorilla = 215,
        PetRaptor = 217,
        PetTallstrider = 218,
        RacialUndead = 220,
        Crossbows = 226,
        Wands = 228,
        Polearms = 229,
        PetScorpid = 236,
        PetTurtle = 251,
        PetGenericHunter = 270,
        PlateMail = 293,
        LanguageGnomish = 313,
        LanguageTroll = 315,
        Enchanting = 333,
        Fishing = 356,
        Skinning = 393,
        Mail = 413,
        Leather = 414,
        Cloth = 415,
        Shield = 433,
        FistWeapons = 473,
        RaptorRiding = 533,
        MechanostriderPiloting = 553,
        UndeadHorsemanship = 554,
        PetBat = 653,
        PetHyena = 654,
        PetBirdOfPrey = 655,
        PetWindSerpent = 656,
        LanguageForsaken = 673,
        KodoRiding = 713,
        RacialTroll = 733,
        RacialGnome = 753,
        RacialHuman = 754,
        Jewelcrafting = 755,
        RacialBloodElf = 756,
        PetEventRemoteControl = 758,
        LanguageDraenei = 759,
        RacialDraenei = 760,
        PetFelguard = 761,
        Riding = 762,
        PetDragonhawk = 763,
        PetNetherRay = 764,
        PetSporebat = 765,
        PetWarpStalker = 766,
        PetRavager = 767,
        PetSerpent = 768,
        Internal = 769,
        Inscription = 773,
        PetMoth = 775,
        Mounts = 777,
        Companions = 778,
        PetExoticChimaera = 780,
        PetExoticDevilsaur = 781,
        PetGhoul = 782,
        PetExoticSilithid = 783,
        PetExoticWorm = 784,
        PetWasp = 785,
        PetExoticClefthoof = 786,
        PetExoticCoreHound = 787,
        PetExoticSpiritBeast = 788,
        RacialWorgen = 789,
        RacialGoblin = 790,
        LanguageGilnean = 791,
        LanguageGoblin = 792,
        Archaeology = 794,
        Hunter = 795,
        DeathKnight = 796,
        Druid = 798,
        Paladin = 800,
        Priest = 804,
        PetWaterElemental = 805,
        PetFox = 808,
        AllGlyphs = 810,
        PetDog = 811,
        PetMonkey = 815,
        PetExoticShaleSpider = 817,
        Beetle = 818,
        AllGuildPerks = 821,
        PetHydra = 824,
        Monk = 829,
        Warrior = 840,
        Warlock = 849,
        RacialPandaren = 899,
        Mage = 904,
        LanguagePandarenNeutral = 905,
        Rogue = 921,
        Shaman = 924,
        FelImp = 927,
        Voidlord = 928,
        Shivarra = 929,
        Observer = 930,
        Wrathguard = 931,
        AllSpecializations = 934,
        Runeforging = 960,
        PetPrimalFireElemental = 962,
        PetPrimalEarthElemental = 963,
        WayOfTheGrill = 975,
        WayOfTheWok = 976,
        WayOfThePot = 977,
        WayOfTheSteamer = 978,
        WayOfTheOven = 979,
        WayOfTheBrew = 980,
        ApprenticeCooking = 981,
        JourneymanCookbook = 982,
        PetRodent = 983,
        PetCrane = 984,
        PetWaterStrider = 985,
        PetExoticQuilen = 986,
        PetGoat = 987,
        PetBasilisk = 988,
        NoPlayers = 999,
        PetDirehorn = 1305,
        PetPrimalStormElemental = 1748,
        PetWaterElementalMinorTalentVersion = 1777,
        PetRiverbeast = 1819,
        Unused = 1830,
        DemonHunter = 1848,
        Logging = 1945,
        PetTerrorguard = 1981,
        PetAbyssal = 1982,
        PetStag = 1993,
        TradingPost = 2000,
        Warglaives = 2152,
        PetMechanical = 2189,
        PetAbomination = 2216,
        PetOxen = 2279,
        PetScalehide = 2280,
        PetFeathermane = 2361,
        RacialNightborne = 2419,
        RacialHighmountainTauren = 2420,
        RacialLightforgedDraenei = 2421,
        RacialVoidElf = 2423,
        KulTiranBlacksmithing = 2437,
        LegionBlacksmithing = 2454,
        LanguageShalassian = 2464,
        LanguageThalassian2 = 2465,
        DraenorBlacksmithing = 2472,
        PandariaBlacksmithing = 2473,
        CataclysmBlacksmithing = 2474,
        NorthrendBlacksmithing = 2475,
        OutlandBlacksmithing = 2476,
        ClassicBlacksmithing = 2477,
        KulTiranAlchemy = 2478,
        LegionAlchemy = 2479,
        DraenorAlchemy = 2480,
        PandariaAlchemy = 2481,
        CataclysmAlchemy = 2482,
        NorthrendAlchemy = 2483,
        OutlandAlchemy = 2484,
        ClassicAlchemy = 2485,
        KulTiranEnchanting = 2486,
        LegionEnchanting = 2487,
        DraenorEnchanting = 2488,
        PandariaEnchanting = 2489,
        CataclysmEnchanting = 2491,
        NorthrendEnchanting = 2492,
        OutlandEnchanting = 2493,
        ClassicEnchanting = 2494,
        KulTiranEngineering = 2499,
        LegionEngineering = 2500,
        DraenorEngineering = 2501,
        PandariaEngineering = 2502,
        CataclysmEngineering = 2503,
        NorthrendEngineering = 2504,
        OutlandEngineering = 2505,
        ClassicEngineering = 2506,
        KulTiranInscription = 2507,
        LegionInscription = 2508,
        DraenorInscription = 2509,
        PandariaInscription = 2510,
        CataclysmInscription = 2511,
        NorthrendInscription = 2512,
        OutlandInscription = 2513,
        ClassicInscription = 2514,
        KulTiranJewelcrafting = 2517,
        LegionJewelcrafting = 2518,
        DraenorJewelcrafting = 2519,
        PandariaJewelcrafting = 2520,
        CataclysmJewelcrafting = 2521,
        NorthrendJewelcrafting = 2522,
        OutlandJewelcrafting = 2523,
        ClassicJewelcrafting = 2524,
        KulTiranLeatherworking = 2525,
        LegionLeatherworking = 2526,
        DraenorLeatherworking = 2527,
        PandariaLeatherworking = 2528,
        CataclysmLeatherworking = 2529,
        NorthrendLeatherworking = 2530,
        OutlandLeatherworking = 2531,
        ClassicLeatherworking = 2532,
        KulTiranTailoring = 2533,
        LegionTailoring = 2534,
        DraenorTailoring = 2535,
        PandariaTailoring = 2536,
        CataclysmTailoring = 2537,
        NorthrendTailoring = 2538,
        OutlandTailoring = 2539,
        ClassicTailoring = 2540,
        KulTiranCooking = 2541,
        LegionCooking = 2542,
        DraenorCooking = 2543,
        PandariaCooking = 2544,
        CataclysmCooking = 2545,
        NorthrendCooking = 2546,
        OutlandCooking = 2547,
        ClassicCooking = 2548,
        KulTiranHerbalism = 2549,
        LegionHerbalism = 2550,
        DraenorHerbalism = 2551,
        PandariaHerbalism = 2552,
        CataclysmHerbalism = 2553,
        NorthrendHerbalism = 2554,
        OutlandHerbalism = 2555,
        ClassicHerbalism = 2556,
        KulTiranSkinning = 2557,
        LegionSkinning = 2558,
        DraenorSkinning = 2559,
        PandariaSkinning = 2560,
        CataclysmSkinning = 2561,
        NorthrendSkinning = 2562,
        OutlandSkinning = 2563,
        ClassicSkinning = 2564,
        KulTiranMining = 2565,
        LegionMining = 2566,
        DraenorMining = 2567,
        PandariaMining = 2568,
        CataclysmMining = 2569,
        NorthrendMining = 2570,
        OutlandMining = 2571,
        ClassicMining = 2572,
        KulTiranFishing = 2585,
        LegionFishing = 2586,
        DraenorFishing = 2587,
        PandariaFishing = 2588,
        CataclysmFishing = 2589,
        NorthrendFishing = 2590,
        OutlandFishing = 2591,
        ClassicFishing = 2592,
        RacialDarkIronDwarf = 2597,
        RacialMagHarOrc = 2598,
        PetLizard = 2703,
        PetHorse = 2704,
        PetExoticPterrordax = 2705,
        PetToad = 2706,
        PetExoticKrolusk = 2707,
        SecondPetHunter = 2716,
        PetBloodBeast = 2719,
        JunkyardTinkering = 2720,
        RacialZandalariTroll = 2721,
        RacialKulTiran = 2723,
        AzeritePower = 2727,
        MountEquipement = 2734,
        ShadowlandsAlchemy = 2750,
        ShadowlandsBlacksmithing = 2751,
        ShadowlandsCooking = 2752,
        ShadowlandsEnchanting = 2753,
        ShadowlandsFishing = 2754,
        ShadowlandsEngineering = 2755,
        ShadowlandsInscription = 2756,
        ShadowlandsJewelcrafting = 2757,
        ShadowlandsLeatherworking = 2758,
        ShadowlandsTailoring = 2759,
        ShadowlandsHerbalism = 2760,
        ShadowlandsMining = 2761,
        ShadowlandsSkinning = 2762,
        RacialDarkIronDwarf2 = 2773,
        RacialMechagnome = 2774,
        RacialVulpera = 2775,
        LanguageVulpera = 2776,
        SoulCyphering = 2777,
        AbominableStitching = 2787,
        AscensionCrafting = 2791,
        PetMammoth = 2805,
        PetCourser = 2806,
        PetCamel = 2807,
        StygiaCrafting = 2811,
        LanguageCypher = 2817,
        PhotoformSynthesis = 2819,
        ArcanaManipulation = 2821,
        DragonIslesBlacksmithing = 2822,
        DragonIslesAlchemy = 2823,
        DragonIslesCoocking = 2824,
        DragonIslesEnchanting = 2825,
        DragonIslesFishing = 2826,
        DragonIslesEngineering = 2827,
        DragonIslesInscription = 2828,
        DragonIslesJewelcrafting = 2829,
        DragonIslesLeatherworking = 2830,
        DragonIslesTailoring = 2831,
        DragonIslesHerbalism = 2832,
        DragonIslesMining = 2833,
        DragonIslesSkining = 2834,
        Crafting = 2846,
        DragonIslesTuskarrFishingGear = 2847,
        PetLesserDragonkin = 2850,
    }

    public enum SkillState
    {
        Unchanged = 0,
        Changed = 1,
        New = 2,
        Deleted = 3
    }

    public enum SkillCategory : sbyte
    {
        Unk = 0,
        Attributes = 5,
        Weapon = 6,
        Class = 7,
        Armor = 8,
        Secondary = 9,
        Languages = 10,
        Profession = 11,
        Generic = 12
    }

    public enum SkillRangeType
    {
        Language,                                   // 300..300
        Level,                                      // 1..max skill for level
        Mono,                                       // 1..1, grey monolite bar
        Rank,                                       // 1..skill for known rank
        None                                        // 0..0 always
    }
}
