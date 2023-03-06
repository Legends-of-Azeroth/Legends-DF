﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Game.BattleGrounds;
using Game.Collision;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps.Interfaces;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scenarios;
using Game.Scripting.Interfaces.IMap;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.IWorldState;

namespace Game.Maps
{
    public class Map : IDisposable
    {
        public Dictionary<uint, Dictionary<uint, Grid>> Grids { get { return _grids; } }
        LimitedThreadTaskManager _threadManager = new LimitedThreadTaskManager(ConfigMgr.GetDefaultValue("Map.ParellelUpdateTasks", 20));
        Dictionary<uint, Dictionary<uint, object>> _locks = new Dictionary<uint, Dictionary<uint, object>>();


        public IEnumerable<uint> GridXKeys()
        {
            return _grids.Keys.ToList();
        }

        public IEnumerable<uint> GridYKeys(uint x)
        {
            lock(_grids)
                if (_grids.TryGetValue(x, out var yGrid))
                    return yGrid.Keys.ToList();

            return Enumerable.Empty<uint>();
        }

        public Map(uint id, long expiry, uint instanceId, Difficulty spawnmode)
        {
            i_mapRecord = CliDB.MapStorage.LookupByKey(id);
            i_spawnMode = spawnmode;
            i_InstanceId = instanceId;
            m_VisibleDistance = SharedConst.DefaultVisibilityDistance;
            m_VisibilityNotifyPeriod = SharedConst.DefaultVisibilityNotifyPeriod;
            i_gridExpiry = expiry;
            m_terrain = Global.TerrainMgr.LoadTerrain(id);
            _zonePlayerCountMap.Clear();

            //lets initialize visibility distance for map
            _threadManager.Schedule(InitVisibilityDistance);
            _weatherUpdateTimer = new IntervalTimer();
            _weatherUpdateTimer.SetInterval(1 * Time.InMilliseconds);

            GetGuidSequenceGenerator(HighGuid.Transport).Set(Global.ObjectMgr.GetGenerator(HighGuid.Transport).GetNextAfterMaxUsed());

            _threadManager.Schedule(() => { _poolData = Global.PoolMgr.InitPoolsForMap(this); });

            _threadManager.Schedule(() => Global.TransportMgr.CreateTransportsForMap(this));

            _threadManager.Schedule(() => Global.MMapMgr.LoadMapInstance(Global.WorldMgr.GetDataPath(), GetId(), i_InstanceId));

            _worldStateValues = Global.WorldStateMgr.GetInitialWorldStatesForMap(this);

            Global.OutdoorPvPMgr.CreateOutdoorPvPForMap(this);
            Global.BattleFieldMgr.CreateBattlefieldsForMap(this);

            OnCreateMap(this);
            _threadManager.Wait();
        }

        public void Dispose()
        {
            OnDestroyMap(this);

            // Delete all waiting spawns
            // This doesn't delete from database.
            UnloadAllRespawnInfos();

            for (var i = 0; i < i_worldObjects.Count; ++i)
            {
                WorldObject obj = i_worldObjects[i];
                Cypher.Assert(obj.IsWorldObject());
                obj.RemoveFromWorld();
                obj.ResetMap();
            }

            if (!m_scriptSchedule.Empty())
                Global.MapMgr.DecreaseScheduledScriptCount((uint)m_scriptSchedule.Sum(kvp => kvp.Value.Count));

            Global.OutdoorPvPMgr.DestroyOutdoorPvPForMap(this);
            Global.BattleFieldMgr.DestroyBattlefieldsForMap(this);

            Global.MMapMgr.UnloadMapInstance(GetId(), i_InstanceId);
        }
        #region Script Updates
        //MapScript
        public static void OnCreateMap(Map map)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                Global.ScriptMgr.ForEach<IMapOnCreate<Map>>(p => p.OnCreate(map));

            if (record != null && record.IsDungeon())
                Global.ScriptMgr.ForEach<IMapOnCreate<InstanceMap>>(p => p.OnCreate(map.ToInstanceMap()));

            if (record != null && record.IsBattleground())
                Global.ScriptMgr.ForEach<IMapOnCreate<BattlegroundMap>>(p => p.OnCreate(map.ToBattlegroundMap()));
        }
        public static void OnDestroyMap(Map map)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                Global.ScriptMgr.ForEach<IMapOnDestroy<Map>>(p => p.OnDestroy(map));

            if (record != null && record.IsDungeon())
                Global.ScriptMgr.ForEach<IMapOnDestroy<InstanceMap>>(p => p.OnDestroy(map.ToInstanceMap()));

            if (record != null && record.IsBattleground())
                Global.ScriptMgr.ForEach<IMapOnDestroy<BattlegroundMap>>(p => p.OnDestroy(map.ToBattlegroundMap()));
        }
        public static void OnPlayerEnterMap(Map map, Player player)
        {
            Cypher.Assert(map != null);
            Cypher.Assert(player != null);

            Global.ScriptMgr.ForEach<IPlayerOnMapChanged>(p => p.OnMapChanged(player));

            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                Global.ScriptMgr.ForEach<IMapOnPlayerEnter<Map>>(p => p.OnPlayerEnter(map, player));

            if (record != null && record.IsDungeon())
                Global.ScriptMgr.ForEach<IMapOnPlayerEnter<InstanceMap>>(p => p.OnPlayerEnter(map.ToInstanceMap(), player));

            if (record != null && record.IsBattleground())
                Global.ScriptMgr.ForEach<IMapOnPlayerEnter<BattlegroundMap>>(p => p.OnPlayerEnter(map.ToBattlegroundMap(), player));
        }
        public static void OnPlayerLeaveMap(Map map, Player player)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                Global.ScriptMgr.ForEach<IMapOnPlayerLeave<Map>>(p => p.OnPlayerLeave(map, player));

            if (record != null && record.IsDungeon())
                Global.ScriptMgr.ForEach<IMapOnPlayerLeave<InstanceMap>>(p => p.OnPlayerLeave(map.ToInstanceMap(), player));

            if (record != null && record.IsBattleground())
                Global.ScriptMgr.ForEach<IMapOnPlayerLeave<BattlegroundMap>>(p => p.OnPlayerLeave(map.ToBattlegroundMap(), player));
        }
        public static void OnMapUpdate(Map map, uint diff)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                Global.ScriptMgr.ForEach<IMapOnUpdate<Map>>(p => p.OnUpdate(map, diff));

            if (record != null && record.IsDungeon())
                Global.ScriptMgr.ForEach<IMapOnUpdate<InstanceMap>>(p => p.OnUpdate(map.ToInstanceMap(), diff));

            if (record != null && record.IsBattleground())
                Global.ScriptMgr.ForEach<IMapOnUpdate<BattlegroundMap>>(p => p.OnUpdate(map.ToBattlegroundMap(), diff));
        }
        #endregion

        public void LoadAllCells()
        {
            LimitedThreadTaskManager _manager = new LimitedThreadTaskManager(50);
            for (uint cellX = 0; cellX < MapConst.TotalCellsPerMap; cellX++)
                for (uint cellY = 0; cellY < MapConst.TotalCellsPerMap; cellY++)
                {
                    _manager.Schedule(() =>
                        LoadGrid((cellX + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells, (cellY + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells)
                        );
                }

            _manager.Wait();
        }

        public virtual void InitVisibilityDistance()
        {
            //init visibility for continents
            m_VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceOnContinents();
            m_VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodOnContinents();
        }

        public void AddToGrid<T>(T obj, Cell cell) where T : WorldObject
        {
            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());
            switch (obj.GetTypeId())
            {
                case TypeId.Corpse:
                    if (grid.IsGridObjectDataLoaded())
                    {
                        // Corpses are a special object type - they can be added to grid via a call to AddToMap
                        // or loaded through ObjectGridLoader.
                        // Both corpses loaded from database and these freshly generated by Player::CreateCoprse are added to _corpsesByCell
                        // ObjectGridLoader loads all corpses from _corpsesByCell even if they were already added to grid before it was loaded
                        // so we need to explicitly check it here (Map::AddToGrid is only called from Player::BuildPlayerRepop, not from ObjectGridLoader)
                        // to avoid failing an assertion in GridObject::AddToGrid
                        if (obj.IsWorldObject())
                        {
                            obj.SetCurrentCell(cell);
                            grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddWorldObject(obj);
                        }
                        else
                            grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);
                    }
                    return;
                case TypeId.GameObject:
                case TypeId.AreaTrigger:
                    grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);
                    break;
                case TypeId.DynamicObject:
                default:
                    if (obj.IsWorldObject())
                        grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddWorldObject(obj);
                    else
                        grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);
                    break;
            }

            obj.SetCurrentCell(cell);
        }

        public void RemoveFromGrid(WorldObject obj, Cell cell)
        {
            if (cell == null)
                return;

            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());
            if (grid == null)
                return;

            if (obj.IsWorldObject())
                grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).RemoveWorldObject(obj);
            else
                grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).RemoveGridObject(obj);

            obj.SetCurrentCell(null);
        }

        void SwitchGridContainers(WorldObject obj, bool on)
        {
            if (obj.IsPermanentWorldObject())
                return;

            CellCoord p = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            if (!p.IsCoordValid())
            {
                Log.outError(LogFilter.Maps, "Map.SwitchGridContainers: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
                    obj.GetGUID(), obj.GetPositionX(), obj.GetPositionY(), p.X_coord, p.Y_coord);
                return;
            }

            var cell = new Cell(p);
            if (!IsGridLoaded(cell.GetGridX(), cell.GetGridY()))
                return;

            Log.outDebug(LogFilter.Maps, "Switch object {0} from grid[{1}, {2}] {3}", obj.GetGUID(), cell.GetGridX(), cell.GetGridY(), on);
            Grid ngrid = GetGrid(cell.GetGridX(), cell.GetGridY());
            Cypher.Assert(ngrid != null);

            RemoveFromGrid(obj, cell);

            GridCell gridCell = ngrid.GetGridCell(cell.GetCellX(), cell.GetCellY());
            if (on)
            {
                gridCell.AddWorldObject(obj);
                AddWorldObject(obj);
            }
            else
            {
                gridCell.AddGridObject(obj);
                RemoveWorldObject(obj);
            }

            obj.SetCurrentCell(cell);
            obj.ToCreature().m_isTempWorldObject = on;
        }

        void DeleteFromWorld(Player player)
        {
            Global.ObjAccessor.RemoveObject(player);
            RemoveUpdateObject(player); // @todo I do not know why we need this, it should be removed in ~Object anyway
            player.Dispose();
        }

        void DeleteFromWorld(WorldObject obj)
        {
            obj.Dispose();
        }

        void EnsureGridCreated(GridCoord p)
        {
            object lockobj = null;

            lock(_locks)
                lockobj = _locks.GetOrAdd(p.X_coord, p.Y_coord, () => new object());

            lock (lockobj)
                if (GetGrid(p.X_coord, p.Y_coord) == null)
                {
                    Log.outDebug(LogFilter.Maps, "Creating grid[{0}, {1}] for map {2} instance {3}", p.X_coord, p.Y_coord, GetId(), i_InstanceId);

                    var grid = new Grid(p.X_coord * MapConst.MaxGrids + p.Y_coord, p.X_coord, p.Y_coord, i_gridExpiry, WorldConfig.GetBoolValue(WorldCfg.GridUnload));
                    grid.SetGridState(GridState.Idle);
                    SetGrid(grid, p.X_coord, p.Y_coord);

                    //z coord
                    int gx = (int)((MapConst.MaxGrids - 1) - p.X_coord);
                    int gy = (int)((MapConst.MaxGrids - 1) - p.Y_coord);

                    if (gx > -1 && gy > -1)
                        m_terrain.LoadMapAndVMap(gx, gy);
                }
        }

        void EnsureGridLoadedForActiveObject(Cell cell, WorldObject obj)
        {
            EnsureGridLoaded(cell);
            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());

            if (obj.IsPlayer())
                GetMultiPersonalPhaseTracker().LoadGrid(obj.GetPhaseShift(), grid, this, cell);

            // refresh grid state & timer
            if (grid.GetGridState() != GridState.Active)
            {
                Log.outDebug(LogFilter.Maps, "Active object {0} triggers loading of grid [{1}, {2}] on map {3}",
                    obj.GetGUID(), cell.GetGridX(), cell.GetGridY(), GetId());
                ResetGridExpiry(grid, 0.1f);
                grid.SetGridState(GridState.Active);
            }
        }

        private bool EnsureGridLoaded(Cell cell)
        {
            EnsureGridCreated(new GridCoord(cell.GetGridX(), cell.GetGridY()));
            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());

            if (grid != null && !IsGridObjectDataLoaded(cell.GetGridX(), cell.GetGridY()))
            {
                Log.outDebug(LogFilter.Maps, "Loading grid[{0}, {1}] for map {2} instance {3}", cell.GetGridX(),
                    cell.GetGridY(), GetId(), i_InstanceId);

                SetGridObjectDataLoaded(true, cell.GetGridX(), cell.GetGridY());

                LoadGridObjects(grid, cell);

                Balance();
                return true;
            }

            return false;
        }

        public virtual void LoadGridObjects(Grid grid, Cell cell)
        {
            if (grid == null)
                return;

            ObjectGridLoader loader = new(grid, this, cell, GridType.Grid);
            loader.LoadN();
        }

        void GridMarkNoUnload(uint x, uint y)
        {
            // First make sure this grid is loaded
            float gX = (((float)x - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
            float gY = (((float)y - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
            Cell cell = new(gX, gY);
            EnsureGridLoaded(cell);

            // Mark as don't unload
            var grid = GetGrid(x, y);
            grid.SetUnloadExplicitLock(true);
        }

        void GridUnmarkNoUnload(uint x, uint y)
        {
            // If grid is loaded, clear unload lock
            if (IsGridLoaded(x, y))
            {
                var grid = GetGrid(x, y);
                grid.SetUnloadExplicitLock(false);
            }
        }

        public void LoadGrid(float x, float y)
        {
            EnsureGridLoaded(new Cell(x, y));
        }

        public void LoadGridForActiveObject(float x, float y, WorldObject obj)
        {
            EnsureGridLoadedForActiveObject(new Cell(x, y), obj);
        }

        public virtual bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            CellCoord cellCoord = GridDefines.ComputeCellCoord(player.GetPositionX(), player.GetPositionY());
            if (!cellCoord.IsCoordValid())
            {
                Log.outError(LogFilter.Maps, "Map.AddPlayer (GUID: {0}) has invalid coordinates X:{1} Y:{2}",
                    player.GetGUID().ToString(), player.GetPositionX(), player.GetPositionY());
                return false;
            }
            var cell = new Cell(cellCoord);
            EnsureGridLoadedForActiveObject(cell, player);
            AddToGrid(player, cell);

            Cypher.Assert(player.GetMap() == this);
            player.SetMap(this);
            player.AddToWorld();

            if (initPlayer)
                SendInitSelf(player);

            SendInitTransports(player);

            if (initPlayer)
                player.m_clientGUIDs.Clear();

            player.UpdateObjectVisibility(false);
            PhasingHandler.SendToPlayer(player);

            if (player.IsAlive())
                ConvertCorpseToBones(player.GetGUID());

            m_activePlayers.Add(player);

            OnPlayerEnterMap(this, player);
            return true;
        }

        public void UpdatePersonalPhasesForPlayer(Player player)
        {
            Cell cell = new(player.GetPositionX(), player.GetPositionY());
            GetMultiPersonalPhaseTracker().OnOwnerPhaseChanged(player, GetGrid(cell.GetGridX(), cell.GetGridY()), this, cell);
        }

        public int GetWorldStateValue(int worldStateId)
        {
            return _worldStateValues.LookupByKey(worldStateId);
        }

        public Dictionary<int, int> GetWorldStateValues() { return _worldStateValues; }

        public void SetWorldStateValue(int worldStateId, int value, bool hidden)
        {
            int oldValue = 0;
            if (!_worldStateValues.TryAdd(worldStateId, 0))
            {
                oldValue = _worldStateValues[worldStateId];
                if (oldValue == value)
                    return;
            }

            _worldStateValues[worldStateId] = value;

            WorldStateTemplate worldStateTemplate = Global.WorldStateMgr.GetWorldStateTemplate(worldStateId);
            if (worldStateTemplate != null)
                Global.ScriptMgr.RunScript<IWorldStateOnValueChange>(script => script.OnValueChange(worldStateTemplate.Id, oldValue, value, this), worldStateTemplate.ScriptId);

            // Broadcast update to all players on the map
            UpdateWorldState updateWorldState = new();
            updateWorldState.VariableID = (uint)worldStateId;
            updateWorldState.Value = value;
            updateWorldState.Hidden = hidden;
            updateWorldState.Write();

            foreach (var player in GetPlayers())
            {
                if (worldStateTemplate != null && !worldStateTemplate.AreaIds.Empty())
                {
                    bool isInAllowedArea = worldStateTemplate.AreaIds.Any(requiredAreaId => Global.DB2Mgr.IsInArea(player.GetAreaId(), requiredAreaId));
                    if (!isInAllowedArea)
                        continue;
                }

                player.SendPacket(updateWorldState);
            }
        }

        void InitializeObject(WorldObject obj)
        {
            if (!obj.IsTypeId(TypeId.Unit) || !obj.IsTypeId(TypeId.GameObject))
                return;
            obj._moveState = ObjectCellMoveState.None;
        }

        public bool AddToMap(WorldObject obj)
        {
            //TODO: Needs clean up. An object should not be added to map twice.
            if (obj.IsInWorld)
            {
                obj.UpdateObjectVisibility(true);
                return true;
            }

            CellCoord cellCoord = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            if (!cellCoord.IsCoordValid())
            {
                Log.outError(LogFilter.Maps,
                    "Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]", obj.GetGUID(),
                    obj.GetPositionX(), obj.GetPositionY(), cellCoord.X_coord, cellCoord.Y_coord);
                return false; //Should delete object
            }

            var cell = new Cell(cellCoord);
            if (obj.IsActiveObject())
                EnsureGridLoadedForActiveObject(cell, obj);
            else
                EnsureGridCreated(new GridCoord(cell.GetGridX(), cell.GetGridY()));
            AddToGrid(obj, cell);
            Log.outDebug(LogFilter.Maps, "Object {0} enters grid[{1}, {2}]", obj.GetGUID().ToString(), cell.GetGridX(), cell.GetGridY());

            obj.AddToWorld();

            InitializeObject(obj);

            if (obj.IsActiveObject())
                AddToActive(obj);

            //something, such as vehicle, needs to be update immediately
            //also, trigger needs to cast spell, if not update, cannot see visual
            obj.SetIsNewObject(true);
            obj.UpdateObjectVisibilityOnCreate();
            obj.SetIsNewObject(false);
            return true;
        }

        public bool AddToMap(Transport obj)
        {
            //TODO: Needs clean up. An object should not be added to map twice.
            if (obj.IsInWorld)
                return true;

            CellCoord cellCoord = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            if (!cellCoord.IsCoordValid())
            {
                Log.outError(LogFilter.Maps,
                    "Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]", obj.GetGUID(),
                    obj.GetPositionX(), obj.GetPositionY(), cellCoord.X_coord, cellCoord.Y_coord);
                return false; //Should delete object
            }

            _transports.Add(obj);

            if (obj.GetExpectedMapId() == GetId())
            {
                obj.AddToWorld();

                // Broadcast creation to players
                foreach (var player in GetPlayers())
                {
                    if (player.GetTransport() != obj && player.InSamePhase(obj))
                    {
                        var data = new UpdateData(GetId());
                        obj.BuildCreateUpdateBlockForPlayer(data, player);
                        player.m_visibleTransports.Add(obj.GetGUID());
                        data.BuildPacket(out UpdateObject packet);
                        player.SendPacket(packet);
                    }
                }
            }

            return true;
        }

        public bool IsGridLoaded(uint gridId) { return IsGridLoaded(gridId % MapConst.MaxGrids, gridId / MapConst.MaxGrids); }

        public bool IsGridLoaded(float x, float y) { return IsGridLoaded(GridDefines.ComputeGridCoord(x, y)); }

        public bool IsGridLoaded(Position pos) { return IsGridLoaded(pos.GetPositionX(), pos.GetPositionY()); }

        public bool IsGridLoaded(uint x, uint y)
        {
            return (GetGrid(x, y) != null && IsGridObjectDataLoaded(x, y));
        }

        public bool IsGridLoaded(GridCoord p)
        {
            return (GetGrid(p.X_coord, p.Y_coord) != null && IsGridObjectDataLoaded(p.X_coord, p.Y_coord));
        }

        void VisitNearbyCellsOf(WorldObject obj, IGridNotifier gridVisitor)
        {
            // Check for valid position
            if (!obj.IsPositionValid())
                return;

            // Update mobs/objects in ALL visible cells around object!
            CellArea area = Cell.CalculateCellArea(obj.GetPositionX(), obj.GetPositionY(), obj.GetGridActivationRange());

            for (uint x = area.low_bound.X_coord; x <= area.high_bound.X_coord; ++x)
            {
                for (uint y = area.low_bound.Y_coord; y <= area.high_bound.Y_coord; ++y)
                {
                    // marked cells are those that have been visited
                    // don't visit the same cell twice
                    uint cell_id = (y * MapConst.TotalCellsPerMap) + x;
                    if (IsCellMarked(cell_id))
                        continue;

                    MarkCell(cell_id);
                    var pair = new CellCoord(x, y);
                    var cell = new Cell(pair);
                    cell.SetNoCreate();
                    Visit(cell, gridVisitor);
                }
            }
        }

        public void UpdatePlayerZoneStats(uint oldZone, uint newZone)
        {
            // Nothing to do if no change
            if (oldZone == newZone)
                return;

            if (oldZone != MapConst.InvalidZone)
            {
                Cypher.Assert(_zonePlayerCountMap[oldZone] != 0, $"A player left zone {oldZone} (went to {newZone}) - but there were no players in the zone!");
                --_zonePlayerCountMap[oldZone];
            }

            if (!_zonePlayerCountMap.ContainsKey(newZone))
                _zonePlayerCountMap[newZone] = 0;

            ++_zonePlayerCountMap[newZone];
        }

        public virtual void Update(uint diff)
        {
            _dynamicTree.Update(diff);

            // update worldsessions for existing players
            for (var i = 0; i < m_activePlayers.Count; ++i)
            {
                Player player = m_activePlayers[i];
                if (player.IsInWorld)
                {
                    WorldSession session = player.GetSession();
                    var updater = new MapSessionFilter(session);
                    _threadManager.Schedule(() => session.Update(diff, updater));
                }
            }

            /// process any due respawns
            if (_respawnCheckTimer <= diff)
            {
                _threadManager.Schedule(ProcessRespawns);
                _threadManager.Schedule(UpdateSpawnGroupConditions);
                _respawnCheckTimer = WorldConfig.GetUIntValue(WorldCfg.RespawnMinCheckIntervalMs);
            }
            else
                _respawnCheckTimer -= diff;

            _threadManager.Wait();
            // update active cells around players and active objects
            ResetMarkedCells();

            var update = new UpdaterNotifier(diff, GridType.All);

            for (var i = 0; i < m_activePlayers.Count; ++i)
            {
                Player player = m_activePlayers[i];

                if (!player.IsInWorld)
                    continue;

                _threadManager.Schedule(() =>
                {
                    // update players at tick
                    _threadManager.Schedule(() => player.Update(diff));

                    _threadManager.Schedule(() => VisitNearbyCellsOf(player, update));

                    // If player is using far sight or mind vision, visit that object too
                    WorldObject viewPoint = player.GetViewpoint();
                    if (viewPoint)
                        _threadManager.Schedule(() => VisitNearbyCellsOf(viewPoint, update));

                    // Handle updates for creatures in combat with player and are more than 60 yards away
                    if (player.IsInCombat())
                    {
                        List<Unit> toVisit = new();
                        foreach (var pair in player.GetCombatManager().GetPvECombatRefs())
                        {
                            Creature unit = pair.Value.GetOther(player).ToCreature();
                            if (unit != null)
                                if (unit.GetMapId() == player.GetMapId() && !unit.IsWithinDistInMap(player, GetVisibilityRange(), false))
                                    toVisit.Add(unit);
                        }

                        foreach (Unit unit in toVisit)
                            _threadManager.Schedule(() => VisitNearbyCellsOf(unit, update));
                    }

                    { // Update any creatures that own auras the player has applications of
                        List<Unit> toVisit = new();
                        player.GetAppliedAurasQuery().IsPlayer(false).ForEachResult(aur =>
                        {
                            Unit caster = aur.GetBase().GetCaster();
                            if (caster != null)
                                if (!caster.IsWithinDistInMap(player, GetVisibilityRange(), false))
                                    toVisit.Add(caster);
                        });

                        foreach (Unit unit in toVisit)
                            _threadManager.Schedule(() => VisitNearbyCellsOf(unit, update));
                    }

                    { // Update player's summons
                        List<Unit> toVisit = new();

                        // Totems
                        foreach (ObjectGuid summonGuid in player.m_SummonSlot)
                        {
                            if (!summonGuid.IsEmpty())
                            {
                                Creature unit = GetCreature(summonGuid);
                                if (unit != null)
                                    if (unit.GetMapId() == player.GetMapId() && !unit.IsWithinDistInMap(player, GetVisibilityRange(), false))
                                        toVisit.Add(unit);
                            }
                        }

                        foreach (Unit unit in toVisit)
                            _threadManager.Schedule(() => VisitNearbyCellsOf(unit, update));
                    }
                });
            }

            for (var i = 0; i < m_activeNonPlayers.Count; ++i)
            {
                WorldObject obj = m_activeNonPlayers[i];
                if (!obj.IsInWorld)
                    continue;

                _threadManager.Schedule(() => VisitNearbyCellsOf(obj, update));
            }

            for (var i = 0; i < _transports.Count; ++i)
            {
                Transport transport = _transports[i];
                if (!transport)
                    continue;

                transport.Update(diff);
            }

            _threadManager.Wait();
            _threadManager.Schedule(SendObjectUpdates);

            // Process necessary scripts
            if (!m_scriptSchedule.Empty())
            {
                lock (i_scriptLock)
                    ScriptsProcess();
            }

            _weatherUpdateTimer.Update(diff);
            if (_weatherUpdateTimer.Passed())
            {
                foreach (var zoneInfo in _zoneDynamicInfo)
                {
                    if (zoneInfo.Value.DefaultWeather != null && !zoneInfo.Value.DefaultWeather.Update((uint)_weatherUpdateTimer.GetInterval()))
                        zoneInfo.Value.DefaultWeather = null;
                }

                _weatherUpdateTimer.Reset();
            }

            // update phase shift objects
            _threadManager.Schedule(() => GetMultiPersonalPhaseTracker().Update(this, diff));

            MoveAllCreaturesInMoveList();
            MoveAllGameObjectsInMoveList();
            MoveAllAreaTriggersInMoveList();

            if (!m_activePlayers.Empty() || !m_activeNonPlayers.Empty())
                ProcessRelocationNotifies(diff);

            OnMapUpdate(this, diff);

            _threadManager.Wait();
        }

        void ProcessRelocationNotifies(uint diff)
        {
            var xKeys = GridXKeys();
            foreach (var x in xKeys)
            {
                foreach (var y in GridYKeys(x))
                {
                    var grid = GetGrid(x, y);
                    if (grid == null)
                        continue;

                    if (grid.GetGridState() != GridState.Active)
                        continue;

                    grid.GetGridInfoRef().GetRelocationTimer().TUpdate((int)diff);
                    if (!grid.GetGridInfoRef().GetRelocationTimer().TPassed())
                        continue;

                    uint gx = grid.GetX();
                    uint gy = grid.GetY();

                    var cell_min = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);
                    var cell_max = new CellCoord(cell_min.X_coord + MapConst.MaxCells, cell_min.Y_coord + MapConst.MaxCells);

                    for (uint xx = cell_min.X_coord; xx < cell_max.X_coord; ++xx)
                    {
                        for (uint yy = cell_min.Y_coord; yy < cell_max.Y_coord; ++yy)
                        {
                            uint cell_id = (yy * MapConst.TotalCellsPerMap) + xx;
                            if (!IsCellMarked(cell_id))
                                continue;

                            var pair = new CellCoord(xx, yy);
                            var cell = new Cell(pair);
                            cell.SetNoCreate();

                            var cell_relocation = new DelayedUnitRelocation(cell, pair, this, SharedConst.MaxVisibilityDistance, GridType.All);

                            Visit(cell, cell_relocation);
                        }
                    }
                }
            }
            var reset = new ResetNotifier(GridType.All);

            foreach (var x in xKeys)
            {
                foreach (var y in GridYKeys(x))
                {
                    var grid = GetGrid(x, y);
                    if (grid == null)
                        continue;

                    if (grid.GetGridState() != GridState.Active)
                        continue;

                    if (!grid.GetGridInfoRef().GetRelocationTimer().TPassed())
                        continue;

                    grid.GetGridInfoRef().GetRelocationTimer().TReset((int)diff, m_VisibilityNotifyPeriod);

                    uint gx = grid.GetX();
                    uint gy = grid.GetY();

                    var cell_min = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);
                    var cell_max = new CellCoord(cell_min.X_coord + MapConst.MaxCells,
                        cell_min.Y_coord + MapConst.MaxCells);

                    for (uint xx = cell_min.X_coord; xx < cell_max.X_coord; ++xx)
                    {
                        for (uint yy = cell_min.Y_coord; yy < cell_max.Y_coord; ++yy)
                        {
                            uint cell_id = (yy * MapConst.TotalCellsPerMap) + xx;
                            if (!IsCellMarked(cell_id))
                                continue;

                            var pair = new CellCoord(xx, yy);
                            var cell = new Cell(pair);
                            cell.SetNoCreate();
                            Visit(cell, reset);
                        }
                    }
                }
            }
        }

        public virtual void RemovePlayerFromMap(Player player, bool remove)
        {
            // Before leaving map, update zone/area for stats
            player.UpdateZone(MapConst.InvalidZone, 0);
            OnPlayerLeaveMap(this, player);

            GetMultiPersonalPhaseTracker().MarkAllPhasesForDeletion(player.GetGUID());

            player.CombatStop();

            bool inWorld = player.IsInWorld;
            player.RemoveFromWorld();
            SendRemoveTransports(player);

            if (!inWorld) // if was in world, RemoveFromWorld() called DestroyForNearbyPlayers()
                player.UpdateObjectVisibilityOnDestroy();

            Cell cell = player.GetCurrentCell();
            RemoveFromGrid(player, cell);

            m_activePlayers.Remove(player);

            if (remove)
                DeleteFromWorld(player);
        }

        public void RemoveFromMap(WorldObject obj, bool remove)
        {
            bool inWorld = obj.IsInWorld && obj.GetTypeId() >= TypeId.Unit && obj.GetTypeId() <= TypeId.GameObject;
            obj.RemoveFromWorld();
            if (obj.IsActiveObject())
                RemoveFromActive(obj);

            GetMultiPersonalPhaseTracker().UnregisterTrackedObject(obj);

            if (!inWorld) // if was in world, RemoveFromWorld() called DestroyForNearbyPlayers()
                obj.UpdateObjectVisibilityOnDestroy();

            Cell cell = obj.GetCurrentCell();
            RemoveFromGrid(obj, cell);

            obj.ResetMap();

            if (remove)
                DeleteFromWorld(obj);
        }

        public void RemoveFromMap(Transport obj, bool remove)
        {
            if (obj.IsInWorld)
            {
                obj.RemoveFromWorld();

                UpdateData data = new(GetId());
                if (obj.IsDestroyedObject())
                    obj.BuildDestroyUpdateBlock(data);
                else
                    obj.BuildOutOfRangeUpdateBlock(data);

                data.BuildPacket(out UpdateObject packet);

                foreach (var player in GetPlayers())
                {
                    if (player.GetTransport() != obj && player.m_visibleTransports.Contains(obj.GetGUID()))
                    {
                        player.SendPacket(packet);
                        player.m_visibleTransports.Remove(obj.GetGUID());
                    }
                }
            }

            if (!_transports.Contains(obj))
                return;

            _transports.Remove(obj);

            obj.ResetMap();
            if (remove)
                DeleteFromWorld(obj);
        }

        bool CheckGridIntegrity<T>(T obj, bool moved) where T : WorldObject
        {
            Cell cur_cell = obj.GetCurrentCell();
            Cell xy_cell = new(obj.GetPositionX(), obj.GetPositionY());
            if (xy_cell != cur_cell)
            {
                //$"grid[{GetGridX()}, {GetGridY()}]cell[{GetCellX()}, {GetCellY()}]";
                Log.outDebug(LogFilter.Maps, $"{obj.GetTypeId()} ({obj.GetGUID()}) X: {obj.GetPositionX()} Y: {obj.GetPositionY()} ({(moved ? "final" : "original")}) is in {cur_cell} instead of {xy_cell}");
                return true;                                        // not crash at error, just output error in debug mode
            }

            return true;
        }

        public void PlayerRelocation(Player player, float x, float y, float z, float orientation)
        {
            var oldcell = player.GetCurrentCell();
            var newcell = new Cell(x, y);

            player.Relocate(x, y, z, orientation);
            if (player.IsVehicle())
                player.GetVehicleKit().RelocatePassengers();

            if (oldcell.DiffGrid(newcell) || oldcell.DiffCell(newcell))
            {
                Log.outDebug(LogFilter.Maps, "Player {0} relocation grid[{1}, {2}]cell[{3}, {4}].grid[{5}, {6}]cell[{7}, {8}]",
                    player.GetName(), oldcell.GetGridX(), oldcell.GetGridY(), oldcell.GetCellX(), oldcell.GetCellY(),
                    newcell.GetGridX(), newcell.GetGridY(), newcell.GetCellX(), newcell.GetCellY());

                RemoveFromGrid(player, oldcell);
                if (oldcell.DiffGrid(newcell))
                    EnsureGridLoadedForActiveObject(newcell, player);

                AddToGrid(player, newcell);
            }

            player.UpdatePositionData();
            player.UpdateObjectVisibility(false);
        }

        public void CreatureRelocation(Creature creature, float x, float y, float z, float ang, bool respawnRelocationOnFail = true)
        {
            Cypher.Assert(CheckGridIntegrity(creature, false));

            var new_cell = new Cell(x, y);

            if (!respawnRelocationOnFail && GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = creature.GetCurrentCell();
            // delay creature move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {
                AddCreatureToMoveList(creature, x, y, z, ang);
                // in diffcell/diffgrid case notifiers called at finishing move creature in MoveAllCreaturesInMoveList
            }
            else
            {
                creature.Relocate(x, y, z, ang);
                if (creature.IsVehicle())
                    creature.GetVehicleKit().RelocatePassengers();
                creature.UpdateObjectVisibility(false);
                creature.UpdatePositionData();
                RemoveCreatureFromMoveList(creature);
            }

            Cypher.Assert(CheckGridIntegrity(creature, true));
        }

        public void GameObjectRelocation(GameObject go, float x, float y, float z, float orientation, bool respawnRelocationOnFail = true)
        {
            Cypher.Assert(CheckGridIntegrity(go, false));

            var new_cell = new Cell(x, y);
            if (!respawnRelocationOnFail && GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = go.GetCurrentCell();
            // delay creature move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {
                Log.outDebug(LogFilter.Maps,
                    "GameObject (GUID: {0} Entry: {1}) added to moving list from grid[{2}, {3}]cell[{4}, {5}] to grid[{6}, {7}]cell[{8}, {9}].",
                    go.GetGUID().ToString(), go.GetEntry(), old_cell.GetGridX(), old_cell.GetGridY(), old_cell.GetCellX(),
                    old_cell.GetCellY(), new_cell.GetGridX(), new_cell.GetGridY(), new_cell.GetCellX(),
                    new_cell.GetCellY());
                AddGameObjectToMoveList(go, x, y, z, orientation);
                // in diffcell/diffgrid case notifiers called at finishing move go in Map.MoveAllGameObjectsInMoveList
            }
            else
            {
                go.Relocate(x, y, z, orientation);
                go.AfterRelocation();
                RemoveGameObjectFromMoveList(go);
            }

            Cypher.Assert(CheckGridIntegrity(go, true));
        }

        public void DynamicObjectRelocation(DynamicObject dynObj, float x, float y, float z, float orientation)
        {
            Cypher.Assert(CheckGridIntegrity(dynObj, false));
            Cell new_cell = new(x, y);

            if (GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = dynObj.GetCurrentCell();
            // delay creature move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {

                Log.outDebug(LogFilter.Maps, "DynamicObject (GUID: {0}) added to moving list from grid[{1}, {2}]cell[{3}, {4}] to grid[{5}, {6}]cell[{7}, {8}].",
                    dynObj.GetGUID().ToString(), old_cell.GetGridX(), old_cell.GetGridY(), old_cell.GetCellX(), old_cell.GetCellY(), new_cell.GetGridX(), new_cell.GetGridY(), new_cell.GetCellX(), new_cell.GetCellY());

                AddDynamicObjectToMoveList(dynObj, x, y, z, orientation);
                // in diffcell/diffgrid case notifiers called at finishing move dynObj in Map.MoveAllGameObjectsInMoveList
            }
            else
            {
                dynObj.Relocate(x, y, z, orientation);
                dynObj.UpdatePositionData();
                dynObj.UpdateObjectVisibility(false);
                RemoveDynamicObjectFromMoveList(dynObj);
            }

            Cypher.Assert(CheckGridIntegrity(dynObj, true));
        }

        public void AreaTriggerRelocation(AreaTrigger at, float x, float y, float z, float orientation)
        {
            Cypher.Assert(CheckGridIntegrity(at, false));
            Cell new_cell = new(x, y);

            if (GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = at.GetCurrentCell();
            // delay areatrigger move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {
                Log.outDebug(LogFilter.Maps, "AreaTrigger ({0}) added to moving list from {1} to {2}.", at.GetGUID().ToString(), old_cell.ToString(), new_cell.ToString());

                AddAreaTriggerToMoveList(at, x, y, z, orientation);
                // in diffcell/diffgrid case notifiers called at finishing move at in Map::MoveAllAreaTriggersInMoveList
            }
            else
            {
                at.Relocate(x, y, z, orientation);
                at.UpdateShape();
                at.UpdateObjectVisibility(false);
                RemoveAreaTriggerFromMoveList(at);
            }

            Cypher.Assert(CheckGridIntegrity(at, true));
        }

        void AddCreatureToMoveList(Creature c, float x, float y, float z, float ang)
        {
            if (_creatureToMoveLock) //can this happen?
                return;

            if (c._moveState == ObjectCellMoveState.None)
                creaturesToMove.Add(c);

            c.SetNewCellPosition(x, y, z, ang);
        }

        void AddGameObjectToMoveList(GameObject go, float x, float y, float z, float ang)
        {
            if (_gameObjectsToMoveLock) //can this happen?
                return;

            if (go._moveState == ObjectCellMoveState.None)
                _gameObjectsToMove.Add(go);
            go.SetNewCellPosition(x, y, z, ang);
        }

        void RemoveGameObjectFromMoveList(GameObject go)
        {
            if (_gameObjectsToMoveLock) //can this happen?
                return;

            if (go._moveState == ObjectCellMoveState.Active)
                go._moveState = ObjectCellMoveState.Inactive;
        }

        void RemoveCreatureFromMoveList(Creature c)
        {
            if (_creatureToMoveLock) //can this happen?
                return;

            if (c._moveState == ObjectCellMoveState.Active)
                c._moveState = ObjectCellMoveState.Inactive;
        }

        void AddDynamicObjectToMoveList(DynamicObject dynObj, float x, float y, float z, float ang)
        {
            if (_dynamicObjectsToMoveLock) //can this happen?
                return;

            if (dynObj._moveState == ObjectCellMoveState.None)
                _dynamicObjectsToMove.Add(dynObj);
            dynObj.SetNewCellPosition(x, y, z, ang);
        }

        void RemoveDynamicObjectFromMoveList(DynamicObject dynObj)
        {
            if (_dynamicObjectsToMoveLock) //can this happen?
                return;

            if (dynObj._moveState == ObjectCellMoveState.Active)
                dynObj._moveState = ObjectCellMoveState.Inactive;
        }

        void AddAreaTriggerToMoveList(AreaTrigger at, float x, float y, float z, float ang)
        {
            if (_areaTriggersToMoveLock) //can this happen?
                return;

            if (at._moveState == ObjectCellMoveState.None)
                _areaTriggersToMove.Add(at);
            at.SetNewCellPosition(x, y, z, ang);
        }

        void RemoveAreaTriggerFromMoveList(AreaTrigger at)
        {
            if (_areaTriggersToMoveLock) //can this happen?
                return;

            if (at._moveState == ObjectCellMoveState.Active)
                at._moveState = ObjectCellMoveState.Inactive;
        }

        void MoveAllCreaturesInMoveList()
        {
            _creatureToMoveLock = true;

            for (var i = 0; i < creaturesToMove.Count; ++i)
            {
                Creature creature = creaturesToMove[i];
                if (creature.GetMap() != this) //pet is teleported to another map
                    continue;

                if (creature._moveState != ObjectCellMoveState.Active)
                {
                    creature._moveState = ObjectCellMoveState.None;
                    continue;
                }

                creature._moveState = ObjectCellMoveState.None;
                if (!creature.IsInWorld)
                    continue;

                _threadManager.Schedule(() =>
                {
                    // do move or do move to respawn or remove creature if previous all fail
                    if (CreatureCellRelocation(creature, new Cell(creature._newPosition.posX, creature._newPosition.posY)))
                    {
                        // update pos
                        creature.Relocate(creature._newPosition);
                        if (creature.IsVehicle())
                            creature.GetVehicleKit().RelocatePassengers();
                        creature.UpdatePositionData();
                        creature.UpdateObjectVisibility(false);
                    }
                    else
                    {
                        // if creature can't be move in new cell/grid (not loaded) move it to repawn cell/grid
                        // creature coordinates will be updated and notifiers send
                        if (!CreatureRespawnRelocation(creature, false))
                        {
                            // ... or unload (if respawn grid also not loaded)
                            //This may happen when a player just logs in and a pet moves to a nearby unloaded cell
                            //To avoid this, we can load nearby cells when player log in
                            //But this check is always needed to ensure safety
                            // @todo pets will disappear if this is outside CreatureRespawnRelocation
                            //need to check why pet is frequently relocated to an unloaded cell
                            if (creature.IsPet())
                                ((Pet)creature).Remove(PetSaveMode.NotInSlot, true);
                            else
                                AddObjectToRemoveList(creature);
                        }
                    }
                });
            }

            creaturesToMove.Clear();
            _creatureToMoveLock = false;
        }

        void MoveAllGameObjectsInMoveList()
        {
            _gameObjectsToMoveLock = true;

            for (var i = 0; i < _gameObjectsToMove.Count; ++i)
            {
                GameObject go = _gameObjectsToMove[i];
                if (go.GetMap() != this) //transport is teleported to another map
                    continue;

                if (go._moveState != ObjectCellMoveState.Active)
                {
                    go._moveState = ObjectCellMoveState.None;
                    continue;
                }

                go._moveState = ObjectCellMoveState.None;
                if (!go.IsInWorld)
                    continue;

                _threadManager.Schedule(() =>
                {
                    // do move or do move to respawn or remove creature if previous all fail
                    if (GameObjectCellRelocation(go, new Cell(go._newPosition.posX, go._newPosition.posY)))
                    {
                        // update pos
                        go.Relocate(go._newPosition);
                        go.AfterRelocation();
                    }
                    else
                    {
                        // if GameObject can't be move in new cell/grid (not loaded) move it to repawn cell/grid
                        // GameObject coordinates will be updated and notifiers send
                        if (!GameObjectRespawnRelocation(go, false))
                        {
                            // ... or unload (if respawn grid also not loaded)
                            Log.outDebug(LogFilter.Maps,
                                "GameObject (GUID: {0} Entry: {1}) cannot be move to unloaded respawn grid.",
                                go.GetGUID().ToString(), go.GetEntry());
                            AddObjectToRemoveList(go);
                        }
                    }
                });
            }

            _gameObjectsToMove.Clear();
            _gameObjectsToMoveLock = false;
        }

        void MoveAllDynamicObjectsInMoveList()
        {
            _dynamicObjectsToMoveLock = true;

            for (var i = 0; i < _dynamicObjectsToMove.Count; ++i)
            {
                DynamicObject dynObj = _dynamicObjectsToMove[i];
                if (dynObj.GetMap() != this) //transport is teleported to another map
                    continue;

                if (dynObj._moveState != ObjectCellMoveState.Active)
                {
                    dynObj._moveState = ObjectCellMoveState.None;
                    continue;
                }

                dynObj._moveState = ObjectCellMoveState.None;
                if (!dynObj.IsInWorld)
                    continue;


                _threadManager.Schedule(() =>
                {
                    // do move or do move to respawn or remove creature if previous all fail
                    if (DynamicObjectCellRelocation(dynObj, new Cell(dynObj._newPosition.posX, dynObj._newPosition.posY)))
                    {
                        // update pos
                        dynObj.Relocate(dynObj._newPosition);
                        dynObj.UpdatePositionData();
                        dynObj.UpdateObjectVisibility(false);
                    }
                    else
                        Log.outDebug(LogFilter.Maps, "DynamicObject (GUID: {0}) cannot be moved to unloaded grid.", dynObj.GetGUID().ToString());
                });
            }

            _dynamicObjectsToMove.Clear();
            _dynamicObjectsToMoveLock = false;
        }

        void MoveAllAreaTriggersInMoveList()
        {
            _areaTriggersToMoveLock = true;

            for (var i = 0; i < _areaTriggersToMove.Count; ++i)
            {
                AreaTrigger at = _areaTriggersToMove[i];
                if (at.GetMap() != this) //transport is teleported to another map
                    continue;

                if (at._moveState != ObjectCellMoveState.Active)
                {
                    at._moveState = ObjectCellMoveState.None;
                    continue;
                }

                at._moveState = ObjectCellMoveState.None;
                if (!at.IsInWorld)
                    continue;

                _threadManager.Schedule(() =>
                {
                    // do move or do move to respawn or remove creature if previous all fail
                    if (AreaTriggerCellRelocation(at, new Cell(at._newPosition.posX, at._newPosition.posY)))
                    {
                        // update pos
                        at.Relocate(at._newPosition);
                        at.UpdateShape();
                        at.UpdateObjectVisibility(false);
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Maps, "AreaTrigger ({0}) cannot be moved to unloaded grid.", at.GetGUID().ToString());
                    }
                });
            }

            _areaTriggersToMove.Clear();
            _areaTriggersToMoveLock = false;
        }

        bool MapObjectCellRelocation<T>(T obj, Cell new_cell) where T : WorldObject
        {
            Cell old_cell = obj.GetCurrentCell();
            if (!old_cell.DiffGrid(new_cell)) // in same grid
            {
                // if in same cell then none do
                if (old_cell.DiffCell(new_cell))
                {
                    RemoveFromGrid(obj, old_cell);
                    AddToGrid(obj, new_cell);
                }

                return true;
            }

            // in diff. grids but active creature
            if (obj.IsActiveObject())
            {
                EnsureGridLoadedForActiveObject(new_cell, obj);

                Log.outDebug(LogFilter.Maps,
                    "Active creature (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to grid[{4}, {5}].",
                    obj.GetGUID().ToString(), obj.GetEntry(), old_cell.GetGridX(),
                    old_cell.GetGridY(), new_cell.GetGridX(), new_cell.GetGridY());
                RemoveFromGrid(obj, old_cell);
                AddToGrid(obj, new_cell);

                return true;
            }

            Creature c = obj.ToCreature();
            if (c != null && c.GetCharmerOrOwnerGUID().IsPlayer())
                EnsureGridLoaded(new_cell);

            // in diff. loaded grid normal creature
            var grid = new GridCoord(new_cell.GetGridX(), new_cell.GetGridY());
            if (IsGridLoaded(grid))
            {
                RemoveFromGrid(obj, old_cell);
                EnsureGridCreated(grid);
                AddToGrid(obj, new_cell);
                return true;
            }

            // fail to move: normal creature attempt move to unloaded grid
            return false;
        }

        bool CreatureCellRelocation(Creature c, Cell new_cell)
        {
            return MapObjectCellRelocation(c, new_cell);
        }

        bool GameObjectCellRelocation(GameObject go, Cell new_cell)
        {
            return MapObjectCellRelocation(go, new_cell);
        }

        bool DynamicObjectCellRelocation(DynamicObject go, Cell new_cell)
        {
            return MapObjectCellRelocation(go, new_cell);
        }

        bool AreaTriggerCellRelocation(AreaTrigger at, Cell new_cell)
        {
            return MapObjectCellRelocation(at, new_cell);
        }

        public bool CreatureRespawnRelocation(Creature c, bool diffGridOnly)
        {
            c.GetRespawnPosition(out float resp_x, out float resp_y, out float resp_z, out float resp_o);
            var resp_cell = new Cell(resp_x, resp_y);

            //creature will be unloaded with grid
            if (diffGridOnly && !c.GetCurrentCell().DiffGrid(resp_cell))
                return true;

            c.CombatStop();
            c.GetMotionMaster().Clear();

            // teleport it to respawn point (like normal respawn if player see)
            if (CreatureCellRelocation(c, resp_cell))
            {
                c.Relocate(resp_x, resp_y, resp_z, resp_o);
                c.GetMotionMaster().Initialize(); // prevent possible problems with default move generators
                c.UpdatePositionData();
                c.UpdateObjectVisibility(false);
                return true;
            }

            return false;
        }

        public bool GameObjectRespawnRelocation(GameObject go, bool diffGridOnly)
        {
            go.GetRespawnPosition(out float resp_x, out float resp_y, out float resp_z, out float resp_o);
            var resp_cell = new Cell(resp_x, resp_y);

            //GameObject will be unloaded with grid
            if (diffGridOnly && !go.GetCurrentCell().DiffGrid(resp_cell))
                return true;

            Log.outDebug(LogFilter.Maps,
                "GameObject (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to respawn grid[{4}, {5}].",
                go.GetGUID().ToString(), go.GetEntry(), go.GetCurrentCell().GetGridX(), go.GetCurrentCell().GetGridY(),
                resp_cell.GetGridX(), resp_cell.GetGridY());

            // teleport it to respawn point (like normal respawn if player see)
            if (GameObjectCellRelocation(go, resp_cell))
            {
                go.Relocate(resp_x, resp_y, resp_z, resp_o);
                go.UpdatePositionData();
                go.UpdateObjectVisibility(false);
                return true;
            }
            return false;
        }

        public bool UnloadGrid(Grid grid, bool unloadAll)
        {
            uint x = grid.GetX();
            uint y = grid.GetY();

            if (!unloadAll)
            {
                //pets, possessed creatures (must be active), transport passengers
                if (grid.GetWorldObjectCountInNGrid<Creature>() != 0)
                    return false;

                if (ActiveObjectsNearGrid(grid))
                    return false;
            }

            Log.outDebug(LogFilter.Maps, "Unloading grid[{0}, {1}] for map {2}", x, y, GetId());

            if (!unloadAll)
            {
                // Finish creature moves, remove and delete all creatures with delayed remove before moving to respawn grids
                // Must know real mob position before move
                MoveAllCreaturesInMoveList();
                MoveAllGameObjectsInMoveList();
                MoveAllAreaTriggersInMoveList();
                _threadManager.Wait();
                // move creatures to respawn grids if this is diff.grid or to remove list
                ObjectGridEvacuator worker = new(GridType.Grid);
                grid.VisitAllGrids(worker);

                // Finish creature moves, remove and delete all creatures with delayed remove before unload
                MoveAllCreaturesInMoveList();
                MoveAllGameObjectsInMoveList();
                MoveAllAreaTriggersInMoveList();
                _threadManager.Wait();
            }

            {
                ObjectGridCleaner worker = new(GridType.Grid);
                grid.VisitAllGrids(worker);
            }

            RemoveAllObjectsInRemoveList();

            // After removing all objects from the map, purge empty tracked phases
            GetMultiPersonalPhaseTracker().UnloadGrid(grid);

            {
                ObjectGridUnloader worker = new();
                grid.VisitAllGrids(worker);
            }

            Cypher.Assert(i_objectsToRemove.Empty());
            lock (_grids)
                _grids.Remove(x, y);

            int gx = (int)((MapConst.MaxGrids - 1) - x);
            int gy = (int)((MapConst.MaxGrids - 1) - y);

            m_terrain.UnloadMap(gx, gy);

            Log.outDebug(LogFilter.Maps, "Unloading grid[{0}, {1}] for map {2} finished", x, y, GetId());
            return true;
        }

        public virtual void RemoveAllPlayers()
        {
            if (HavePlayers())
            {
                foreach (Player pl in m_activePlayers)
                {
                    if (!pl.IsBeingTeleportedFar())
                    {
                        // this is happening for bg
                        Log.outError(LogFilter.Maps, $"Map.UnloadAll: player {pl.GetName()} is still in map {GetId()} during unload, this should not happen!");
                        pl.TeleportTo(pl.GetHomebind());
                    }
                }
            }
        }

        public void UnloadAll()
        {
            // clear all delayed moves, useless anyway do this moves before map unload.
            creaturesToMove.Clear();
            _gameObjectsToMove.Clear();

            foreach (var x in GridXKeys())
            {
                foreach (var y in GridYKeys(x))
                {
                    var grid = GetGrid(x, y);
                    if (grid == null)
                        continue;

                    UnloadGrid(grid, true); // deletes the grid and removes it from the GridRefManager
                }
            }

            for (var i = 0; i < _transports.Count; ++i)
                RemoveFromMap(_transports[i], true);

            _transports.Clear();

            foreach (var corpse in _corpsesByCell.Values.ToList())
            {
                corpse.RemoveFromWorld();
                corpse.ResetMap();
                corpse.Dispose();
            }

            _corpsesByCell.Clear();
            _corpsesByPlayer.Clear();
            _corpseBones.Clear();
        }

        public static bool IsInWMOInterior(uint mogpFlags)
        {
            return (mogpFlags & 0x2000) != 0;
        }

        public void GetFullTerrainStatusForPosition(PhaseShift phaseShift, float x, float y, float z, PositionFullTerrainStatus data, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            m_terrain.GetFullTerrainStatusForPosition(phaseShift, GetId(), x, y, z, data, reqLiquidType, collisionHeight, _dynamicTree);
        }

        public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            return m_terrain.GetLiquidStatus(phaseShift, GetId(), x, y, z, reqLiquidType, out _, collisionHeight);
        }

        public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, out LiquidData data, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            return m_terrain.GetLiquidStatus(phaseShift, GetId(), x, y, z, reqLiquidType, out data, collisionHeight);
        }

        private bool GetAreaInfo(PhaseShift phaseShift, float x, float y, float z, out uint mogpflags, out int adtId, out int rootId, out int groupId)
        {
            return m_terrain.GetAreaInfo(phaseShift, GetId(), x, y, z, out mogpflags, out adtId, out rootId, out groupId, _dynamicTree);
        }

        public uint GetAreaId(PhaseShift phaseShift, Position pos)
        {
            return m_terrain.GetAreaId(phaseShift, GetId(), pos.posX, pos.posY, pos.posZ, _dynamicTree);
        }

        public uint GetAreaId(PhaseShift phaseShift, float x, float y, float z)
        {
            return m_terrain.GetAreaId(phaseShift, GetId(), x, y, z, _dynamicTree);
        }

        public uint GetZoneId(PhaseShift phaseShift, Position pos)
        {
            return m_terrain.GetZoneId(phaseShift, GetId(), pos.posX, pos.posY, pos.posZ, _dynamicTree);
        }

        public uint GetZoneId(PhaseShift phaseShift, float x, float y, float z)
        {
            return m_terrain.GetZoneId(phaseShift, GetId(), x, y, z, _dynamicTree);
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, Position pos)
        {
            m_terrain.GetZoneAndAreaId(phaseShift, GetId(), out zoneid, out areaid, pos.posX, pos.posY, pos.posZ, _dynamicTree);
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, float x, float y, float z)
        {
            m_terrain.GetZoneAndAreaId(phaseShift, GetId(), out zoneid, out areaid, x, y, z, _dynamicTree);
        }

        public float GetHeight(PhaseShift phaseShift, float x, float y, float z, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            return Math.Max(GetStaticHeight(phaseShift, x, y, z, vmap, maxSearchDist), GetGameObjectFloor(phaseShift, x, y, z, maxSearchDist));
        }

        public float GetHeight(PhaseShift phaseShift, Position pos, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            return GetHeight(phaseShift, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), vmap, maxSearchDist);
        }

        public float GetMinHeight(PhaseShift phaseShift, float x, float y)
        {
            return m_terrain.GetMinHeight(phaseShift, GetId(), x, y);
        }

        public float GetGridHeight(PhaseShift phaseShift, float x, float y)
        {
            return m_terrain.GetGridHeight(phaseShift, GetId(), x, y);
        }

        public float GetStaticHeight(PhaseShift phaseShift, float x, float y, float z, bool checkVMap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            return m_terrain.GetStaticHeight(phaseShift, GetId(), x, y, z, checkVMap, maxSearchDist);
        }

        public float GetWaterLevel(PhaseShift phaseShift, float x, float y)
        {
            return m_terrain.GetWaterLevel(phaseShift, GetId(), x, y);
        }

        public bool IsInWater(PhaseShift phaseShift, float x, float y, float z, out LiquidData data)
        {
            return m_terrain.IsInWater(phaseShift, GetId(), x, y, z, out data);
        }

        public bool IsUnderWater(PhaseShift phaseShift, float x, float y, float z)
        {
            return m_terrain.IsUnderWater(phaseShift, GetId(), x, y, z);
        }

        public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            float ground = 0;
            return m_terrain.GetWaterOrGroundLevel(phaseShift, GetId(), x, y, z, ref ground, false, collisionHeight, _dynamicTree);
        }

        public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, ref float ground, bool swim = false, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            return m_terrain.GetWaterOrGroundLevel(phaseShift, GetId(), x, y, z, ref ground, swim, collisionHeight, _dynamicTree);
        }

        public bool IsInLineOfSight(PhaseShift phaseShift, float x1, float y1, float z1, float x2, float y2, float z2, LineOfSightChecks checks, ModelIgnoreFlags ignoreFlags)
        {
            if (checks.HasAnyFlag(LineOfSightChecks.Vmap) && !Global.VMapMgr.IsInLineOfSight(PhasingHandler.GetTerrainMapId(phaseShift, GetId(), m_terrain, x1, y1), x1, y1, z1, x2, y2, z2, ignoreFlags))
                return false;

            if (WorldConfig.GetBoolValue(WorldCfg.CheckGobjectLos) && checks.HasAnyFlag(LineOfSightChecks.Gobject) && !_dynamicTree.IsInLineOfSight(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2), phaseShift))
                return false;

            return true;
        }

        public bool GetObjectHitPos(PhaseShift phaseShift, float x1, float y1, float z1, float x2, float y2, float z2, out float rx, out float ry, out float rz, float modifyDist)
        {
            var startPos = new Vector3(x1, y1, z1);
            var dstPos = new Vector3(x2, y2, z2);

            var resultPos = new Vector3();
            bool result = _dynamicTree.GetObjectHitPos(startPos, dstPos, ref resultPos, modifyDist, phaseShift);

            rx = resultPos.X;
            ry = resultPos.Y;
            rz = resultPos.Z;
            return result;
        }

        public static TransferAbortParams PlayerCannotEnter(uint mapid, Player player)
        {
            var entry = CliDB.MapStorage.LookupByKey(mapid);
            if (entry == null)
                return new TransferAbortParams(TransferAbortReason.MapNotAllowed);

            if (!entry.IsDungeon())
                return null;

            Difficulty targetDifficulty = player.GetDifficultyID(entry);
            // Get the highest available difficulty if current setting is higher than the instance allows
            var mapDiff = Global.DB2Mgr.GetDownscaledMapDifficultyData(mapid, ref targetDifficulty);
            if (mapDiff == null)
                return new TransferAbortParams(TransferAbortReason.Difficulty);

            //Bypass checks for GMs
            if (player.IsGameMaster())
                return null;

            //Other requirements
            {
                TransferAbortParams abortParams = new();
                if (!player.Satisfy(Global.ObjectMgr.GetAccessRequirement(mapid, targetDifficulty), mapid, abortParams, true))
                    return abortParams;
            }

            Group group = player.GetGroup();
            if (entry.IsRaid() && (int)entry.Expansion() >= WorldConfig.GetIntValue(WorldCfg.Expansion)) // can only enter in a raid group but raids from old expansion don't need a group
                if ((!group || !group.IsRaidGroup()) && !WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreRaid))
                    return new TransferAbortParams(TransferAbortReason.NeedGroup);

            if (entry.Instanceable())
            {
                //Get instance where player's group is bound & its map
                uint instanceIdToCheck = Global.MapMgr.FindInstanceIdForPlayer(mapid, player);
                Map boundMap = Global.MapMgr.FindMap(mapid, instanceIdToCheck);
                if (boundMap != null)
                {
                    TransferAbortParams denyReason = boundMap.CannotEnter(player);
                    if (denyReason != null)
                        return denyReason;
                }

                // players are only allowed to enter 10 instances per hour
                if (!entry.GetFlags2().HasFlag(MapFlags2.IgnoreInstanceFarmLimit) && entry.IsDungeon() && !player.CheckInstanceCount(instanceIdToCheck) && !player.IsDead())
                    return new TransferAbortParams(TransferAbortReason.TooManyInstances);
            }

            return null;
        }

        public string GetMapName()
        {
            return i_mapRecord.MapName[Global.WorldMgr.GetDefaultDbcLocale()];
        }

        public void SendInitSelf(Player player)
        {
            var data = new UpdateData(player.GetMapId());

            // attach to player data current transport data
            Transport transport = player.GetTransport<Transport>();
            if (transport != null)
            {
                transport.BuildCreateUpdateBlockForPlayer(data, player);
                player.m_visibleTransports.Add(transport.GetGUID());
            }

            player.BuildCreateUpdateBlockForPlayer(data, player);

            // build other passengers at transport also (they always visible and marked as visible and will not send at visibility update at add to map
            if (transport != null)
            {
                foreach (WorldObject passenger in transport.GetPassengers())
                {
                    if (player != passenger && player.HaveAtClient(passenger))
                        passenger.BuildCreateUpdateBlockForPlayer(data, player);
                }
            }
            data.BuildPacket(out UpdateObject packet);
            player.SendPacket(packet);
        }

        void SendInitTransports(Player player)
        {
            var transData = new UpdateData(GetId());

            foreach (Transport transport in _transports)
            {
                if (transport.IsInWorld && transport != player.GetTransport() && player.InSamePhase(transport))
                {
                    transport.BuildCreateUpdateBlockForPlayer(transData, player);
                    player.m_visibleTransports.Add(transport.GetGUID());
                }
            }

            transData.BuildPacket(out UpdateObject packet);
            player.SendPacket(packet);
        }

        void SendRemoveTransports(Player player)
        {
            var transData = new UpdateData(player.GetMapId());
            foreach (Transport transport in _transports)
            {
                if (player.m_visibleTransports.Contains(transport.GetGUID()) && transport != player.GetTransport())
                {
                    transport.BuildOutOfRangeUpdateBlock(transData);
                    player.m_visibleTransports.Remove(transport.GetGUID());
                }
            }

            transData.BuildPacket(out UpdateObject packet);
            player.SendPacket(packet);
        }

        public void SendUpdateTransportVisibility(Player player)
        {
            // Hack to send out transports
            UpdateData transData = new(player.GetMapId());
            foreach (var transport in _transports)
            {
                if (!transport.IsInWorld)
                    continue;

                var hasTransport = player.m_visibleTransports.Contains(transport.GetGUID());
                if (player.InSamePhase(transport))
                {
                    if (!hasTransport)
                    {
                        transport.BuildCreateUpdateBlockForPlayer(transData, player);
                        player.m_visibleTransports.Add(transport.GetGUID());
                    }
                }
                else
                {
                    transport.BuildOutOfRangeUpdateBlock(transData);
                    player.m_visibleTransports.Remove(transport.GetGUID());
                }
            }

            transData.BuildPacket(out UpdateObject packet);
            player.SendPacket(packet);
        }

        void SetGrid(Grid grid, uint x, uint y)
        {
            if (x >= MapConst.MaxGrids || y >= MapConst.MaxGrids)
            {
                Log.outError(LogFilter.Maps, "Map.setNGrid Invalid grid coordinates found: {0}, {1}!", x, y);
                return;
            }

            lock (_grids)
                _grids.Add(x, y, grid);
        }

        void SendObjectUpdates()
        {
            Dictionary<Player, UpdateData> update_players = new();

            lock (_updateObjects)
                while (!_updateObjects.Empty())
                {
                    WorldObject obj = _updateObjects[0];
                    Cypher.Assert(obj.IsInWorld);
                    _updateObjects.RemoveAt(0);
                    obj.BuildUpdate(update_players);
                }

            foreach (var iter in update_players)
            {
                iter.Value.BuildPacket(out UpdateObject packet);
                iter.Key.SendPacket(packet);
            }
        }

        bool CheckRespawn(RespawnInfo info)
        {
            SpawnData data = Global.ObjectMgr.GetSpawnData(info.type, info.spawnId);
            Cypher.Assert(data != null, $"Invalid respawn info with type {info.type}, spawnID {info.spawnId} in respawn queue.");

            // First, check if this creature's spawn group is inactive
            if (!IsSpawnGroupActive(data.spawnGroupData.groupId))
            {
                info.respawnTime = 0;
                return false;
            }

            // Next, check if there's already an instance of this object that would block the respawn
            // Only do this for unpooled spawns
            bool alreadyExists = false;
            switch (info.type)
            {
                case SpawnObjectType.Creature:
                {
                    // escort check for creatures only (if the world config boolean is set)
                    bool isEscort = WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) && data.spawnGroupData.flags.HasFlag(SpawnGroupFlags.EscortQuestNpc);

                    var range = _creatureBySpawnIdStore.LookupByKey(info.spawnId);
                    foreach (var creature in range)
                    {
                        if (!creature.IsAlive())
                            continue;

                        // escort NPCs are allowed to respawn as long as all other instances are already escorting
                        if (isEscort && creature.IsEscorted())
                            continue;

                        alreadyExists = true;
                        break;
                    }
                    break;
                }
                case SpawnObjectType.GameObject:
                    // gameobject check is simpler - they cannot be dead or escorting
                    if (_gameobjectBySpawnIdStore.ContainsKey(info.spawnId))
                        alreadyExists = true;
                    break;
                default:
                    Cypher.Assert(false, $"Invalid spawn type {info.type} with spawnId {info.spawnId} on map {GetId()}");
                    return true;
            }

            if (alreadyExists)
            {
                info.respawnTime = 0;
                return false;
            }

            // next, check linked respawn time
            ObjectGuid thisGUID = info.type == SpawnObjectType.GameObject ? ObjectGuid.Create(HighGuid.GameObject, GetId(), info.entry, info.spawnId) : ObjectGuid.Create(HighGuid.Creature, GetId(), info.entry, info.spawnId);
            long linkedTime = GetLinkedRespawnTime(thisGUID);
            if (linkedTime != 0)
            {
                long now = GameTime.GetGameTime();
                long respawnTime;
                if (linkedTime == long.MaxValue)
                    respawnTime = linkedTime;
                else if (Global.ObjectMgr.GetLinkedRespawnGuid(thisGUID) == thisGUID) // never respawn, save "something" in DB
                    respawnTime = now + Time.Week;
                else // set us to check again shortly after linked unit
                    respawnTime = Math.Max(now, linkedTime) + RandomHelper.URand(5, 15);
                info.respawnTime = respawnTime;
                return false;
            }

            // everything ok, let's spawn
            return true;
        }

        public void Respawn(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null)
        {
            RespawnInfo info = GetRespawnInfo(type, spawnId);
            if (info != null)
                Respawn(info, dbTrans);
        }

        public void Respawn(RespawnInfo info, SQLTransaction dbTrans = null)
        {
            if (info.respawnTime <= GameTime.GetGameTime())
                return;

            info.respawnTime = GameTime.GetGameTime();
            SaveRespawnInfoDB(info, dbTrans);
        }

        public void RemoveRespawnTime(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null, bool alwaysDeleteFromDB = false)
        {
            RespawnInfo info = GetRespawnInfo(type, spawnId);
            if (info != null)
                DeleteRespawnInfo(info, dbTrans);
            // Some callers might need to make sure the database doesn't contain any respawn time
            else if (alwaysDeleteFromDB)
                DeleteRespawnInfoFromDB(type, spawnId, dbTrans);
        }

        int DespawnAll(SpawnObjectType type, ulong spawnId)
        {
            List<WorldObject> toUnload = new();
            switch (type)
            {
                case SpawnObjectType.Creature:
                    foreach (var creature in GetCreatureBySpawnIdStore().LookupByKey(spawnId))
                        toUnload.Add(creature);
                    break;
                case SpawnObjectType.GameObject:
                    foreach (var obj in GetGameObjectBySpawnIdStore().LookupByKey(spawnId))
                        toUnload.Add(obj);
                    break;
                default:
                    break;
            }

            foreach (WorldObject o in toUnload)
                AddObjectToRemoveList(o);

            return toUnload.Count;
        }

        bool AddRespawnInfo(RespawnInfo info)
        {
            if (info.spawnId == 0)
            {
                Log.outError(LogFilter.Maps, $"Attempt to insert respawn info for zero spawn id (type {info.type})");
                return false;
            }

            var bySpawnIdMap = GetRespawnMapForType(info.type);
            if (bySpawnIdMap == null)
                return false;

            // check if we already have the maximum possible number of respawns scheduled
            if (SpawnData.TypeHasData(info.type))
            {
                var existing = bySpawnIdMap.LookupByKey(info.spawnId);
                if (existing != null) // spawnid already has a respawn scheduled
                {
                    if (info.respawnTime <= existing.respawnTime) // delete existing in this case
                        DeleteRespawnInfo(existing);
                    else
                        return false;
                }
                Cypher.Assert(!bySpawnIdMap.ContainsKey(info.spawnId), $"Insertion of respawn info with id ({info.type},{info.spawnId}) into spawn id map failed - state desync.");
            }
            else
                Cypher.Assert(false, $"Invalid respawn info for spawn id ({info.type},{info.spawnId}) being inserted");

            RespawnInfo ri = new(info);
            _respawnTimes.Add(ri);
            bySpawnIdMap.Add(ri.spawnId, ri);
            return true;
        }

        static void PushRespawnInfoFrom(List<RespawnInfo> data, Dictionary<ulong, RespawnInfo> map)
        {
            foreach (var pair in map)
                data.Add(pair.Value);
        }

        public void GetRespawnInfo(List<RespawnInfo> respawnData, SpawnObjectTypeMask types)
        {
            if ((types & SpawnObjectTypeMask.Creature) != 0)
                PushRespawnInfoFrom(respawnData, _creatureRespawnTimesBySpawnId);
            if ((types & SpawnObjectTypeMask.GameObject) != 0)
                PushRespawnInfoFrom(respawnData, _gameObjectRespawnTimesBySpawnId);
        }

        public RespawnInfo GetRespawnInfo(SpawnObjectType type, ulong spawnId)
        {
            var map = GetRespawnMapForType(type);
            if (map == null)
                return null;

            var respawnInfo = map.LookupByKey(spawnId);
            if (respawnInfo == null)
                return null;

            return respawnInfo;
        }

        Dictionary<ulong, RespawnInfo> GetRespawnMapForType(SpawnObjectType type)
        {
            switch (type)
            {
                case SpawnObjectType.Creature:
                    return _creatureRespawnTimesBySpawnId;
                case SpawnObjectType.GameObject:
                    return _gameObjectRespawnTimesBySpawnId;
                case SpawnObjectType.AreaTrigger:
                    return null;
                default:
                    Cypher.Assert(false);
                    return null;
            }
        }

        void UnloadAllRespawnInfos() // delete everything from memory
        {
            _respawnTimes.Clear();
            _creatureRespawnTimesBySpawnId.Clear();
            _gameObjectRespawnTimesBySpawnId.Clear();
        }

        void DeleteRespawnInfo(RespawnInfo info, SQLTransaction dbTrans = null)
        {
            // Delete from all relevant containers to ensure consistency
            Cypher.Assert(info != null);

            // spawnid store
            var spawnMap = GetRespawnMapForType(info.type);
            if (spawnMap == null)
                return;

            var respawnInfo = spawnMap.LookupByKey(info.spawnId);
            Cypher.Assert(respawnInfo != null, $"Respawn stores inconsistent for map {GetId()}, spawnid {info.spawnId} (type {info.type})");
            spawnMap.Remove(info.spawnId);

            // respawn heap
            _respawnTimes.Remove(info);

            // database
            DeleteRespawnInfoFromDB(info.type, info.spawnId, dbTrans);
        }

        void DeleteRespawnInfoFromDB(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null)
        {
            if (Instanceable())
                return;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_RESPAWN);
            stmt.AddValue(0, (ushort)type);
            stmt.AddValue(1, spawnId);
            stmt.AddValue(2, GetId());
            stmt.AddValue(3, GetInstanceId());
            DB.Characters.ExecuteOrAppend(dbTrans, stmt);
        }

        void DoRespawn(SpawnObjectType type, ulong spawnId, uint gridId)
        {
            if (!IsGridLoaded(gridId)) // if grid isn't loaded, this will be processed in grid load handler
                return;

            switch (type)
            {
                case SpawnObjectType.Creature:
                {
                    Creature obj = new();
                    if (!obj.LoadFromDB(spawnId, this, true, true))
                        obj.Dispose();
                    break;
                }
                case SpawnObjectType.GameObject:
                {
                    GameObject obj = new();
                    if (!obj.LoadFromDB(spawnId, this, true))
                        obj.Dispose();
                    break;
                }
                default:
                    Cypher.Assert(false, $"Invalid spawn type {type} (spawnid {spawnId}) on map {GetId()}");
                    break;
            }
        }

        void ProcessRespawns()
        {
            long now = GameTime.GetGameTime();
            while (!_respawnTimes.Empty())
            {
                RespawnInfo next = _respawnTimes.First();
                if (now < next.respawnTime) // done for this tick
                    break;

                uint poolId = Global.PoolMgr.IsPartOfAPool(next.type, next.spawnId);
                if (poolId != 0) // is this part of a pool?
                { // if yes, respawn will be handled by (external) pooling logic, just delete the respawn time
                    // step 1: remove entry from maps to avoid it being reachable by outside logic
                    _respawnTimes.Remove(next);
                    GetRespawnMapForType(next.type).Remove(next.spawnId);

                    // step 2: tell pooling logic to do its thing
                    Global.PoolMgr.UpdatePool(GetPoolData(), poolId, next.type, next.spawnId);

                    // step 3: get rid of the actual entry
                    RemoveRespawnTime(next.type, next.spawnId, null, true);
                    GetRespawnMapForType(next.type).Remove(next.spawnId);
                }
                else if (CheckRespawn(next)) // see if we're allowed to respawn
                { // ok, respawn
                  // step 1: remove entry from maps to avoid it being reachable by outside logic
                    _respawnTimes.Remove(next);
                    GetRespawnMapForType(next.type).Remove(next.spawnId);

                    // step 2: do the respawn, which involves external logic
                    DoRespawn(next.type, next.spawnId, next.gridId);

                    // step 3: get rid of the actual entry
                    RemoveRespawnTime(next.type, next.spawnId, null, true);
                    GetRespawnMapForType(next.type).Remove(next.spawnId);
                }
                else if (next.respawnTime == 0)
                { // just remove this respawn entry without rescheduling
                    _respawnTimes.Remove(next);
                    GetRespawnMapForType(next.type).Remove(next.spawnId);
                    RemoveRespawnTime(next.type, next.spawnId, null, true);
                }
                else
                { // new respawn time, update heap position
                    Cypher.Assert(now < next.respawnTime); // infinite loop guard
                    SaveRespawnInfoDB(next);
                }
            }
        }

        public void ApplyDynamicModeRespawnScaling(WorldObject obj, ulong spawnId, ref uint respawnDelay, uint mode)
        {
            Cypher.Assert(mode == 1);
            Cypher.Assert(obj.GetMap() == this);

            if (IsBattlegroundOrArena())
                return;

            SpawnObjectType type;
            switch (obj.GetTypeId())
            {
                case TypeId.Unit:
                    type = SpawnObjectType.Creature;
                    break;
                case TypeId.GameObject:
                    type = SpawnObjectType.GameObject;
                    break;
                default:
                    return;
            }

            SpawnMetadata data = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);
            if (data == null)
                return;

            if (!data.spawnGroupData.flags.HasFlag(SpawnGroupFlags.DynamicSpawnRate))
                return;

            if (!_zonePlayerCountMap.ContainsKey(obj.GetZoneId()))
                return;

            uint playerCount = _zonePlayerCountMap[obj.GetZoneId()];
            if (playerCount == 0)
                return;

            double adjustFactor = WorldConfig.GetFloatValue(type == SpawnObjectType.GameObject ? WorldCfg.RespawnDynamicRateGameobject : WorldCfg.RespawnDynamicRateCreature) / playerCount;
            if (adjustFactor >= 1.0) // nothing to do here
                return;

            uint timeMinimum = WorldConfig.GetUIntValue(type == SpawnObjectType.GameObject ? WorldCfg.RespawnDynamicMinimumGameObject : WorldCfg.RespawnDynamicMinimumCreature);
            if (respawnDelay <= timeMinimum)
                return;

            respawnDelay = (uint)Math.Max(Math.Ceiling(respawnDelay * adjustFactor), timeMinimum);
        }

        public bool ShouldBeSpawnedOnGridLoad<T>(ulong spawnId) { return ShouldBeSpawnedOnGridLoad(SpawnData.TypeFor<T>(), spawnId); }

        bool ShouldBeSpawnedOnGridLoad(SpawnObjectType type, ulong spawnId)
        {
            Cypher.Assert(SpawnData.TypeHasData(type));
            // check if the object is on its respawn timer
            if (GetRespawnTime(type, spawnId) != 0)
                return false;

            SpawnMetadata spawnData = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);
            // check if the object is part of a spawn group
            SpawnGroupTemplateData spawnGroup = spawnData.spawnGroupData;
            if (!spawnGroup.flags.HasFlag(SpawnGroupFlags.System))
                if (!IsSpawnGroupActive(spawnGroup.groupId))
                    return false;

            if (spawnData.ToSpawnData().poolId != 0)
                if (!GetPoolData().IsSpawnedObject(type, spawnId))
                    return false;

            return true;
        }

        SpawnGroupTemplateData GetSpawnGroupData(uint groupId)
        {
            SpawnGroupTemplateData data = Global.ObjectMgr.GetSpawnGroupData(groupId);
            if (data != null && (data.flags.HasAnyFlag(SpawnGroupFlags.System) || data.mapId == GetId()))
                return data;

            return null;
        }

        public bool SpawnGroupSpawn(uint groupId, bool ignoreRespawn = false, bool force = false, List<WorldObject> spawnedObjects = null)
        {
            var groupData = GetSpawnGroupData(groupId);
            if (groupData == null || groupData.flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                Log.outError(LogFilter.Maps, $"Tried to spawn non-existing (or system) spawn group {groupId}. on map {GetId()} Blocked.");
                return false;
            }

            SetSpawnGroupActive(groupId, true); // start processing respawns for the group

            List<SpawnData> toSpawn = new();
            foreach (var data in Global.ObjectMgr.GetSpawnMetadataForGroup(groupId))
            {
                Cypher.Assert(groupData.mapId == data.MapId);

                var respawnMap = GetRespawnMapForType(data.type);
                if (respawnMap == null)
                    continue;

                if (force || ignoreRespawn)
                    RemoveRespawnTime(data.type, data.SpawnId);

                bool hasRespawnTimer = respawnMap.ContainsKey(data.SpawnId);
                if (SpawnData.TypeHasData(data.type))
                {
                    // has a respawn timer
                    if (hasRespawnTimer)
                        continue;

                    // has a spawn already active
                    if (!force)
                    {
                        WorldObject obj = GetWorldObjectBySpawnId(data.type, data.SpawnId);
                        if (obj != null)
                            if ((data.type != SpawnObjectType.Creature) || obj.ToCreature().IsAlive())
                                continue;
                    }

                    toSpawn.Add(data.ToSpawnData());
                }
            }

            foreach (SpawnData data in toSpawn)
            {
                // don't spawn if the current map difficulty is not used by the spawn
                if (!data.SpawnDifficulties.Contains(GetDifficultyID()))
                    continue;

                // don't spawn if the grid isn't loaded (will be handled in grid loader)
                if (!IsGridLoaded(data.SpawnPoint))
                    continue;

                // now do the actual (re)spawn
                switch (data.type)
                {
                    case SpawnObjectType.Creature:
                    {
                        Creature creature = new();
                        if (!creature.LoadFromDB(data.SpawnId, this, true, force))
                            creature.Dispose();
                        else if (spawnedObjects != null)
                            spawnedObjects.Add(creature);
                        break;
                    }
                    case SpawnObjectType.GameObject:
                    {
                        GameObject gameobject = new();
                        if (!gameobject.LoadFromDB(data.SpawnId, this, true))
                            gameobject.Dispose();
                        else if (spawnedObjects != null)
                            spawnedObjects.Add(gameobject);
                        break;
                    }
                    case SpawnObjectType.AreaTrigger:
                    {
                        AreaTrigger areaTrigger = new AreaTrigger();
                        if (!areaTrigger.LoadFromDB(data.SpawnId, this, true, false))
                            areaTrigger.Dispose();
                        else if (spawnedObjects != null)
                            spawnedObjects.Add(areaTrigger);
                        break;
                    }
                    default:
                        Cypher.Assert(false, $"Invalid spawn type {data.type} with spawnId {data.SpawnId}");
                        return false;
                }
            }

            return true;
        }

        public bool SpawnGroupDespawn(uint groupId, bool deleteRespawnTimes = false)
        {
            return SpawnGroupDespawn(groupId, deleteRespawnTimes, out _);
        }

        public bool SpawnGroupDespawn(uint groupId, bool deleteRespawnTimes, out int count)
        {
            count = 0;
            SpawnGroupTemplateData groupData = GetSpawnGroupData(groupId);
            if (groupData == null || groupData.flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                Log.outError(LogFilter.Maps, $"Tried to despawn non-existing (or system) spawn group {groupId} on map {GetId()}. Blocked.");
                return false;
            }

            foreach (var data in Global.ObjectMgr.GetSpawnMetadataForGroup(groupId))
            {
                Cypher.Assert(groupData.mapId == data.MapId);
                if (deleteRespawnTimes)
                    RemoveRespawnTime(data.type, data.SpawnId);
                count += DespawnAll(data.type, data.SpawnId);
            }

            SetSpawnGroupActive(groupId, false); // stop processing respawns for the group, too
            return true;
        }

        public void SetSpawnGroupActive(uint groupId, bool state)
        {
            SpawnGroupTemplateData data = GetSpawnGroupData(groupId);
            if (data == null || data.flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                Log.outError(LogFilter.Maps, $"Tried to set non-existing (or system) spawn group {groupId} to {(state ? "active" : "inactive")} on map {GetId()}. Blocked.");
                return;
            }
            if (state != !data.flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn)) // toggled
                _toggledSpawnGroupIds.Add(groupId);
            else
                _toggledSpawnGroupIds.Remove(groupId);
        }

        // Disable the spawn group, which prevents any creatures in the group from respawning until re-enabled
        // This will not affect any already-present creatures in the group
        public void SetSpawnGroupInactive(uint groupId) { SetSpawnGroupActive(groupId, false); }

        public bool IsSpawnGroupActive(uint groupId)
        {
            SpawnGroupTemplateData data = GetSpawnGroupData(groupId);
            if (data == null)
            {
                Log.outError(LogFilter.Maps, $"Tried to query state of non-existing spawn group {groupId} on map {GetId()}.");
                return false;
            }

            if (data.flags.HasAnyFlag(SpawnGroupFlags.System))
                return true;

            // either manual spawn group and toggled, or not manual spawn group and not toggled...
            return _toggledSpawnGroupIds.Contains(groupId) != !data.flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn);
        }

        public void UpdateSpawnGroupConditions()
        {
            var spawnGroups = Global.ObjectMgr.GetSpawnGroupsForMap(GetId());
            foreach (uint spawnGroupId in spawnGroups)
            {
                SpawnGroupTemplateData spawnGroupTemplate = GetSpawnGroupData(spawnGroupId);

                bool isActive = IsSpawnGroupActive(spawnGroupId);
                bool shouldBeActive = Global.ConditionMgr.IsMapMeetingNotGroupedConditions(ConditionSourceType.SpawnGroup, spawnGroupId, this);

                if (spawnGroupTemplate.flags.HasFlag(SpawnGroupFlags.ManualSpawn))
                {
                    // Only despawn the group if it isn't meeting conditions
                    if (isActive && !shouldBeActive && spawnGroupTemplate.flags.HasFlag(SpawnGroupFlags.DespawnOnConditionFailure))
                        SpawnGroupDespawn(spawnGroupId, true);

                    continue;
                }

                if (isActive == shouldBeActive)
                    continue;

                if (shouldBeActive)
                    SpawnGroupSpawn(spawnGroupId);
                else if (spawnGroupTemplate.flags.HasFlag(SpawnGroupFlags.DespawnOnConditionFailure))
                    SpawnGroupDespawn(spawnGroupId, true);
                else
                    SetSpawnGroupInactive(spawnGroupId);
            }
        }

        public void AddFarSpellCallback(FarSpellCallback callback)
        {
            _farSpellCallbacks.Enqueue(new FarSpellCallback(callback));
        }

        public virtual void DelayedUpdate(uint diff)
        {
            while (_farSpellCallbacks.TryDequeue(out FarSpellCallback callback))
                callback(this);

            RemoveAllObjectsInRemoveList();

            // Don't unload grids if it's Battleground, since we may have manually added GOs, creatures, those doesn't load from DB at grid re-load !
            // This isn't really bother us, since as soon as we have instanced BG-s, the whole map unloads as the BG gets ended
            if (!IsBattlegroundOrArena())
            {
                foreach (var xkvp in _grids)
                {
                    foreach (var ykvp in xkvp.Value)
                    {
                        Grid grid = ykvp.Value;
                        if (grid != null)
                            grid.Update(this, diff);
                    }
                }
            }
        }

        public void AddObjectToRemoveList(WorldObject obj)
        {
            Cypher.Assert(obj.GetMapId() == GetId() && obj.GetInstanceId() == GetInstanceId());

            obj.SetDestroyedObject(true);
            obj.CleanupsBeforeDelete(false); // remove or simplify at least cross referenced links

            i_objectsToRemove.Add(obj);
        }

        public void AddObjectToSwitchList(WorldObject obj, bool on)
        {
            Cypher.Assert(obj.GetMapId() == GetId() && obj.GetInstanceId() == GetInstanceId());
            // i_objectsToSwitch is iterated only in Map::RemoveAllObjectsInRemoveList() and it uses
            // the contained objects only if GetTypeId() == TYPEID_UNIT , so we can return in all other cases
            if (!obj.IsTypeId(TypeId.Unit))
                return;

            if (!i_objectsToSwitch.ContainsKey(obj))
                i_objectsToSwitch.Add(obj, on);
            else if (i_objectsToSwitch[obj] != on)
                i_objectsToSwitch.Remove(obj);
            else
                Cypher.Assert(false);
        }

        void RemoveAllObjectsInRemoveList()
        {
            while (!i_objectsToSwitch.Empty())
            {
                KeyValuePair<WorldObject, bool> pair = i_objectsToSwitch.First();
                WorldObject obj = pair.Key;
                bool on = pair.Value;
                i_objectsToSwitch.Remove(pair.Key);

                if (!obj.IsPermanentWorldObject())
                {
                    switch (obj.GetTypeId())
                    {
                        case TypeId.Unit:
                            SwitchGridContainers(obj.ToCreature(), on);
                            break;
                        default:
                            break;
                    }
                }
            }

            while (!i_objectsToRemove.Empty())
            {
                WorldObject obj = i_objectsToRemove.First();

                switch (obj.GetTypeId())
                {
                    case TypeId.Corpse:
                    {
                        Corpse corpse = ObjectAccessor.GetCorpse(obj, obj.GetGUID());
                        if (corpse == null)
                            Log.outError(LogFilter.Maps, "Tried to delete corpse/bones {0} that is not in map.", obj.GetGUID().ToString());
                        else
                            RemoveFromMap(corpse, true);
                        break;
                    }
                    case TypeId.DynamicObject:
                        RemoveFromMap(obj, true);
                        break;
                    case TypeId.AreaTrigger:
                        RemoveFromMap(obj, true);
                        break;
                    case TypeId.Conversation:
                        RemoveFromMap(obj, true);
                        break;
                    case TypeId.GameObject:
                        GameObject go = obj.ToGameObject();
                        Transport transport = go.ToTransport();
                        if (transport)
                            RemoveFromMap(transport, true);
                        else
                            RemoveFromMap(go, true);
                        break;
                    case TypeId.Unit:
                        // in case triggered sequence some spell can continue casting after prev CleanupsBeforeDelete call
                        // make sure that like sources auras/etc removed before destructor start
                        obj.ToCreature().CleanupsBeforeDelete();
                        RemoveFromMap(obj.ToCreature(), true);
                        break;
                    default:
                        Log.outError(LogFilter.Maps, "Non-grid object (TypeId: {0}) is in grid object remove list, ignored.", obj.GetTypeId());
                        break;
                }

                i_objectsToRemove.Remove(obj);
            }
        }

        public uint GetPlayersCountExceptGMs()
        {
            uint count = 0;
            foreach (Player pl in m_activePlayers)
                if (!pl.IsGameMaster())
                    ++count;
            return count;
        }

        public void SendToPlayers(ServerPacket data)
        {
            foreach (Player pl in m_activePlayers)
                pl.SendPacket(data);
        }

        public bool ActiveObjectsNearGrid(Grid grid)
        {
            var cell_min = new CellCoord(grid.GetX() * MapConst.MaxCells,
                grid.GetY() * MapConst.MaxCells);
            var cell_max = new CellCoord(cell_min.X_coord + MapConst.MaxCells,
                cell_min.Y_coord + MapConst.MaxCells);

            //we must find visible range in cells so we unload only non-visible cells...
            float viewDist = GetVisibilityRange();
            uint cell_range = (uint)Math.Ceiling(viewDist / MapConst.SizeofCells) + 1;

            cell_min.Dec_x(cell_range);
            cell_min.Dec_y(cell_range);
            cell_max.Inc_x(cell_range);
            cell_max.Inc_y(cell_range);

            foreach (Player pl in m_activePlayers)
            {
                CellCoord p = GridDefines.ComputeCellCoord(pl.GetPositionX(), pl.GetPositionY());
                if ((cell_min.X_coord <= p.X_coord && p.X_coord <= cell_max.X_coord) &&
                    (cell_min.Y_coord <= p.Y_coord && p.Y_coord <= cell_max.Y_coord))
                    return true;
            }

            foreach (WorldObject obj in m_activeNonPlayers)
            {
                CellCoord p = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
                if ((cell_min.X_coord <= p.X_coord && p.X_coord <= cell_max.X_coord) &&
                    (cell_min.Y_coord <= p.Y_coord && p.Y_coord <= cell_max.Y_coord))
                    return true;
            }

            return false;
        }

        public void AddToActive(WorldObject obj)
        {
            AddToActiveHelper(obj);

            Position respawnLocation = null;
            switch (obj.GetTypeId())
            {
                case TypeId.Unit:
                    Creature creature = obj.ToCreature();
                    if (creature != null && !creature.IsPet() && creature.GetSpawnId() != 0)
                    {
                        respawnLocation = new();
                        creature.GetRespawnPosition(out respawnLocation.posX, out respawnLocation.posY, out respawnLocation.posZ);
                    }
                    break;
                case TypeId.GameObject:
                    GameObject gameObject = obj.ToGameObject(); ;
                    if (gameObject != null && gameObject.GetSpawnId() != 0)
                    {
                        respawnLocation = new();
                        gameObject.GetRespawnPosition(out respawnLocation.posX, out respawnLocation.posY, out respawnLocation.posZ, out _);
                    }
                    break;
                default:
                    break;
            }

            if (respawnLocation != null)
            {
                GridCoord p = GridDefines.ComputeGridCoord(respawnLocation.GetPositionX(), respawnLocation.GetPositionY());
                if (GetGrid(p.X_coord, p.Y_coord) != null)
                    GetGrid(p.X_coord, p.Y_coord).IncUnloadActiveLock();
                else
                {
                    GridCoord p2 = GridDefines.ComputeGridCoord(obj.GetPositionX(), obj.GetPositionY());
                    Log.outError(LogFilter.Maps, $"Active object {obj.GetGUID()} added to grid[{p.X_coord}, {p.Y_coord}] but spawn grid[{p2.X_coord}, {p2.Y_coord}] was not loaded.");
                }
            }
        }

        void AddToActiveHelper(WorldObject obj)
        {
            m_activeNonPlayers.Add(obj);
        }

        public void RemoveFromActive(WorldObject obj)
        {
            RemoveFromActiveHelper(obj);

            Position respawnLocation = null;
            switch (obj.GetTypeId())
            {
                case TypeId.Unit:
                    Creature creature = obj.ToCreature();
                    if (creature != null && !creature.IsPet() && creature.GetSpawnId() != 0)
                    {
                        respawnLocation = new();
                        creature.GetRespawnPosition(out respawnLocation.posX, out respawnLocation.posY, out respawnLocation.posZ);
                    }
                    break;
                case TypeId.GameObject:
                    GameObject gameObject = obj.ToGameObject();
                    if (gameObject != null && gameObject.GetSpawnId() != 0)
                    {
                        respawnLocation = new();
                        gameObject.GetRespawnPosition(out respawnLocation.posX, out respawnLocation.posY, out respawnLocation.posZ, out _);
                    }
                    break;
                default:
                    break;
            }

            if (respawnLocation != null)
            {
                GridCoord p = GridDefines.ComputeGridCoord(respawnLocation.GetPositionX(), respawnLocation.GetPositionY());
                if (GetGrid(p.X_coord, p.Y_coord) != null)
                    GetGrid(p.X_coord, p.Y_coord).DecUnloadActiveLock();
                else
                {
                    GridCoord p2 = GridDefines.ComputeGridCoord(obj.GetPositionX(), obj.GetPositionY());
                    Log.outDebug(LogFilter.Maps, $"Active object {obj.GetGUID()} removed from grid[{p.X_coord}, {p.Y_coord}] but spawn grid[{p2.X_coord}, {p2.Y_coord}] was not loaded.");
                }
            }
        }

        void RemoveFromActiveHelper(WorldObject obj)
        {
            m_activeNonPlayers.Remove(obj);
        }

        public void SaveRespawnTime(SpawnObjectType type, ulong spawnId, uint entry, long respawnTime, uint gridId = 0, SQLTransaction dbTrans = null, bool startup = false)
        {
            SpawnMetadata data = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Maps, $"Map {GetId()} attempt to save respawn time for nonexistant spawnid ({type},{spawnId}).");
                return;
            }

            if (respawnTime == 0)
            {
                // Delete only
                RemoveRespawnTime(data.type, data.SpawnId, dbTrans);
                return;
            }

            RespawnInfo ri = new();
            ri.type = data.type;
            ri.spawnId = data.SpawnId;
            ri.entry = entry;
            ri.respawnTime = respawnTime;
            ri.gridId = gridId;
            bool success = AddRespawnInfo(ri);

            if (startup)
            {
                if (!success)
                    Log.outError(LogFilter.Maps, $"Attempt to load saved respawn {respawnTime} for ({type},{spawnId}) failed - duplicate respawn? Skipped.");
            }
            else if (success)
                SaveRespawnInfoDB(ri, dbTrans);
        }

        public void SaveRespawnInfoDB(RespawnInfo info, SQLTransaction dbTrans = null)
        {
            if (Instanceable())
                return;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_RESPAWN);
            stmt.AddValue(0, (ushort)info.type);
            stmt.AddValue(1, info.spawnId);
            stmt.AddValue(2, info.respawnTime);
            stmt.AddValue(3, GetId());
            stmt.AddValue(4, GetInstanceId());
            DB.Characters.ExecuteOrAppend(dbTrans, stmt);
        }

        public void LoadRespawnTimes()
        {
            if (Instanceable())
                return;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_RESPAWNS);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    SpawnObjectType type = (SpawnObjectType)result.Read<ushort>(0);
                    var spawnId = result.Read<ulong>(1);
                    var respawnTime = result.Read<long>(2);

                    if (SpawnData.TypeHasData(type))
                    {
                        SpawnData data = Global.ObjectMgr.GetSpawnData(type, spawnId);
                        if (data != null)
                            SaveRespawnTime(type, spawnId, data.Id, respawnTime, GridDefines.ComputeGridCoord(data.SpawnPoint.GetPositionX(), data.SpawnPoint.GetPositionY()).GetId(), null, true);
                        else
                            Log.outError(LogFilter.Maps, $"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - spawn does not exist, ignoring");
                    }
                    else
                        Log.outError(LogFilter.Maps, $"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - invalid spawn type, ignoring");

                } while (result.NextRow());
            }
        }

        public void DeleteRespawnTimes()
        {
            UnloadAllRespawnInfos();
            DeleteRespawnTimesInDB();
        }

        public void DeleteRespawnTimesInDB()
        {
            if (Instanceable())
                return;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_RESPAWNS);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());
            DB.Characters.Execute(stmt);
        }

        public long GetLinkedRespawnTime(ObjectGuid guid)
        {
            ObjectGuid linkedGuid = Global.ObjectMgr.GetLinkedRespawnGuid(guid);
            switch (linkedGuid.GetHigh())
            {
                case HighGuid.Creature:
                    return GetCreatureRespawnTime(linkedGuid.GetCounter());
                case HighGuid.GameObject:
                    return GetGORespawnTime(linkedGuid.GetCounter());
                default:
                    break;
            }

            return 0L;
        }

        public void LoadCorpseData()
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSES);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());

            //        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
            // SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
                return;

            MultiMap<ulong, uint> phases = new();
            MultiMap<ulong, ChrCustomizationChoice> customizations = new();

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSE_PHASES);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());

            //        0          1
            // SELECT OwnerGuid, PhaseId FROM corpse_phases cp LEFT JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
            SQLResult phaseResult = DB.Characters.Query(stmt);
            if (!phaseResult.IsEmpty())
            {
                do
                {
                    ulong guid = phaseResult.Read<ulong>(0);
                    uint phaseId = phaseResult.Read<uint>(1);

                    phases.Add(guid, phaseId);

                } while (phaseResult.NextRow());
            }

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSE_CUSTOMIZATIONS);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());

            //        0             1                            2
            // SELECT cc.ownerGuid, cc.chrCustomizationOptionID, cc.chrCustomizationChoiceID FROM corpse_customizations cc LEFT JOIN corpse c ON cc.ownerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
            SQLResult customizationResult = DB.Characters.Query(stmt);
            if (!customizationResult.IsEmpty())
            {
                do
                {
                    ulong guid = customizationResult.Read<ulong>(0);

                    ChrCustomizationChoice choice = new();
                    choice.ChrCustomizationOptionID = customizationResult.Read<uint>(1);
                    choice.ChrCustomizationChoiceID = customizationResult.Read<uint>(2);
                    customizations.Add(guid, choice);

                } while (customizationResult.NextRow());
            }

            do
            {
                CorpseType type = (CorpseType)result.Read<byte>(13);
                ulong guid = result.Read<ulong>(15);
                if (type >= CorpseType.Max || type == CorpseType.Bones)
                {
                    Log.outError(LogFilter.Maps, "Corpse (guid: {0}) have wrong corpse type ({1}), not loading.", guid, type);
                    continue;
                }

                Corpse corpse = new(type);
                if (!corpse.LoadCorpseFromDB(GenerateLowGuid(HighGuid.Corpse), result.GetFields()))
                    continue;

                foreach (var phaseId in phases[guid])
                    PhasingHandler.AddPhase(corpse, phaseId, false);

                corpse.SetCustomizations(customizations[guid]);

                AddCorpse(corpse);
            } while (result.NextRow());
        }

        public void DeleteCorpseData()
        {
            // DELETE cp, c FROM corpse_phases cp INNER JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSES_FROM_MAP);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());
            DB.Characters.Execute(stmt);
        }

        public void AddCorpse(Corpse corpse)
        {
            corpse.SetMap(this);

            _corpsesByCell.Add(corpse.GetCellCoord().GetId(), corpse);
            if (corpse.GetCorpseType() != CorpseType.Bones)
                _corpsesByPlayer[corpse.GetOwnerGUID()] = corpse;
            else
                _corpseBones.Add(corpse);
        }

        void RemoveCorpse(Corpse corpse)
        {
            Cypher.Assert(corpse);

            corpse.UpdateObjectVisibilityOnDestroy();
            if (corpse.GetCurrentCell() != null)
                RemoveFromMap(corpse, false);
            else
            {
                corpse.RemoveFromWorld();
                corpse.ResetMap();
            }

            _corpsesByCell.Remove(corpse.GetCellCoord().GetId(), corpse);
            if (corpse.GetCorpseType() != CorpseType.Bones)
                _corpsesByPlayer.Remove(corpse.GetOwnerGUID());
            else
                _corpseBones.Remove(corpse);
        }

        public Corpse ConvertCorpseToBones(ObjectGuid ownerGuid, bool insignia = false)
        {
            Corpse corpse = GetCorpseByPlayer(ownerGuid);
            if (!corpse)
                return null;

            RemoveCorpse(corpse);

            // remove corpse from DB
            SQLTransaction trans = new();
            corpse.DeleteFromDB(trans);
            DB.Characters.CommitTransaction(trans);

            Corpse bones = null;

            // create the bones only if the map and the grid is loaded at the corpse's location
            // ignore bones creating option in case insignia
            if ((insignia ||
                (IsBattlegroundOrArena() ? WorldConfig.GetBoolValue(WorldCfg.DeathBonesBgOrArena) : WorldConfig.GetBoolValue(WorldCfg.DeathBonesWorld))) &&
                !IsRemovalGrid(corpse.GetPositionX(), corpse.GetPositionY()))
            {
                // Create bones, don't change Corpse
                bones = new Corpse();
                bones.Create(corpse.GetGUID().GetCounter(), this);

                bones.ReplaceAllCorpseDynamicFlags((CorpseDynFlags)(byte)corpse.m_corpseData.DynamicFlags);
                bones.SetOwnerGUID(corpse.m_corpseData.Owner);
                bones.SetPartyGUID(corpse.m_corpseData.PartyGUID);
                bones.SetGuildGUID(corpse.m_corpseData.GuildGUID);
                bones.SetDisplayId(corpse.m_corpseData.DisplayID);
                bones.SetRace(corpse.m_corpseData.RaceID);
                bones.SetSex(corpse.m_corpseData.Sex);
                bones.SetClass(corpse.m_corpseData.Class);
                bones.SetCustomizations(corpse.m_corpseData.Customizations);
                bones.ReplaceAllFlags((CorpseFlags)(corpse.m_corpseData.Flags | (uint)CorpseFlags.Bones));
                bones.SetFactionTemplate(corpse.m_corpseData.FactionTemplate);
                for (int i = 0; i < EquipmentSlot.End; ++i)
                    bones.SetItem((uint)i, corpse.m_corpseData.Items[i]);

                bones.SetCellCoord(corpse.GetCellCoord());
                bones.Relocate(corpse.GetPositionX(), corpse.GetPositionY(), corpse.GetPositionZ(), corpse.GetOrientation());

                PhasingHandler.InheritPhaseShift(bones, corpse);

                AddCorpse(bones);

                bones.UpdatePositionData();
                bones.SetZoneScript();

                // add bones in grid store if grid loaded where corpse placed
                AddToMap(bones);
            }

            // all references to the corpse should be removed at this point
            corpse.Dispose();

            return bones;
        }

        public void RemoveOldCorpses()
        {
            long now = GameTime.GetGameTime();

            List<ObjectGuid> corpses = new();

            foreach (var p in _corpsesByPlayer)
                if (p.Value.IsExpired(now))
                    corpses.Add(p.Key);

            foreach (ObjectGuid ownerGuid in corpses)
                ConvertCorpseToBones(ownerGuid);

            List<Corpse> expiredBones = new();
            foreach (Corpse bones in _corpseBones)
                if (bones.IsExpired(now))
                    expiredBones.Add(bones);

            foreach (Corpse bones in expiredBones)
            {
                RemoveCorpse(bones);
                bones.Dispose();
            }
        }

        public void SendZoneDynamicInfo(uint zoneId, Player player)
        {
            var zoneInfo = _zoneDynamicInfo.LookupByKey(zoneId);
            if (zoneInfo == null)
                return;

            uint music = zoneInfo.MusicId;
            if (music != 0)
                player.SendPacket(new PlayMusic(music));

            SendZoneWeather(zoneInfo, player);

            foreach (var lightOverride in zoneInfo.LightOverrides)
            {
                OverrideLight overrideLight = new();
                overrideLight.AreaLightID = lightOverride.AreaLightId;
                overrideLight.OverrideLightID = lightOverride.OverrideLightId;
                overrideLight.TransitionMilliseconds = lightOverride.TransitionMilliseconds;
                player.SendPacket(overrideLight);
            }
        }

        public void SendZoneWeather(uint zoneId, Player player)
        {
            if (!player.HasAuraType(AuraType.ForceWeather))
            {
                var zoneInfo = _zoneDynamicInfo.LookupByKey(zoneId);
                if (zoneInfo == null)
                    return;

                SendZoneWeather(zoneInfo, player);
            }
        }

        void SendZoneWeather(ZoneDynamicInfo zoneDynamicInfo, Player player)
        {
            WeatherState weatherId = zoneDynamicInfo.WeatherId;
            if (weatherId != 0)
            {
                WeatherPkt weather = new(weatherId, zoneDynamicInfo.Intensity);
                player.SendPacket(weather);
            }
            else if (zoneDynamicInfo.DefaultWeather != null)
            {
                zoneDynamicInfo.DefaultWeather.SendWeatherUpdateToPlayer(player);
            }
            else
                Weather.SendFineWeatherUpdateToPlayer(player);
        }

        public void SetZoneMusic(uint zoneId, uint musicId)
        {
            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            _zoneDynamicInfo[zoneId].MusicId = musicId;

            var players = GetPlayers();
            if (!players.Empty())
            {
                PlayMusic playMusic = new(musicId);

                foreach (var player in players)
                    if (player.GetZoneId() == zoneId && !player.HasAuraType(AuraType.ForceWeather))
                        player.SendPacket(playMusic);
            }
        }

        public Weather GetOrGenerateZoneDefaultWeather(uint zoneId)
        {
            WeatherData weatherData = Global.WeatherMgr.GetWeatherData(zoneId);
            if (weatherData == null)
                return null;

            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            ZoneDynamicInfo info = _zoneDynamicInfo[zoneId];
            if (info.DefaultWeather == null)
            {
                info.DefaultWeather = new Weather(zoneId, weatherData);
                info.DefaultWeather.ReGenerate();
                info.DefaultWeather.UpdateWeather();
            }

            return info.DefaultWeather;
        }

        public WeatherState GetZoneWeather(uint zoneId)
        {
            ZoneDynamicInfo zoneDynamicInfo = _zoneDynamicInfo.LookupByKey(zoneId);
            if (zoneDynamicInfo != null)
            {
                if (zoneDynamicInfo.WeatherId != 0)
                    return zoneDynamicInfo.WeatherId;

                if (zoneDynamicInfo.DefaultWeather != null)
                    return zoneDynamicInfo.DefaultWeather.GetWeatherState();
            }

            return WeatherState.Fine;
        }

        public void SetZoneWeather(uint zoneId, WeatherState weatherId, float intensity)
        {
            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            ZoneDynamicInfo info = _zoneDynamicInfo[zoneId];
            info.WeatherId = weatherId;
            info.Intensity = intensity;

            var players = GetPlayers();
            if (!players.Empty())
            {
                WeatherPkt weather = new(weatherId, intensity);

                foreach (var player in players)
                {
                    if (player.GetZoneId() == zoneId)
                        player.SendPacket(weather);
                }
            }
        }

        public void SetZoneOverrideLight(uint zoneId, uint areaLightId, uint overrideLightId, TimeSpan transitionTime)
        {
            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            ZoneDynamicInfo info = _zoneDynamicInfo[zoneId];
            // client can support only one override for each light (zone independent)
            info.LightOverrides.RemoveAll(lightOverride => lightOverride.AreaLightId == areaLightId);

            // set new override (if any)
            if (overrideLightId != 0)
            {
                ZoneDynamicInfo.LightOverride lightOverride = new();
                lightOverride.AreaLightId = areaLightId;
                lightOverride.OverrideLightId = overrideLightId;
                lightOverride.TransitionMilliseconds = (uint)transitionTime.TotalMilliseconds;
                info.LightOverrides.Add(lightOverride);
            }

            var players = GetPlayers();

            if (!players.Empty())
            {
                OverrideLight overrideLight = new();
                overrideLight.AreaLightID = areaLightId;
                overrideLight.OverrideLightID = overrideLightId;
                overrideLight.TransitionMilliseconds = (uint)transitionTime.TotalMilliseconds;

                foreach (var player in players)
                    if (player.GetZoneId() == zoneId)
                        player.SendPacket(overrideLight);
            }
        }

        public void UpdateAreaDependentAuras()
        {
            var players = GetPlayers();
            foreach (var player in players)
            {
                if (player)
                {
                    if (player.IsInWorld)
                    {
                        player.UpdateAreaDependentAuras(player.GetAreaId());
                        player.UpdateZoneDependentAuras(player.GetZoneId());
                    }
                }
            }
        }

        public virtual string GetDebugInfo()
        {
            return $"Id: {GetId()} InstanceId: {GetInstanceId()} Difficulty: {GetDifficultyID()} HasPlayers: {HavePlayers()}";
        }

        public MapRecord GetEntry()
        {
            return i_mapRecord;
        }

        public bool CanUnload(uint diff)
        {
            if (m_unloadTimer == 0)
                return false;

            if (m_unloadTimer <= diff)
                return true;

            m_unloadTimer -= diff;
            return false;
        }

        public float GetVisibilityRange()
        {
            return m_VisibleDistance;
        }

        public bool IsRemovalGrid(float x, float y)
        {
            GridCoord p = GridDefines.ComputeGridCoord(x, y);
            return GetGrid(p.X_coord, p.Y_coord) == null ||
                   GetGrid(p.X_coord, p.Y_coord).GetGridState() == GridState.Removal;
        }
        public bool IsRemovalGrid(Position pos) { return IsRemovalGrid(pos.GetPositionX(), pos.GetPositionY()); }

        private bool GetUnloadLock(GridCoord p)
        {
            return GetGrid(p.X_coord, p.Y_coord).GetUnloadLock();
        }

        void SetUnloadLock(GridCoord p, bool on)
        {
            GetGrid(p.X_coord, p.Y_coord).SetUnloadExplicitLock(on);
        }

        public void ResetGridExpiry(Grid grid, float factor = 1)
        {
            grid.ResetTimeTracker((long)(i_gridExpiry * factor));
        }

        public long GetGridExpiry()
        {
            return i_gridExpiry;
        }

        public TerrainInfo GetTerrain() { return m_terrain; }

        public uint GetInstanceId()
        {
            return i_InstanceId;
        }

        public virtual TransferAbortParams CannotEnter(Player player) { return null; }

        public Difficulty GetDifficultyID()
        {
            return i_spawnMode;
        }

        public MapDifficultyRecord GetMapDifficulty()
        {
            return Global.DB2Mgr.GetMapDifficultyData(GetId(), GetDifficultyID());
        }

        public ItemContext GetDifficultyLootItemContext()
        {
            MapDifficultyRecord mapDifficulty = GetMapDifficulty();
            if (mapDifficulty != null && mapDifficulty.ItemContext != 0)
                return (ItemContext)mapDifficulty.ItemContext;

            DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(GetDifficultyID());
            if (difficulty != null)
                return (ItemContext)difficulty.ItemContext;

            return ItemContext.None;
        }

        public uint GetId()
        {
            return i_mapRecord.Id;
        }

        public bool Instanceable()
        {
            return i_mapRecord != null && i_mapRecord.Instanceable();
        }

        public bool IsDungeon()
        {
            return i_mapRecord != null && i_mapRecord.IsDungeon();
        }

        public bool IsNonRaidDungeon()
        {
            return i_mapRecord != null && i_mapRecord.IsNonRaidDungeon();
        }

        public bool IsRaid()
        {
            return i_mapRecord != null && i_mapRecord.IsRaid();
        }

        public bool IsHeroic()
        {
            DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(i_spawnMode);
            if (difficulty != null)
                return difficulty.Flags.HasAnyFlag(DifficultyFlags.Heroic);
            return false;
        }

        public bool Is25ManRaid()
        {
            // since 25man difficulties are 1 and 3, we can check them like that
            return IsRaid() && (i_spawnMode == Difficulty.Raid25N || i_spawnMode == Difficulty.Raid25HC);
        }

        public bool IsBattleground()
        {
            return i_mapRecord != null && i_mapRecord.IsBattleground();
        }

        public bool IsBattleArena()
        {
            return i_mapRecord != null && i_mapRecord.IsBattleArena();
        }

        public bool IsBattlegroundOrArena()
        {
            return i_mapRecord != null && i_mapRecord.IsBattlegroundOrArena();
        }

        public bool IsScenario()
        {
            return i_mapRecord != null && i_mapRecord.IsScenario();
        }

        public bool IsGarrison()
        {
            return i_mapRecord != null && i_mapRecord.IsGarrison();
        }

        private bool GetEntrancePos(out uint mapid, out float x, out float y)
        {
            mapid = 0;
            x = 0;
            y = 0;

            if (i_mapRecord == null)
                return false;

            return i_mapRecord.GetEntrancePos(out mapid, out x, out y);
        }

        void ResetMarkedCells()
        {
            marked_cells.SetAll(false);
        }

        private bool IsCellMarked(uint pCellId)
        {
            return marked_cells.Get((int)pCellId);
        }

        void MarkCell(uint pCellId)
        {
            marked_cells.Set((int)pCellId, true);
        }

        public bool HavePlayers()
        {
            return !m_activePlayers.Empty();
        }

        public void AddWorldObject(WorldObject obj)
        {
            i_worldObjects.Add(obj);
        }

        public void RemoveWorldObject(WorldObject obj)
        {
            i_worldObjects.Remove(obj);
        }

        public void DoOnPlayers(Action<Player> action)
        {
            foreach (var player in GetPlayers())
                action(player);
        }

        public List<Player> GetPlayers()
        {
            return m_activePlayers;
        }

        public int GetActiveNonPlayersCount()
        {
            return m_activeNonPlayers.Count;
        }

        public Dictionary<ObjectGuid, WorldObject> GetObjectsStore() { return _objectsStore; }

        public MultiMap<ulong, Creature> GetCreatureBySpawnIdStore() { return _creatureBySpawnIdStore; }

        public MultiMap<ulong, GameObject> GetGameObjectBySpawnIdStore() { return _gameobjectBySpawnIdStore; }

        public MultiMap<ulong, AreaTrigger> GetAreaTriggerBySpawnIdStore() { return _areaTriggerBySpawnIdStore; }

        public List<Corpse> GetCorpsesInCell(uint cellId)
        {
            return _corpsesByCell.LookupByKey(cellId);
        }

        public Corpse GetCorpseByPlayer(ObjectGuid ownerGuid)
        {
            return _corpsesByPlayer.LookupByKey(ownerGuid);
        }

        public InstanceMap ToInstanceMap() { return IsDungeon() ? (this as InstanceMap) : null; }

        public BattlegroundMap ToBattlegroundMap() { return IsBattlegroundOrArena() ? (this as BattlegroundMap) : null; }

        public void Balance()
        {
            _dynamicTree.Balance();
        }

        public void RemoveGameObjectModel(GameObjectModel model)
        {
            _dynamicTree.Remove(model);
        }

        public void InsertGameObjectModel(GameObjectModel model)
        {
            _dynamicTree.Insert(model);
        }

        public bool ContainsGameObjectModel(GameObjectModel model)
        {
            return _dynamicTree.Contains(model);
        }

        public float GetGameObjectFloor(PhaseShift phaseShift, float x, float y, float z, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            return _dynamicTree.GetHeight(x, y, z, maxSearchDist, phaseShift);
        }

        public virtual uint GetOwnerGuildId(Team team = Team.Other)
        {
            return 0;
        }

        public long GetRespawnTime(SpawnObjectType type, ulong spawnId)
        {
            var map = GetRespawnMapForType(type);
            if (map != null)
            {
                var respawnInfo = map.LookupByKey(spawnId);
                return (respawnInfo == null) ? 0 : respawnInfo.respawnTime;
            }
            return 0;
        }

        public long GetCreatureRespawnTime(ulong spawnId) { return GetRespawnTime(SpawnObjectType.Creature, spawnId); }

        public long GetGORespawnTime(ulong spawnId) { return GetRespawnTime(SpawnObjectType.GameObject, spawnId); }

        void SetTimer(uint t)
        {
            i_gridExpiry = t < MapConst.MinGridDelay ? MapConst.MinGridDelay : t;
        }

        private Grid GetGrid(uint x, uint y)
        {
            if (x > MapConst.MaxGrids || y > MapConst.MaxGrids)
                return null;

            lock (_grids)
                if (_grids.TryGetValue(x, out var ygrid) && ygrid.TryGetValue(y, out var grid))
                    return grid;

            return null;
        }

        private bool IsGridObjectDataLoaded(uint x, uint y)
        {
            var grid = GetGrid(x, y);

            if (grid == null)
                return false;

            return grid.IsGridObjectDataLoaded();
        }

        void SetGridObjectDataLoaded(bool pLoaded, uint x, uint y)
        {
            var grid = GetGrid(x, y);

            if (grid != null)
                grid.SetGridObjectDataLoaded(pLoaded);
        }

        public AreaTrigger GetAreaTrigger(ObjectGuid guid)
        {
            if (!guid.IsAreaTrigger())
                return null;

            return (AreaTrigger)_objectsStore.LookupByKey(guid);
        }

        public SceneObject GetSceneObject(ObjectGuid guid)
        {
            return _objectsStore.LookupByKey(guid) as SceneObject;
        }

        public Conversation GetConversation(ObjectGuid guid)
        {
            return (Conversation)_objectsStore.LookupByKey(guid);
        }

        public Player GetPlayer(ObjectGuid guid)
        {
            return Global.ObjAccessor.GetPlayer(this, guid);
        }

        public Corpse GetCorpse(ObjectGuid guid)
        {
            if (!guid.IsCorpse())
                return null;

            return (Corpse)_objectsStore.LookupByKey(guid);
        }

        public Creature GetCreature(ObjectGuid guid)
        {
            if (!guid.IsCreatureOrVehicle())
                return null;

            return (Creature)_objectsStore.LookupByKey(guid);
        }

        public DynamicObject GetDynamicObject(ObjectGuid guid)
        {
            if (!guid.IsDynamicObject())
                return null;

            return (DynamicObject)_objectsStore.LookupByKey(guid);
        }

        public GameObject GetGameObject(ObjectGuid guid)
        {
            if (!guid.IsAnyTypeGameObject())
                return null;

            return (GameObject)_objectsStore.LookupByKey(guid);
        }

        public Pet GetPet(ObjectGuid guid)
        {
            if (!guid.IsPet())
                return null;

            return (Pet)_objectsStore.LookupByKey(guid);
        }

        public Transport GetTransport(ObjectGuid guid)
        {
            if (!guid.IsMOTransport())
                return null;

            GameObject go = GetGameObject(guid);
            return go ? go.ToTransport() : null;
        }

        public Creature GetCreatureBySpawnId(ulong spawnId)
        {
            var bounds = GetCreatureBySpawnIdStore().LookupByKey(spawnId);
            if (bounds.Empty())
                return null;

            var foundCreature = bounds.Find(creature => creature.IsAlive());

            return foundCreature != null ? foundCreature : bounds[0];
        }

        public GameObject GetGameObjectBySpawnId(ulong spawnId)
        {
            var bounds = GetGameObjectBySpawnIdStore().LookupByKey(spawnId);
            if (bounds.Empty())
                return null;

            var foundGameObject = bounds.Find(gameobject => gameobject.IsSpawned());

            return foundGameObject != null ? foundGameObject : bounds[0];
        }

        public AreaTrigger GetAreaTriggerBySpawnId(ulong spawnId)
        {
            var bounds = GetAreaTriggerBySpawnIdStore().LookupByKey(spawnId);
            if (bounds.Empty())
                return null;

            return bounds.FirstOrDefault();
        }

        public WorldObject GetWorldObjectBySpawnId(SpawnObjectType type, ulong spawnId)
        {
            switch (type)
            {
                case SpawnObjectType.Creature:
                    return GetCreatureBySpawnId(spawnId);
                case SpawnObjectType.GameObject:
                    return GetGameObjectBySpawnId(spawnId);
                case SpawnObjectType.AreaTrigger:
                    return GetAreaTriggerBySpawnId(spawnId);
                default:
                    return null;
            }
        }

        public void Visit(Cell cell, IGridNotifier visitor)
        {
            uint x = cell.GetGridX();
            uint y = cell.GetGridY();
            uint cell_x = cell.GetCellX();
            uint cell_y = cell.GetCellY();

            if (!cell.NoCreate() || IsGridLoaded(x, y))
            {
                EnsureGridLoaded(cell);
                GetGrid(x, y).VisitGrid(cell_x, cell_y, visitor);
            }
        }

        public TempSummon SummonCreature(uint entry, Position pos, SummonPropertiesRecord properties = null, uint duration = 0, WorldObject summoner = null, uint spellId = 0, uint vehId = 0, ObjectGuid privateObjectOwner = default, SmoothPhasingInfo smoothPhasingInfo = null)
        {
            var mask = UnitTypeMask.Summon;
            if (properties != null)
            {
                switch (properties.Control)
                {
                    case SummonCategory.Pet:
                        mask = UnitTypeMask.Guardian;
                        break;
                    case SummonCategory.Puppet:
                        mask = UnitTypeMask.Puppet;
                        break;
                    case SummonCategory.Vehicle:
                        mask = UnitTypeMask.Minion;
                        break;
                    case SummonCategory.Wild:
                    case SummonCategory.Ally:
                    case SummonCategory.Unk:
                    {
                        switch (properties.Title)
                        {
                            case SummonTitle.Minion:
                            case SummonTitle.Guardian:
                            case SummonTitle.Runeblade:
                                mask = UnitTypeMask.Guardian;
                                break;
                            case SummonTitle.Totem:
                            case SummonTitle.LightWell:
                                mask = UnitTypeMask.Totem;
                                break;
                            case SummonTitle.Vehicle:
                            case SummonTitle.Mount:
                                mask = UnitTypeMask.Summon;
                                break;
                            case SummonTitle.Companion:
                                mask = UnitTypeMask.Minion;
                                break;
                            default:
                                if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup)) // Mirror Image, Summon Gargoyle
                                    mask = UnitTypeMask.Guardian;
                                break;
                        }
                        break;
                    }
                    default:
                        return null;
                }
            }

            Unit summonerUnit = summoner != null ? summoner.ToUnit() : null;

            TempSummon summon;
            switch (mask)
            {
                case UnitTypeMask.Summon:
                    summon = new TempSummon(properties, summonerUnit, false);
                    break;
                case UnitTypeMask.Guardian:
                    summon = new Guardian(properties, summonerUnit, false);
                    break;
                case UnitTypeMask.Puppet:
                    summon = new Puppet(properties, summonerUnit);
                    break;
                case UnitTypeMask.Totem:
                    summon = new Totem(properties, summonerUnit);
                    break;
                case UnitTypeMask.Minion:
                    summon = new Minion(properties, summonerUnit, false);
                    break;
                default:
                    return null;
            }

            if (!summon.Create(GenerateLowGuid(HighGuid.Creature), this, entry, pos, null, vehId, true))
                return null;

            ITransport transport = summoner != null ? summoner.GetTransport() : null;
            if (transport != null)
            {
                pos.GetPosition(out float x, out float y, out float z, out float o);
                transport.CalculatePassengerOffset(ref x, ref y, ref z, ref o);
                summon.m_movementInfo.transport.pos.Relocate(x, y, z, o);

                // This object must be added to transport before adding to map for the client to properly display it
                transport.AddPassenger(summon);
            }

            // Set the summon to the summoner's phase
            if (summoner != null && !(properties != null && properties.GetFlags().HasFlag(SummonPropertiesFlags.IgnoreSummonerPhase)))
                PhasingHandler.InheritPhaseShift(summon, summoner);

            summon.SetCreatedBySpell(spellId);
            summon.SetHomePosition(pos);
            summon.InitStats(duration);
            summon.SetPrivateObjectOwner(privateObjectOwner);

            if (smoothPhasingInfo != null)
            {
                if (summoner != null && smoothPhasingInfo.ReplaceObject.HasValue)
                {
                    WorldObject replacedObject = Global.ObjAccessor.GetWorldObject(summoner, smoothPhasingInfo.ReplaceObject.Value);
                    if (replacedObject != null)
                    {
                        SmoothPhasingInfo originalSmoothPhasingInfo = smoothPhasingInfo;
                        originalSmoothPhasingInfo.ReplaceObject = summon.GetGUID();
                        replacedObject.GetOrCreateSmoothPhasing().SetViewerDependentInfo(privateObjectOwner, originalSmoothPhasingInfo);

                        summon.SetDemonCreatorGUID(privateObjectOwner);
                    }
                }

                summon.GetOrCreateSmoothPhasing().SetSingleInfo(smoothPhasingInfo);
            }

            if (!AddToMap(summon.ToCreature()))
            {
                // Returning false will cause the object to be deleted - remove from transport
                if (transport != null)
                    transport.RemovePassenger(summon);

                summon.Dispose();
                return null;
            }

            summon.InitSummon();

            // call MoveInLineOfSight for nearby creatures
            AIRelocationNotifier notifier = new(summon, GridType.All);
            Cell.VisitGrid(summon, notifier, GetVisibilityRange());

            return summon;
        }

        public ulong GenerateLowGuid(HighGuid high)
        {
            return GetGuidSequenceGenerator(high).Generate();
        }

        public ulong GetMaxLowGuid(HighGuid high)
        {
            return GetGuidSequenceGenerator(high).GetNextAfterMaxUsed();
        }

        ObjectGuidGenerator GetGuidSequenceGenerator(HighGuid high)
        {
            if (!_guidGenerators.ContainsKey(high))
                _guidGenerators[high] = new ObjectGuidGenerator(high);

            return _guidGenerators[high];
        }

        public void AddUpdateObject(WorldObject obj)
        {
            lock (_updateObjects)
                if (obj != null)
                    _updateObjects.Add(obj);
        }

        public void RemoveUpdateObject(WorldObject obj)
        {
            lock (_updateObjects)
                _updateObjects.Remove(obj);
        }

        public static implicit operator bool(Map map)
        {
            return map != null;
        }

        public MultiPersonalPhaseTracker GetMultiPersonalPhaseTracker() { return _multiPersonalPhaseTracker; }

        public SpawnedPoolData GetPoolData() { return _poolData; }

        #region Scripts

        // Put scripts in the execution queue
        public void ScriptsStart(ScriptsType scriptsType, uint id, WorldObject source, WorldObject target)
        {
            var scripts = Global.ObjectMgr.GetScriptsMapByType(scriptsType);

            // Find the script map
            MultiMap<uint, ScriptInfo> list = scripts.LookupByKey(id);
            if (list == null)
                return;

            // prepare static data
            ObjectGuid sourceGUID = source != null ? source.GetGUID() : ObjectGuid.Empty; //some script commands doesn't have source
            ObjectGuid targetGUID = target != null ? target.GetGUID() : ObjectGuid.Empty;
            ObjectGuid ownerGUID = (source != null && source.IsTypeMask(TypeMask.Item)) ? ((Item)source).GetOwnerGUID() : ObjectGuid.Empty;

            // Schedule script execution for all scripts in the script map
            bool immedScript = false;
            foreach (var script in list.KeyValueList)
            {
                ScriptAction sa;
                sa.sourceGUID = sourceGUID;
                sa.targetGUID = targetGUID;
                sa.ownerGUID = ownerGUID;

                sa.script = script.Value;
                m_scriptSchedule.Add(GameTime.GetGameTime() + script.Key, sa);
                if (script.Key == 0)
                    immedScript = true;

                Global.MapMgr.IncreaseScheduledScriptsCount();
            }
            // If one of the effects should be immediate, launch the script execution
            if (immedScript)
            {
                lock (i_scriptLock)
                    ScriptsProcess();
            }
        }

        public void ScriptCommandStart(ScriptInfo script, uint delay, WorldObject source, WorldObject target)
        {
            // NOTE: script record _must_ exist until command executed

            // prepare static data
            ObjectGuid sourceGUID = source != null ? source.GetGUID() : ObjectGuid.Empty;
            ObjectGuid targetGUID = target != null ? target.GetGUID() : ObjectGuid.Empty;
            ObjectGuid ownerGUID = (source != null && source.IsTypeMask(TypeMask.Item)) ? ((Item)source).GetOwnerGUID() : ObjectGuid.Empty;

            var sa = new ScriptAction();
            sa.sourceGUID = sourceGUID;
            sa.targetGUID = targetGUID;
            sa.ownerGUID = ownerGUID;

            sa.script = script;
            m_scriptSchedule.Add(GameTime.GetGameTime() + delay, sa);

            Global.MapMgr.IncreaseScheduledScriptsCount();

            // If effects should be immediate, launch the script execution
            if (delay == 0)
            {
                lock (i_scriptLock)
                    ScriptsProcess();
            }
        }

        // Helpers for ScriptProcess method.
        private Player _GetScriptPlayerSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo)
        {
            Player player = null;
            if (source == null && target == null)
                Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
            else
            {
                // Check target first, then source.
                if (target != null)
                    player = target.ToPlayer();
                if (player == null && source != null)
                    player = source.ToPlayer();

                if (player == null)
                    Log.outError(LogFilter.Scripts, "{0} neither source nor target object is player (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
                        scriptInfo.GetDebugInfo(), source ? source.GetTypeId() : 0, source ? source.GetEntry() : 0, source ? source.GetGUID().ToString() : "",
                        target ? target.GetTypeId() : 0, target ? target.GetEntry() : 0, target ? target.GetGUID().ToString() : "");
            }
            return player;
        }

        private Creature _GetScriptCreatureSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse = false)
        {
            Creature creature = null;
            if (source == null && target == null)
                Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
            else
            {
                if (bReverse)
                {
                    // Check target first, then source.
                    if (target != null)
                        creature = target.ToCreature();
                    if (creature == null && source != null)
                        creature = source.ToCreature();
                }
                else
                {
                    // Check source first, then target.
                    if (source != null)
                        creature = source.ToCreature();
                    if (creature == null && target != null)
                        creature = target.ToCreature();
                }

                if (creature == null)
                    Log.outError(LogFilter.Scripts, "{0} neither source nor target are creatures (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
                        scriptInfo.GetDebugInfo(), source ? source.GetTypeId() : 0, source ? source.GetEntry() : 0, source ? source.GetGUID().ToString() : "",
                        target ? target.GetTypeId() : 0, target ? target.GetEntry() : 0, target ? target.GetGUID().ToString() : "");
            }
            return creature;
        }

        private GameObject _GetScriptGameObjectSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse)
        {
            GameObject gameobject = null;
            if (source == null && target == null)
                Log.outError(LogFilter.MapsScript, $"{scriptInfo.GetDebugInfo()} source and target objects are NULL.");
            else
            {
                if (bReverse)
                {
                    // Check target first, then source.
                    if (target != null)
                        gameobject = target.ToGameObject();
                    if (gameobject == null && source != null)
                        gameobject = source.ToGameObject();
                }
                else
                {
                    // Check source first, then target.
                    if (source != null)
                        gameobject = source.ToGameObject();
                    if (gameobject == null && target != null)
                        gameobject = target.ToGameObject();
                }

                if (gameobject == null)
                    Log.outError(LogFilter.MapsScript, $"{scriptInfo.GetDebugInfo()} neither source nor target are gameobjects " +
                        $"(source: TypeId: {(source != null ? source.GetTypeId() : 0)}, Entry: {(source != null ? source.GetEntry() : 0)}, {(source != null ? source.GetGUID() : ObjectGuid.Empty)}; " +
                        $"target: TypeId: {(target != null ? target.GetTypeId() : 0)}, Entry: {(target != null ? target.GetEntry() : 0)}, {(target != null ? target.GetGUID() : ObjectGuid.Empty)}), skipping.");
            }
            return gameobject;
        }

        private Unit _GetScriptUnit(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            Unit unit = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(),
                    isSource ? "source" : "target");
            else if (!obj.IsTypeMask(TypeMask.Unit))
                Log.outError(LogFilter.Scripts,
                    "{0} {1} object is not unit (TypeId: {2}, Entry: {3}, GUID: {4}), skipping.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            else
            {
                unit = obj.ToUnit();
                if (unit == null)
                    Log.outError(LogFilter.Scripts, "{0} {1} object could not be casted to unit.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
            }
            return unit;
        }

        private Player _GetScriptPlayer(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            Player player = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(),
                    isSource ? "source" : "target");
            else
            {
                player = obj.ToPlayer();
                if (player == null)
                    Log.outError(LogFilter.Scripts, "{0} {1} object is not a player (TypeId: {2}, Entry: {3}, GUID: {4}).",
                        scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            }
            return player;
        }

        private Creature _GetScriptCreature(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            Creature creature = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
            else
            {
                creature = obj.ToCreature();
                if (creature == null)
                    Log.outError(LogFilter.Scripts,
                        "{0} {1} object is not a creature (TypeId: {2}, Entry: {3}, GUID: {4}).", scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            }
            return creature;
        }

        private WorldObject _GetScriptWorldObject(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            WorldObject pWorldObject = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
            else
            {
                pWorldObject = obj;
                if (pWorldObject == null)
                    Log.outError(LogFilter.Scripts,
                        "{0} {1} object is not a world object (TypeId: {2}, Entry: {3}, GUID: {4}).", scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            }
            return pWorldObject;
        }

        void _ScriptProcessDoor(WorldObject source, WorldObject target, ScriptInfo scriptInfo)
        {
            bool bOpen = false;
            ulong guid = scriptInfo.ToggleDoor.GOGuid;
            int nTimeToToggle = Math.Max(15, (int)scriptInfo.ToggleDoor.ResetDelay);
            switch (scriptInfo.command)
            {
                case ScriptCommands.OpenDoor:
                    bOpen = true;
                    break;
                case ScriptCommands.CloseDoor:
                    break;
                default:
                    Log.outError(LogFilter.Scripts, "{0} unknown command for _ScriptProcessDoor.", scriptInfo.GetDebugInfo());
                    return;
            }
            if (guid == 0)
                Log.outError(LogFilter.Scripts, "{0} door guid is not specified.", scriptInfo.GetDebugInfo());
            else if (source == null)
                Log.outError(LogFilter.Scripts, "{0} source object is NULL.", scriptInfo.GetDebugInfo());
            else if (!source.IsTypeMask(TypeMask.Unit))
                Log.outError(LogFilter.Scripts,
                    "{0} source object is not unit (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", scriptInfo.GetDebugInfo(), source.GetTypeId(), source.GetEntry(), source.GetGUID().ToString());
            else
            {
                if (source == null)
                    Log.outError(LogFilter.Scripts,
                        "{0} source object could not be casted to world object (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", scriptInfo.GetDebugInfo(), source.GetTypeId(), source.GetEntry(), source.GetGUID().ToString());
                else
                {
                    GameObject pDoor = _FindGameObject(source, guid);
                    if (pDoor == null)
                        Log.outError(LogFilter.Scripts, "{0} gameobject was not found (guid: {1}).", scriptInfo.GetDebugInfo(), guid);
                    else if (pDoor.GetGoType() != GameObjectTypes.Door)
                        Log.outError(LogFilter.Scripts, "{0} gameobject is not a door (GoType: {1}, Entry: {2}, GUID: {3}).", scriptInfo.GetDebugInfo(), pDoor.GetGoType(), pDoor.GetEntry(), pDoor.GetGUID().ToString());
                    else if (bOpen == (pDoor.GetGoState() == GameObjectState.Ready))
                    {
                        pDoor.UseDoorOrButton((uint)nTimeToToggle);

                        if (target != null && target.IsTypeMask(TypeMask.GameObject))
                        {
                            GameObject goTarget = target.ToGameObject();
                            if (goTarget != null && goTarget.GetGoType() == GameObjectTypes.Button)
                                goTarget.UseDoorOrButton((uint)nTimeToToggle);
                        }
                    }
                }
            }
        }

        private GameObject _FindGameObject(WorldObject searchObject, ulong guid)
        {
            var bounds = searchObject.GetMap().GetGameObjectBySpawnIdStore().LookupByKey(guid);
            if (bounds.Empty())
                return null;

            return bounds[0];
        }

        // Process queued scripts
        void ScriptsProcess()
        {
            if (m_scriptSchedule.Empty())
                return;

            // Process overdue queued scripts
            var iter = m_scriptSchedule.FirstOrDefault();

            while (!m_scriptSchedule.Empty())
            {
                if (iter.Key > GameTime.GetGameTime())
                    break; // we are a sorted dictionary, once we hit this value we can break all other are going to be greater.

                if (iter.Value == default && iter.Key == default)
                    break; // we have a default on get first or defalt. stack is empty

                foreach (var step in iter.Value)
                {

                    WorldObject source = null;
                    if (!step.sourceGUID.IsEmpty())
                    {
                        switch (step.sourceGUID.GetHigh())
                        {
                            case HighGuid.Item: // as well as HIGHGUID_CONTAINER
                                Player player = GetPlayer(step.ownerGUID);
                                if (player != null)
                                    source = player.GetItemByGuid(step.sourceGUID);
                                break;
                            case HighGuid.Creature:
                            case HighGuid.Vehicle:
                                source = GetCreature(step.sourceGUID);
                                break;
                            case HighGuid.Pet:
                                source = GetPet(step.sourceGUID);
                                break;
                            case HighGuid.Player:
                                source = GetPlayer(step.sourceGUID);
                                break;
                            case HighGuid.GameObject:
                            case HighGuid.Transport:
                                source = GetGameObject(step.sourceGUID);
                                break;
                            case HighGuid.Corpse:
                                source = GetCorpse(step.sourceGUID);
                                break;
                            default:
                                Log.outError(LogFilter.Scripts, "{0} source with unsupported high guid (GUID: {1}, high guid: {2}).",
                                    step.script.GetDebugInfo(), step.sourceGUID, step.sourceGUID.ToString());
                                break;
                        }
                    }

                    WorldObject target = null;
                    if (!step.targetGUID.IsEmpty())
                    {
                        switch (step.targetGUID.GetHigh())
                        {
                            case HighGuid.Creature:
                            case HighGuid.Vehicle:
                                target = GetCreature(step.targetGUID);
                                break;
                            case HighGuid.Pet:
                                target = GetPet(step.targetGUID);
                                break;
                            case HighGuid.Player:
                                target = GetPlayer(step.targetGUID);
                                break;
                            case HighGuid.GameObject:
                            case HighGuid.Transport:
                                target = GetGameObject(step.targetGUID);
                                break;
                            case HighGuid.Corpse:
                                target = GetCorpse(step.targetGUID);
                                break;
                            default:
                                Log.outError(LogFilter.Scripts, "{0} target with unsupported high guid {1}.", step.script.GetDebugInfo(), step.targetGUID.ToString());
                                break;
                        }
                    }

                    switch (step.script.command)
                    {
                        case ScriptCommands.Talk:
                            {
                                if (step.script.Talk.ChatType > ChatMsg.Whisper && step.script.Talk.ChatType != ChatMsg.RaidBossWhisper)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} invalid chat type ({1}) specified, skipping.",
                                        step.script.GetDebugInfo(), step.script.Talk.ChatType);
                                    break;
                                }

                                if (step.script.Talk.Flags.HasAnyFlag(eScriptFlags.TalkUsePlayer))
                                    source = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                                else
                                    source = _GetScriptCreatureSourceOrTarget(source, target, step.script);

                                if (source)
                                {
                                    Unit sourceUnit = source.ToUnit();
                                    if (!sourceUnit)
                                    {
                                        Log.outError(LogFilter.Scripts, "{0} source object ({1}) is not an unit, skipping.", step.script.GetDebugInfo(), source.GetGUID().ToString());
                                        break;
                                    }

                                    switch (step.script.Talk.ChatType)
                                    {
                                        case ChatMsg.Say:
                                            sourceUnit.Say((uint)step.script.Talk.TextID, target);
                                            break;
                                        case ChatMsg.Yell:
                                            sourceUnit.Yell((uint)step.script.Talk.TextID, target);
                                            break;
                                        case ChatMsg.Emote:
                                        case ChatMsg.RaidBossEmote:
                                            sourceUnit.TextEmote((uint)step.script.Talk.TextID, target, step.script.Talk.ChatType == ChatMsg.RaidBossEmote);
                                            break;
                                        case ChatMsg.Whisper:
                                        case ChatMsg.RaidBossWhisper:
                                            {
                                                Player receiver = target ? target.ToPlayer() : null;
                                                if (!receiver)
                                                    Log.outError(LogFilter.Scripts, "{0} attempt to whisper to non-player unit, skipping.", step.script.GetDebugInfo());
                                                else
                                                    sourceUnit.Whisper((uint)step.script.Talk.TextID, receiver, step.script.Talk.ChatType == ChatMsg.RaidBossWhisper);
                                                break;
                                            }
                                        default:
                                            break; // must be already checked at load
                                    }
                                }
                                break;
                            }
                        case ScriptCommands.Emote:
                            {
                                // Source or target must be Creature.
                                Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                                if (cSource)
                                {
                                    if (step.script.Emote.Flags.HasAnyFlag(eScriptFlags.EmoteUseState))
                                        cSource.SetEmoteState((Emote)step.script.Emote.EmoteID);
                                    else
                                        cSource.HandleEmoteCommand((Emote)step.script.Emote.EmoteID);
                                }
                                break;
                            }
                        case ScriptCommands.MoveTo:
                            {
                                // Source or target must be Creature.
                                Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                                if (cSource)
                                {
                                    Unit unit = cSource.ToUnit();
                                    if (step.script.MoveTo.TravelTime != 0)
                                    {
                                        float speed =
                                            unit.GetDistance(step.script.MoveTo.DestX, step.script.MoveTo.DestY,
                                                step.script.MoveTo.DestZ) / (step.script.MoveTo.TravelTime * 0.001f);
                                        unit.MonsterMoveWithSpeed(step.script.MoveTo.DestX, step.script.MoveTo.DestY,
                                            step.script.MoveTo.DestZ, speed);
                                    }
                                    else
                                        unit.NearTeleportTo(step.script.MoveTo.DestX, step.script.MoveTo.DestY,
                                            step.script.MoveTo.DestZ, unit.GetOrientation());
                                }
                                break;
                            }
                        case ScriptCommands.TeleportTo:
                            {
                                if (step.script.TeleportTo.Flags.HasAnyFlag(eScriptFlags.TeleportUseCreature))
                                {
                                    // Source or target must be Creature.
                                    Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                                    if (cSource)
                                        cSource.NearTeleportTo(step.script.TeleportTo.DestX, step.script.TeleportTo.DestY,
                                            step.script.TeleportTo.DestZ, step.script.TeleportTo.Orientation);
                                }
                                else
                                {
                                    // Source or target must be Player.
                                    Player player = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                                    if (player)
                                        player.TeleportTo(step.script.TeleportTo.MapID, step.script.TeleportTo.DestX,
                                            step.script.TeleportTo.DestY, step.script.TeleportTo.DestZ, step.script.TeleportTo.Orientation);
                                }
                                break;
                            }
                        case ScriptCommands.QuestExplored:
                            {
                                if (!source)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} source object is NULL.", step.script.GetDebugInfo());
                                    break;
                                }
                                if (!target)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} target object is NULL.", step.script.GetDebugInfo());
                                    break;
                                }

                                // when script called for item spell casting then target == (unit or GO) and source is player
                                WorldObject worldObject;
                                Player player = target.ToPlayer();
                                if (player != null)
                                {
                                    if (!source.IsTypeId(TypeId.Unit) && !source.IsTypeId(TypeId.GameObject) && !source.IsTypeId(TypeId.Player))
                                    {
                                        Log.outError(LogFilter.Scripts, "{0} source is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
                                            step.script.GetDebugInfo(), source.GetTypeId(), source.GetEntry(), source.GetGUID().ToString());
                                        break;
                                    }
                                    worldObject = source;
                                }
                                else
                                {
                                    player = source.ToPlayer();
                                    if (player != null)
                                    {
                                        if (!target.IsTypeId(TypeId.Unit) && !target.IsTypeId(TypeId.GameObject) && !target.IsTypeId(TypeId.Player))
                                        {
                                            Log.outError(LogFilter.Scripts,
                                                "{0} target is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", step.script.GetDebugInfo(), target.GetTypeId(), target.GetEntry(), target.GetGUID().ToString());
                                            break;
                                        }
                                        worldObject = target;
                                    }
                                    else
                                    {
                                        Log.outError(LogFilter.Scripts, "{0} neither source nor target is player (Entry: {0}, GUID: {1}; target: Entry: {2}, GUID: {3}), skipping.",
                                            step.script.GetDebugInfo(), source.GetEntry(), source.GetGUID().ToString(), target.GetEntry(), target.GetGUID().ToString());
                                        break;
                                    }
                                }

                                // quest id and flags checked at script loading
                                if ((!worldObject.IsTypeId(TypeId.Unit) || worldObject.ToUnit().IsAlive()) &&
                                    (step.script.QuestExplored.Distance == 0 ||
                                     worldObject.IsWithinDistInMap(player, step.script.QuestExplored.Distance)))
                                    player.AreaExploredOrEventHappens(step.script.QuestExplored.QuestID);
                                else
                                    player.FailQuest(step.script.QuestExplored.QuestID);

                                break;
                            }

                        case ScriptCommands.KillCredit:
                            {
                                // Source or target must be Player.
                                Player player = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                                if (player)
                                {
                                    if (step.script.KillCredit.Flags.HasAnyFlag(eScriptFlags.KillcreditRewardGroup))
                                        player.RewardPlayerAndGroupAtEvent(step.script.KillCredit.CreatureEntry, player);
                                    else
                                        player.KilledMonsterCredit(step.script.KillCredit.CreatureEntry, ObjectGuid.Empty);
                                }
                                break;
                            }
                        case ScriptCommands.RespawnGameobject:
                            {
                                if (step.script.RespawnGameObject.GOGuid == 0)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} gameobject guid (datalong) is not specified.", step.script.GetDebugInfo());
                                    break;
                                }

                                // Source or target must be WorldObject.
                                WorldObject pSummoner = _GetScriptWorldObject(source, true, step.script);
                                if (pSummoner)
                                {
                                    GameObject pGO = _FindGameObject(pSummoner, step.script.RespawnGameObject.GOGuid);
                                    if (pGO == null)
                                    {
                                        Log.outError(LogFilter.Scripts, "{0} gameobject was not found (guid: {1}).", step.script.GetDebugInfo(), step.script.RespawnGameObject.GOGuid);
                                        break;
                                    }

                                    if (pGO.GetGoType() == GameObjectTypes.FishingNode ||
                                        pGO.GetGoType() == GameObjectTypes.Door || pGO.GetGoType() == GameObjectTypes.Button ||
                                        pGO.GetGoType() == GameObjectTypes.Trap)
                                    {
                                        Log.outError(LogFilter.Scripts,
                                            "{0} can not be used with gameobject of type {1} (guid: {2}).", step.script.GetDebugInfo(), pGO.GetGoType(), step.script.RespawnGameObject.GOGuid);
                                        break;
                                    }

                                    // Check that GO is not spawned
                                    if (!pGO.IsSpawned())
                                    {
                                        int nTimeToDespawn = Math.Max(5, (int)step.script.RespawnGameObject.DespawnDelay);
                                        pGO.SetLootState(LootState.Ready);
                                        pGO.SetRespawnTime(nTimeToDespawn);

                                        pGO.GetMap().AddToMap(pGO);
                                    }
                                }
                                break;
                            }
                        case ScriptCommands.TempSummonCreature:
                            {
                                // Source must be WorldObject.
                                WorldObject pSummoner = _GetScriptWorldObject(source, true, step.script);
                                if (pSummoner)
                                {
                                    if (step.script.TempSummonCreature.CreatureEntry == 0)
                                        Log.outError(LogFilter.Scripts, "{0} creature entry (datalong) is not specified.", step.script.GetDebugInfo());
                                    else
                                    {
                                        float x = step.script.TempSummonCreature.PosX;
                                        float y = step.script.TempSummonCreature.PosY;
                                        float z = step.script.TempSummonCreature.PosZ;
                                        float o = step.script.TempSummonCreature.Orientation;

                                        if (pSummoner.SummonCreature(step.script.TempSummonCreature.CreatureEntry, x, y, z, o, TempSummonType.TimedOrDeadDespawn, TimeSpan.FromMilliseconds(step.script.TempSummonCreature.DespawnDelay)) == null)
                                            Log.outError(LogFilter.Scripts, "{0} creature was not spawned (entry: {1}).", step.script.GetDebugInfo(), step.script.TempSummonCreature.CreatureEntry);
                                    }
                                }
                                break;
                            }

                        case ScriptCommands.OpenDoor:
                        case ScriptCommands.CloseDoor:
                            _ScriptProcessDoor(source, target, step.script);
                            break;
                        case ScriptCommands.ActivateObject:
                            {
                                // Source must be Unit.
                                Unit unit = _GetScriptUnit(source, true, step.script);
                                if (unit)
                                {
                                    // Target must be GameObject.
                                    if (target == null)
                                    {
                                        Log.outError(LogFilter.Scripts, "{0} target object is NULL.", step.script.GetDebugInfo());
                                        break;
                                    }

                                    if (!target.IsTypeId(TypeId.GameObject))
                                    {
                                        Log.outError(LogFilter.Scripts,
                                            "{0} target object is not gameobject (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", step.script.GetDebugInfo(), target.GetTypeId(), target.GetEntry(),
                                            target.GetGUID().ToString());
                                        break;
                                    }
                                    GameObject pGO = target.ToGameObject();
                                    if (pGO)
                                        pGO.Use(unit);
                                }
                                break;
                            }
                        case ScriptCommands.RemoveAura:
                            {
                                // Source (datalong2 != 0) or target (datalong2 == 0) must be Unit.
                                bool bReverse = step.script.RemoveAura.Flags.HasAnyFlag(eScriptFlags.RemoveauraReverse);
                                Unit unit = _GetScriptUnit(bReverse ? source : target, bReverse, step.script);
                                if (unit)
                                    unit.RemoveAura(step.script.RemoveAura.SpellID);
                                break;
                            }
                        case ScriptCommands.CastSpell:
                            {
                                if (source == null && target == null)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", step.script.GetDebugInfo());
                                    break;
                                }

                                WorldObject uSource = null;
                                WorldObject uTarget = null;
                                // source/target cast spell at target/source (script.datalong2: 0: s.t 1: s.s 2: t.t 3: t.s
                                switch (step.script.CastSpell.Flags)
                                {
                                    case eScriptFlags.CastspellSourceToTarget: // source . target
                                        uSource = source;
                                        uTarget = target;
                                        break;
                                    case eScriptFlags.CastspellSourceToSource: // source . source
                                        uSource = source;
                                        uTarget = uSource;
                                        break;
                                    case eScriptFlags.CastspellTargetToTarget: // target . target
                                        uSource = target;
                                        uTarget = uSource;
                                        break;
                                    case eScriptFlags.CastspellTargetToSource: // target . source
                                        uSource = target;
                                        uTarget = source;
                                        break;
                                    case eScriptFlags.CastspellSearchCreature: // source . creature with entry
                                        uSource = source;
                                        uTarget = uSource?.FindNearestCreature((uint)Math.Abs(step.script.CastSpell.CreatureEntry), step.script.CastSpell.SearchRadius);
                                        break;
                                }

                                if (uSource == null)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} no source worldobject found for spell {1}", step.script.GetDebugInfo(), step.script.CastSpell.SpellID);
                                    break;
                                }

                                if (uTarget == null)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} no target worldobject found for spell {1}", step.script.GetDebugInfo(), step.script.CastSpell.SpellID);
                                    break;
                                }

                                bool triggered = ((int)step.script.CastSpell.Flags != 4)
                                    ? step.script.CastSpell.CreatureEntry.HasAnyFlag((int)eScriptFlags.CastspellTriggered)
                                    : step.script.CastSpell.CreatureEntry < 0;
                                uSource.CastSpell(uTarget, step.script.CastSpell.SpellID, triggered);
                                break;
                            }

                        case ScriptCommands.PlaySound:
                            // Source must be WorldObject.
                            WorldObject obj = _GetScriptWorldObject(source, true, step.script);
                            if (obj)
                            {
                                // PlaySound.Flags bitmask: 0/1=anyone/target
                                Player player2 = null;
                                if (step.script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundTargetPlayer))
                                {
                                    // Target must be Player.
                                    player2 = _GetScriptPlayer(target, false, step.script);
                                    if (target == null)
                                        break;
                                }

                                // PlaySound.Flags bitmask: 0/2=without/with distance dependent
                                if (step.script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundDistanceSound))
                                    obj.PlayDistanceSound(step.script.PlaySound.SoundID, player2);
                                else
                                    obj.PlayDirectSound(step.script.PlaySound.SoundID, player2);
                            }
                            break;

                        case ScriptCommands.CreateItem:
                            // Target or source must be Player.
                            Player pReceiver = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                            if (pReceiver)
                            {
                                var dest = new List<ItemPosCount>();
                                InventoryResult msg = pReceiver.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, step.script.CreateItem.ItemEntry, step.script.CreateItem.Amount);
                                if (msg == InventoryResult.Ok)
                                {
                                    Item item = pReceiver.StoreNewItem(dest, step.script.CreateItem.ItemEntry, true);
                                    if (item != null)
                                        pReceiver.SendNewItem(item, step.script.CreateItem.Amount, false, true);
                                }
                                else
                                    pReceiver.SendEquipError(msg, null, null, step.script.CreateItem.ItemEntry);
                            }
                            break;

                        case ScriptCommands.DespawnSelf:
                            {
                                // First try with target or source creature, then with target or source gameobject
                                Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script, true);
                                if (cSource != null)
                                    cSource.DespawnOrUnsummon(TimeSpan.FromMilliseconds(step.script.DespawnSelf.DespawnDelay));
                                else
                                {
                                    GameObject goSource = _GetScriptGameObjectSourceOrTarget(source, target, step.script, true);
                                    if (goSource != null)
                                        goSource.DespawnOrUnsummon(TimeSpan.FromMilliseconds(step.script.DespawnSelf.DespawnDelay));
                                }
                                break;
                            }
                        case ScriptCommands.LoadPath:
                            {
                                // Source must be Unit.
                                Unit unit = _GetScriptUnit(source, true, step.script);
                                if (unit)
                                {
                                    if (Global.WaypointMgr.GetPath(step.script.LoadPath.PathID) == null)
                                        Log.outError(LogFilter.Scripts, "{0} source object has an invalid path ({1}), skipping.", step.script.GetDebugInfo(), step.script.LoadPath.PathID);
                                    else
                                        unit.GetMotionMaster().MovePath(step.script.LoadPath.PathID, step.script.LoadPath.IsRepeatable != 0);
                                }
                                break;
                            }
                        case ScriptCommands.CallscriptToUnit:
                            {
                                if (step.script.CallScript.CreatureEntry == 0)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} creature entry is not specified, skipping.", step.script.GetDebugInfo());
                                    break;
                                }
                                if (step.script.CallScript.ScriptID == 0)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} script id is not specified, skipping.", step.script.GetDebugInfo());
                                    break;
                                }

                                Creature cTarget = null;
                                var creatureBounds = _creatureBySpawnIdStore.LookupByKey(step.script.CallScript.CreatureEntry);
                                if (!creatureBounds.Empty())
                                {
                                    // Prefer alive (last respawned) creature
                                    var foundCreature = creatureBounds.Find(creature => creature.IsAlive());

                                    cTarget = foundCreature ?? creatureBounds[0];
                                }

                                if (cTarget == null)
                                {
                                    Log.outError(LogFilter.Scripts, "{0} target was not found (entry: {1})", step.script.GetDebugInfo(), step.script.CallScript.CreatureEntry);
                                    break;
                                }

                                // Insert script into schedule but do not start it
                                ScriptsStart((ScriptsType)step.script.CallScript.ScriptType, step.script.CallScript.ScriptID, cTarget, null);
                                break;
                            }

                        case ScriptCommands.Kill:
                            {
                                // Source or target must be Creature.
                                Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                                if (cSource)
                                {
                                    if (cSource.IsDead())
                                        Log.outError(LogFilter.Scripts, "{0} creature is already dead (Entry: {1}, GUID: {2})", step.script.GetDebugInfo(), cSource.GetEntry(), cSource.GetGUID().ToString());
                                    else
                                    {
                                        cSource.SetDeathState(DeathState.JustDied);
                                        if (step.script.Kill.RemoveCorpse == 1)
                                            cSource.RemoveCorpse();
                                    }
                                }
                                break;
                            }
                        case ScriptCommands.Orientation:
                            {
                                // Source must be Unit.
                                Unit sourceUnit = _GetScriptUnit(source, true, step.script);
                                if (sourceUnit)
                                {
                                    if (step.script.Orientation.Flags.HasAnyFlag(eScriptFlags.OrientationFaceTarget))
                                    {
                                        // Target must be Unit.
                                        Unit targetUnit = _GetScriptUnit(target, false, step.script);
                                        if (targetUnit == null)
                                            break;

                                        sourceUnit.SetFacingToObject(targetUnit);
                                    }
                                    else
                                        sourceUnit.SetFacingTo(step.script.Orientation._Orientation);
                                }
                                break;
                            }
                        case ScriptCommands.Equip:
                            {
                                // Source must be Creature.
                                Creature cSource = _GetScriptCreature(source, target, step.script);
                                if (cSource)
                                    cSource.LoadEquipment((int)step.script.Equip.EquipmentID);
                                break;
                            }
                        case ScriptCommands.Model:
                            {
                                // Source must be Creature.
                                Creature cSource = _GetScriptCreature(source, target, step.script);
                                if (cSource)
                                    cSource.SetDisplayId(step.script.Model.ModelID);
                                break;
                            }
                        case ScriptCommands.CloseGossip:
                            {
                                // Source must be Player.
                                Player player = _GetScriptPlayer(source, true, step.script);
                                if (player != null)
                                    player.PlayerTalkClass.SendCloseGossip();
                                break;
                            }
                        case ScriptCommands.Playmovie:
                            {
                                // Source must be Player.
                                Player player = _GetScriptPlayer(source, true, step.script);
                                if (player)
                                    player.SendMovieStart(step.script.PlayMovie.MovieID);
                                break;
                            }
                        case ScriptCommands.Movement:
                            {
                                // Source must be Creature.
                                Creature cSource = _GetScriptCreature(source, true, step.script);
                                if (cSource)
                                {
                                    if (!cSource.IsAlive())
                                        return;

                                    cSource.GetMotionMaster().MoveIdle();

                                    switch ((MovementGeneratorType)step.script.Movement.MovementType)
                                    {
                                        case MovementGeneratorType.Random:
                                            cSource.GetMotionMaster().MoveRandom(step.script.Movement.MovementDistance);
                                            break;
                                        case MovementGeneratorType.Waypoint:
                                            cSource.GetMotionMaster().MovePath((uint)step.script.Movement.Path, false);
                                            break;
                                    }
                                }
                                break;
                            }
                        case ScriptCommands.PlayAnimkit:
                            {
                                // Source must be Creature.
                                Creature cSource = _GetScriptCreature(source, true, step.script);
                                if (cSource)
                                    cSource.PlayOneShotAnimKitId((ushort)step.script.PlayAnimKit.AnimKitID);
                                break;
                            }
                        default:
                            Log.outError(LogFilter.Scripts, "Unknown script command {0}.", step.script.GetDebugInfo());
                            break;
                    }

                    Global.MapMgr.DecreaseScheduledScriptCount();
                }

                m_scriptSchedule.Remove(iter.Key);
                iter = m_scriptSchedule.FirstOrDefault();
            }
        }
        #endregion

        #region Fields
        internal object _mapLock = new();

        bool _creatureToMoveLock;
        readonly List<Creature> creaturesToMove = new();

        bool _gameObjectsToMoveLock;
        readonly List<GameObject> _gameObjectsToMove = new();

        bool _dynamicObjectsToMoveLock;
        readonly List<DynamicObject> _dynamicObjectsToMove = new();

        bool _areaTriggersToMoveLock;
        readonly List<AreaTrigger> _areaTriggersToMove = new();
        readonly DynamicMapTree _dynamicTree = new();
        readonly SortedSet<RespawnInfo> _respawnTimes = new(new CompareRespawnInfo());
        readonly Dictionary<ulong, RespawnInfo> _creatureRespawnTimesBySpawnId = new();
        readonly Dictionary<ulong, RespawnInfo> _gameObjectRespawnTimesBySpawnId = new();
        readonly List<uint> _toggledSpawnGroupIds = new();
        uint _respawnCheckTimer;
        readonly Dictionary<uint, uint> _zonePlayerCountMap = new();
        readonly List<Transport> _transports = new();
        readonly Dictionary<uint, Dictionary<uint, Grid>> _grids = new();
        readonly MapRecord i_mapRecord;
        readonly List<WorldObject> i_objectsToRemove = new();
        readonly Dictionary<WorldObject, bool> i_objectsToSwitch = new();
        readonly Difficulty i_spawnMode;
        readonly List<WorldObject> i_worldObjects = new();
        protected List<WorldObject> m_activeNonPlayers = new();
        protected List<Player> m_activePlayers = new();
        readonly TerrainInfo m_terrain;
        readonly SortedDictionary<long, List<ScriptAction>> m_scriptSchedule = new();
        readonly BitSet marked_cells = new(MapConst.TotalCellsPerMap * MapConst.TotalCellsPerMap);
        public Dictionary<ulong, CreatureGroup> CreatureGroupHolder = new();
        internal uint i_InstanceId;
        long i_gridExpiry;
        readonly object i_scriptLock = new();

        public int m_VisibilityNotifyPeriod;
        public float m_VisibleDistance;
        internal uint m_unloadTimer;
        readonly Dictionary<uint, ZoneDynamicInfo> _zoneDynamicInfo = new();
        readonly IntervalTimer _weatherUpdateTimer;
        readonly Dictionary<HighGuid, ObjectGuidGenerator> _guidGenerators = new();
        SpawnedPoolData _poolData;
        readonly Dictionary<ObjectGuid, WorldObject> _objectsStore = new();
        readonly MultiMap<ulong, Creature> _creatureBySpawnIdStore = new();
        readonly MultiMap<ulong, GameObject> _gameobjectBySpawnIdStore = new();
        readonly MultiMap<ulong, AreaTrigger> _areaTriggerBySpawnIdStore = new();
        readonly MultiMap<uint, Corpse> _corpsesByCell = new();
        readonly Dictionary<ObjectGuid, Corpse> _corpsesByPlayer = new();
        readonly List<Corpse> _corpseBones = new();
        readonly List<WorldObject> _updateObjects = new();

        public delegate void FarSpellCallback(Map map);

        readonly Queue<FarSpellCallback> _farSpellCallbacks = new();
        readonly MultiPersonalPhaseTracker _multiPersonalPhaseTracker = new();
        readonly Dictionary<int, int> _worldStateValues = new();
        #endregion
    }

    public class InstanceMap : Map
    {
        public InstanceMap(uint id, long expiry, uint InstanceId, Difficulty spawnMode, int instanceTeam, InstanceLock instanceLock) : base(id, expiry, InstanceId, spawnMode)
        {
            i_instanceLock = instanceLock;

            //lets initialize visibility distance for dungeons
            InitVisibilityDistance();

            // the timer is started by default, and stopped when the first player joins
            // this make sure it gets unloaded if for some reason no player joins
            m_unloadTimer = (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);

            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceAlliance, instanceTeam == TeamId.Alliance ? 1 : 0, false, this);
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceHorde, instanceTeam == TeamId.Horde ? 1 : 0, false, this);

            if (i_instanceLock != null)
            {
                i_instanceLock.SetInUse(true);
                i_instanceExpireEvent = i_instanceLock.GetExpiryTime(); // ignore extension state for reset event (will ask players to accept extended save on expiration)
            }
        }

        ~InstanceMap()
        {
            if (i_instanceLock != null)
                i_instanceLock.SetInUse(false);
        }

        public override void InitVisibilityDistance()
        {
            //init visibility distance for instances
            m_VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceInInstances();
            m_VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodInInstances();
        }

        public override TransferAbortParams CannotEnter(Player player)
        {
            if (player.GetMap() == this)
            {
                Log.outError(LogFilter.Maps, "InstanceMap:CannotEnter - player {0} ({1}) already in map {2}, {3}, {4}!", player.GetName(), player.GetGUID().ToString(), GetId(), GetInstanceId(), GetDifficultyID());
                Cypher.Assert(false);
                return new TransferAbortParams(TransferAbortReason.Error);
            }

            // allow GM's to enter
            if (player.IsGameMaster())
                return base.CannotEnter(player);

            // cannot enter if the instance is full (player cap), GMs don't count
            uint maxPlayers = GetMaxPlayers();
            if (GetPlayersCountExceptGMs() >= maxPlayers)
            {
                Log.outInfo(LogFilter.Maps, "MAP: Instance '{0}' of map '{1}' cannot have more than '{2}' players. Player '{3}' rejected", GetInstanceId(), GetMapName(), maxPlayers, player.GetName());
                return new TransferAbortParams(TransferAbortReason.MaxPlayers);
            }

            // cannot enter while an encounter is in progress (unless this is a relog, in which case it is permitted)
            if (!player.IsLoading() && IsRaid() && GetInstanceScript() != null && GetInstanceScript().IsEncounterInProgress())
                return new TransferAbortParams(TransferAbortReason.ZoneInCombat);

            if (i_instanceLock != null)
            {
                // cannot enter if player is permanent saved to a different instance id
                TransferAbortReason lockError = Global.InstanceLockMgr.CanJoinInstanceLock(player.GetGUID(), new MapDb2Entries(GetEntry(), GetMapDifficulty()), i_instanceLock);
                if (lockError != TransferAbortReason.None)
                    return new TransferAbortParams(lockError);
            }

            return base.CannotEnter(player);
        }

        public override bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            // increase current instances (hourly limit)
            player.AddInstanceEnterTime(GetInstanceId(), GameTime.GetGameTime());

            MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());
            if (entries.MapDifficulty.HasResetSchedule() && i_instanceLock != null && i_instanceLock.GetData().CompletedEncountersMask != 0)
            {
                if (!entries.MapDifficulty.IsUsingEncounterLocks())
                {
                    InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);
                    if (playerLock == null || (playerLock.IsExpired() && playerLock.IsExtended()) ||
                        playerLock.GetData().CompletedEncountersMask != i_instanceLock.GetData().CompletedEncountersMask)
                    {
                        PendingRaidLock pendingRaidLock = new();
                        pendingRaidLock.TimeUntilLock = 60000;
                        pendingRaidLock.CompletedMask = i_instanceLock.GetData().CompletedEncountersMask;
                        pendingRaidLock.Extending = playerLock != null && playerLock.IsExtended();
                        pendingRaidLock.WarningOnly = entries.Map.IsFlexLocking(); // events it triggers:  1 : INSTANCE_LOCK_WARNING   0 : INSTANCE_LOCK_STOP / INSTANCE_LOCK_START
                        player.GetSession().SendPacket(pendingRaidLock);
                        if (!entries.Map.IsFlexLocking())
                            player.SetPendingBind(GetInstanceId(), 60000);
                    }
                }
            }

            Log.outInfo(LogFilter.Maps, "MAP: Player '{0}' entered instance '{1}' of map '{2}'", player.GetName(),
                        GetInstanceId(), GetMapName());
            // initialize unload state
            m_unloadTimer = 0;

            // this will acquire the same mutex so it cannot be in the previous block
            base.AddPlayerToMap(player, initPlayer);

            if (i_data != null)
                i_data.OnPlayerEnter(player);

            if (i_scenario != null)
                i_scenario.OnPlayerEnter(player);

            return true;
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            if (i_data != null)
            {
                i_data.Update(diff);
                i_data.UpdateCombatResurrection(diff);
            }

            if (i_scenario != null)
                i_scenario.Update(diff);

            if (i_instanceExpireEvent.HasValue && i_instanceExpireEvent.Value < GameTime.GetSystemTime())
            {
                Reset(InstanceResetMethod.Expire);
                i_instanceExpireEvent = Global.InstanceLockMgr.GetNextResetTime(new MapDb2Entries(GetEntry(), GetMapDifficulty()));
            }
        }

        public override void RemovePlayerFromMap(Player player, bool remove)
        {
            Log.outInfo(LogFilter.Maps, "MAP: Removing player '{0}' from instance '{1}' of map '{2}' before relocating to another map", player.GetName(), GetInstanceId(), GetMapName());

            if (i_data != null)
                i_data.OnPlayerLeave(player);

            // if last player set unload timer
            if (m_unloadTimer == 0 && GetPlayers().Count == 1)
                m_unloadTimer = (i_instanceLock != null && i_instanceLock.IsExpired()) ? 1 : (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);

            if (i_scenario != null)
                i_scenario.OnPlayerExit(player);

            base.RemovePlayerFromMap(player, remove);
        }

        public void CreateInstanceData()
        {
            if (i_data != null)
                return;

            InstanceTemplate mInstance = Global.ObjectMgr.GetInstanceTemplate(GetId());
            if (mInstance != null)
            {
                i_script_id = mInstance.ScriptId;
                i_data = Global.ScriptMgr.RunScriptRet<IInstanceMapGetInstanceScript, InstanceScript>(p => p.GetInstanceScript(this), GetScriptId(), null);
            }

            if (i_data == null)
                return;

            if (i_instanceLock == null || i_instanceLock.GetInstanceId() == 0)
            {
                i_data.Create();
                return;
            }

            MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());
            if (!entries.IsInstanceIdBound() || !IsRaid() || !entries.MapDifficulty.IsRestoringDungeonState() || i_owningGroupRef.IsValid())
            {
                i_data.Create();
                return;
            }

            InstanceLockData lockData = i_instanceLock.GetInstanceInitializationData();
            i_data.SetCompletedEncountersMask(lockData.CompletedEncountersMask);
            i_data.SetEntranceLocation(lockData.EntranceWorldSafeLocId);
            if (!lockData.Data.IsEmpty())
            {
                Log.outDebug(LogFilter.Maps, $"Loading instance data for `{Global.ObjectMgr.GetScriptName(i_script_id)}` with id {i_InstanceId}");
                i_data.Load(lockData.Data);
            }
            else
                i_data.Create();
        }

        public Group GetOwningGroup() { return i_owningGroupRef.GetTarget(); }
        
        public void TrySetOwningGroup(Group group)
        {
            if (!i_owningGroupRef.IsValid())
                i_owningGroupRef.Link(group, this);
        }

        public InstanceResetResult Reset(InstanceResetMethod method)
        {
            // raids can be reset if no boss was killed
            if (method != InstanceResetMethod.Expire && i_instanceLock != null && i_instanceLock.GetData().CompletedEncountersMask != 0)
                return InstanceResetResult.CannotReset;

            if (HavePlayers())
            {
                switch (method)
                {
                    case InstanceResetMethod.Manual:
                        // notify the players to leave the instance so it can be reset
                        foreach (var player in GetPlayers())
                            player.SendResetFailedNotify(GetId());
                        break;
                    case InstanceResetMethod.OnChangeDifficulty:
                        // no client notification
                        break;
                    case InstanceResetMethod.Expire:
                    {
                        RaidInstanceMessage raidInstanceMessage = new();
                        raidInstanceMessage.Type = InstanceResetWarningType.Expired;
                        raidInstanceMessage.MapID = GetId();
                        raidInstanceMessage.DifficultyID = GetDifficultyID();
                        raidInstanceMessage.Write();

                        PendingRaidLock pendingRaidLock = new();
                        pendingRaidLock.TimeUntilLock = 60000;
                        pendingRaidLock.CompletedMask = i_instanceLock.GetData().CompletedEncountersMask;
                        pendingRaidLock.Extending = true;
                        pendingRaidLock.WarningOnly = GetEntry().IsFlexLocking();
                        pendingRaidLock.Write();

                        foreach (Player player in GetPlayers())
                        {
                            player.SendPacket(raidInstanceMessage);
                            player.SendPacket(pendingRaidLock);

                            if (!pendingRaidLock.WarningOnly)
                                player.SetPendingBind(GetInstanceId(), 60000);
                        }
                        break;
                    }
                    default:
                        break;
                }

                return InstanceResetResult.NotEmpty;
            }
            else
            {
                // unloaded at next update
                m_unloadTimer = 1;
            }

            return InstanceResetResult.Success;
        }

        public string GetScriptName()
        {
            return Global.ObjectMgr.GetScriptName(i_script_id);
        }

        public void UpdateInstanceLock(UpdateBossStateSaveDataEvent updateSaveDataEvent)
        {
            if (i_instanceLock != null)
            {
                uint instanceCompletedEncounters = i_instanceLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);

                MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());

                SQLTransaction trans = new();

                if (entries.IsInstanceIdBound())
                    Global.InstanceLockMgr.UpdateSharedInstanceLock(trans, new InstanceLockUpdateEvent(GetInstanceId(), i_data.GetSaveData(), 
                        instanceCompletedEncounters, updateSaveDataEvent.DungeonEncounter, i_data.GetEntranceLocationForCompletedEncounters(instanceCompletedEncounters)));

                foreach (var player in GetPlayers())
                {
                    // never instance bind GMs with GM mode enabled
                    if (player.IsGameMaster())
                        continue;

                    InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);
                    string oldData = "";
                    uint playerCompletedEncounters = 0;
                    if (playerLock != null)
                    {
                        oldData = playerLock.GetData().Data;
                        playerCompletedEncounters = playerLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);
                    }

                    bool isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

                    InstanceLock newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans, player.GetGUID(), entries, new InstanceLockUpdateEvent(GetInstanceId(), i_data.UpdateBossStateSaveData(oldData, updateSaveDataEvent),
                        instanceCompletedEncounters, updateSaveDataEvent.DungeonEncounter, i_data.GetEntranceLocationForCompletedEncounters(playerCompletedEncounters)));

                    if (isNewLock)
                    {
                        InstanceSaveCreated data = new();
                        data.Gm = player.IsGameMaster();
                        player.SendPacket(data);

                        player.GetSession().SendCalendarRaidLockoutAdded(newLock);
                    }
                }

                DB.Characters.CommitTransaction(trans);
            }
        }
        
        public void UpdateInstanceLock(UpdateAdditionalSaveDataEvent updateSaveDataEvent)
        {
            if (i_instanceLock != null)
            {
                uint instanceCompletedEncounters = i_instanceLock.GetData().CompletedEncountersMask;

                MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());

                SQLTransaction trans = new();

                if (entries.IsInstanceIdBound())
                    Global.InstanceLockMgr.UpdateSharedInstanceLock(trans, new InstanceLockUpdateEvent(GetInstanceId(), i_data.GetSaveData(), instanceCompletedEncounters, null, null));

                foreach (var player in GetPlayers())
                {
                    // never instance bind GMs with GM mode enabled
                    if (player.IsGameMaster())
                        continue;

                    InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);
                    string oldData = "";
                    if (playerLock != null)
                        oldData = playerLock.GetData().Data;

                    bool isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

                    InstanceLock newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans, player.GetGUID(), entries, new InstanceLockUpdateEvent(GetInstanceId(), i_data.UpdateAdditionalSaveData(oldData, updateSaveDataEvent),
                        instanceCompletedEncounters, null, null));

                    if (isNewLock)
                    {
                        InstanceSaveCreated data = new();
                        data.Gm = player.IsGameMaster();
                        player.SendPacket(data);

                        player.GetSession().SendCalendarRaidLockoutAdded(newLock);
                    }
                }

                DB.Characters.CommitTransaction(trans);
            }
        }

        public void CreateInstanceLockForPlayer(Player player)
        {
            MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());
            InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);

            bool isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

            SQLTransaction trans = new();

            InstanceLock newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans, player.GetGUID(), entries, new InstanceLockUpdateEvent(GetInstanceId(), i_data.GetSaveData(), i_instanceLock.GetData().CompletedEncountersMask, null, null));

            DB.Characters.CommitTransaction(trans);

            if (isNewLock)
            {
                InstanceSaveCreated data = new();
                data.Gm = player.IsGameMaster();
                player.SendPacket(data);

                player.GetSession().SendCalendarRaidLockoutAdded(newLock);
            }
        }

        public uint GetMaxPlayers()
        {
            MapDifficultyRecord mapDiff = GetMapDifficulty();
            if (mapDiff != null && mapDiff.MaxPlayers != 0)
                return mapDiff.MaxPlayers;

            return GetEntry().MaxPlayers;
        }

        public int GetTeamIdInInstance()
        {
            if (Global.WorldStateMgr.GetValue(WorldStates.TeamInInstanceAlliance, this) != 0)
                return TeamId.Alliance;
            if (Global.WorldStateMgr.GetValue(WorldStates.TeamInInstanceHorde, this) != 0)
                return TeamId.Horde;
            return TeamId.Neutral;
        }

        public Team GetTeamInInstance() { return GetTeamIdInInstance() == TeamId.Alliance ? Team.Alliance : Team.Horde; }

        public uint GetScriptId()
        {
            return i_script_id;
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nScriptId: {GetScriptId()} ScriptName: {GetScriptName()}";
        }

        public InstanceScript GetInstanceScript()
        {
            return i_data;
        }

        public InstanceScenario GetInstanceScenario() { return i_scenario; }

        public void SetInstanceScenario(InstanceScenario scenario) { i_scenario = scenario; }

        public InstanceLock GetInstanceLock() { return i_instanceLock; }

        InstanceScript i_data;
        uint i_script_id;
        InstanceScenario i_scenario;
        readonly InstanceLock i_instanceLock;
        readonly GroupInstanceReference i_owningGroupRef = new();
        DateTime? i_instanceExpireEvent;
    }

    public class BattlegroundMap : Map
    {
        public BattlegroundMap(uint id, uint expiry, uint InstanceId, Difficulty spawnMode)
            : base(id, expiry, InstanceId, spawnMode)
        {
            InitVisibilityDistance();
        }

        public override void InitVisibilityDistance()
        {
            m_VisibleDistance = IsBattleArena() ? Global.WorldMgr.GetMaxVisibleDistanceInArenas() : Global.WorldMgr.GetMaxVisibleDistanceInBG();
            m_VisibilityNotifyPeriod = IsBattleArena() ? Global.WorldMgr.GetVisibilityNotifyPeriodInArenas() : Global.WorldMgr.GetVisibilityNotifyPeriodInBG();
        }

        public override TransferAbortParams CannotEnter(Player player)
        {
            if (player.GetMap() == this)
            {
                Log.outError(LogFilter.Maps, "BGMap:CannotEnter - player {0} is already in map!", player.GetGUID().ToString());
                Cypher.Assert(false);
                return new TransferAbortParams(TransferAbortReason.Error);
            }

            if (player.GetBattlegroundId() != GetInstanceId())
                return new TransferAbortParams(TransferAbortReason.LockedToDifferentInstance);

            return base.CannotEnter(player);
        }

        public override bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            player.m_InstanceValid = true;
            return base.AddPlayerToMap(player, initPlayer);
        }

        public override void RemovePlayerFromMap(Player player, bool remove)
        {
            Log.outInfo(LogFilter.Maps,
                "MAP: Removing player '{0}' from bg '{1}' of map '{2}' before relocating to another map", player.GetName(),
                GetInstanceId(), GetMapName());
            base.RemovePlayerFromMap(player, remove);
        }

        public void SetUnload()
        {
            m_unloadTimer = 1;
        }

        public override void RemoveAllPlayers()
        {
            if (HavePlayers())
                foreach (Player player in m_activePlayers)
                    if (!player.IsBeingTeleportedFar())
                        player.TeleportTo(player.GetBattlegroundEntryPoint());
        }

        public Battleground GetBG() { return m_bg; }
        public void SetBG(Battleground bg) { m_bg = bg; }

        Battleground m_bg;
    }

    public class TransferAbortParams
    {
        public TransferAbortReason Reason;
        public byte Arg;
        public uint MapDifficultyXConditionId;

        public TransferAbortParams(TransferAbortReason reason = TransferAbortReason.None, byte arg = 0, uint mapDifficultyXConditionId = 0)
        {
            Reason = reason;
            Arg = arg;
            MapDifficultyXConditionId = mapDifficultyXConditionId;
        }
    }
    
    public struct ScriptAction
    {
        public ObjectGuid ownerGUID;

        // owner of source if source is item
        public ScriptInfo script;

        public ObjectGuid sourceGUID;
        public ObjectGuid targetGUID;
    }

    public class ZoneDynamicInfo
    {
        public uint MusicId;
        public Weather DefaultWeather;
        public WeatherState WeatherId;
        public float Intensity;
        public List<LightOverride> LightOverrides = new();

        public struct LightOverride
        {
            public uint AreaLightId;
            public uint OverrideLightId;
            public uint TransitionMilliseconds;
        }
    }

    public class PositionFullTerrainStatus
    {
        public struct AreaInfo
        {
            public int AdtId;
            public int RootId;
            public int GroupId;
            public uint MogpFlags;

            public AreaInfo(int adtId, int rootId, int groupId, uint flags)
            {
                AdtId = adtId;
                RootId = rootId;
                GroupId = groupId;
                MogpFlags = flags;
            }
        }

        public uint AreaId;
        public float FloorZ;
        public bool outdoors = true;
        public ZLiquidStatus LiquidStatus;
        public AreaInfo? areaInfo;
        public LiquidData LiquidInfo;
    }

    public class RespawnInfo
    {
        public SpawnObjectType type;
        public ulong spawnId;
        public uint entry;
        public long respawnTime;
        public uint gridId;

        public RespawnInfo() { }
        public RespawnInfo(RespawnInfo info)
        {
            type = info.type;
            spawnId = info.spawnId;
            entry = info.entry;
            respawnTime = info.respawnTime;
            gridId = info.gridId;
        }
    }

    struct CompareRespawnInfo : IComparer<RespawnInfo>
    {
        public int Compare(RespawnInfo a, RespawnInfo b)
        {
            if (a == b)
                return 0;
            if (a.respawnTime != b.respawnTime)
                return a.respawnTime.CompareTo(b.respawnTime);
            if (a.spawnId != b.spawnId)
                return a.spawnId.CompareTo(b.spawnId);

            Cypher.Assert(a.type != b.type, $"Duplicate respawn entry for spawnId ({a.type},{a.spawnId}) found!");
            return a.type.CompareTo(b.type);
        }
    }
}