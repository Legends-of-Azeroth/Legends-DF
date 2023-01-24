﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.World
{
    class xp_boost_PlayerScript : ScriptObjectAutoAdd, IPlayerOnGiveXP
    {
        public xp_boost_PlayerScript() : base("xp_boost_PlayerScript") { }

        public void OnGiveXP(Player player, ref uint amount, Unit victim)
        {
            if (IsXPBoostActive())
                amount *= (uint)WorldConfig.GetFloatValue(WorldCfg.RateXpBoost);
        }

        bool IsXPBoostActive()
        {
            long time = GameTime.GetGameTime();
            var localTm = Time.UnixTimeToDateTime(time);
            uint weekdayMaskBoosted = WorldConfig.GetUIntValue(WorldCfg.XpBoostDaymask);
            uint weekdayMask = 1u << localTm.Day;
            bool currentDayBoosted = (weekdayMask & weekdayMaskBoosted) != 0;
            return currentDayBoosted;
        }
    }
}
