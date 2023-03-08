﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_admiral_ripsnarl;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47714)]
public class npc_vapor : ScriptedAI
{
	private readonly InstanceScript _instance;

	private bool _form_1;
	private bool _form_2;
	private bool _form_3;

	public npc_vapor(Creature creature) : base(creature)
	{
		_instance = creature.InstanceScript;
	}

	public override void Reset()
	{
		_events.Reset();
		_form_1 = false;
		_form_2 = false;
		_form_3 = false;
	}

	public override void JustEnteredCombat(Unit who)
	{
		if (!me)
			return;

		if (IsHeroic())
			me.AddAura(eSpells.CONDENSATION, me);
	}

	public override void JustDied(Unit killer)
	{
		var Ripsnarl = me.FindNearestCreature(DMCreatures.NPC_ADMIRAL_RIPSNARL, 250, true);

		if (Ripsnarl != null)
		{
			var pAI = (boss_admiral_ripsnarl)Ripsnarl.AI;

			if (pAI != null)
				pAI.VaporsKilled();
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		_events.Update(diff);

		if (me.HasAura(eSpells.CONDENSE) && !_form_1)
		{
			_events.ScheduleEvent(VaporEvents.EVENT_CONDENSING_VAPOR, TimeSpan.FromMilliseconds(2000));
			_form_1 = true;
		}
		else if (me.HasAura(eSpells.CONDENSE_2) && !_form_2)
		{
			me.SetDisplayId(25654);
			_events.CancelEvent(VaporEvents.EVENT_CONDENSING_VAPOR);
			_events.ScheduleEvent(VaporEvents.EVENT_SWIRLING_VAPOR, TimeSpan.FromMilliseconds(2000));
			_form_2 = true;
		}
		else if (me.HasAura(eSpells.CONDENSE_3) && !_form_3)
		{
			me.SetDisplayId(36455);
			_events.CancelEvent(VaporEvents.EVENT_SWIRLING_VAPOR);
			_events.ScheduleEvent(VaporEvents.EVENT_FREEZING_VAPOR, TimeSpan.FromMilliseconds(2000));
			_form_3 = true;
		}

		uint eventId;

		while ((eventId = _events.ExecuteEvent()) != 0)
			switch (eventId)
			{
				case VaporEvents.EVENT_CONDENSING_VAPOR:
					DoCastVictim(eSpells.CONDENSING_VAPOR);
					_events.ScheduleEvent(VaporEvents.EVENT_SWIRLING_VAPOR, TimeSpan.FromMilliseconds(3500));

					break;
				case VaporEvents.EVENT_SWIRLING_VAPOR:
					DoCastVictim(eSpells.SWIRLING_VAPOR);
					_events.ScheduleEvent(VaporEvents.EVENT_SWIRLING_VAPOR, TimeSpan.FromMilliseconds(3500));

					break;
				case VaporEvents.EVENT_FREEZING_VAPOR:
					DoCastVictim(eSpells.FREEZING_VAPOR);
					_events.ScheduleEvent(VaporEvents.EVENT_COALESCE, TimeSpan.FromMilliseconds(5000));

					break;
				case VaporEvents.EVENT_COALESCE:
					DoCastVictim(eSpells.COALESCE);

					break;
			}

		DoMeleeAttackIfReady();
	}

	public struct VaporEvents
	{
		public const uint EVENT_CONDENSING_VAPOR = 1;
		public const uint EVENT_SWIRLING_VAPOR = 2;
		public const uint EVENT_FREEZING_VAPOR = 3;
		public const uint EVENT_COALESCE = 4;
	}
}