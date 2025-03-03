﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.m_Events.LunarFestival;

internal struct SpellIds
{
	//Fireworks
	public const uint RocketBlue = 26344;
	public const uint RocketGreen = 26345;
	public const uint RocketPurple = 26346;
	public const uint RocketRed = 26347;
	public const uint RocketWhite = 26348;
	public const uint RocketYellow = 26349;
	public const uint RocketBigBlue = 26351;
	public const uint RocketBigGreen = 26352;
	public const uint RocketBigPurple = 26353;
	public const uint RocketBigRed = 26354;
	public const uint RocketBigWhite = 26355;
	public const uint RocketBigYellow = 26356;
	public const uint LunarFortune = 26522;

	//Omen
	public const uint OmenCleave = 15284;
	public const uint OmenStarfall = 26540;
	public const uint OmenSummonSpotlight = 26392;
	public const uint EluneCandle = 26374;

	//EluneCandle
	public const uint EluneCandleOmenHead = 26622;
	public const uint EluneCandleOmenChest = 26624;
	public const uint EluneCandleOmenHandR = 26625;
	public const uint EluneCandleOmenHandL = 26649;
	public const uint EluneCandleNormal = 26636;
}

internal struct CreatureIds
{
	//Fireworks
	public const uint Omen = 15467;
	public const uint MinionOfOmen = 15466;
	public const uint FireworkBlue = 15879;
	public const uint FireworkGreen = 15880;
	public const uint FireworkPurple = 15881;
	public const uint FireworkRed = 15882;
	public const uint FireworkYellow = 15883;
	public const uint FireworkWhite = 15884;
	public const uint FireworkBigBlue = 15885;
	public const uint FireworkBigGreen = 15886;
	public const uint FireworkBigPurple = 15887;
	public const uint FireworkBigRed = 15888;
	public const uint FireworkBigYellow = 15889;
	public const uint FireworkBigWhite = 15890;

	public const uint ClusterBlue = 15872;
	public const uint ClusterRed = 15873;
	public const uint ClusterGreen = 15874;
	public const uint ClusterPurple = 15875;
	public const uint ClusterWhite = 15876;
	public const uint ClusterYellow = 15877;
	public const uint ClusterBigBlue = 15911;
	public const uint ClusterBigGreen = 15912;
	public const uint ClusterBigPurple = 15913;
	public const uint ClusterBigRed = 15914;
	public const uint ClusterBigWhite = 15915;
	public const uint ClusterBigYellow = 15916;
	public const uint ClusterElune = 15918;
}

internal struct GameObjectIds
{
	//Fireworks
	public const uint FireworkLauncher1 = 180771;
	public const uint FireworkLauncher2 = 180868;
	public const uint FireworkLauncher3 = 180850;
	public const uint ClusterLauncher1 = 180772;
	public const uint ClusterLauncher2 = 180859;
	public const uint ClusterLauncher3 = 180869;
	public const uint ClusterLauncher4 = 180874;

	//Omen
	public const uint EluneTrap1 = 180876;
	public const uint EluneTrap2 = 180877;
}

internal struct MiscConst
{
	//Fireworks
	public const uint AnimGoLaunchFirework = 3;
	public const uint ZoneMoonglade = 493;

	//Omen
	public static Position OmenSummonPos = new(7558.993f, -2839.999f, 450.0214f, 4.46f);
}

[Script]
internal class npc_firework : ScriptedAI
{
	public npc_firework(Creature creature) : base(creature) { }

	public override void Reset()
	{
		var launcher = FindNearestLauncher();

		if (launcher)
		{
			launcher.SendCustomAnim(MiscConst.AnimGoLaunchFirework);
			Me.Location.Orientation = launcher.Location.Orientation + MathF.PI / 2;
		}
		else
		{
			return;
		}

		if (isCluster())
		{
			// Check if we are near Elune'ara lake south, if so try to summon Omen or a minion
			if (Me.Zone == MiscConst.ZoneMoonglade)
				if (!Me.FindNearestCreature(CreatureIds.Omen, 100.0f) &&
					Me.GetDistance2d(MiscConst.OmenSummonPos.X, MiscConst.OmenSummonPos.Y) <= 100.0f)
					switch (RandomHelper.URand(0, 9))
					{
						case 0:
						case 1:
						case 2:
						case 3:
							Creature minion = Me.SummonCreature(CreatureIds.MinionOfOmen, Me.Location.X + RandomHelper.FRand(-5.0f, 5.0f), Me.Location.Y + RandomHelper.FRand(-5.0f, 5.0f), Me.Location.Z, 0.0f, TempSummonType.CorpseTimedDespawn, TimeSpan.FromSeconds(20));

							if (minion)
								minion.AI.AttackStart(Me.SelectNearestPlayer(20.0f));

							break;
						case 9:
							Me.SummonCreature(CreatureIds.Omen, MiscConst.OmenSummonPos);

							break;
					}

			if (Me.Entry == CreatureIds.ClusterElune)
				DoCast(SpellIds.LunarFortune);

			var displacement = 0.7f;

			for (byte i = 0; i < 4; i++)
				Me.SummonGameObject(GetFireworkGameObjectId(), Me.Location.X + (i % 2 == 0 ? displacement : -displacement), Me.Location.Y + (i > 1 ? displacement : -displacement), Me.Location.Z + 4.0f, Me.Location.Orientation, Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(Me.Location.Orientation, 0.0f, 0.0f)), TimeSpan.FromSeconds(1));
		}
		else
			//me.CastSpell(me, GetFireworkSpell(me.GetEntry()), true);
		{
			Me.CastSpell(Me.Location, GetFireworkSpell(Me.Entry), new CastSpellExtraArgs(true));
		}
	}

	private bool isCluster()
	{
		switch (Me.Entry)
		{
			case CreatureIds.FireworkBlue:
			case CreatureIds.FireworkGreen:
			case CreatureIds.FireworkPurple:
			case CreatureIds.FireworkRed:
			case CreatureIds.FireworkYellow:
			case CreatureIds.FireworkWhite:
			case CreatureIds.FireworkBigBlue:
			case CreatureIds.FireworkBigGreen:
			case CreatureIds.FireworkBigPurple:
			case CreatureIds.FireworkBigRed:
			case CreatureIds.FireworkBigYellow:
			case CreatureIds.FireworkBigWhite:
				return false;
			case CreatureIds.ClusterBlue:
			case CreatureIds.ClusterGreen:
			case CreatureIds.ClusterPurple:
			case CreatureIds.ClusterRed:
			case CreatureIds.ClusterYellow:
			case CreatureIds.ClusterWhite:
			case CreatureIds.ClusterBigBlue:
			case CreatureIds.ClusterBigGreen:
			case CreatureIds.ClusterBigPurple:
			case CreatureIds.ClusterBigRed:
			case CreatureIds.ClusterBigYellow:
			case CreatureIds.ClusterBigWhite:
			case CreatureIds.ClusterElune:
			default:
				return true;
		}
	}

	private GameObject FindNearestLauncher()
	{
		GameObject launcher = null;

		if (isCluster())
		{
			var launcher1 = GetClosestGameObjectWithEntry(Me, GameObjectIds.ClusterLauncher1, 0.5f);
			var launcher2 = GetClosestGameObjectWithEntry(Me, GameObjectIds.ClusterLauncher2, 0.5f);
			var launcher3 = GetClosestGameObjectWithEntry(Me, GameObjectIds.ClusterLauncher3, 0.5f);
			var launcher4 = GetClosestGameObjectWithEntry(Me, GameObjectIds.ClusterLauncher4, 0.5f);

			if (launcher1)
				launcher = launcher1;
			else if (launcher2)
				launcher = launcher2;
			else if (launcher3)
				launcher = launcher3;
			else if (launcher4)
				launcher = launcher4;
		}
		else
		{
			var launcher1 = GetClosestGameObjectWithEntry(Me, GameObjectIds.FireworkLauncher1, 0.5f);
			var launcher2 = GetClosestGameObjectWithEntry(Me, GameObjectIds.FireworkLauncher2, 0.5f);
			var launcher3 = GetClosestGameObjectWithEntry(Me, GameObjectIds.FireworkLauncher3, 0.5f);

			if (launcher1)
				launcher = launcher1;
			else if (launcher2)
				launcher = launcher2;
			else if (launcher3)
				launcher = launcher3;
		}

		return launcher;
	}

	private uint GetFireworkSpell(uint entry)
	{
		switch (entry)
		{
			case CreatureIds.FireworkBlue:
				return SpellIds.RocketBlue;
			case CreatureIds.FireworkGreen:
				return SpellIds.RocketGreen;
			case CreatureIds.FireworkPurple:
				return SpellIds.RocketPurple;
			case CreatureIds.FireworkRed:
				return SpellIds.RocketRed;
			case CreatureIds.FireworkYellow:
				return SpellIds.RocketYellow;
			case CreatureIds.FireworkWhite:
				return SpellIds.RocketWhite;
			case CreatureIds.FireworkBigBlue:
				return SpellIds.RocketBigBlue;
			case CreatureIds.FireworkBigGreen:
				return SpellIds.RocketBigGreen;
			case CreatureIds.FireworkBigPurple:
				return SpellIds.RocketBigPurple;
			case CreatureIds.FireworkBigRed:
				return SpellIds.RocketBigRed;
			case CreatureIds.FireworkBigYellow:
				return SpellIds.RocketBigYellow;
			case CreatureIds.FireworkBigWhite:
				return SpellIds.RocketBigWhite;
			default:
				return 0;
		}
	}

	private uint GetFireworkGameObjectId()
	{
		uint spellId = 0;

		switch (Me.Entry)
		{
			case CreatureIds.ClusterBlue:
				spellId = GetFireworkSpell(CreatureIds.FireworkBlue);

				break;
			case CreatureIds.ClusterGreen:
				spellId = GetFireworkSpell(CreatureIds.FireworkGreen);

				break;
			case CreatureIds.ClusterPurple:
				spellId = GetFireworkSpell(CreatureIds.FireworkPurple);

				break;
			case CreatureIds.ClusterRed:
				spellId = GetFireworkSpell(CreatureIds.FireworkRed);

				break;
			case CreatureIds.ClusterYellow:
				spellId = GetFireworkSpell(CreatureIds.FireworkYellow);

				break;
			case CreatureIds.ClusterWhite:
				spellId = GetFireworkSpell(CreatureIds.FireworkWhite);

				break;
			case CreatureIds.ClusterBigBlue:
				spellId = GetFireworkSpell(CreatureIds.FireworkBigBlue);

				break;
			case CreatureIds.ClusterBigGreen:
				spellId = GetFireworkSpell(CreatureIds.FireworkBigGreen);

				break;
			case CreatureIds.ClusterBigPurple:
				spellId = GetFireworkSpell(CreatureIds.FireworkBigPurple);

				break;
			case CreatureIds.ClusterBigRed:
				spellId = GetFireworkSpell(CreatureIds.FireworkBigRed);

				break;
			case CreatureIds.ClusterBigYellow:
				spellId = GetFireworkSpell(CreatureIds.FireworkBigYellow);

				break;
			case CreatureIds.ClusterBigWhite:
				spellId = GetFireworkSpell(CreatureIds.FireworkBigWhite);

				break;
			case CreatureIds.ClusterElune:
				spellId = GetFireworkSpell(RandomHelper.URand(CreatureIds.FireworkBlue, CreatureIds.FireworkWhite));

				break;
		}

		var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

		if (spellInfo != null &&
			spellInfo.GetEffect(0).Effect == SpellEffectName.SummonObjectWild)
			return (uint)spellInfo.GetEffect(0).MiscValue;

		return 0;
	}
}

[Script]
internal class npc_omen : ScriptedAI
{
	public npc_omen(Creature creature) : base(creature)
	{
		Me.SetImmuneToPC(true);
		Me.MotionMaster.MovePoint(1, 7549.977f, -2855.137f, 456.9678f);
	}

	public override void MovementInform(MovementGeneratorType type, uint pointId)
	{
		if (type != MovementGeneratorType.Point)
			return;

		if (pointId == 1)
		{
			Me.SetHomePosition(Me.Location.X, Me.Location.Y, Me.Location.Z, Me.Location.Orientation);
			Me.SetImmuneToPC(false);
			var player = Me.SelectNearestPlayer(40.0f);

			if (player)
				AttackStart(player);
		}
	}

	public override void JustEngagedWith(Unit attacker)
	{
		Scheduler.CancelAll();

		Scheduler.Schedule(TimeSpan.FromSeconds(3),
							TimeSpan.FromSeconds(5),
							task =>
							{
								DoCastVictim(SpellIds.OmenCleave);
								task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							TimeSpan.FromSeconds(10),
							1,
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0);

								if (target)
									DoCast(target, SpellIds.OmenStarfall);

								task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(16));
							});
	}

	public override void JustDied(Unit killer)
	{
		DoCast(SpellIds.OmenSummonSpotlight);
	}

	public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
	{
		if (spellInfo.Id == SpellIds.EluneCandle)
		{
			if (Me.HasAura(SpellIds.OmenStarfall))
				Me.RemoveAura(SpellIds.OmenStarfall);

			Scheduler.RescheduleGroup(1, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(16));
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff);

		DoMeleeAttackIfReady();
	}
}

[Script]
internal class npc_giant_spotlight : ScriptedAI
{
	public npc_giant_spotlight(Creature creature) : base(creature) { }

	public override void Reset()
	{
		Scheduler.CancelAll();

		Scheduler.Schedule(TimeSpan.FromMinutes(5),
							task =>
							{
								var trap = Me.FindNearestGameObject(GameObjectIds.EluneTrap1, 5.0f);

								if (trap)
									trap.RemoveFromWorld();

								trap = Me.FindNearestGameObject(GameObjectIds.EluneTrap2, 5.0f);

								if (trap)
									trap.RemoveFromWorld();

								var omen = Me.FindNearestCreature(CreatureIds.Omen, 5.0f, false);

								if (omen)
									omen.DespawnOrUnsummon();

								Me.DespawnOrUnsummon();
							});
	}

	public override void UpdateAI(uint diff)
	{
		Scheduler.Update(diff);
	}
}

[Script] // 26374 - Elune's Candle
internal class spell_lunar_festival_elune_candle : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		uint spellId = 0;

		if (HitUnit.Entry == CreatureIds.Omen)
			switch (RandomHelper.URand(0, 3))
			{
				case 0:
					spellId = SpellIds.EluneCandleOmenHead;

					break;
				case 1:
					spellId = SpellIds.EluneCandleOmenChest;

					break;
				case 2:
					spellId = SpellIds.EluneCandleOmenHandR;

					break;
				case 3:
					spellId = SpellIds.EluneCandleOmenHandL;

					break;
			}
		else
			spellId = SpellIds.EluneCandleNormal;

		Caster.CastSpell(HitUnit, spellId, true);
	}
}