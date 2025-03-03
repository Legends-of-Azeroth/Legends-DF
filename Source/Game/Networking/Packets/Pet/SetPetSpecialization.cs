﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class SetPetSpecialization : ServerPacket
{
	public ushort SpecID;
	public SetPetSpecialization() : base(ServerOpcodes.SetPetSpecialization) { }

	public override void Write()
	{
		_worldPacket.WriteUInt16(SpecID);
	}
}