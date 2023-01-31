﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Maps;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.MoveChangeTransport, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveDoubleJump, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveFallLand, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveFallReset, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveHeartbeat, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveJump, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetFacing, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetFacingHeartbeat, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetFly, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetPitch, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetRunMode, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetWalkMode, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartAscend, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartBackward, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartDescend, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartForward, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartPitchDown, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartPitchUp, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartStrafeLeft, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartStrafeRight, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartSwim, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartTurnLeft, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartTurnRight, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStop, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopAscend, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopPitch, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopStrafe, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopSwim, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopTurn, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveUpdateFallSpeed, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMovement(ClientPlayerMovement packet)
        {
            HandleMovementOpcode(packet.GetOpcode(), packet.Status);
        }

        private void HandleMovementOpcode(ClientOpcodes opcode, MovementInfo movementInfo)
        {
            Unit mover = GetPlayer().GetUnitBeingMoved();
            Player plrMover = mover.ToPlayer();

            if (plrMover && plrMover.IsBeingTeleported())
                return;

            GetPlayer().ValidateMovementInfo(movementInfo);

            if (movementInfo.Guid != mover.GetGUID())
            {
                Log.outError(LogFilter.Network, "HandleMovementOpcodes: Guid error");

                return;
            }

            if (!movementInfo.Pos.IsPositionValid())
                return;


            if (!mover.MoveSpline.Finalized())
                return;

            // stop some emotes at player move
            if (plrMover && (plrMover.GetEmoteState() != 0))
                plrMover.SetEmoteState(Emote.OneshotNone);

            //handle special cases
            if (!movementInfo.Transport.Guid.IsEmpty())
            {
                // We were teleported, skip packets that were broadcast before teleport
                if (movementInfo.Pos.GetExactDist2d(mover) > MapConst.SizeofGrids)
                    return;

                if (Math.Abs(movementInfo.Transport.Pos.GetPositionX()) > 75f ||
                    Math.Abs(movementInfo.Transport.Pos.GetPositionY()) > 75f ||
                    Math.Abs(movementInfo.Transport.Pos.GetPositionZ()) > 75f)
                    return;

                if (!GridDefines.IsValidMapCoord(movementInfo.Pos.X + movementInfo.Transport.Pos.X,
                                                 movementInfo.Pos.Y + movementInfo.Transport.Pos.Y,
                                                 movementInfo.Pos.Z + movementInfo.Transport.Pos.Z,
                                                 movementInfo.Pos.Orientation + movementInfo.Transport.Pos.Orientation))
                    return;

                if (plrMover)
                {
                    if (plrMover.GetTransport() == null)
                    {
                        GameObject go = plrMover.GetMap().GetGameObject(movementInfo.Transport.Guid);

                        if (go != null)
                        {
                            ITransport transport = go.ToTransportBase();

                            transport?.AddPassenger(plrMover);
                        }
                    }
                    else if (plrMover.GetTransport().GetTransportGUID() != movementInfo.Transport.Guid)
                    {
                        plrMover.GetTransport().RemovePassenger(plrMover);
                        GameObject go = plrMover.GetMap().GetGameObject(movementInfo.Transport.Guid);

                        if (go != null)
                        {
                            ITransport transport = go.ToTransportBase();

                            if (transport != null)
                                transport.AddPassenger(plrMover);
                            else
                                movementInfo.ResetTransport();
                        }
                        else
                        {
                            movementInfo.ResetTransport();
                        }
                    }
                }

                if (mover.GetTransport() == null &&
                    !mover.GetVehicle())
                    movementInfo.Transport.Reset();
            }
            else if (plrMover && plrMover.GetTransport() != null) // if we were on a Transport, leave
            {
                plrMover.GetTransport().RemovePassenger(plrMover);
            }

            // fall Damage generation (ignore in flight case that can be triggered also at lags in moment teleportation to another map).
            if (opcode == ClientOpcodes.MoveFallLand &&
                plrMover &&
                !plrMover.IsInFlight())
                plrMover.HandleFall(movementInfo);

            // interrupt parachutes upon falling or landing in water
            if (opcode == ClientOpcodes.MoveFallLand ||
                opcode == ClientOpcodes.MoveStartSwim ||
                opcode == ClientOpcodes.MoveSetFly)
                mover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LandingOrFlight); // Parachutes

            movementInfo.Guid = mover.GetGUID();
            movementInfo.Time = AdjustClientMovementTime(movementInfo.Time);
            mover.MovementInfo = movementInfo;

            // Some vehicles allow the passenger to turn by himself
            Vehicle vehicle = mover.GetVehicle();

            if (vehicle)
            {
                VehicleSeatRecord seat = vehicle.GetSeatForPassenger(mover);

                if (seat != null)
                    if (seat.HasFlag(VehicleSeatFlags.AllowTurning))
                        if (movementInfo.Pos.GetOrientation() != mover.GetOrientation())
                        {
                            mover.SetOrientation(movementInfo.Pos.GetOrientation());
                            mover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Turning);
                        }

                return;
            }

            mover.UpdatePosition(movementInfo.Pos);

            MoveUpdate moveUpdate = new();
            moveUpdate.Status = mover.MovementInfo;
            mover.SendMessageToSet(moveUpdate, GetPlayer());

            if (plrMover) // nothing is charmed, or player charmed
            {
                if (plrMover.IsSitState() &&
                    movementInfo.HasMovementFlag(MovementFlag.MaskMoving | MovementFlag.MaskTurning))
                    plrMover.SetStandState(UnitStandStateType.Stand);

                plrMover.UpdateFallInformationIfNeed(movementInfo, opcode);

                if (movementInfo.Pos.Z < plrMover.GetMap().GetMinHeight(plrMover.GetPhaseShift(), movementInfo.Pos.GetPositionX(), movementInfo.Pos.GetPositionY()))
                {
                    if (!(plrMover.GetBattleground() && plrMover.GetBattleground().HandlePlayerUnderMap(GetPlayer())))
                        // NOTE: this is actually called many times while falling
                        // even after the player has been teleported away
                        // @todo discard movement packets after the player is rooted
                        if (plrMover.IsAlive())
                        {
                            Log.outDebug(LogFilter.Player, $"FALLDAMAGE Below map. Map min height: {plrMover.GetMap().GetMinHeight(plrMover.GetPhaseShift(), movementInfo.Pos.GetPositionX(), movementInfo.Pos.GetPositionY())}, Player debug info:\n{plrMover.GetDebugInfo()}");
                            plrMover.SetPlayerFlag(PlayerFlags.IsOutOfBounds);
                            plrMover.EnvironmentalDamage(EnviromentalDamage.FallToVoid, (uint)GetPlayer().GetMaxHealth());

                            // player can be alive if GM/etc
                            // change the death State to CORPSE to prevent the death timer from
                            // starting in the next player update
                            if (plrMover.IsAlive())
                                plrMover.KillPlayer();
                        }
                }
                else
                {
                    plrMover.RemovePlayerFlag(PlayerFlags.IsOutOfBounds);
                }

                if (opcode == ClientOpcodes.MoveJump)
                {
                    plrMover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.Jump); // Mind Control
                    Unit.ProcSkillsAndAuras(plrMover, null, new ProcFlagsInit(ProcFlags.Jump), new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.WorldPortResponse, Status = SessionStatus.Transfer)]
        private void HandleMoveWorldportAck(WorldPortResponse packet)
        {
            HandleMoveWorldportAck();
        }

        private void HandleMoveWorldportAck()
        {
            Player player = GetPlayer();

            // ignore unexpected far teleports
            if (!player.IsBeingTeleportedFar())
                return;

            bool seamlessTeleport = player.IsBeingTeleportedSeamlessly();
            player.SetSemaphoreTeleportFar(false);

            // get the teleport destination
            WorldLocation loc = player.GetTeleportDest();

            // possible errors in the coordinate validity check
            if (!GridDefines.IsValidMapCoord(loc))
            {
                LogoutPlayer(false);

                return;
            }

            // get the destination map entry, not the current one, this will fix _homebind and reset greeting
            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(loc.GetMapId());

            // reset instance validity, except if going to an instance inside an instance
            if (!player.InstanceValid &&
                !mapEntry.IsDungeon())
                player.InstanceValid = true;

            Map oldMap = player.GetMap();
            Map newMap = GetPlayer().GetTeleportDestInstanceId().HasValue ? Global.MapMgr.FindMap(loc.GetMapId(), GetPlayer().GetTeleportDestInstanceId().Value) : Global.MapMgr.CreateMap(loc.GetMapId(), GetPlayer());

            MovementInfo.TransportInfo transportInfo = player.MovementInfo.Transport;
            ITransport transport = player.GetTransport();

            transport?.RemovePassenger(player);

            if (player.IsInWorld)
            {
                Log.outError(LogFilter.Network, $"Player (Name {player.GetName()}) is still in world when teleported from map {oldMap.GetId()} to new map {loc.GetMapId()}");
                oldMap.RemovePlayerFromMap(player, false);
            }

            // relocate the player to the teleport destination
            // the CannotEnter checks are done in TeleporTo but conditions may change
            // while the player is in transit, for example the map may get full
            if (newMap == null ||
                newMap.CannotEnter(player) != null)
            {
                Log.outError(LogFilter.Network, $"Map {loc.GetMapId()} could not be created for {(newMap ? newMap.GetMapName() : "Unknown")} ({player.GetGUID()}), porting player to _homebind");
                player.TeleportTo(player.GetHomebind());

                return;
            }

            float z = loc.GetPositionZ() + player.GetHoverOffset();
            player.Relocate(loc.GetPositionX(), loc.GetPositionY(), z, loc.GetOrientation());
            player.SetFallInformation(0, player.GetPositionZ());

            player.ResetMap();
            player.SetMap(newMap);

            ResumeToken resumeToken = new();
            resumeToken.SequenceIndex = player.MovementCounter;
            resumeToken.Reason = seamlessTeleport ? 2 : 1u;
            SendPacket(resumeToken);

            if (!seamlessTeleport)
                player.SendInitialPacketsBeforeAddToMap();

            // move player between Transport copies on each map
            Transport newTransport = newMap.GetTransport(transportInfo.Guid);

            if (newTransport != null)
            {
                player.MovementInfo.Transport = transportInfo;
                newTransport.AddPassenger(player);
            }

            if (!player.GetMap().AddPlayerToMap(player, !seamlessTeleport))
            {
                Log.outError(LogFilter.Network, $"WORLD: failed to teleport player {player.GetName()} ({player.GetGUID()}) to map {loc.GetMapId()} ({(newMap ? newMap.GetMapName() : "Unknown")}) because of unknown reason!");
                player.ResetMap();
                player.SetMap(oldMap);
                player.TeleportTo(player.GetHomebind());

                return;
            }

            // Battleground State prepare (in case join to BG), at relogin/tele player not invited
            // only add to bg group and object, if the player was invited (else he entered through command)
            if (player.InBattleground())
            {
                // cleanup setting if outdated
                if (!mapEntry.IsBattlegroundOrArena())
                {
                    // We're not in BG
                    player.SetBattlegroundId(0, BattlegroundTypeId.None);
                    // reset destination bg team
                    player.SetBGTeam(0);
                }
                // join to bg case
                else
                {
                    Battleground bg = player.GetBattleground();

                    if (bg)
                        if (player.IsInvitedForBattlegroundInstance(player.GetBattlegroundId()))
                            bg.AddPlayer(player);
                }
            }

            if (!seamlessTeleport)
            {
                player.SendInitialPacketsAfterAddToMap();
            }
            else
            {
                player.UpdateVisibilityForPlayer();
                Garrison garrison = player.GetGarrison();

                garrison?.SendRemoteInfo();
            }

            // flight fast teleport case
            if (player.IsInFlight())
            {
                if (!player.InBattleground())
                {
                    if (!seamlessTeleport)
                    {
                        // short preparations to continue flight
                        MovementGenerator movementGenerator = player.GetMotionMaster().GetCurrentMovementGenerator();
                        movementGenerator.Initialize(player);
                    }

                    return;
                }

                // Battlegroundstate prepare, stop flight
                player.FinishTaxiFlight();
            }

            if (!player.IsAlive() &&
                player.GetTeleportOptions().HasAnyFlag(TeleportToOptions.ReviveAtTeleport))
                player.ResurrectPlayer(0.5f);

            // resurrect character at enter into instance where his corpse exist after add to map
            if (mapEntry.IsDungeon() &&
                !player.IsAlive())
                if (player.GetCorpseLocation().GetMapId() == mapEntry.Id)
                {
                    player.ResurrectPlayer(0.5f, false);
                    player.SpawnCorpseBones();
                }

            if (mapEntry.IsDungeon())
            {
                // check if this instance has a reset Time and send it to player if so
                MapDb2Entries entries = new(mapEntry.Id, newMap.GetDifficultyID());

                if (entries.MapDifficulty.HasResetSchedule())
                {
                    RaidInstanceMessage raidInstanceMessage = new();
                    raidInstanceMessage.Type = InstanceResetWarningType.Welcome;
                    raidInstanceMessage.MapID = mapEntry.Id;
                    raidInstanceMessage.DifficultyID = newMap.GetDifficultyID();

                    InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(GetPlayer().GetGUID(), entries);

                    if (playerLock != null)
                    {
                        raidInstanceMessage.Locked = !playerLock.IsExpired();
                        raidInstanceMessage.Extended = playerLock.IsExtended();
                    }
                    else
                    {
                        raidInstanceMessage.Locked = false;
                        raidInstanceMessage.Extended = false;
                    }

                    SendPacket(raidInstanceMessage);
                }

                // check if instance is valid
                if (!player.CheckInstanceValidity(false))
                    player.InstanceValid = false;
            }

            // update zone immediately, otherwise leave channel will cause crash in mtmap
            player.GetZoneAndAreaId(out uint newzone, out uint newarea);
            player.UpdateZone(newzone, newarea);

            // honorless Target
            if (player.PvpInfo.IsHostile)
                player.CastSpell(player, 2479, true);

            // in friendly area
            else if (player.IsPvP() &&
                     !player.HasPlayerFlag(PlayerFlags.InPVP))
                player.UpdatePvP(false, false);

            // resummon pet
            player.ResummonPetTemporaryUnSummonedIfAny();

            //lets process all delayed operations on successful teleport
            player.ProcessDelayedOperations();
        }

        [WorldPacketHandler(ClientOpcodes.SuspendTokenResponse, Status = SessionStatus.Transfer)]
        private void HandleSuspendTokenResponse(SuspendTokenResponse suspendTokenResponse)
        {
            if (!_player.IsBeingTeleportedFar())
                return;

            WorldLocation loc = GetPlayer().GetTeleportDest();

            if (CliDB.MapStorage.LookupByKey(loc.GetMapId()).IsDungeon())
            {
                UpdateLastInstance updateLastInstance = new();
                updateLastInstance.MapID = loc.GetMapId();
                SendPacket(updateLastInstance);
            }

            NewWorld packet = new();
            packet.MapID = loc.GetMapId();
            packet.Loc.Pos = loc;
            packet.Reason = (uint)(!_player.IsBeingTeleportedSeamlessly() ? NewWorldReason.Normal : NewWorldReason.Seamless);
            SendPacket(packet);

            if (_player.IsBeingTeleportedSeamlessly())
                HandleMoveWorldportAck();
        }

        [WorldPacketHandler(ClientOpcodes.MoveTeleportAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMoveTeleportAck(MoveTeleportAck packet)
        {
            Player plMover = GetPlayer().GetUnitBeingMoved().ToPlayer();

            if (!plMover ||
                !plMover.IsBeingTeleportedNear())
                return;

            if (packet.MoverGUID != plMover.GetGUID())
                return;

            plMover.SetSemaphoreTeleportNear(false);

            uint old_zone = plMover.GetZoneId();

            WorldLocation dest = plMover.GetTeleportDest();

            plMover.UpdatePosition(dest, true);
            plMover.SetFallInformation(0, GetPlayer().GetPositionZ());

            uint newzone, newarea;
            plMover.GetZoneAndAreaId(out newzone, out newarea);
            plMover.UpdateZone(newzone, newarea);

            // new zone
            if (old_zone != newzone)
            {
                // honorless Target
                if (plMover.PvpInfo.IsHostile)
                    plMover.CastSpell(plMover, 2479, true);

                // in friendly area
                else if (plMover.IsPvP() &&
                         !plMover.HasPlayerFlag(PlayerFlags.InPVP))
                    plMover.UpdatePvP(false, false);
            }

            // resummon pet
            GetPlayer().ResummonPetTemporaryUnSummonedIfAny();

            //lets process all delayed operations on successful teleport
            GetPlayer().ProcessDelayedOperations();
        }

        [WorldPacketHandler(ClientOpcodes.MoveForceFlightBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceFlightSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForcePitchRateChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceRunBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceRunSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceSwimBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceSwimSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceTurnRateChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceWalkSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleForceSpeedChangeAck(MovementSpeedAck packet)
        {
            GetPlayer().ValidateMovementInfo(packet.Ack.Status);

            // now can skip not our packet
            if (GetPlayer().GetGUID() != packet.Ack.Status.Guid)
                return;

            /*----------------*/
            // client ACK send one packet for mounted/run case and need skip all except last from its
            // in other cases anti-cheat check can be fail in false case
            UnitMoveType move_type;

            ClientOpcodes opcode = packet.GetOpcode();

            switch (opcode)
            {
                case ClientOpcodes.MoveForceWalkSpeedChangeAck:
                    move_type = UnitMoveType.Walk;

                    break;
                case ClientOpcodes.MoveForceRunSpeedChangeAck:
                    move_type = UnitMoveType.Run;

                    break;
                case ClientOpcodes.MoveForceRunBackSpeedChangeAck:
                    move_type = UnitMoveType.RunBack;

                    break;
                case ClientOpcodes.MoveForceSwimSpeedChangeAck:
                    move_type = UnitMoveType.Swim;

                    break;
                case ClientOpcodes.MoveForceSwimBackSpeedChangeAck:
                    move_type = UnitMoveType.SwimBack;

                    break;
                case ClientOpcodes.MoveForceTurnRateChangeAck:
                    move_type = UnitMoveType.TurnRate;

                    break;
                case ClientOpcodes.MoveForceFlightSpeedChangeAck:
                    move_type = UnitMoveType.Flight;

                    break;
                case ClientOpcodes.MoveForceFlightBackSpeedChangeAck:
                    move_type = UnitMoveType.FlightBack;

                    break;
                case ClientOpcodes.MoveForcePitchRateChangeAck:
                    move_type = UnitMoveType.PitchRate;

                    break;
                default:
                    Log.outError(LogFilter.Network, "WorldSession.HandleForceSpeedChangeAck: Unknown move Type opcode: {0}", opcode);

                    return;
            }

            // skip all forced speed changes except last and unexpected
            // in run/mounted case used one ACK and it must be skipped. _forced_speed_changes[MOVE_RUN] store both.
            if (GetPlayer().ForcedSpeedChanges[(int)move_type] > 0)
            {
                --GetPlayer().ForcedSpeedChanges[(int)move_type];

                if (GetPlayer().ForcedSpeedChanges[(int)move_type] > 0)
                    return;
            }

            if (GetPlayer().GetTransport() == null &&
                Math.Abs(GetPlayer().GetSpeed(move_type) - packet.Speed) > 0.01f)
            {
                if (GetPlayer().GetSpeed(move_type) > packet.Speed) // must be greater - just correct
                {
                    Log.outError(LogFilter.Network,
                                 "{0}SpeedChange player {1} is NOT correct (must be {2} instead {3}), Force set to correct value",
                                 move_type,
                                 GetPlayer().GetName(),
                                 GetPlayer().GetSpeed(move_type),
                                 packet.Speed);

                    GetPlayer().SetSpeedRate(move_type, GetPlayer().GetSpeedRate(move_type));
                }
                else // must be lesser - cheating
                {
                    Log.outDebug(LogFilter.Server,
                                 "Player {0} from account Id {1} kicked for incorrect speed (must be {2} instead {3})",
                                 GetPlayer().GetName(),
                                 GetPlayer().Session.GetAccountId(),
                                 GetPlayer().GetSpeed(move_type),
                                 packet.Speed);

                    GetPlayer().Session.KickPlayer("WorldSession::HandleForceSpeedChangeAck Incorrect speed");
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetActiveMover)]
        private void HandleSetActiveMover(SetActiveMover packet)
        {
            if (GetPlayer().IsInWorld)
                if (_player.GetUnitBeingMoved().GetGUID() != packet.ActiveMover)
                    Log.outError(LogFilter.Network, "HandleSetActiveMover: incorrect mover Guid: mover is {0} and should be {1},", packet.ActiveMover.ToString(), _player.GetUnitBeingMoved().GetGUID().ToString());
        }

        [WorldPacketHandler(ClientOpcodes.MoveKnockBackAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMoveKnockBackAck(MoveKnockBackAck movementAck)
        {
            GetPlayer().ValidateMovementInfo(movementAck.Ack.Status);

            if (GetPlayer().GetUnitBeingMoved().GetGUID() != movementAck.Ack.Status.Guid)
                return;

            movementAck.Ack.Status.Time = AdjustClientMovementTime(movementAck.Ack.Status.Time);
            GetPlayer().MovementInfo = movementAck.Ack.Status;

            MoveUpdateKnockBack updateKnockBack = new();
            updateKnockBack.Status = GetPlayer().MovementInfo;
            GetPlayer().SendMessageToSet(updateKnockBack, false);
        }

        [WorldPacketHandler(ClientOpcodes.MoveEnableDoubleJumpAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveEnableSwimToFlyTransAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveFeatherFallAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceRootAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceUnrootAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveGravityDisableAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveGravityEnableAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveHoverAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetCanFlyAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetCanTurnWhileFallingAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetIgnoreMovementForcesAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveWaterWalkAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMovementAckMessage(MovementAckMessage movementAck)
        {
            GetPlayer().ValidateMovementInfo(movementAck.Ack.Status);
        }

        [WorldPacketHandler(ClientOpcodes.SummonResponse)]
        private void HandleSummonResponseOpcode(SummonResponse packet)
        {
            if (!GetPlayer().IsAlive() ||
                GetPlayer().IsInCombat())
                return;

            GetPlayer().SummonIfPossible(packet.Accept);
        }

        [WorldPacketHandler(ClientOpcodes.MoveSetCollisionHeightAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleSetCollisionHeightAck(MoveSetCollisionHeightAck packet)
        {
            GetPlayer().ValidateMovementInfo(packet.Data.Status);
        }

        [WorldPacketHandler(ClientOpcodes.MoveApplyMovementForceAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMoveApplyMovementForceAck(MoveApplyMovementForceAck moveApplyMovementForceAck)
        {
            Unit mover = _player.GetUnitBeingMoved();
            Cypher.Assert(mover != null);
            _player.ValidateMovementInfo(moveApplyMovementForceAck.Ack.Status);

            // prevent tampered movement _data
            if (moveApplyMovementForceAck.Ack.Status.Guid != mover.GetGUID())
            {
                Log.outError(LogFilter.Network, $"HandleMoveApplyMovementForceAck: Guid error, expected {mover.GetGUID()}, got {moveApplyMovementForceAck.Ack.Status.Guid}");

                return;
            }

            moveApplyMovementForceAck.Ack.Status.Time = AdjustClientMovementTime(moveApplyMovementForceAck.Ack.Status.Time);

            MoveUpdateApplyMovementForce updateApplyMovementForce = new();
            updateApplyMovementForce.Status = moveApplyMovementForceAck.Ack.Status;
            updateApplyMovementForce.Force = moveApplyMovementForceAck.Force;
            mover.SendMessageToSet(updateApplyMovementForce, false);
        }

        [WorldPacketHandler(ClientOpcodes.MoveRemoveMovementForceAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMoveRemoveMovementForceAck(MoveRemoveMovementForceAck moveRemoveMovementForceAck)
        {
            Unit mover = _player.GetUnitBeingMoved();
            Cypher.Assert(mover != null);
            _player.ValidateMovementInfo(moveRemoveMovementForceAck.Ack.Status);

            // prevent tampered movement _data
            if (moveRemoveMovementForceAck.Ack.Status.Guid != mover.GetGUID())
            {
                Log.outError(LogFilter.Network, $"HandleMoveRemoveMovementForceAck: Guid error, expected {mover.GetGUID()}, got {moveRemoveMovementForceAck.Ack.Status.Guid}");

                return;
            }

            moveRemoveMovementForceAck.Ack.Status.Time = AdjustClientMovementTime(moveRemoveMovementForceAck.Ack.Status.Time);

            MoveUpdateRemoveMovementForce updateRemoveMovementForce = new();
            updateRemoveMovementForce.Status = moveRemoveMovementForceAck.Ack.Status;
            updateRemoveMovementForce.TriggerGUID = moveRemoveMovementForceAck.ID;
            mover.SendMessageToSet(updateRemoveMovementForce, false);
        }

        [WorldPacketHandler(ClientOpcodes.MoveSetModMovementForceMagnitudeAck, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMoveSetModMovementForceMagnitudeAck(MovementSpeedAck setModMovementForceMagnitudeAck)
        {
            Unit mover = _player.GetUnitBeingMoved();
            Cypher.Assert(mover != null); // there must always be a mover
            _player.ValidateMovementInfo(setModMovementForceMagnitudeAck.Ack.Status);

            // prevent tampered movement _data
            if (setModMovementForceMagnitudeAck.Ack.Status.Guid != mover.GetGUID())
            {
                Log.outError(LogFilter.Network, $"HandleSetModMovementForceMagnitudeAck: Guid error, expected {mover.GetGUID()}, got {setModMovementForceMagnitudeAck.Ack.Status.Guid}");

                return;
            }

            // skip all except last
            if (_player.MovementForceModMagnitudeChanges > 0)
            {
                --_player.MovementForceModMagnitudeChanges;

                if (_player.MovementForceModMagnitudeChanges == 0)
                {
                    float expectedModMagnitude = 1.0f;
                    MovementForces movementForces = mover.GetMovementForces();

                    if (movementForces != null)
                        expectedModMagnitude = movementForces.GetModMagnitude();

                    if (Math.Abs(expectedModMagnitude - setModMovementForceMagnitudeAck.Speed) > 0.01f)
                    {
                        Log.outDebug(LogFilter.Misc, $"Player {_player.GetName()} from account Id {_player.Session.GetAccountId()} kicked for incorrect movement Force magnitude (must be {expectedModMagnitude} instead {setModMovementForceMagnitudeAck.Speed})");
                        _player.Session.KickPlayer("WorldSession::HandleMoveSetModMovementForceMagnitudeAck Incorrect magnitude");

                        return;
                    }
                }
            }

            setModMovementForceMagnitudeAck.Ack.Status.Time = AdjustClientMovementTime(setModMovementForceMagnitudeAck.Ack.Status.Time);

            MoveUpdateSpeed updateModMovementForceMagnitude = new(ServerOpcodes.MoveUpdateModMovementForceMagnitude);
            updateModMovementForceMagnitude.Status = setModMovementForceMagnitudeAck.Ack.Status;
            updateModMovementForceMagnitude.Speed = setModMovementForceMagnitudeAck.Speed;
            mover.SendMessageToSet(updateModMovementForceMagnitude, false);
        }

        [WorldPacketHandler(ClientOpcodes.MoveTimeSkipped, Processing = PacketProcessing.Inplace)]
        private void HandleMoveTimeSkipped(MoveTimeSkipped moveTimeSkipped)
        {
            Unit mover = GetPlayer().GetUnitBeingMoved();

            if (mover == null)
            {
                Log.outWarn(LogFilter.Player, $"WorldSession.HandleMoveTimeSkipped wrong mover State from the unit moved by {GetPlayer().GetGUID()}");

                return;
            }

            // prevent tampered movement _data
            if (moveTimeSkipped.MoverGUID != mover.GetGUID())
            {
                Log.outWarn(LogFilter.Player, $"WorldSession.HandleMoveTimeSkipped wrong Guid from the unit moved by {GetPlayer().GetGUID()}");

                return;
            }

            mover.MovementInfo.Time += moveTimeSkipped.TimeSkipped;

            MoveSkipTime moveSkipTime = new();
            moveSkipTime.MoverGUID = moveTimeSkipped.MoverGUID;
            moveSkipTime.TimeSkipped = moveTimeSkipped.TimeSkipped;
            mover.SendMessageToSet(moveSkipTime, _player);
        }

        [WorldPacketHandler(ClientOpcodes.MoveSplineDone, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMoveSplineDoneOpcode(MoveSplineDone moveSplineDone)
        {
            MovementInfo movementInfo = moveSplineDone.Status;
            _player.ValidateMovementInfo(movementInfo);

            // in taxi flight packet received in 2 case:
            // 1) end taxi path in far (multi-node) flight
            // 2) switch from one map to other in case multim-map taxi path
            // we need process only (1)

            uint curDest = GetPlayer().Taxi.GetTaxiDestination();

            if (curDest != 0)
            {
                TaxiNodesRecord curDestNode = CliDB.TaxiNodesStorage.LookupByKey(curDest);

                // far teleport case
                if (curDestNode != null &&
                    curDestNode.ContinentID != GetPlayer().GetMapId() &&
                    GetPlayer().GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Flight)
                {
                    FlightPathMovementGenerator flight = GetPlayer().GetMotionMaster().GetCurrentMovementGenerator() as FlightPathMovementGenerator;

                    if (flight != null)
                    {
                        // short preparations to continue flight
                        flight.SetCurrentNodeAfterTeleport();
                        TaxiPathNodeRecord node = flight.GetPath()[(int)flight.GetCurrentNode()];
                        flight.SkipCurrentNode();

                        GetPlayer().TeleportTo(curDestNode.ContinentID, node.Loc.X, node.Loc.Y, node.Loc.Z, GetPlayer().GetOrientation());
                    }
                }

                return;
            }

            // at this point only 1 node is expected (final destination)
            if (GetPlayer().Taxi.GetPath().Count != 1)
                return;

            GetPlayer().CleanupAfterTaxiFlight();
            GetPlayer().SetFallInformation(0, GetPlayer().GetPositionZ());

            if (GetPlayer().PvpInfo.IsHostile)
                GetPlayer().CastSpell(GetPlayer(), 2479, true);
        }

        [WorldPacketHandler(ClientOpcodes.TimeSyncResponse, Processing = PacketProcessing.ThreadSafe)]
        private void HandleTimeSyncResponse(TimeSyncResponse timeSyncResponse)
        {
            if (!_pendingTimeSyncRequests.ContainsKey(timeSyncResponse.SequenceIndex))
                return;

            uint serverTimeAtSent = _pendingTimeSyncRequests.LookupByKey(timeSyncResponse.SequenceIndex);
            _pendingTimeSyncRequests.Remove(timeSyncResponse.SequenceIndex);

            // Time it took for the request to travel to the client, for the client to process it and reply and for response to travel back to the server.
            // we are going to make 2 assumptions:
            // 1) we assume that the request processing Time equals 0.
            // 2) we assume that the packet took as much Time to travel from server to client than it took to travel from client to server.
            uint roundTripDuration = Time.GetMSTimeDiff(serverTimeAtSent, timeSyncResponse.GetReceivedTime());
            uint lagDelay = roundTripDuration / 2;

            /*
			clockDelta = serverTime - clientTime
			where
			serverTime: Time that was displayed on the clock of the SERVER at the moment when the client processed the SMSG_TIME_SYNC_REQUEST packet.
			clientTime:  Time that was displayed on the clock of the CLIENT at the moment when the client processed the SMSG_TIME_SYNC_REQUEST packet.

			Once clockDelta has been computed, we can compute the Time of an event on server clock when we know the Time of that same event on the client clock,
			using the following relation:
			serverTime = clockDelta + clientTime
			*/
            long clockDelta = (long)(serverTimeAtSent + lagDelay) - (long)timeSyncResponse.ClientTime;
            _timeSyncClockDeltaQueue.PushFront(Tuple.Create(clockDelta, roundTripDuration));
            ComputeNewClockDelta();
        }

        private void ComputeNewClockDelta()
        {
            // implementation of the technique described here: https://web.archive.org/web/20180430214420/http://www.mine-control.com/zack/timesync/timesync.html
            // to reduce the skew induced by dropped TCP packets that get resent.

            //accumulator_set < uint32, features < tag::mean, tag::median, tag::variance(lazy) > > latencyAccumulator;
            List<uint> latencyList = new();

            foreach (var pair in _timeSyncClockDeltaQueue)
                latencyList.Add(pair.Item2);

            uint latencyMedian = (uint)Math.Round(latencyList.Average(p => p));       //median(latencyAccumulator));
            uint latencyStandardDeviation = (uint)Math.Round(Math.Sqrt(latencyList.Variance())); //variance(latencyAccumulator)));

            //accumulator_set<long, features<tag::mean>> clockDeltasAfterFiltering;
            List<long> clockDeltasAfterFiltering = new();
            uint sampleSizeAfterFiltering = 0;

            foreach (var pair in _timeSyncClockDeltaQueue)
                if (pair.Item2 < latencyStandardDeviation + latencyMedian)
                {
                    clockDeltasAfterFiltering.Add(pair.Item1);
                    sampleSizeAfterFiltering++;
                }

            if (sampleSizeAfterFiltering != 0)
            {
                long meanClockDelta = (long)(Math.Round(clockDeltasAfterFiltering.Average()));

                if (Math.Abs(meanClockDelta - _timeSyncClockDelta) > 25)
                    _timeSyncClockDelta = meanClockDelta;
            }
            else if (_timeSyncClockDelta == 0)
            {
                var back = _timeSyncClockDeltaQueue.Back();
                _timeSyncClockDelta = back.Item1;
            }
        }

        [WorldPacketHandler(ClientOpcodes.MoveInitActiveMoverComplete, Processing = PacketProcessing.ThreadSafe)]
        private void HandleMoveInitActiveMoverComplete(MoveInitActiveMoverComplete moveInitActiveMoverComplete)
        {
            _player.SetPlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime);
            _player.SetTransportServerTime((int)(GameTime.GetGameTimeMS() - moveInitActiveMoverComplete.Ticks));

            _player.UpdateObjectVisibility(false);
        }
    }
}