﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman;

//100943
[CreatureScript(100943)]
public class npc_earthen_shield_totem : ScriptedAI
{
	public npc_earthen_shield_totem(Creature creature) : base(creature) { }

	public override void Reset()
	{
		Me.CastSpell(Me, ShamanSpells.AT_EARTHEN_SHIELD_TOTEM, true);
	}
}