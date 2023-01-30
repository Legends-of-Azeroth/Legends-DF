﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Groups
{
    public class RaidMarker
    {
        public ObjectGuid TransportGUID;

        public RaidMarker(uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
        {
            Location = new WorldLocation(mapId, positionX, positionY, positionZ);
            TransportGUID = transportGuid;
        }

        public WorldLocation Location { get; set; }
    }
}