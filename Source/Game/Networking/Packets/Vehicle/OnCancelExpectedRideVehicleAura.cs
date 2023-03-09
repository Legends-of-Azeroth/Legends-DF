﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class OnCancelExpectedRideVehicleAura : ServerPacket
{
	public OnCancelExpectedRideVehicleAura() : base(ServerOpcodes.OnCancelExpectedRideVehicleAura, ConnectionType.Instance) { }

	public override void Write() { }
}