﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.AI
{
    // default predicate function to select target based on distance, player and/or aura criteria
    public class DefaultTargetSelector : ICheck<Unit>
    {
        readonly Unit _me;
        readonly float _dist;
        readonly bool _playerOnly;
        readonly Unit _exception;
        readonly int _aura;

        /// <param name="unit">the reference unit</param>
        /// <param name="dist">if 0: ignored, if > 0: maximum distance to the reference unit, if < 0: minimum distance to the reference unit</param>
        /// <param name="playerOnly">self explaining</param>
        /// <param name="withTank">allow current tank to be selected</param>
        /// <param name="aura">if 0: ignored, if > 0: the target shall have the aura, if < 0, the target shall NOT have the aura</param>
        public DefaultTargetSelector(Unit unit, float dist, bool playerOnly, bool withTank, int aura)
        {
            _me = unit;
            _dist = dist;
            _playerOnly = playerOnly;
            _exception = !withTank ? unit.GetThreatManager().LastVictim : null;
            _aura = aura;
        }

        public bool Invoke(Unit target)
        {
            if (_me == null)
                return false;

            if (target == null)
                return false;

            if (_exception != null && target == _exception)
                return false;

            if (_playerOnly && !target.IsTypeId(TypeId.Player))
                return false;

            if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
                return false;

            if (_dist < 0.0f && _me.IsWithinCombatRange(target, -_dist))
                return false;

            if (_aura != 0)
            {
                if (_aura > 0)
                {
                    if (!target.HasAura((uint)_aura))
                        return false;
                }
                else
                {
                    if (target.HasAura((uint)-_aura))
                        return false;
                }
            }

            return false;
        }
    }

    // Target selector for spell casts checking range, auras and attributes
    // todo Add more checks from Spell.CheckCast
    public class SpellTargetSelector : ICheck<Unit>
    {
        readonly Unit _caster;
        readonly SpellInfo _spellInfo;

        public SpellTargetSelector(Unit caster, uint spellId)
        {
            _caster = caster;
            _spellInfo = Global.SpellMgr.GetSpellInfo(spellId, caster.Map.DifficultyID);

            Cypher.Assert(_spellInfo != null);
        }

        public bool Invoke(Unit target)
        {
            if (target == null)
                return false;

            if (_spellInfo.CheckTarget(_caster, target) != SpellCastResult.SpellCastOk)
                return false;

            // copypasta from Spell.CheckRange
            float minRange = 0.0f;
            float maxRange = 0.0f;
            float rangeMod = 0.0f;
            if (_spellInfo.RangeEntry != null)
            {
                if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
                {
                    rangeMod = _caster.CombatReach + 4.0f / 3.0f;
                    rangeMod += target.CombatReach;

                    rangeMod = Math.Max(rangeMod, SharedConst.NominalMeleeRange);
                }
                else
                {
                    float meleeRange = 0.0f;
                    if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                    {
                        meleeRange = _caster.CombatReach + 4.0f / 3.0f;
                        meleeRange += target.CombatReach;

                        meleeRange = Math.Max(meleeRange, SharedConst.NominalMeleeRange);
                    }

                    minRange = _caster.GetSpellMinRangeForTarget(target, _spellInfo) + meleeRange;
                    maxRange = _caster.GetSpellMaxRangeForTarget(target, _spellInfo);

                    rangeMod = _caster.CombatReach;
                    rangeMod += target.CombatReach;

                    if (minRange > 0.0f && !_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                        minRange += rangeMod;
                }

                if (_caster.IsMoving && target.IsMoving && !_caster.IsWalking && !target.IsWalking &&
                    (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee) || target.IsTypeId(TypeId.Player)))
                    rangeMod += 8.0f / 3.0f;
            }

            maxRange += rangeMod;

            minRange *= minRange;
            maxRange *= maxRange;

            if (target != _caster)
            {
                if (_caster.Location.GetExactDistSq(target.Location) > maxRange)
                    return false;

                if (minRange > 0.0f && _caster.Location.GetExactDistSq(target.Location) < minRange)
                    return false;
            }

            return true;
        }
    }

    // Very simple target selector, will just skip main target
    // NOTE: When passing to UnitAI.SelectTarget remember to use 0 as position for random selection
    //       because tank will not be in the temporary list
    public class NonTankTargetSelector : ICheck<Unit>
    {
        readonly Unit _source;
        readonly bool _playerOnly;

        public NonTankTargetSelector(Unit source, bool playerOnly = true)
        {
            _source = source;
            _playerOnly = playerOnly;
        }

        public bool Invoke(Unit target)
        {
            if (target == null)
                return false;

            if (_playerOnly && !target.IsTypeId(TypeId.Player))
                return false;

            Unit currentVictim = _source.GetThreatManager().CurrentVictim;
            if (currentVictim != null)
                return target != currentVictim;

            return target != _source.Victim;
        }
    }

    // Simple selector for units using mana
    class PowerUsersSelector : ICheck<Unit>
    {
        readonly Unit _me;
        readonly PowerType _power;
        readonly float _dist;
        readonly bool _playerOnly;

        public PowerUsersSelector(Unit unit, PowerType power, float dist, bool playerOnly)
        {
            _me = unit;
            _power = power;
            _dist = dist;
            _playerOnly = playerOnly;
        }

        public bool Invoke(Unit target)
        {
            if (_me == null || target == null)
                return false;

            if (target.DisplayPowerType != _power)
                return false;

            if (_playerOnly && target.TypeId != TypeId.Player)
                return false;

            if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
                return false;

            if (_dist < 0.0f && _me.IsWithinCombatRange(target, -_dist))
                return false;

            return true;
        }
    }

    class FarthestTargetSelector : ICheck<Unit>
    {
        readonly Unit _me;
        readonly float _dist;
        readonly bool _playerOnly;
        readonly bool _inLos;

        public FarthestTargetSelector(Unit unit, float dist, bool playerOnly, bool inLos)
        {
            _me = unit;
            _dist = dist;
            _playerOnly = playerOnly;
            _inLos = inLos;
        }

        public bool Invoke(Unit target)
        {
            if (_me == null || target == null)
                return false;

            if (_playerOnly && target.TypeId != TypeId.Player)
                return false;

            if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
                return false;

            if (_inLos && !_me.IsWithinLOSInMap(target))
                return false;

            return true;
        }
    }
}
