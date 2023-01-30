﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;
using Game.Networking;

namespace Game.Maps.Dos
{
    public class PacketSenderOwning<T> : IDoWork<Player> where T : ServerPacket, new()
    {
        public T Data = new();

        public void Invoke(Player player)
        {
            player.SendPacket(Data);
        }
    }
}