﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AreaTriggerFlags
{
	HasAbsoluteOrientation = 0x01, // Nyi
	HasDynamicShape = 0x02,        // Implemented For Spheres
	HasAttached = 0x04,
	HasFaceMovementDir = 0x08,
	HasFollowsTerrain = 0x010, // Nyi
	Unk1 = 0x020,
	HasTargetRollPitchYaw = 0x040, // Nyi
	HasAnimID = 0x080,
	Unk3 = 0x100,
	HasAnimKitID = 0x200,
	HasCircularMovement = 0x400,
	Unk5 = 0x800
}