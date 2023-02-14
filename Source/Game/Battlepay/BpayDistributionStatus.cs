﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Battlepay
{
    public enum BpayDistributionStatus
    {
        NONE = 0,
        AVAILABLE = 1,
        ADD_TO_PROCESS = 2,
        PROCESS_COMPLETE = 3,
        FINISHED = 4
    }
}
