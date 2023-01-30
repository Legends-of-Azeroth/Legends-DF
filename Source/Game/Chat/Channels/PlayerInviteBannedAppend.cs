﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal readonly struct PlayerInviteBannedAppend : IChannelAppender
    {
        public PlayerInviteBannedAppend(string playerName)
        {
            _playerName = playerName;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.PlayerInviteBannedNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.Sender = _playerName;
        }

        private readonly string _playerName;
    }
}