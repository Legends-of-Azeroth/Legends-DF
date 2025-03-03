﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_DISINTEGRATE,
	            EvokerSpells.BLUE_DISINTEGRATE_2,
                EvokerSpells.BLUE_ETERNITY_SURGE,
                EvokerSpells.BLUE_ETERNITY_SURGE_2,
                EvokerSpells.BLUE_SHATTERING_STAR)]
public class spell_evoker_iridescence_blue_spell : SpellScript, ISpellAfterCast
{
    void ISpellAfterCast.AfterCast()
    {
        if (Caster.TryGetAura(EvokerSpells.IRIDESCENCE_BLUE, out var aura))
            aura.ModStackAmount(-1);
    }
}