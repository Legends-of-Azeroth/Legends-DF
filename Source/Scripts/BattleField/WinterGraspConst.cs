﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.BattleFields;

internal static class WGConst
{
	public const uint MapId = 571; // Northrend

	public const byte MaxOutsideNpcs = 14;
	public const byte OutsideAllianceNpc = 7;
	public const byte MaxWorkshops = 6;

	#region Data

	public static BfWGCoordGY[] WGGraveYard =
	{
		new(5104.750f, 2300.940f, 368.579f, 0.733038f, 1329, WGGossipText.GYNE, TeamIds.Neutral), new(5099.120f, 3466.036f, 368.484f, 5.317802f, 1330, WGGossipText.GYNW, TeamIds.Neutral), new(4314.648f, 2408.522f, 392.642f, 6.268125f, 1333, WGGossipText.GYSE, TeamIds.Neutral), new(4331.716f, 3235.695f, 390.251f, 0.008500f, 1334, WGGossipText.GYSW, TeamIds.Neutral), new(5537.986f, 2897.493f, 517.057f, 4.819249f, 1285, WGGossipText.GYKeep, TeamIds.Neutral), new(5032.454f, 3711.382f, 372.468f, 3.971623f, 1331, WGGossipText.GYHorde, TeamIds.Horde), new(5140.790f, 2179.120f, 390.950f, 1.972220f, 1332, WGGossipText.GYAlliance, TeamIds.Alliance)
	};

	public static WorldStates[] ClockWorldState =
	{
		WorldStates.BattlefieldWgTimeBattleEnd, WorldStates.BattlefieldWgTimeNextBattle
	};

	public static uint[] WintergraspFaction =
	{
		1732, 1735, 35
	};

	public static Position WintergraspStalkerPos = new(4948.985f, 2937.789f, 550.5172f, 1.815142f);

	public static Position RelicPos = new(5440.379f, 2840.493f, 430.2816f, -1.832595f);
	public static Quaternion RelicRot = new(0.0f, 0.0f, -0.7933531f, 0.6087617f);

	//Destructible (Wall, Tower..)
	public static WintergraspBuildingSpawnData[] WGGameObjectBuilding =
	{
		// Wall (Not spawned in db)
		// Entry  WS      X          Y          Z           O                rX   rY   rZ             rW             Type
		new(190219, 3749, 5371.457f, 3047.472f, 407.5710f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.00000000f, WGGameObjectBuildingType.Wall), new(190220, 3750, 5331.264f, 3047.105f, 407.9228f, 0.05235888f, 0.0f, 0.0f, 0.026176450f, 0.99965730f, WGGameObjectBuildingType.Wall), new(191795, 3764, 5385.841f, 2909.490f, 409.7127f, 0.00872424f, 0.0f, 0.0f, 0.004362106f, 0.99999050f, WGGameObjectBuildingType.Wall), new(191796, 3772, 5384.452f, 2771.835f, 410.2704f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.00000000f, WGGameObjectBuildingType.Wall), new(191799, 3762, 5371.436f, 2630.610f, 408.8163f, 3.13285800f, 0.0f, 0.0f, 0.999990500f, 0.00436732f, WGGameObjectBuildingType.Wall), new(191800, 3766, 5301.838f, 2909.089f, 409.8661f, 0.00872424f, 0.0f, 0.0f, 0.004362106f, 0.99999050f, WGGameObjectBuildingType.Wall), new(191801, 3770, 5301.063f, 2771.411f, 409.9014f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.00000000f, WGGameObjectBuildingType.Wall), new(191802, 3751, 5280.197f, 2995.583f, 408.8249f, 1.61442800f, 0.0f, 0.0f, 0.722363500f, 0.69151360f, WGGameObjectBuildingType.Wall), new(191803, 3752, 5279.136f, 2956.023f, 408.6041f, 1.57079600f, 0.0f, 0.0f, 0.707106600f, 0.70710690f, WGGameObjectBuildingType.Wall), new(191804, 3767, 5278.685f, 2882.513f, 409.5388f, 1.57079600f, 0.0f, 0.0f, 0.707106600f, 0.70710690f, WGGameObjectBuildingType.Wall), new(191806, 3769, 5279.502f, 2798.945f, 409.9983f, 1.57079600f, 0.0f, 0.0f, 0.707106600f, 0.70710690f, WGGameObjectBuildingType.Wall), new(191807, 3759, 5279.937f, 2724.766f, 409.9452f, 1.56207000f, 0.0f, 0.0f, 0.704014800f, 0.71018530f, WGGameObjectBuildingType.Wall), new(191808, 3760, 5279.601f, 2683.786f, 409.8488f, 1.55334100f, 0.0f, 0.0f, 0.700908700f, 0.71325110f, WGGameObjectBuildingType.Wall), new(191809, 3761, 5330.955f, 2630.777f, 409.2826f, 3.13285800f, 0.0f, 0.0f, 0.999990500f, 0.00436732f, WGGameObjectBuildingType.Wall), new(190369, 3753, 5256.085f, 2933.963f, 409.3571f, 3.13285800f, 0.0f, 0.0f, 0.999990500f, 0.00436732f, WGGameObjectBuildingType.Wall), new(190370, 3758, 5257.463f, 2747.327f, 409.7427f, -3.13285800f, 0.0f, 0.0f, -0.999990500f, 0.00436732f, WGGameObjectBuildingType.Wall), new(190371, 3754, 5214.960f, 2934.089f, 409.1905f, -0.00872424f, 0.0f, 0.0f, -0.004362106f, 0.99999050f, WGGameObjectBuildingType.Wall), new(190372, 3757, 5215.821f, 2747.566f, 409.1884f, -3.13285800f, 0.0f, 0.0f, -0.999990500f, 0.00436732f, WGGameObjectBuildingType.Wall), new(190374, 3755, 5162.273f, 2883.043f, 410.2556f, 1.57952200f, 0.0f, 0.0f, 0.710185100f, 0.70401500f, WGGameObjectBuildingType.Wall), new(190376, 3756, 5163.724f, 2799.838f, 409.2270f, 1.57952200f, 0.0f, 0.0f, 0.710185100f, 0.70401500f, WGGameObjectBuildingType.Wall),

		// Tower of keep (Not spawned in db)
		new(190221, 3711, 5281.154f, 3044.588f, 407.8434f, 3.115388f, 0.0f, 0.0f, 0.9999142f, 0.013101960f, WGGameObjectBuildingType.KeepTower),   // NW
		new(190373, 3713, 5163.757f, 2932.228f, 409.1904f, 3.124123f, 0.0f, 0.0f, 0.9999619f, 0.008734641f, WGGameObjectBuildingType.KeepTower),   // SW
		new(190377, 3714, 5166.397f, 2748.368f, 409.1884f, -1.570796f, 0.0f, 0.0f, -0.7071066f, 0.707106900f, WGGameObjectBuildingType.KeepTower), // SE
		new(190378, 3712, 5281.192f, 2632.479f, 409.0985f, -1.588246f, 0.0f, 0.0f, -0.7132492f, 0.700910500f, WGGameObjectBuildingType.KeepTower), // NE

		// Wall (with passage) (Not spawned in db)
		new(191797, 3765, 5343.290f, 2908.860f, 409.5757f, 0.00872424f, 0.0f, 0.0f, 0.004362106f, 0.9999905f, WGGameObjectBuildingType.Wall), new(191798, 3771, 5342.719f, 2771.386f, 409.6249f, 3.14159300f, 0.0f, 0.0f, -1.000000000f, 0.0000000f, WGGameObjectBuildingType.Wall), new(191805, 3768, 5279.126f, 2840.797f, 409.7826f, 1.57952200f, 0.0f, 0.0f, 0.710185100f, 0.7040150f, WGGameObjectBuildingType.Wall),

		// South tower (Not spawned in db)
		new(190356, 3704, 4557.173f, 3623.943f, 395.8828f, 1.675516f, 0.0f, 0.0f, 0.7431450f, 0.669130400f, WGGameObjectBuildingType.Tower),   // W
		new(190357, 3705, 4398.172f, 2822.497f, 405.6270f, -3.124123f, 0.0f, 0.0f, -0.9999619f, 0.008734641f, WGGameObjectBuildingType.Tower), // S
		new(190358, 3706, 4459.105f, 1944.326f, 434.9912f, -2.002762f, 0.0f, 0.0f, -0.8422165f, 0.539139500f, WGGameObjectBuildingType.Tower), // E

		// Door of forteress (Not spawned in db)
		new(WGGameObjects.FortressGate, 3763, 5162.991f, 2841.232f, 410.1892f, -3.132858f, 0.0f, 0.0f, -0.9999905f, 0.00436732f, WGGameObjectBuildingType.Door),

		// Last door (Not spawned in db)
		new(WGGameObjects.VaultGate, 3773, 5397.108f, 2841.54f, 425.9014f, 3.141593f, 0.0f, 0.0f, -1.0f, 0.0f, WGGameObjectBuildingType.DoorLast)
	};

	public static StaticWintergraspTowerInfo[] TowerData =
	{
		new(WintergraspTowerIds.FortressNW, WintergraspText.NwKeeptowerDamage, WintergraspText.NwKeeptowerDestroy), new(WintergraspTowerIds.FortressSW, WintergraspText.SwKeeptowerDamage, WintergraspText.SwKeeptowerDestroy), new(WintergraspTowerIds.FortressSE, WintergraspText.SeKeeptowerDamage, WintergraspText.SeKeeptowerDestroy), new(WintergraspTowerIds.FortressNE, WintergraspText.NeKeeptowerDamage, WintergraspText.NeKeeptowerDestroy), new(WintergraspTowerIds.Shadowsight, WintergraspText.WesternTowerDamage, WintergraspText.WesternTowerDestroy), new(WintergraspTowerIds.WintersEdge, WintergraspText.SouthernTowerDamage, WintergraspText.SouthernTowerDestroy), new(WintergraspTowerIds.Flamewatch, WintergraspText.EasternTowerDamage, WintergraspText.EasternTowerDestroy)
	};

	public static Position[] WGTurret =
	{
		new(5391.19f, 3060.8f, 419.616f, 1.69557f), new(5266.75f, 2976.5f, 421.067f, 3.20354f), new(5234.86f, 2948.8f, 420.88f, 1.61311f), new(5323.05f, 2923.7f, 421.645f, 1.5817f), new(5363.82f, 2923.87f, 421.709f, 1.60527f), new(5264.04f, 2861.34f, 421.587f, 3.21142f), new(5264.68f, 2819.78f, 421.656f, 3.15645f), new(5322.16f, 2756.69f, 421.646f, 4.69978f), new(5363.78f, 2756.77f, 421.629f, 4.78226f), new(5236.2f, 2732.68f, 421.649f, 4.72336f), new(5265.02f, 2704.63f, 421.7f, 3.12507f), new(5350.87f, 2616.03f, 421.243f, 4.72729f), new(5390.95f, 2615.5f, 421.126f, 4.6409f), new(5148.8f, 2820.24f, 421.621f, 3.16043f), new(5147.98f, 2861.93f, 421.63f, 3.18792f)
	};

	public static WintergraspGameObjectData[] WGPortalDefenderData =
	{
		// Player teleporter
		new(5153.408f, 2901.349f, 409.1913f, -0.06981169f, 0.0f, 0.0f, -0.03489876f, 0.9993908f, 190763, 191575), new(5268.698f, 2666.421f, 409.0985f, -0.71558490f, 0.0f, 0.0f, -0.35020730f, 0.9366722f, 190763, 191575), new(5197.050f, 2944.814f, 409.1913f, 2.33874000f, 0.0f, 0.0f, 0.92050460f, 0.3907318f, 190763, 191575), new(5196.671f, 2737.345f, 409.1892f, -2.93213900f, 0.0f, 0.0f, -0.99452110f, 0.1045355f, 190763, 191575), new(5314.580f, 3055.852f, 408.8620f, 0.54105060f, 0.0f, 0.0f, 0.26723770f, 0.9636307f, 190763, 191575), new(5391.277f, 2828.094f, 418.6752f, -2.16420600f, 0.0f, 0.0f, -0.88294700f, 0.4694727f, 190763, 191575), new(5153.931f, 2781.671f, 409.2455f, 1.65806200f, 0.0f, 0.0f, 0.73727700f, 0.6755905f, 190763, 191575), new(5311.445f, 2618.931f, 409.0916f, -2.37364400f, 0.0f, 0.0f, -0.92718320f, 0.3746083f, 190763, 191575), new(5269.208f, 3013.838f, 408.8276f, -1.76278200f, 0.0f, 0.0f, -0.77162460f, 0.6360782f, 190763, 191575), new(5401.634f, 2853.667f, 418.6748f, 2.63544400f, 0.0f, 0.0f, 0.96814730f, 0.2503814f, 192819, 192819), // return portal inside fortress, neutral

		// Vehicle teleporter
		new(5314.515f, 2703.687f, 408.5502f, -0.89011660f, 0.0f, 0.0f, -0.43051050f, 0.9025856f, 192951, 192951), new(5316.252f, 2977.042f, 408.5385f, -0.82030330f, 0.0f, 0.0f, -0.39874840f, 0.9170604f, 192951, 192951)
	};

	public static WintergraspTowerData[] AttackTowers =
	{
		//West Tower
		new()
		{
			towerEntry = 190356,
			GameObject = new WintergraspGameObjectData[]
			{
				new(4559.113f, 3606.216f, 419.9992f, 4.799657f, 0.0f, 0.0f, -0.67558960f, 0.73727790f, 192488, 192501), // Flag on tower
				new(4539.420f, 3622.490f, 420.0342f, 3.211419f, 0.0f, 0.0f, -0.99939060f, 0.03490613f, 192488, 192501), // Flag on tower
				new(4555.258f, 3641.648f, 419.9740f, 1.675514f, 0.0f, 0.0f, 0.74314400f, 0.66913150f, 192488, 192501),  // Flag on tower
				new(4574.872f, 3625.911f, 420.0792f, 0.087266f, 0.0f, 0.0f, 0.04361916f, 0.99904820f, 192488, 192501),  // Flag on tower
				new(4433.899f, 3534.142f, 360.2750f, 4.433136f, 0.0f, 0.0f, -0.79863550f, 0.60181500f, 192269, 192278), // Flag near workshop
				new(4572.933f, 3475.519f, 363.0090f, 1.422443f, 0.0f, 0.0f, 0.65275960f, 0.75756520f, 192269, 192277)   // Flag near bridge
			},
			CreatureBottom = new WintergraspObjectPositionData[]
			{
				new(4418.688477f, 3506.251709f, 358.975494f, 4.293305f, WGNpcs.GuardH, WGNpcs.GuardA) // Roaming Guard
			}
		},

		//South Tower
		new()
		{
			towerEntry = 190357,
			GameObject = new WintergraspGameObjectData[]
			{
				new(4416.004f, 2822.666f, 429.8512f, 6.2657330f, 0.0f, 0.0f, -0.00872612f, 0.99996190f, 192488, 192501), // Flag on tower
				new(4398.819f, 2804.698f, 429.7920f, 4.6949370f, 0.0f, 0.0f, -0.71325020f, 0.70090960f, 192488, 192501), // Flag on tower
				new(4387.622f, 2719.566f, 389.9351f, 4.7385700f, 0.0f, 0.0f, -0.69779010f, 0.71630230f, 192366, 192414), // Flag near tower
				new(4464.124f, 2855.453f, 406.1106f, 0.8290324f, 0.0f, 0.0f, 0.40274720f, 0.91531130f, 192366, 192429),  // Flag near tower
				new(4526.457f, 2810.181f, 391.1997f, 3.2899610f, 0.0f, 0.0f, -0.99724960f, 0.07411628f, 192269, 192278)  // Flag near bridge
			},
			CreatureBottom = new WintergraspObjectPositionData[]
			{
				new(4452.859863f, 2808.870117f, 402.604004f, 6.056290f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4455.899902f, 2835.958008f, 401.122559f, 0.034907f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4412.649414f, 2953.792236f, 374.799957f, 0.980838f, WGNpcs.GuardH, WGNpcs.GuardA), // Roaming Guard
				new(4362.089844f, 2811.510010f, 407.337006f, 3.193950f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4412.290039f, 2753.790039f, 401.015015f, 5.829400f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4421.939941f, 2773.189941f, 400.894989f, 5.707230f, WGNpcs.GuardH, WGNpcs.GuardA)  // Standing Guard
			}
		},

		//East Tower
		new()
		{
			towerEntry = 190358,
			GameObject = new WintergraspGameObjectData[]
			{
				new(4466.793f, 1960.418f, 459.1437f, 1.151916f, 0.0f, 0.0f, 0.5446386f, 0.8386708f, 192488, 192501),  // Flag on tower
				new(4475.351f, 1937.031f, 459.0702f, 5.846854f, 0.0f, 0.0f, -0.2164392f, 0.9762961f, 192488, 192501), // Flag on tower
				new(4451.758f, 1928.104f, 459.0759f, 4.276057f, 0.0f, 0.0f, -0.8433914f, 0.5372996f, 192488, 192501), // Flag on tower
				new(4442.987f, 1951.898f, 459.0930f, 2.740162f, 0.0f, 0.0f, 0.9799242f, 0.1993704f, 192488, 192501)   // Flag on tower
			},
			CreatureBottom = new WintergraspObjectPositionData[]
			{
				new(4501.060059f, 1990.280029f, 431.157013f, 1.029740f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4463.830078f, 2015.180054f, 430.299988f, 1.431170f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4494.580078f, 1943.760010f, 435.627014f, 6.195920f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4450.149902f, 1897.579956f, 435.045013f, 4.398230f, WGNpcs.GuardH, WGNpcs.GuardA), // Standing Guard
				new(4428.870117f, 1906.869995f, 432.648010f, 3.996800f, WGNpcs.GuardH, WGNpcs.GuardA)  // Standing Guard
			}
		}
	};

	public static WintergraspTowerCannonData[] TowerCannon =
	{
		new()
		{
			towerEntry = 190221,
			TurretTop = new Position[]
			{
				new(5255.88f, 3047.63f, 438.499f, 3.13677f), new(5280.9f, 3071.32f, 438.499f, 1.62879f)
			}
		},
		new()
		{
			towerEntry = 190373,
			TurretTop = new Position[]
			{
				new(5138.59f, 2935.16f, 439.845f, 3.11723f), new(5163.06f, 2959.52f, 439.846f, 1.47258f)
			}
		},
		new()
		{
			towerEntry = 190377,
			TurretTop = new Position[]
			{
				new(5163.84f, 2723.74f, 439.844f, 1.3994f), new(5139.69f, 2747.4f, 439.844f, 3.17221f)
			}
		},
		new()
		{
			towerEntry = 190378,
			TurretTop = new Position[]
			{
				new(5278.21f, 2607.23f, 439.755f, 4.71944f), new(5255.01f, 2631.98f, 439.755f, 3.15257f)
			}
		},
		new()
		{
			towerEntry = 190356,
			TowerCannonBottom = new Position[]
			{
				new(4537.380371f, 3599.531738f, 402.886993f, 3.998462f), new(4581.497559f, 3604.087158f, 402.886963f, 5.651723f)
			},
			TurretTop = new Position[]
			{
				new(4469.448242f, 1966.623779f, 465.647217f, 1.153573f), new(4581.895996f, 3626.438477f, 426.539062f, 0.117806f)
			}
		},
		new()
		{
			towerEntry = 190357,
			TowerCannonBottom = new Position[]
			{
				new(4421.640137f, 2799.935791f, 412.630920f, 5.459298f), new(4420.263184f, 2845.340332f, 412.630951f, 0.742197f)
			},
			TurretTop = new Position[]
			{
				new(4423.430664f, 2822.762939f, 436.283142f, 6.223487f), new(4397.825684f, 2847.629639f, 436.283325f, 1.579430f), new(4398.814941f, 2797.266357f, 436.283051f, 4.703747f)
			}
		},
		new()
		{
			towerEntry = 190358,
			TowerCannonBottom = new Position[]
			{
				new(4448.138184f, 1974.998779f, 441.995911f, 1.967238f), new(4448.713379f, 1955.148682f, 441.995178f, 0.380733f)
			},
			TurretTop = new Position[]
			{
				new(4469.448242f, 1966.623779f, 465.647217f, 1.153573f), new(4481.996582f, 1933.658325f, 465.647186f, 5.873029f)
			}
		}
	};

	public static StaticWintergraspWorkshopInfo[] WorkshopData =
	{
		new()
		{
			WorkshopId = WGWorkshopIds.Ne,
			WorldStateId = WorldStates.BattlefieldWgWorkshopNe,
			AllianceCaptureTextId = WintergraspText.SunkenRingCaptureAlliance,
			AllianceAttackTextId = WintergraspText.SunkenRingAttackAlliance,
			HordeCaptureTextId = WintergraspText.SunkenRingCaptureHorde,
			HordeAttackTextId = WintergraspText.SunkenRingAttackHorde
		},
		new()
		{
			WorkshopId = WGWorkshopIds.Nw,
			WorldStateId = WorldStates.BattlefieldWgWorkshopNw,
			AllianceCaptureTextId = WintergraspText.BrokenTempleCaptureAlliance,
			AllianceAttackTextId = WintergraspText.BrokenTempleAttackAlliance,
			HordeCaptureTextId = WintergraspText.BrokenTempleCaptureHorde,
			HordeAttackTextId = WintergraspText.BrokenTempleAttackHorde
		},
		new()
		{
			WorkshopId = WGWorkshopIds.Se,
			WorldStateId = WorldStates.BattlefieldWgWorkshopSe,
			AllianceCaptureTextId = WintergraspText.EastsparkCaptureAlliance,
			AllianceAttackTextId = WintergraspText.EastsparkAttackAlliance,
			HordeCaptureTextId = WintergraspText.EastsparkCaptureHorde,
			HordeAttackTextId = WintergraspText.EastsparkAttackHorde
		},
		new()
		{
			WorkshopId = WGWorkshopIds.Sw,
			WorldStateId = WorldStates.BattlefieldWgWorkshopSw,
			AllianceCaptureTextId = WintergraspText.WestsparkCaptureAlliance,
			AllianceAttackTextId = WintergraspText.WestsparkAttackAlliance,
			HordeCaptureTextId = WintergraspText.WestsparkCaptureHorde,
			HordeAttackTextId = WintergraspText.WestsparkAttackHorde
		},

		// KEEP WORKSHOPS - It can't be taken, so it doesn't have a textids
		new()
		{
			WorkshopId = WGWorkshopIds.KeepWest,
			WorldStateId = WorldStates.BattlefieldWgWorkshopKW
		},
		new()
		{
			WorkshopId = WGWorkshopIds.KeepEast,
			WorldStateId = WorldStates.BattlefieldWgWorkshopKE
		}
	};

	#endregion
}

internal struct WGData
{
	public const int DamagedTowerDef = 0;
	public const int BrokenTowerDef = 1;
	public const int DamagedTowerAtt = 2;
	public const int BrokenTowerAtt = 3;
	public const int MaxVehicleA = 4;
	public const int MaxVehicleH = 5;
	public const int VehicleA = 6;
	public const int VehicleH = 7;
	public const int Max = 8;
}

internal struct WGAchievements
{
	public const uint WinWg = 1717;
	public const uint WinWg100 = 1718;         // @Todo: Has To Be Implemented
	public const uint WgGnomeslaughter = 1723; // @Todo: Has To Be Implemented
	public const uint WgTowerDestroy = 1727;
	public const uint DestructionDerbyA = 1737; // @Todo: Has To Be Implemented
	public const uint WgTowerCannonKill = 1751; // @Todo: Has To Be Implemented
	public const uint WgMasterA = 1752;         // @Todo: Has To Be Implemented
	public const uint WinWgTimer10 = 1755;
	public const uint StoneKeeper50 = 2085;     // @Todo: Has To Be Implemented
	public const uint StoneKeeper100 = 2086;    // @Todo: Has To Be Implemented
	public const uint StoneKeeper250 = 2087;    // @Todo: Has To Be Implemented
	public const uint StoneKeeper500 = 2088;    // @Todo: Has To Be Implemented
	public const uint StoneKeeper1000 = 2089;   // @Todo: Has To Be Implemented
	public const uint WgRanger = 2199;          // @Todo: Has To Be Implemented
	public const uint DestructionDerbyH = 2476; // @Todo: Has To Be Implemented
	public const uint WgMasterH = 2776;         // @Todo: Has To Be Implemented
}

internal struct WGSpells
{
	// Wartime Auras
	public const uint Recruit = 37795;
	public const uint Corporal = 33280;
	public const uint Lieutenant = 55629;
	public const uint Tenacity = 58549;
	public const uint TenacityVehicle = 59911;
	public const uint TowerControl = 62064;
	public const uint SpiritualImmunity = 58729;
	public const uint GreatHonor = 58555;
	public const uint GreaterHonor = 58556;
	public const uint GreatestHonor = 58557;
	public const uint AllianceFlag = 14268;
	public const uint HordeFlag = 14267;
	public const uint GrabPassenger = 61178;

	// Reward Spells
	public const uint VictoryReward = 56902;
	public const uint DefeatReward = 58494;
	public const uint DamagedTower = 59135;
	public const uint DestroyedTower = 59136;
	public const uint DamagedBuilding = 59201;
	public const uint IntactBuilding = 59203;

	public const uint TeleportBridge = 59096;
	public const uint TeleportFortress = 60035;

	public const uint TeleportDalaran = 53360;
	public const uint VictoryAura = 60044;

	// Other Spells
	public const uint WintergraspWater = 36444;
	public const uint EssenceOfWintergrasp = 58045;
	public const uint WintergraspRestrictedFlightArea = 91604;

	// Phasing Spells
	public const uint HordeControlsFactoryPhaseShift = 56618;    // Adds Phase 16
	public const uint AllianceControlsFactoryPhaseShift = 56617; // Adds Phase 32

	public const uint HordeControlPhaseShift = 55773;    // Adds Phase 64
	public const uint AllianceControlPhaseShift = 55774; // Adds Phase 128
}

internal struct WGNpcs
{
	public const uint GuardH = 30739;
	public const uint GuardA = 30740;
	public const uint Stalker = 15214;

	public const uint TaunkaSpiritGuide = 31841;  // Horde Spirit Guide For Wintergrasp
	public const uint DwarvenSpiritGuide = 31842; // Alliance Spirit Guide For Wintergrasp

	public const uint SiegeEngineAlliance = 28312;
	public const uint SiegeEngineHorde = 32627;
	public const uint Catapult = 27881;
	public const uint Demolisher = 28094;
	public const uint TowerCannon = 28366;
}

internal struct WGGameObjects
{
	public const uint FactoryBannerNe = 190475;
	public const uint FactoryBannerNw = 190487;
	public const uint FactoryBannerSe = 194959;
	public const uint FactoryBannerSw = 194962;

	public const uint TitanSRelic = 192829;

	public const uint FortressTower1 = 190221;
	public const uint FortressTower2 = 190373;
	public const uint FortressTower3 = 190377;
	public const uint FortressTower4 = 190378;

	public const uint ShadowsightTower = 190356;
	public const uint WinterSEdgeTower = 190357;
	public const uint FlamewatchTower = 190358;

	public const uint FortressGate = 190375;
	public const uint VaultGate = 191810;

	public const uint KeepCollisionWall = 194323;
}

internal struct WintergraspTowerIds
{
	public const byte FortressNW = 0;
	public const byte FortressSW = 1;
	public const byte FortressSE = 2;
	public const byte FortressNE = 3;
	public const byte Shadowsight = 4;
	public const byte WintersEdge = 5;
	public const byte Flamewatch = 6;
}

internal struct WGWorkshopIds
{
	public const byte Se = 0;
	public const byte Sw = 1;
	public const byte Ne = 2;
	public const byte Nw = 3;
	public const byte KeepWest = 4;
	public const byte KeepEast = 5;
}

internal struct WGGossipText
{
	public const int GYNE = 20071;
	public const int GYNW = 20072;
	public const int GYSE = 20074;
	public const int GYSW = 20073;
	public const int GYKeep = 20070;
	public const int GYHorde = 20075;
	public const int GYAlliance = 20076;
}

internal struct WGGraveyardId
{
	public const uint WorkshopNE = 0;
	public const uint WorkshopNW = 1;
	public const uint WorkshopSE = 2;
	public const uint WorkshopSW = 3;
	public const uint Keep = 4;
	public const uint Horde = 5;
	public const uint Alliance = 6;
	public const uint Max = 7;
}

internal struct WintergraspQuests
{
	public const uint VictoryAlliance = 13181;
	public const uint VictoryHorde = 13183;
	public const uint CreditTowersDestroyed = 35074;
	public const uint CreditDefendSiege = 31284;
}

internal struct WintergraspText
{
	// Invisible Stalker
	public const byte SouthernTowerDamage = 1;
	public const byte SouthernTowerDestroy = 2;
	public const byte EasternTowerDamage = 3;
	public const byte EasternTowerDestroy = 4;
	public const byte WesternTowerDamage = 5;
	public const byte WesternTowerDestroy = 6;
	public const byte NwKeeptowerDamage = 7;
	public const byte NwKeeptowerDestroy = 8;
	public const byte SeKeeptowerDamage = 9;
	public const byte SeKeeptowerDestroy = 10;
	public const byte BrokenTempleAttackAlliance = 11;
	public const byte BrokenTempleCaptureAlliance = 12;
	public const byte BrokenTempleAttackHorde = 13;
	public const byte BrokenTempleCaptureHorde = 14;
	public const byte EastsparkAttackAlliance = 15;
	public const byte EastsparkCaptureAlliance = 16;
	public const byte EastsparkAttackHorde = 17;
	public const byte EastsparkCaptureHorde = 18;
	public const byte SunkenRingAttackAlliance = 19;
	public const byte SunkenRingCaptureAlliance = 20;
	public const byte SunkenRingAttackHorde = 21;
	public const byte SunkenRingCaptureHorde = 22;
	public const byte WestsparkAttackAlliance = 23;
	public const byte WestsparkCaptureAlliance = 24;
	public const byte WestsparkAttackHorde = 25;
	public const byte WestsparkCaptureHorde = 26;

	public const byte StartGrouping = 27;
	public const byte StartBattle = 28;
	public const byte FortressDefendAlliance = 29;
	public const byte FortressCaptureAlliance = 30;
	public const byte FortressDefendHorde = 31;
	public const byte FortressCaptureHorde = 32;

	public const byte NeKeeptowerDamage = 33;
	public const byte NeKeeptowerDestroy = 34;
	public const byte SwKeeptowerDamage = 35;
	public const byte SwKeeptowerDestroy = 36;

	public const byte RankCorporal = 37;
	public const byte RankFirstLieutenant = 38;
}

internal enum WGGameObjectState
{
	None,
	NeutralIntact,
	NeutralDamage,
	NeutralDestroy,
	HordeIntact,
	HordeDamage,
	HordeDestroy,
	AllianceIntact,
	AllianceDamage,
	AllianceDestroy
}

internal enum WGGameObjectBuildingType
{
	Door,
	Titanrelic,
	Wall,
	DoorLast,
	KeepTower,
	Tower
}

//Data Structs
internal struct BfWGCoordGY
{
	public BfWGCoordGY(float x, float y, float z, float o, uint graveyardId, int textId, uint startControl)
	{
		pos = new Position(x, y, z, o);
		GraveyardID = graveyardId;
		TextId = textId;
		StartControl = startControl;
	}

	public Position pos;
	public uint GraveyardID;
	public int TextId; // for gossip menu
	public uint StartControl;
}

internal struct WintergraspBuildingSpawnData
{
	public WintergraspBuildingSpawnData(uint entry, uint worldstate, float x, float y, float z, float o, float rX, float rY, float rZ, float rW, WGGameObjectBuildingType type)
	{
		Entry = entry;
		WorldState = worldstate;
		Pos = new Position(x, y, z, o);
		Rot = new Quaternion(rX, rY, rZ, rW);
		BuildingType = type;
	}

	public uint Entry;
	public uint WorldState;
	public Position Pos;
	public Quaternion Rot;
	public WGGameObjectBuildingType BuildingType;
}

internal struct WintergraspGameObjectData
{
	public WintergraspGameObjectData(float x, float y, float z, float o, float rX, float rY, float rZ, float rW, uint hordeEntry, uint allianceEntry)
	{
		Pos = new Position(x, y, z, o);
		Rot = new Quaternion(rX, rY, rZ, rW);
		HordeEntry = hordeEntry;
		AllianceEntry = allianceEntry;
	}

	public Position Pos;
	public Quaternion Rot;
	public uint HordeEntry;
	public uint AllianceEntry;
}

internal class WintergraspTowerData
{
	// Creature: Turrets and Guard // @todo: Killed on Tower destruction ? Tower Damage ? Requires confirming
	public WintergraspObjectPositionData[] CreatureBottom = new WintergraspObjectPositionData[9];
	public WintergraspGameObjectData[] GameObject = new WintergraspGameObjectData[6]; // Gameobject position and entry (Horde/Alliance)
	public uint towerEntry;                                                           // Gameobject Id of tower
}

internal struct WintergraspObjectPositionData
{
	public WintergraspObjectPositionData(float x, float y, float z, float o, uint hordeEntry, uint allianceEntry)
	{
		Pos = new Position(x, y, z, o);
		HordeEntry = hordeEntry;
		AllianceEntry = allianceEntry;
	}

	public Position Pos;
	public uint HordeEntry;
	public uint AllianceEntry;
}

internal class WintergraspTowerCannonData
{
	public Position[] TowerCannonBottom;

	public uint towerEntry;
	public Position[] TurretTop;

	public WintergraspTowerCannonData()
	{
		TowerCannonBottom = Array.Empty<Position>();
		TurretTop = Array.Empty<Position>();
	}
}

internal class StaticWintergraspWorkshopInfo
{
	public byte AllianceAttackTextId;
	public byte AllianceCaptureTextId;
	public byte HordeAttackTextId;
	public byte HordeCaptureTextId;
	public byte WorkshopId;
	public WorldStates WorldStateId;
}

internal class StaticWintergraspTowerInfo
{
	public byte DamagedTextId;
	public byte DestroyedTextId;

	public byte TowerId;

	public StaticWintergraspTowerInfo(byte towerId, byte damagedTextId, byte destroyedTextId)
	{
		TowerId = towerId;
		DamagedTextId = damagedTextId;
		DestroyedTextId = destroyedTextId;
	}
}