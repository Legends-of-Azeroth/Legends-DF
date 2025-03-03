﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_unstable_power", ItemSpellIds.UnstablePowerAuraStack)]
[Script("spell_item_restless_strength", ItemSpellIds.RestlessStrengthAuraStack)]
internal class spell_item_zandalarian_charm : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	private readonly uint _spellId;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_item_zandalarian_charm(uint SpellId)
	{
		_spellId = SpellId;
	}


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var spellInfo = eventInfo.SpellInfo;

		if (spellInfo != null)
			if (spellInfo.Id != ScriptSpellId)
				return true;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleStackDrop, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		Target.RemoveAuraFromStack(_spellId);
	}
}