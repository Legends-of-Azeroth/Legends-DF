﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds.Zones;

public class BgStrandOfAncients : Battleground
{
	// Status of each gate (Destroy/Damage/Intact)
	readonly SAGateState[] GateStatus = new SAGateState[SAMiscConst.Gates.Length];

	// Team witch conntrol each graveyard
	readonly int[] GraveyardStatus = new int[SAGraveyards.Max];

	// Score of each round
	readonly SARoundScore[] RoundScores = new SARoundScore[2];
	readonly Dictionary<uint /*id*/, uint /*timer*/> DemoliserRespawnList = new();

	/// Id of attacker team
	int Attackers;

	// Totale elapsed time of current round
	uint TotalTime;

	// Max time of round
	uint EndRoundTimer;

	// For know if boats has start moving or not yet
	bool ShipsStarted;

	// Statu of battle (Start or not, and what round)
	SAStatus Status;

	// used for know we are in timer phase or not (used for worldstate update)
	bool TimerEnabled;

	// 5secs before starting the 1min countdown for second round
	uint UpdateWaitTimer;

	// for know if warning about second round start has been sent
	bool SignaledRoundTwo;

	// for know if warning about second round start has been sent
	bool SignaledRoundTwoHalfMin;

	// for know if second round has been init
	bool InitSecondRound;

	public BgStrandOfAncients(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
	{
		StartMessageIds[BattlegroundConst.EventIdFourth] = 0;

		BgObjects = new ObjectGuid[SAObjectTypes.MaxObj];
		BgCreatures = new ObjectGuid[SACreatureTypes.Max + SAGraveyards.Max];
		TimerEnabled = false;
		UpdateWaitTimer = 0;
		SignaledRoundTwo = false;
		SignaledRoundTwoHalfMin = false;
		InitSecondRound = false;
		Attackers = TeamIds.Alliance;
		TotalTime = 0;
		EndRoundTimer = 0;
		ShipsStarted = false;
		Status = SAStatus.NotStarted;

		for (byte i = 0; i < GateStatus.Length; ++i)
			GateStatus[i] = SAGateState.HordeGateOk;

		for (byte i = 0; i < 2; i++)
		{
			RoundScores[i].winner = TeamIds.Alliance;
			RoundScores[i].time = 0;
		}
	}

	public override void Reset()
	{
		TotalTime = 0;
		Attackers = (RandomHelper.URand(0, 1) != 0 ? TeamIds.Alliance : TeamIds.Horde);

		for (byte i = 0; i <= 5; i++)
			GateStatus[i] = SAGateState.HordeGateOk;

		ShipsStarted = false;
		Status = SAStatus.Warmup;
	}

	public override bool SetupBattleground()
	{
		return ResetObjs();
	}

	public override void PostUpdateImpl(uint diff)
	{
		if (InitSecondRound)
		{
			if (UpdateWaitTimer < diff)
			{
				if (!SignaledRoundTwo)
				{
					SignaledRoundTwo = true;
					InitSecondRound = false;
					SendBroadcastText(SABroadcastTexts.RoundTwoStartOneMinute, ChatMsg.BgSystemNeutral);
				}
			}
			else
			{
				UpdateWaitTimer -= diff;

				return;
			}
		}

		TotalTime += diff;

		if (Status == SAStatus.Warmup)
		{
			EndRoundTimer = SATimers.RoundLength;
			UpdateWorldState(SAWorldStateIds.Timer, (int)(GameTime.GetGameTime() + EndRoundTimer));

			if (TotalTime >= SATimers.WarmupLength)
			{
				var c = GetBGCreature(SACreatureTypes.Kanrethad);

				if (c)
					SendChatMessage(c, SATextIds.RoundStarted);

				TotalTime = 0;
				ToggleTimer();
				DemolisherStartState(false);
				Status = SAStatus.RoundOne;
				TriggerGameEvent(Attackers == TeamIds.Alliance ? 23748 : 21702u);
			}

			if (TotalTime >= SATimers.BoatStart)
				StartShips();

			return;
		}
		else if (Status == SAStatus.SecondWarmup)
		{
			if (RoundScores[0].time < SATimers.RoundLength)
				EndRoundTimer = RoundScores[0].time;
			else
				EndRoundTimer = SATimers.RoundLength;

			UpdateWorldState(SAWorldStateIds.Timer, (int)(GameTime.GetGameTime() + EndRoundTimer));

			if (TotalTime >= 60000)
			{
				var c = GetBGCreature(SACreatureTypes.Kanrethad);

				if (c)
					SendChatMessage(c, SATextIds.RoundStarted);

				TotalTime = 0;
				ToggleTimer();
				DemolisherStartState(false);
				Status = SAStatus.RoundTwo;
				TriggerGameEvent(Attackers == TeamIds.Alliance ? 23748 : 21702u);
				// status was set to STATUS_WAIT_JOIN manually for Preparation, set it back now
				SetStatus(BattlegroundStatus.InProgress);

				foreach (var pair in GetPlayers())
				{
					var p = Global.ObjAccessor.FindPlayer(pair.Key);

					if (p)
						p.RemoveAura(BattlegroundConst.SpellPreparation);
				}
			}

			if (TotalTime >= 30000)
				if (!SignaledRoundTwoHalfMin)
				{
					SignaledRoundTwoHalfMin = true;
					SendBroadcastText(SABroadcastTexts.RoundTwoStartHalfMinute, ChatMsg.BgSystemNeutral);
				}

			StartShips();

			return;
		}
		else if (GetStatus() == BattlegroundStatus.InProgress)
		{
			if (Status == SAStatus.RoundOne)
			{
				if (TotalTime >= SATimers.RoundLength)
				{
					CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Alliance);
					CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Horde);
					RoundScores[0].winner = (uint)Attackers;
					RoundScores[0].time = SATimers.RoundLength;
					TotalTime = 0;
					Status = SAStatus.SecondWarmup;
					Attackers = (Attackers == TeamIds.Alliance) ? TeamIds.Horde : TeamIds.Alliance;
					UpdateWaitTimer = 5000;
					SignaledRoundTwo = false;
					SignaledRoundTwoHalfMin = false;
					InitSecondRound = true;
					ToggleTimer();
					ResetObjs();
					GetBgMap().UpdateAreaDependentAuras();

					return;
				}
			}
			else if (Status == SAStatus.RoundTwo)
			{
				if (TotalTime >= EndRoundTimer)
				{
					CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Alliance);
					CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Horde);
					RoundScores[1].time = SATimers.RoundLength;
					RoundScores[1].winner = (uint)((Attackers == TeamIds.Alliance) ? TeamIds.Horde : TeamIds.Alliance);

					if (RoundScores[0].time == RoundScores[1].time)
						EndBattleground(0);
					else if (RoundScores[0].time < RoundScores[1].time)
						EndBattleground(RoundScores[0].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
					else
						EndBattleground(RoundScores[1].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);

					return;
				}
			}

			if (Status == SAStatus.RoundOne || Status == SAStatus.RoundTwo)
				UpdateDemolisherSpawns();
		}
	}

	public override void AddPlayer(Player player)
	{
		var isInBattleground = IsPlayerInBattleground(player.GUID);
		base.AddPlayer(player);

		if (!isInBattleground)
			PlayerScores[player.GUID] = new BattlegroundSAScore(player.GUID, player.GetBgTeam());

		SendTransportInit(player);

		if (!isInBattleground)
			TeleportToEntrancePosition(player);
	}

	public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team) { }

	public override void HandleAreaTrigger(Player source, uint trigger, bool entered)
	{
		// this is wrong way to implement these things. On official it done by gameobject spell cast.
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;
	}

	public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker = null)
	{
		var go = obj.AsGameObject;

		if (go)
			switch (go.GoType)
			{
				case GameObjectTypes.Goober:
					if (invoker)
						if (eventId == (uint)SAEventIds.BG_SA_EVENT_TITAN_RELIC_ACTIVATED)
							TitanRelicActivated(invoker.AsPlayer);

					break;
				case GameObjectTypes.DestructibleBuilding:
				{
					var gate = GetGate(obj.Entry);

					if (gate != null)
					{
						var gateId = gate.GateId;

						// damaged
						if (eventId == go.Template.DestructibleBuilding.DamagedEvent)
						{
							GateStatus[gateId] = Attackers == TeamIds.Horde ? SAGateState.AllianceGateDamaged : SAGateState.HordeGateDamaged;

							var c = obj.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

							if (c)
								SendChatMessage(c, (byte)gate.DamagedText, invoker);

							PlaySoundToAll(Attackers == TeamIds.Alliance ? SASoundIds.WallAttackedAlliance : SASoundIds.WallAttackedHorde);
						}
						// destroyed
						else if (eventId == go.Template.DestructibleBuilding.DestroyedEvent)
						{
							GateStatus[gate.GateId] = Attackers == TeamIds.Horde ? SAGateState.AllianceGateDestroyed : SAGateState.HordeGateDestroyed;

							if (gateId < 5)
								DelObject((int)gateId + 14);

							var c = obj.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

							if (c)
								SendChatMessage(c, (byte)gate.DestroyedText, invoker);

							PlaySoundToAll(Attackers == TeamIds.Alliance ? SASoundIds.WallDestroyedAlliance : SASoundIds.WallDestroyedHorde);

							var rewardHonor = true;

							switch (gateId)
							{
								case SAObjectTypes.GreenGate:
									if (IsGateDestroyed(SAObjectTypes.BlueGate))
										rewardHonor = false;

									break;
								case SAObjectTypes.BlueGate:
									if (IsGateDestroyed(SAObjectTypes.GreenGate))
										rewardHonor = false;

									break;
								case SAObjectTypes.RedGate:
									if (IsGateDestroyed(SAObjectTypes.PurpleGate))
										rewardHonor = false;

									break;
								case SAObjectTypes.PurpleGate:
									if (IsGateDestroyed(SAObjectTypes.RedGate))
										rewardHonor = false;

									break;
								default:
									break;
							}

							if (invoker)
							{
								var unit = invoker.AsUnit;

								if (unit)
								{
									var player = unit.CharmerOrOwnerPlayerOrPlayerItself;

									if (player)
									{
										UpdatePlayerScore(player, ScoreType.DestroyedWall, 1);

										if (rewardHonor)
											UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(1));
									}
								}
							}

							UpdateObjectInteractionFlags();
						}
						else
						{
							break;
						}

						UpdateWorldState(gate.WorldState, (int)GateStatus[gateId]);
					}

					break;
				}
				default:
					break;
			}
	}

	public override void HandleKillUnit(Creature creature, Player killer)
	{
		if (creature.Entry == SACreatureIds.Demolisher)
		{
			UpdatePlayerScore(killer, ScoreType.DestroyedDemolisher, 1);
			var worldStateId = Attackers == TeamIds.Horde ? SAWorldStateIds.DestroyedHordeVehicles : SAWorldStateIds.DestroyedAllianceVehicles;
			var currentDestroyedVehicles = Global.WorldStateMgr.GetValue((int)worldStateId, GetBgMap());
			UpdateWorldState(worldStateId, currentDestroyedVehicles + 1);
		}
	}

	public override void DestroyGate(Player player, GameObject go) { }

	public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
	{
		uint safeloc;

		var teamId = GetTeamIndexByTeamId(GetPlayerTeam(player.GUID));

		if (teamId == Attackers)
			safeloc = SAMiscConst.GYEntries[SAGraveyards.BeachGy];
		else
			safeloc = SAMiscConst.GYEntries[SAGraveyards.DefenderLastGy];

		var closest = Global.ObjectMgr.GetWorldSafeLoc(safeloc);
		var nearest = player.Location.GetExactDistSq(closest.Loc);

		for (byte i = SAGraveyards.RightCapturableGy; i < SAGraveyards.Max; i++)
		{
			if (GraveyardStatus[i] != teamId)
				continue;

			var ret = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);
			var dist = player.Location.GetExactDistSq(ret.Loc);

			if (dist < nearest)
			{
				closest = ret;
				nearest = dist;
			}
		}

		return closest;
	}

	public override void EventPlayerClickedOnFlag(Player source, GameObject go)
	{
		switch (go.Entry)
		{
			case 191307:
			case 191308:
				if (CanInteractWithObject(SAObjectTypes.LeftFlag))
					CaptureGraveyard(SAGraveyards.LeftCapturableGy, source);

				break;
			case 191305:
			case 191306:
				if (CanInteractWithObject(SAObjectTypes.RightFlag))
					CaptureGraveyard(SAGraveyards.RightCapturableGy, source);

				break;
			case 191310:
			case 191309:
				if (CanInteractWithObject(SAObjectTypes.CentralFlag))
					CaptureGraveyard(SAGraveyards.CentralCapturableGy, source);

				break;
			default:
				return;
		}
	}

	public override void EndBattleground(TeamFaction winner)
	{
		// honor reward for winning
		if (winner == TeamFaction.Alliance)
			RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);
		else if (winner == TeamFaction.Horde)
			RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);

		// complete map_end rewards (even if no team wins)
		RewardHonorToTeam(GetBonusHonorFromKill(2), TeamFaction.Alliance);
		RewardHonorToTeam(GetBonusHonorFromKill(2), TeamFaction.Horde);

		base.EndBattleground(winner);
	}

	public override bool IsSpellAllowed(uint spellId, Player player)
	{
		switch (spellId)
		{
			case SASpellIds.AllianceControlPhaseShift:
				return Attackers == TeamIds.Horde;
			case SASpellIds.HordeControlPhaseShift:
				return Attackers == TeamIds.Alliance;
			case BattlegroundConst.SpellPreparation:
				return Status == SAStatus.Warmup || Status == SAStatus.SecondWarmup;
			default:
				break;
		}

		return true;
	}

	public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
	{
		if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
			return false;

		switch (type)
		{
			case ScoreType.DestroyedDemolisher:
				player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SAObjectives.DemolishersDestroyed);

				break;
			case ScoreType.DestroyedWall:
				player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SAObjectives.GatesDestroyed);

				break;
			default:
				break;
		}

		return true;
	}

	bool ResetObjs()
	{
		foreach (var pair in GetPlayers())
		{
			var player = Global.ObjAccessor.FindPlayer(pair.Key);

			if (player)
				SendTransportsRemove(player);
		}

		var atF = SAMiscConst.Factions[Attackers];
		var defF = SAMiscConst.Factions[Attackers != 0 ? TeamIds.Alliance : TeamIds.Horde];

		for (byte i = 0; i < SAObjectTypes.MaxObj; i++)
			DelObject(i);

		for (byte i = 0; i < SACreatureTypes.Max; i++)
			DelCreature(i);

		for (byte i = SACreatureTypes.Max; i < SACreatureTypes.Max + SAGraveyards.Max; i++)
			DelCreature(i);

		for (byte i = 0; i < GateStatus.Length; ++i)
			GateStatus[i] = Attackers == TeamIds.Horde ? SAGateState.AllianceGateOk : SAGateState.HordeGateOk;

		if (!AddCreature(SAMiscConst.NpcEntries[SACreatureTypes.Kanrethad], SACreatureTypes.Kanrethad, SAMiscConst.NpcSpawnlocs[SACreatureTypes.Kanrethad]))
		{
			Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Kanrethad, aborted. Entry: {SAMiscConst.NpcEntries[SACreatureTypes.Kanrethad]}");

			return false;
		}

		for (byte i = 0; i <= SAObjectTypes.PortalDeffenderRed; i++)
			if (!AddObject(i, SAMiscConst.ObjEntries[i], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
			{
				Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn BG_SA_PORTAL_DEFFENDER_RED, Entry: {SAMiscConst.ObjEntries[i]}");

				continue;
			}

		for (var i = SAObjectTypes.BoatOne; i <= SAObjectTypes.BoatTwo; i++)
		{
			uint boatid = 0;

			switch (i)
			{
				case SAObjectTypes.BoatOne:
					boatid = Attackers != 0 ? SAGameObjectIds.BoatOneH : SAGameObjectIds.BoatOneA;

					break;
				case SAObjectTypes.BoatTwo:
					boatid = Attackers != 0 ? SAGameObjectIds.BoatTwoH : SAGameObjectIds.BoatTwoA;

					break;
				default:
					break;
			}

			if (!AddObject(i,
							boatid,
							SAMiscConst.ObjSpawnlocs[i].X,
							SAMiscConst.ObjSpawnlocs[i].Y,
							SAMiscConst.ObjSpawnlocs[i].Z + (Attackers != 0 ? -3.750f : 0),
							SAMiscConst.ObjSpawnlocs[i].Orientation,
							0,
							0,
							0,
							0,
							BattlegroundConst.RespawnOneDay))
			{
				Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn one of the BG_SA_BOAT, Entry: {boatid}");

				continue;
			}
		}

		for (byte i = SAObjectTypes.Sigil1; i <= SAObjectTypes.LeftFlagpole; i++)
			if (!AddObject(i, SAMiscConst.ObjEntries[i], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
			{
				Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Sigil, Entry: {SAMiscConst.ObjEntries[i]}");

				continue;
			}

		// MAD props for Kiper for discovering those values - 4 hours of his work.
		GetBGObject(SAObjectTypes.BoatOne).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.0002f));
		GetBGObject(SAObjectTypes.BoatTwo).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.00001f));
		SpawnBGObject(SAObjectTypes.BoatOne, BattlegroundConst.RespawnImmediately);
		SpawnBGObject(SAObjectTypes.BoatTwo, BattlegroundConst.RespawnImmediately);

		//Cannons and demolishers - NPCs are spawned
		//By capturing GYs.
		for (byte i = 0; i < SACreatureTypes.Demolisher5; i++)
			if (!AddCreature(SAMiscConst.NpcEntries[i], i, SAMiscConst.NpcSpawnlocs[i], Attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance, 600))
			{
				Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Cannon or demolisher, Entry: {SAMiscConst.NpcEntries[i]}, Attackers: {(Attackers == TeamIds.Alliance ? "Horde(1)" : "Alliance(0)")}");

				continue;
			}

		OverrideGunFaction();
		DemolisherStartState(true);

		for (byte i = 0; i <= SAObjectTypes.PortalDeffenderRed; i++)
		{
			SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
			GetBGObject(i).Faction = defF;
		}

		GetBGObject(SAObjectTypes.TitanRelic).Faction = atF;
		GetBGObject(SAObjectTypes.TitanRelic).Refresh();

		TotalTime = 0;
		ShipsStarted = false;

		//Graveyards
		for (byte i = 0; i < SAGraveyards.Max; i++)
		{
			var sg = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);

			if (sg == null)
			{
				Log.outError(LogFilter.Battleground, $"SOTA: Can't find GY entry {SAMiscConst.GYEntries[i]}");

				return false;
			}

			if (i == SAGraveyards.BeachGy)
			{
				GraveyardStatus[i] = Attackers;
				AddSpiritGuide(i + SACreatureTypes.Max, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SAMiscConst.GYOrientation[i], Attackers);
			}
			else
			{
				GraveyardStatus[i] = ((Attackers == TeamIds.Horde) ? TeamIds.Alliance : TeamIds.Horde);

				if (!AddSpiritGuide(i + SACreatureTypes.Max, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SAMiscConst.GYOrientation[i], Attackers == TeamIds.Horde ? TeamIds.Alliance : TeamIds.Horde))
					Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn GY: {i}");
			}
		}

		//GY capture points
		for (byte i = SAObjectTypes.CentralFlag; i <= SAObjectTypes.LeftFlag; i++)
		{
			if (!AddObject(i, (SAMiscConst.ObjEntries[i] - (Attackers == TeamIds.Alliance ? 1u : 0)), SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
			{
				Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Central Flag Entry: {SAMiscConst.ObjEntries[i] - (Attackers == TeamIds.Alliance ? 1 : 0)}");

				continue;
			}

			GetBGObject(i).Faction = atF;
		}

		UpdateObjectInteractionFlags();

		for (byte i = SAObjectTypes.Bomb; i < SAObjectTypes.MaxObj; i++)
		{
			if (!AddObject(i, SAMiscConst.ObjEntries[SAObjectTypes.Bomb], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
			{
				Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn SA Bomb Entry: {SAMiscConst.ObjEntries[SAObjectTypes.Bomb] + i}");

				continue;
			}

			GetBGObject(i).Faction = atF;
		}

		//Player may enter BEFORE we set up BG - lets update his worldstates anyway...
		UpdateWorldState(SAWorldStateIds.RightGyHorde, GraveyardStatus[SAGraveyards.RightCapturableGy] == TeamIds.Horde ? 1 : 0);
		UpdateWorldState(SAWorldStateIds.LeftGyHorde, GraveyardStatus[SAGraveyards.LeftCapturableGy] == TeamIds.Horde ? 1 : 0);
		UpdateWorldState(SAWorldStateIds.CenterGyHorde, GraveyardStatus[SAGraveyards.CentralCapturableGy] == TeamIds.Horde ? 1 : 0);

		UpdateWorldState(SAWorldStateIds.RightGyAlliance, GraveyardStatus[SAGraveyards.RightCapturableGy] == TeamIds.Alliance ? 1 : 0);
		UpdateWorldState(SAWorldStateIds.LeftGyAlliance, GraveyardStatus[SAGraveyards.LeftCapturableGy] == TeamIds.Alliance ? 1 : 0);
		UpdateWorldState(SAWorldStateIds.CenterGyAlliance, GraveyardStatus[SAGraveyards.CentralCapturableGy] == TeamIds.Alliance ? 1 : 0);

		if (Attackers == TeamIds.Alliance)
		{
			UpdateWorldState(SAWorldStateIds.AllyAttacks, 1);
			UpdateWorldState(SAWorldStateIds.HordeAttacks, 0);

			UpdateWorldState(SAWorldStateIds.RightAttTokenAll, 1);
			UpdateWorldState(SAWorldStateIds.LeftAttTokenAll, 1);
			UpdateWorldState(SAWorldStateIds.RightAttTokenHrd, 0);
			UpdateWorldState(SAWorldStateIds.LeftAttTokenHrd, 0);

			UpdateWorldState(SAWorldStateIds.HordeDefenceToken, 1);
			UpdateWorldState(SAWorldStateIds.AllianceDefenceToken, 0);
		}
		else
		{
			UpdateWorldState(SAWorldStateIds.HordeAttacks, 1);
			UpdateWorldState(SAWorldStateIds.AllyAttacks, 0);

			UpdateWorldState(SAWorldStateIds.RightAttTokenAll, 0);
			UpdateWorldState(SAWorldStateIds.LeftAttTokenAll, 0);
			UpdateWorldState(SAWorldStateIds.RightAttTokenHrd, 1);
			UpdateWorldState(SAWorldStateIds.LeftAttTokenHrd, 1);

			UpdateWorldState(SAWorldStateIds.HordeDefenceToken, 0);
			UpdateWorldState(SAWorldStateIds.AllianceDefenceToken, 1);
		}

		UpdateWorldState(SAWorldStateIds.AttackerTeam, Attackers);
		UpdateWorldState(SAWorldStateIds.PurpleGate, 1);
		UpdateWorldState(SAWorldStateIds.RedGate, 1);
		UpdateWorldState(SAWorldStateIds.BlueGate, 1);
		UpdateWorldState(SAWorldStateIds.GreenGate, 1);
		UpdateWorldState(SAWorldStateIds.YellowGate, 1);
		UpdateWorldState(SAWorldStateIds.AncientGate, 1);

		for (var i = SAObjectTypes.BoatOne; i <= SAObjectTypes.BoatTwo; i++)
			foreach (var pair in GetPlayers())
			{
				var player = Global.ObjAccessor.FindPlayer(pair.Key);

				if (player)
					SendTransportInit(player);
			}

		// set status manually so preparation is cast correctly in 2nd round too
		SetStatus(BattlegroundStatus.WaitJoin);

		TeleportPlayers();

		return true;
	}

	void StartShips()
	{
		if (ShipsStarted)
			return;

		GetBGObject(SAObjectTypes.BoatOne).SetGoState(GameObjectState.TransportStopped);
		GetBGObject(SAObjectTypes.BoatTwo).SetGoState(GameObjectState.TransportStopped);

		for (var i = SAObjectTypes.BoatOne; i <= SAObjectTypes.BoatTwo; i++)
			foreach (var pair in GetPlayers())
			{
				var p = Global.ObjAccessor.FindPlayer(pair.Key);

				if (p)
				{
					UpdateData data = new(p.Location.MapId);
					GetBGObject(i).BuildValuesUpdateBlockForPlayer(data, p);

					data.BuildPacket(out var pkt);
					p.SendPacket(pkt);
				}
			}

		ShipsStarted = true;
	}

	void TeleportPlayers()
	{
		foreach (var pair in GetPlayers())
		{
			var player = Global.ObjAccessor.FindPlayer(pair.Key);

			if (player)
			{
				// should remove spirit of redemption
				if (player.HasAuraType(AuraType.SpiritOfRedemption))
					player.RemoveAurasByType(AuraType.ModShapeshift);

				if (!player.IsAlive)
				{
					player.ResurrectPlayer(1.0f);
					player.SpawnCorpseBones();
				}

				player.ResetAllPowers();
				player.CombatStopWithPets(true);

				player.CastSpell(player, BattlegroundConst.SpellPreparation, true);

				TeleportToEntrancePosition(player);
			}
		}
	}

	void TeleportToEntrancePosition(Player player)
	{
		if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == Attackers)
		{
			if (!ShipsStarted)
			{
				// player.AddUnitMovementFlag(MOVEMENTFLAG_ONTRANSPORT);

				if (RandomHelper.URand(0, 1) != 0)
					player.TeleportTo(607, 2682.936f, -830.368f, 15.0f, 2.895f, 0);
				else
					player.TeleportTo(607, 2577.003f, 980.261f, 15.0f, 0.807f, 0);
			}
			else
			{
				player.TeleportTo(607, 1600.381f, -106.263f, 8.8745f, 3.78f, 0);
			}
		}
		else
		{
			player.TeleportTo(607, 1209.7f, -65.16f, 70.1f, 0.0f, 0);
		}
	}

	/*
	You may ask what the fuck does it do?
	Prevents owner overwriting guns faction with own.
	*/
	void OverrideGunFaction()
	{
		if (BgCreatures[0].IsEmpty)
			return;

		for (byte i = SACreatureTypes.Gun1; i <= SACreatureTypes.Gun10; i++)
		{
			var gun = GetBGCreature(i);

			if (gun)
				gun.Faction = SAMiscConst.Factions[Attackers != 0 ? TeamIds.Alliance : TeamIds.Horde];
		}

		for (byte i = SACreatureTypes.Demolisher1; i <= SACreatureTypes.Demolisher4; i++)
		{
			var dem = GetBGCreature(i);

			if (dem)
				dem.Faction = SAMiscConst.Factions[Attackers];
		}
	}

	void DemolisherStartState(bool start)
	{
		if (BgCreatures[0].IsEmpty)
			return;

		// set flags only for the demolishers on the beach, factory ones dont need it
		for (byte i = SACreatureTypes.Demolisher1; i <= SACreatureTypes.Demolisher4; i++)
		{
			var dem = GetBGCreature(i);

			if (dem)
			{
				if (start)
					dem.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
				else
					dem.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
			}
		}
	}

	bool CanInteractWithObject(uint objectId)
	{
		switch (objectId)
		{
			case SAObjectTypes.TitanRelic:
				if (!IsGateDestroyed(SAObjectTypes.AncientGate) || !IsGateDestroyed(SAObjectTypes.YellowGate))
					return false;

				goto case SAObjectTypes.CentralFlag;
			case SAObjectTypes.CentralFlag:
				if (!IsGateDestroyed(SAObjectTypes.RedGate) && !IsGateDestroyed(SAObjectTypes.PurpleGate))
					return false;

				goto case SAObjectTypes.LeftFlag;
			case SAObjectTypes.LeftFlag:
			case SAObjectTypes.RightFlag:
				if (!IsGateDestroyed(SAObjectTypes.GreenGate) && !IsGateDestroyed(SAObjectTypes.BlueGate))
					return false;

				break;
			default:
				//ABORT();
				break;
		}

		return true;
	}

	void UpdateObjectInteractionFlags(uint objectId)
	{
		var go = GetBGObject((int)objectId);

		if (go)
		{
			if (CanInteractWithObject(objectId))
				go.RemoveFlag(GameObjectFlags.NotSelectable);
			else
				go.SetFlag(GameObjectFlags.NotSelectable);
		}
	}

	void UpdateObjectInteractionFlags()
	{
		for (byte i = SAObjectTypes.CentralFlag; i <= SAObjectTypes.LeftFlag; ++i)
			UpdateObjectInteractionFlags(i);

		UpdateObjectInteractionFlags(SAObjectTypes.TitanRelic);
	}

	void CaptureGraveyard(int i, Player source)
	{
		if (GraveyardStatus[i] == Attackers)
			return;

		DelCreature(SACreatureTypes.Max + i);
		var teamId = GetTeamIndexByTeamId(GetPlayerTeam(source.GUID));
		GraveyardStatus[i] = teamId;
		var sg = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);

		if (sg == null)
		{
			Log.outError(LogFilter.Battleground, $"CaptureGraveyard: non-existant GY entry: {SAMiscConst.GYEntries[i]}");

			return;
		}

		AddSpiritGuide(i + SACreatureTypes.Max, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SAMiscConst.GYOrientation[i], GraveyardStatus[i]);

		uint npc;
		int flag;

		switch (i)
		{
			case SAGraveyards.LeftCapturableGy:
			{
				flag = SAObjectTypes.LeftFlag;
				DelObject(flag);

				AddObject(flag,
						(SAMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u)),
						SAMiscConst.ObjSpawnlocs[flag],
						0,
						0,
						0,
						0,
						BattlegroundConst.RespawnOneDay);

				npc = SACreatureTypes.Rigspark;
				var rigspark = AddCreature(SAMiscConst.NpcEntries[npc], (int)npc, SAMiscConst.NpcSpawnlocs[npc], Attackers);

				if (rigspark)
					rigspark.AI.Talk(SATextIds.SparklightRigsparkSpawn);

				for (byte j = SACreatureTypes.Demolisher7; j <= SACreatureTypes.Demolisher8; j++)
				{
					AddCreature(SAMiscConst.NpcEntries[j], j, SAMiscConst.NpcSpawnlocs[j], (Attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance), 600);
					var dem = GetBGCreature(j);

					if (dem)
						dem.Faction = SAMiscConst.Factions[Attackers];
				}

				UpdateWorldState(SAWorldStateIds.LeftGyAlliance, GraveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
				UpdateWorldState(SAWorldStateIds.LeftGyHorde, GraveyardStatus[i] == TeamIds.Horde ? 1 : 0);

				var c = source.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

				if (c)
					SendChatMessage(c, teamId == TeamIds.Alliance ? SATextIds.WestGraveyardCapturedA : SATextIds.WestGraveyardCapturedH, source);
			}

				break;
			case SAGraveyards.RightCapturableGy:
			{
				flag = SAObjectTypes.RightFlag;
				DelObject(flag);

				AddObject(flag,
						(SAMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u)),
						SAMiscConst.ObjSpawnlocs[flag],
						0,
						0,
						0,
						0,
						BattlegroundConst.RespawnOneDay);

				npc = SACreatureTypes.Sparklight;
				var sparklight = AddCreature(SAMiscConst.NpcEntries[npc], (int)npc, SAMiscConst.NpcSpawnlocs[npc], Attackers);

				if (sparklight)
					sparklight.AI.Talk(SATextIds.SparklightRigsparkSpawn);

				for (byte j = SACreatureTypes.Demolisher5; j <= SACreatureTypes.Demolisher6; j++)
				{
					AddCreature(SAMiscConst.NpcEntries[j], j, SAMiscConst.NpcSpawnlocs[j], Attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance, 600);

					var dem = GetBGCreature(j);

					if (dem)
						dem.Faction = SAMiscConst.Factions[Attackers];
				}

				UpdateWorldState(SAWorldStateIds.RightGyAlliance, GraveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
				UpdateWorldState(SAWorldStateIds.RightGyHorde, GraveyardStatus[i] == TeamIds.Horde ? 1 : 0);

				var c = source.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

				if (c)
					SendChatMessage(c, teamId == TeamIds.Alliance ? SATextIds.EastGraveyardCapturedA : SATextIds.EastGraveyardCapturedH, source);
			}

				break;
			case SAGraveyards.CentralCapturableGy:
			{
				flag = SAObjectTypes.CentralFlag;
				DelObject(flag);

				AddObject(flag,
						(SAMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u)),
						SAMiscConst.ObjSpawnlocs[flag],
						0,
						0,
						0,
						0,
						BattlegroundConst.RespawnOneDay);

				UpdateWorldState(SAWorldStateIds.CenterGyAlliance, GraveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
				UpdateWorldState(SAWorldStateIds.CenterGyHorde, GraveyardStatus[i] == TeamIds.Horde ? 1 : 0);

				var c = source.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

				if (c)
					SendChatMessage(c, teamId == TeamIds.Alliance ? SATextIds.SouthGraveyardCapturedA : SATextIds.SouthGraveyardCapturedH, source);
			}

				break;
			default:
				//ABORT();
				break;
		}
	}

	void TitanRelicActivated(Player clicker)
	{
		if (!clicker)
			return;

		if (CanInteractWithObject(SAObjectTypes.TitanRelic))
		{
			var clickerTeamId = GetTeamIndexByTeamId(GetPlayerTeam(clicker.GUID));

			if (clickerTeamId == Attackers)
			{
				if (clickerTeamId == TeamIds.Alliance)
					SendBroadcastText(SABroadcastTexts.AllianceCapturedTitanPortal, ChatMsg.BgSystemNeutral);
				else
					SendBroadcastText(SABroadcastTexts.HordeCapturedTitanPortal, ChatMsg.BgSystemNeutral);

				if (Status == SAStatus.RoundOne)
				{
					RoundScores[0].winner = (uint)Attackers;
					RoundScores[0].time = TotalTime;

					// Achievement Storm the Beach (1310)
					foreach (var pair in GetPlayers())
					{
						var player = Global.ObjAccessor.FindPlayer(pair.Key);

						if (player)
							if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == Attackers)
								player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
					}

					Attackers = (Attackers == TeamIds.Alliance) ? TeamIds.Horde : TeamIds.Alliance;
					Status = SAStatus.SecondWarmup;
					TotalTime = 0;
					ToggleTimer();

					var c = GetBGCreature(SACreatureTypes.Kanrethad);

					if (c)
						SendChatMessage(c, SATextIds.Round1Finished);

					UpdateWaitTimer = 5000;
					SignaledRoundTwo = false;
					SignaledRoundTwoHalfMin = false;
					InitSecondRound = true;
					ResetObjs();
					GetBgMap().UpdateAreaDependentAuras();
					CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Alliance);
					CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Horde);
				}
				else if (Status == SAStatus.RoundTwo)
				{
					RoundScores[1].winner = (uint)Attackers;
					RoundScores[1].time = TotalTime;
					ToggleTimer();

					// Achievement Storm the Beach (1310)
					foreach (var pair in GetPlayers())
					{
						var player = Global.ObjAccessor.FindPlayer(pair.Key);

						if (player)
							if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == Attackers && RoundScores[1].winner == Attackers)
								player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
					}

					if (RoundScores[0].time == RoundScores[1].time)
						EndBattleground(0);
					else if (RoundScores[0].time < RoundScores[1].time)
						EndBattleground(RoundScores[0].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
					else
						EndBattleground(RoundScores[1].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
				}
			}
		}
	}

	void ToggleTimer()
	{
		TimerEnabled = !TimerEnabled;
		UpdateWorldState(SAWorldStateIds.EnableTimer, TimerEnabled ? 1 : 0);
	}

	void UpdateDemolisherSpawns()
	{
		for (byte i = SACreatureTypes.Demolisher1; i <= SACreatureTypes.Demolisher8; i++)
			if (!BgCreatures[i].IsEmpty)
			{
				var Demolisher = GetBGCreature(i);

				if (Demolisher)
					if (Demolisher.IsDead)
					{
						// Demolisher is not in list
						if (!DemoliserRespawnList.ContainsKey(i))
						{
							DemoliserRespawnList[i] = GameTime.GetGameTimeMS() + 30000;
						}
						else
						{
							if (DemoliserRespawnList[i] < GameTime.GetGameTimeMS())
							{
								Demolisher.Location.Relocate(SAMiscConst.NpcSpawnlocs[i]);
								Demolisher.Respawn();
								DemoliserRespawnList.Remove(i);
							}
						}
					}
			}
	}

	void SendTransportInit(Player player)
	{
		if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty || !BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
		{
			UpdateData transData = new(player.Location.MapId);

			if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty)
				GetBGObject(SAObjectTypes.BoatOne).BuildCreateUpdateBlockForPlayer(transData, player);

			if (!BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
				GetBGObject(SAObjectTypes.BoatTwo).BuildCreateUpdateBlockForPlayer(transData, player);

			transData.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}

	void SendTransportsRemove(Player player)
	{
		if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty || !BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
		{
			UpdateData transData = new(player.Location.MapId);

			if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty)
				GetBGObject(SAObjectTypes.BoatOne).BuildOutOfRangeUpdateBlock(transData);

			if (!BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
				GetBGObject(SAObjectTypes.BoatTwo).BuildOutOfRangeUpdateBlock(transData);

			transData.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}

	bool IsGateDestroyed(uint gateId)
	{
		return GateStatus[gateId] == SAGateState.AllianceGateDestroyed || GateStatus[gateId] == SAGateState.HordeGateDestroyed;
	}

	SAGateInfo GetGate(uint entry)
	{
		foreach (var gate in SAMiscConst.Gates)
			if (gate.GameObjectId == entry)
				return gate;

		return null;
	}
}

class BattlegroundSAScore : BattlegroundScore
{
	uint DemolishersDestroyed;
	uint GatesDestroyed;
	public BattlegroundSAScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team) { }

	public override void UpdateScore(ScoreType type, uint value)
	{
		switch (type)
		{
			case ScoreType.DestroyedDemolisher:
				DemolishersDestroyed += value;

				break;
			case ScoreType.DestroyedWall:
				GatesDestroyed += value;

				break;
			default:
				base.UpdateScore(type, value);

				break;
		}
	}

	public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
	{
		base.BuildPvPLogPlayerDataPacket(out playerData);

		playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SAObjectives.DemolishersDestroyed, DemolishersDestroyed));
		playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SAObjectives.GatesDestroyed, GatesDestroyed));
	}

	public override uint GetAttr1()
	{
		return DemolishersDestroyed;
	}

	public override uint GetAttr2()
	{
		return GatesDestroyed;
	}
}

struct SARoundScore
{
	public uint winner;
	public uint time;
}

class SAGateInfo
{
	public uint GateId;
	public uint GameObjectId;
	public uint WorldState;
	public uint DamagedText;
	public uint DestroyedText;

	public SAGateInfo(uint gateId, uint gameObjectId, uint worldState, uint damagedText, uint destroyedText)
	{
		GateId = gateId;
		GameObjectId = gameObjectId;
		WorldState = worldState;
		DamagedText = damagedText;
		DestroyedText = destroyedText;
	}
}

#region Consts

struct SAMiscConst
{
	public static uint[] NpcEntries =
	{
		SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon, SACreatureIds.AntiPersonnalCannon,
		// 4 beach demolishers
		SACreatureIds.Demolisher, SACreatureIds.Demolisher, SACreatureIds.Demolisher, SACreatureIds.Demolisher,
		// 4 factory demolishers
		SACreatureIds.Demolisher, SACreatureIds.Demolisher, SACreatureIds.Demolisher, SACreatureIds.Demolisher,
		// Used Demolisher Salesman
		SACreatureIds.RiggerSparklight, SACreatureIds.GorgrilRigspark,
		// Kanrethad
		SACreatureIds.Kanrethad
	};

	public static Position[] NpcSpawnlocs =
	{
		// Cannons
		new(1436.429f, 110.05f, 41.407f, 5.4f), new(1404.9023f, 84.758f, 41.183f, 5.46f), new(1068.693f, -86.951f, 93.81f, 0.02f), new(1068.83f, -127.56f, 96.45f, 0.0912f), new(1422.115f, -196.433f, 42.1825f, 1.0222f), new(1454.887f, -220.454f, 41.956f, 0.9627f), new(1232.345f, -187.517f, 66.945f, 0.45f), new(1249.634f, -224.189f, 66.72f, 0.635f), new(1236.213f, 92.287f, 64.965f, 5.751f), new(1215.11f, 57.772f, 64.739f, 5.78f),
		// Demolishers
		new(1611.597656f, -117.270073f, 8.719355f, 2.513274f), new(1575.562500f, -158.421875f, 5.024450f, 2.129302f), new(1618.047729f, 61.424641f, 7.248210f, 3.979351f), new(1575.103149f, 98.873344f, 2.830360f, 3.752458f),
		// Demolishers 2
		new(1371.055786f, -317.071136f, 35.007359f, 1.947460f), new(1424.034912f, -260.195190f, 31.084425f, 2.820013f), new(1353.139893f, 223.745438f, 35.265411f, 4.343684f), new(1404.809570f, 197.027237f, 32.046032f, 3.605401f),
		// Npcs
		new(1348.644165f, -298.786469f, 31.080130f, 1.710423f), new(1358.191040f, 195.527786f, 31.018187f, 4.171337f), new(841.921f, -134.194f, 196.838f, 6.23082f)
	};

	public static Position[] ObjSpawnlocs =
	{
		new(1411.57f, 108.163f, 28.692f, 5.441f), new(1055.452f, -108.1f, 82.134f, 0.034f), new(1431.3413f, -219.437f, 30.893f, 0.9736f), new(1227.667f, -212.555f, 55.372f, 0.5023f), new(1214.681f, 81.21f, 53.413f, 5.745f), new(878.555f, -108.2f, 117.845f, 0.0f), new(836.5f, -108.8f, 120.219f, 0.0f),
		// Portal
		new(1468.380005f, -225.798996f, 30.896200f, 0.0f), //blue
		new(1394.270020f, 72.551399f, 31.054300f, 0.0f),   //green
		new(1065.260010f, -89.79501f, 81.073402f, 0.0f),   //yellow
		new(1216.069946f, 47.904301f, 54.278198f, 0.0f),   //purple
		new(1255.569946f, -233.548996f, 56.43699f, 0.0f),  //red
		// Ships
		new(2679.696777f, -826.891235f, 3.712860f, 5.78367f), //rot2 1 rot3 0.0002f
		new(2574.003662f, 981.261475f, 2.603424f, 0.807696f),
		// Sigils
		new(1414.054f, 106.72f, 41.442f, 5.441f), new(1060.63f, -107.8f, 94.7f, 0.034f), new(1433.383f, -216.4f, 43.642f, 0.9736f), new(1230.75f, -210.724f, 67.611f, 0.5023f), new(1217.8f, 79.532f, 66.58f, 5.745f),
		// Flagpoles
		new(1215.114258f, -65.711861f, 70.084267f, -3.124123f), new(1338.863892f, -153.336533f, 30.895121f, -2.530723f), new(1309.124268f, 9.410645f, 30.893402f, -1.623156f),
		// Flags
		new(1215.108032f, -65.715767f, 70.084267f, -3.124123f), new(1338.859253f, -153.327316f, 30.895077f, -2.530723f), new(1309.192017f, 9.416233f, 30.893402f, 1.518436f),
		// Bombs
		new(1333.45f, 211.354f, 31.0538f, 5.03666f), new(1334.29f, 209.582f, 31.0532f, 1.28088f), new(1332.72f, 210.049f, 31.0532f, 1.28088f), new(1334.28f, 210.78f, 31.0538f, 3.85856f), new(1332.64f, 211.39f, 31.0532f, 1.29266f), new(1371.41f, 194.028f, 31.5107f, 0.753095f), new(1372.39f, 194.951f, 31.4679f, 0.753095f), new(1371.58f, 196.942f, 30.9349f, 1.01777f), new(1370.43f, 196.614f, 30.9349f, 0.957299f), new(1369.46f, 196.877f, 30.9351f, 2.45348f), new(1370.35f, 197.361f, 30.9349f, 1.08689f), new(1369.47f, 197.941f, 30.9349f, 0.984787f), new(1592.49f, 47.5969f, 7.52271f, 4.63218f), new(1593.91f, 47.8036f, 7.65856f, 4.63218f), new(1593.13f, 46.8106f, 7.54073f, 4.63218f), new(1589.22f, 36.3616f, 7.45975f, 4.64396f), new(1588.24f, 35.5842f, 7.55613f, 4.79564f), new(1588.14f, 36.7611f, 7.49675f, 4.79564f), new(1595.74f, 35.5278f, 7.46602f, 4.90246f), new(1596, 36.6475f, 7.47991f, 4.90246f), new(1597.03f, 36.2356f, 7.48631f, 4.90246f), new(1597.93f, 37.1214f, 7.51725f, 4.90246f), new(1598.16f, 35.888f, 7.50018f, 4.90246f), new(1579.6f, -98.0917f, 8.48478f, 1.37996f), new(1581.2f, -98.401f, 8.47483f, 1.37996f), new(1580.38f, -98.9556f, 8.4772f, 1.38781f), new(1585.68f, -104.966f, 8.88551f, 0.493246f), new(1586.15f, -106.033f, 9.10616f, 0.493246f), new(1584.88f, -105.394f, 8.82985f, 0.493246f), new(1581.87f, -100.899f, 8.46164f, 0.929142f), new(1581.48f, -99.4657f, 8.46926f, 0.929142f), new(1583.2f, -91.2291f, 8.49227f, 1.40038f), new(1581.94f, -91.0119f, 8.49977f, 1.40038f), new(1582.33f, -91.951f, 8.49353f, 1.1844f), new(1342.06f, -304.049f, 30.9532f, 5.59507f), new(1340.96f, -304.536f, 30.9458f, 1.28323f), new(1341.22f, -303.316f, 30.9413f, 0.486051f), new(1342.22f, -302.939f, 30.986f, 4.87643f), new(1382.16f, -287.466f, 32.3063f, 4.80968f), new(1381, -287.58f, 32.2805f, 4.80968f), new(1381.55f, -286.536f, 32.3929f, 2.84225f), new(1382.75f, -286.354f, 32.4099f, 1.00442f), new(1379.92f, -287.34f, 32.2872f, 3.81615f), new(1100.52f, -2.41391f, 70.2984f, 0.131054f), new(1099.35f, -2.13851f, 70.3375f, 4.4586f), new(1099.59f, -1.00329f, 70.238f, 2.49903f), new(1097.79f, 0.571316f, 70.159f, 4.00307f), new(1098.74f, -7.23252f, 70.7972f, 4.1523f), new(1098.46f, -5.91443f, 70.6715f, 4.1523f), new(1097.53f, -7.39704f, 70.7959f, 4.1523f), new(1097.32f, -6.64233f, 70.7424f, 4.1523f), new(1096.45f, -5.96664f, 70.7242f, 4.1523f), new(971.725f, 0.496763f, 86.8467f, 2.09233f), new(973.589f, 0.119518f, 86.7985f, 3.17225f), new(972.524f, 1.25333f, 86.8351f, 5.28497f), new(971.993f, 2.05668f, 86.8584f, 5.28497f), new(973.635f, 2.11805f, 86.8197f, 2.36722f), new(974.791f, 1.74679f, 86.7942f, 1.5936f), new(974.771f, 3.0445f, 86.8125f, 0.647199f), new(979.554f, 3.6037f, 86.7923f, 1.69178f), new(979.758f, 2.57519f, 86.7748f, 1.76639f), new(980.769f, 3.48904f, 86.7939f, 1.76639f), new(979.122f, 2.87109f, 86.7794f, 1.76639f), new(986.167f, 4.85363f, 86.8439f, 1.5779f), new(986.176f, 3.50367f, 86.8217f, 1.5779f), new(987.33f, 4.67389f, 86.8486f, 1.5779f), new(985.23f, 4.65898f, 86.8368f, 1.5779f), new(984.556f, 3.54097f, 86.8137f, 1.5779f),
	};

	public static uint[] ObjEntries =
	{
		190722, 190727, 190724, 190726, 190723, 192549, 192834, 192819, 192819, 192819, 192819, 192819, 0, // Boat
		0,                                                                                                 // Boat
		192687, 192685, 192689, 192690, 192691, 191311, 191311, 191311, 191310, 191306, 191308, 190753
	};

	public static uint[] Factions =
	{
		1732, 1735,
	};

	public static uint[] GYEntries =
	{
		1350, 1349, 1347, 1346, 1348,
	};

	public static float[] GYOrientation =
	{
		6.202f, 1.926f, // right capturable GY
		3.917f,         // left capturable GY
		3.104f,         // center, capturable
		6.148f,         // defender last GY
	};

	public static SAGateInfo[] Gates =
	{
		new(SAObjectTypes.GreenGate, SAGameObjectIds.GateOfTheGreenEmerald, SAWorldStateIds.GreenGate, SATextIds.GreenGateUnderAttack, SATextIds.GreenGateDestroyed), new(SAObjectTypes.YellowGate, SAGameObjectIds.GateOfTheYellowMoon, SAWorldStateIds.YellowGate, SATextIds.YellowGateUnderAttack, SATextIds.YellowGateDestroyed), new(SAObjectTypes.BlueGate, SAGameObjectIds.GateOfTheBlueSapphire, SAWorldStateIds.BlueGate, SATextIds.BlueGateUnderAttack, SATextIds.BlueGateDestroyed), new(SAObjectTypes.RedGate, SAGameObjectIds.GateOfTheRedSun, SAWorldStateIds.RedGate, SATextIds.RedGateUnderAttack, SATextIds.RedGateDestroyed), new(SAObjectTypes.PurpleGate, SAGameObjectIds.GateOfThePurpleAmethyst, SAWorldStateIds.PurpleGate, SATextIds.PurpleGateUnderAttack, SATextIds.PurpleGateDestroyed), new(SAObjectTypes.AncientGate, SAGameObjectIds.ChamberOfAncientRelics, SAWorldStateIds.AncientGate, SATextIds.AncientGateUnderAttack, SATextIds.AncientGateDestroyed)
	};
}

struct SABroadcastTexts
{
	public const uint AllianceCapturedTitanPortal = 28944;
	public const uint HordeCapturedTitanPortal = 28945;

	public const uint RoundTwoStartOneMinute = 29448;
	public const uint RoundTwoStartHalfMinute = 29449;
}

enum SAStatus
{
	NotStarted = 0,
	Warmup,
	RoundOne,
	SecondWarmup,
	RoundTwo,
	BonusRound
}

enum SAGateState
{
	// alliance is defender
	AllianceGateOk = 1,
	AllianceGateDamaged = 2,
	AllianceGateDestroyed = 3,

	// horde is defender
	HordeGateOk = 4,
	HordeGateDamaged = 5,
	HordeGateDestroyed = 6,
}

enum SAEventIds
{
	BG_SA_EVENT_BLUE_GATE_DAMAGED = 19040,
	BG_SA_EVENT_BLUE_GATE_DESTROYED = 19045,

	BG_SA_EVENT_GREEN_GATE_DAMAGED = 19041,
	BG_SA_EVENT_GREEN_GATE_DESTROYED = 19046,

	BG_SA_EVENT_RED_GATE_DAMAGED = 19042,
	BG_SA_EVENT_RED_GATE_DESTROYED = 19047,

	BG_SA_EVENT_PURPLE_GATE_DAMAGED = 19043,
	BG_SA_EVENT_PURPLE_GATE_DESTROYED = 19048,

	BG_SA_EVENT_YELLOW_GATE_DAMAGED = 19044,
	BG_SA_EVENT_YELLOW_GATE_DESTROYED = 19049,

	BG_SA_EVENT_ANCIENT_GATE_DAMAGED = 19836,
	BG_SA_EVENT_ANCIENT_GATE_DESTROYED = 19837,

	BG_SA_EVENT_TITAN_RELIC_ACTIVATED = 22097
}

struct SASpellIds
{
	public const uint TeleportDefender = 52364;
	public const uint TeleportAttackers = 60178;
	public const uint EndOfRound = 52459;
	public const uint RemoveSeaforium = 59077;
	public const uint AllianceControlPhaseShift = 60027;
	public const uint HordeControlPhaseShift = 60028;
}

struct SACreatureIds
{
	public const uint Kanrethad = 29;
	public const uint InvisibleStalker = 15214;
	public const uint WorldTrigger = 22515;
	public const uint WorldTriggerLargeAoiNotImmunePcNpc = 23472;

	public const uint AntiPersonnalCannon = 27894;
	public const uint Demolisher = 28781;
	public const uint RiggerSparklight = 29260;
	public const uint GorgrilRigspark = 29262;
}

struct SAGameObjectIds
{
	public const uint GateOfTheGreenEmerald = 190722;
	public const uint GateOfThePurpleAmethyst = 190723;
	public const uint GateOfTheBlueSapphire = 190724;
	public const uint GateOfTheRedSun = 190726;
	public const uint GateOfTheYellowMoon = 190727;
	public const uint ChamberOfAncientRelics = 192549;

	public const uint BoatOneA = 208000;
	public const uint BoatTwoA = 193185;
	public const uint BoatOneH = 193184;
	public const uint BoatTwoH = 208001;
}

struct SATimers
{
	public const uint BoatStart = 60 * Time.InMilliseconds;
	public const uint WarmupLength = 120 * Time.InMilliseconds;
	public const uint RoundLength = 600 * Time.InMilliseconds;
}

struct SASoundIds
{
	public const uint GraveyardTakenHorde = 8174;
	public const uint GraveyardTakenAlliance = 8212;
	public const uint DefeatHorde = 15905;
	public const uint VictoryHorde = 15906;
	public const uint VictoryAlliance = 15907;
	public const uint DefeatAlliance = 15908;
	public const uint WallDestroyedAlliance = 15909;
	public const uint WallDestroyedHorde = 15910;
	public const uint WallAttackedHorde = 15911;
	public const uint WallAttackedAlliance = 15912;
}

struct SATextIds
{
	// Kanrethad
	public const byte RoundStarted = 1;
	public const byte Round1Finished = 2;

	// Rigger Sparklight / Gorgril Rigspark
	public const byte SparklightRigsparkSpawn = 1;

	// World Trigger
	public const byte BlueGateUnderAttack = 1;
	public const byte GreenGateUnderAttack = 2;
	public const byte RedGateUnderAttack = 3;
	public const byte PurpleGateUnderAttack = 4;
	public const byte YellowGateUnderAttack = 5;
	public const byte YellowGateDestroyed = 6;
	public const byte PurpleGateDestroyed = 7;
	public const byte RedGateDestroyed = 8;
	public const byte GreenGateDestroyed = 9;
	public const byte BlueGateDestroyed = 10;
	public const byte EastGraveyardCapturedA = 11;
	public const byte WestGraveyardCapturedA = 12;
	public const byte SouthGraveyardCapturedA = 13;
	public const byte EastGraveyardCapturedH = 14;
	public const byte WestGraveyardCapturedH = 15;
	public const byte SouthGraveyardCapturedH = 16;
	public const byte AncientGateUnderAttack = 17;
	public const byte AncientGateDestroyed = 18;
}

struct SAWorldStateIds
{
	public const uint Timer = 3557;
	public const uint AllyAttacks = 4352;
	public const uint HordeAttacks = 4353;
	public const uint PurpleGate = 3614;
	public const uint RedGate = 3617;
	public const uint BlueGate = 3620;
	public const uint GreenGate = 3623;
	public const uint YellowGate = 3638;
	public const uint AncientGate = 3849;
	public const uint LeftGyAlliance = 3635;
	public const uint RightGyAlliance = 3636;
	public const uint CenterGyAlliance = 3637;
	public const uint RightAttTokenAll = 3627;
	public const uint LeftAttTokenAll = 3626;
	public const uint LeftAttTokenHrd = 3629;
	public const uint RightAttTokenHrd = 3628;
	public const uint HordeDefenceToken = 3631;
	public const uint AllianceDefenceToken = 3630;
	public const uint RightGyHorde = 3632;
	public const uint LeftGyHorde = 3633;
	public const uint CenterGyHorde = 3634;
	public const uint BonusTimer = 3571;
	public const uint EnableTimer = 3564;
	public const uint AttackerTeam = 3690;
	public const uint DestroyedAllianceVehicles = 3955;
	public const uint DestroyedHordeVehicles = 3956;
}

struct SACreatureTypes
{
	public const int Gun1 = 0;
	public const int Gun2 = 1;
	public const int Gun3 = 2;
	public const int Gun4 = 3;
	public const int Gun5 = 4;
	public const int Gun6 = 5;
	public const int Gun7 = 6;
	public const int Gun8 = 7;
	public const int Gun9 = 8;
	public const int Gun10 = 9;
	public const int Demolisher1 = 10;
	public const int Demolisher2 = 11;
	public const int Demolisher3 = 12;
	public const int Demolisher4 = 13;
	public const int Demolisher5 = 14;
	public const int Demolisher6 = 15;
	public const int Demolisher7 = 16;
	public const int Demolisher8 = 17;
	public const int Sparklight = 18;
	public const int Rigspark = 19;
	public const int Kanrethad = 20;
	public const int Max = 21;
}

struct SAObjectTypes
{
	public const int GreenGate = 0;
	public const int YellowGate = 1;
	public const int BlueGate = 2;
	public const int RedGate = 3;
	public const int PurpleGate = 4;
	public const int AncientGate = 5;
	public const int TitanRelic = 6;
	public const int PortalDeffenderBlue = 7;
	public const int PortalDeffenderGreen = 8;
	public const int PortalDeffenderYellow = 9;
	public const int PortalDeffenderPurple = 10;
	public const int PortalDeffenderRed = 11;
	public const int BoatOne = 12;
	public const int BoatTwo = 13;
	public const int Sigil1 = 14;
	public const int Sigil2 = 15;
	public const int Sigil3 = 16;
	public const int Sigil4 = 17;
	public const int Sigil5 = 18;
	public const int CentralFlagpole = 19;
	public const int RightFlagpole = 20;
	public const int LeftFlagpole = 21;
	public const int CentralFlag = 22;
	public const int RightFlag = 23;
	public const int LeftFlag = 24;
	public const int Bomb = 25;
	public const int MaxObj = Bomb + 68;
}

struct SAGraveyards
{
	public const int BeachGy = 0;
	public const int DefenderLastGy = 1;
	public const int RightCapturableGy = 2;
	public const int LeftCapturableGy = 3;
	public const int CentralCapturableGy = 4;
	public const int Max = 5;
}

enum SAObjectives
{
	GatesDestroyed = 231,
	DemolishersDestroyed = 232
}

#endregion