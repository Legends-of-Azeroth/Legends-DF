﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_pony_mount_check : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var caster = Caster;

		if (!caster)
			return;

		var owner = caster.OwnerUnit.AsPlayer;

		if (!owner ||
			!owner.HasAchieved(GenericSpellIds.AchievementPonyup))
			return;

		if (owner.IsMounted)
		{
			caster.Mount(GenericSpellIds.MountPony);
			caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
		}
		else if (caster.IsMounted)
		{
			caster.Dismount();
			caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
		}
	}
}