﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct VehicleTeleport
{
	public byte VehicleSeatIndex;
	public bool VehicleExitVoluntary;
	public bool VehicleExitTeleport;
}