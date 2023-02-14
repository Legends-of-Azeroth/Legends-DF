﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(228477)]
public class spell_dh_soul_cleave : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHeal(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.GetTypeId() != TypeId.Player)
			return;

		if (caster.HasAura(DemonHunterSpells.SPELL_DH_FEAST_OF_SOULS))
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEAST_OF_SOULS_HEAL, true);
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		// Consume all soul fragments in 25 yards;
		var fragments = new List<List<AreaTrigger>>();
		fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS));
		fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_DEMON));
		fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD));
		var range = GetEffectInfo().BasePoints;

		foreach (var vec in fragments)
		{
			foreach (var at in vec)
			{
				if (!caster.IsWithinDist(at, range))
					continue;

				var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPositionX(), at.GetPositionY(), at.GetPositionZ(), 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(100));

				if (tempSumm != null)
				{
					tempSumm.SetFaction(caster.GetFaction());
					tempSumm.SetSummonerGUID(caster.GetGUID());
					var bp = 0;

					switch (at.GetTemplate().Id.Id)
					{
						case 6007:
						case 5997:
							bp = (int)ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_HEAL_VENGEANCE;

							break;
						case 6710:
							bp = (int)ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD_HEAL;

							break;
					}

					caster.CastSpell(tempSumm, ShatteredSoulsSpells.SPELL_DH_CONSUME_SOUL_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));

					if (at.GetTemplate().Id.Id == 6007)
						caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_DEMON_BONUS, true);

					if (caster.HasAura(DemonHunterSpells.SPELL_DH_FEED_THE_DEMON))
						caster.GetSpellHistory().ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

					if (caster.HasAura(ShatteredSoulsSpells.SPELL_DH_PAINBRINGER))
						caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_PAINBRINGER_BUFF, true);

					var soulBarrier = caster.GetAuraEffect(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, 0);

					if (soulBarrier != null)
					{
						var amount = soulBarrier.GetAmount() + ((float)(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, Difficulty.None).GetEffect(1).BasePoints) / 100.0f) * caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
						soulBarrier.SetAmount(amount);
					}

					at.SetDuration(0);
				}
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleHeal, 3, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}