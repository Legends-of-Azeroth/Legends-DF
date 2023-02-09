﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 47536 - Rapture
internal class spell_pri_rapture : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	private ObjectGuid _raptureTarget;

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PowerWordShield);
	}

	public void AfterCast()
	{
		var caster = GetCaster();
		var target = Global.ObjAccessor.GetUnit(caster, _raptureTarget);

		if (target != null)
			caster.CastSpell(target, PriestSpells.PowerWordShield, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerAndReagentCost | TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleEffectDummy(int effIndex)
	{
		_raptureTarget = GetHitUnit().GetGUID();
	}
}