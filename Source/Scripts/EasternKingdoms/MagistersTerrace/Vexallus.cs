// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.MagistersTerrace.Vexallus;

internal struct TextIds
{
	public const uint SayAggro = 0;
	public const uint SayEnergy = 1;
	public const uint SayOverload = 2;
	public const uint SayKill = 3;
	public const uint EmoteDischargeEnergy = 4;
}

internal struct SpellIds
{
	public const uint ChainLightning = 44318;
	public const uint Overload = 44353;
	public const uint ArcaneShock = 44319;

	public const uint SummonPureEnergy = 44322;   // mod scale -10
	public const uint HSummonPureEnergy1 = 46154; // mod scale -5
	public const uint HSummonPureEnergy2 = 46159; // mod scale -5

	// NpcPureEnergy
	public const uint EnergyBolt = 46156;
	public const uint EnergyFeedback = 44335;
	public const uint PureEnergyPassive = 44326;
}

internal struct MiscConst
{
	public const uint IntervalModifier = 15;
	public const uint IntervalSwitch = 6;
}

[Script]
internal class boss_vexallus : BossAI
{
	private bool _enraged;
	private uint _intervalHealthAmount;

	public boss_vexallus(Creature creature) : base(creature, DataTypes.Vexallus)
	{
		_intervalHealthAmount = 1;
		_enraged = false;
	}

	public override void Reset()
	{
		_Reset();
		_intervalHealthAmount = 1;
		_enraged = false;
	}

	public override void KilledUnit(Unit victim)
	{
		Talk(TextIds.SayKill);
	}

	public override void JustEngagedWith(Unit who)
	{
		Talk(TextIds.SayAggro);
		base.JustEngagedWith(who);

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

								if (target)
									DoCast(target, SpellIds.ChainLightning);

								task.Repeat();
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(5),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 20.0f, true);

								if (target)
									DoCast(target, SpellIds.ArcaneShock);

								task.Repeat(TimeSpan.FromSeconds(8));
							});
	}

	public override void JustSummoned(Creature summoned)
	{
		var temp = SelectTarget(SelectTargetMethod.Random, 0);

		if (temp)
			summoned.MotionMaster.MoveFollow(temp, 0, 0);

		Summons.Summon(summoned);
	}

	public override void DamageTaken(Unit who, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		if (_enraged)
			return;

		// 85%, 70%, 55%, 40%, 25%
		if (!HealthAbovePct((int)(100 - MiscConst.IntervalModifier * _intervalHealthAmount)))
		{
			// increase amount, unless we're at 10%, then we switch and return
			if (_intervalHealthAmount == MiscConst.IntervalSwitch)
			{
				_enraged = true;
				Scheduler.CancelAll();

				Scheduler.Schedule(TimeSpan.FromSeconds(1.2),
									task =>
									{
										DoCastVictim(SpellIds.Overload);
										task.Repeat(TimeSpan.FromSeconds(2));
									});

				return;
			}
			else
			{
				++_intervalHealthAmount;
			}

			Talk(TextIds.SayEnergy);
			Talk(TextIds.EmoteDischargeEnergy);

			if (IsHeroic())
			{
				DoCast(Me, SpellIds.HSummonPureEnergy1);
				DoCast(Me, SpellIds.HSummonPureEnergy2);
			}
			else
			{
				DoCast(Me, SpellIds.SummonPureEnergy);
			}
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}

[Script]
internal class npc_pure_energy : ScriptedAI
{
	public npc_pure_energy(Creature creature) : base(creature)
	{
		Me.SetDisplayFromModel(1);
	}

	public override void JustDied(Unit killer)
	{
		if (killer)
			killer.CastSpell(killer, SpellIds.EnergyFeedback, true);

		Me.RemoveAura(SpellIds.PureEnergyPassive);
	}
}