// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Lucifron;

internal struct SpellIds
{
	public const uint ImpendingDoom = 19702;
	public const uint LucifronCurse = 19703;
	public const uint ShadowShock = 20603;
}

[Script]
internal class boss_lucifron : BossAI
{
	public boss_lucifron(Creature creature) : base(creature, DataTypes.Lucifron) { }

	public override void JustEngagedWith(Unit victim)
	{
		base.JustEngagedWith(victim);

		Scheduler.Schedule(TimeSpan.FromSeconds(10),
							task =>
							{
								DoCastVictim(SpellIds.ImpendingDoom);
								task.Repeat(TimeSpan.FromSeconds(20));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(20),
							task =>
							{
								DoCastVictim(SpellIds.LucifronCurse);
								task.Repeat(TimeSpan.FromSeconds(15));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.ShadowShock);
								task.Repeat(TimeSpan.FromSeconds(6));
							});
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}