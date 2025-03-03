﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer;

// Called when a player gains XP (before anything is given);
public interface IPlayerOnGiveXP : IScriptObject
{
	void OnGiveXP(Player player, ref uint amount, Unit victim);
}