﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.AI
{
    public class TotemAI : NullCreatureAI
    {
        ObjectGuid _victimGuid;

        public TotemAI(Creature creature) : base(creature)
        {
            Cypher.Assert(creature.IsTotem, $"TotemAI: AI assigned to a no-totem creature ({creature.GUID})!");
            _victimGuid = ObjectGuid.Empty;
        }

        public override void UpdateAI(uint diff)
        {
            if (me.ToTotem().GetTotemType() != TotemType.Active)
                return;

            if (!me.IsAlive || me.IsNonMeleeSpellCast(false))
                return;

            // Search spell
            var spellInfo = Global.SpellMgr.GetSpellInfo(me.ToTotem().GetSpell(), me.Map.GetDifficultyID());
            if (spellInfo == null)
                return;

            // Get spell range
            float max_range = spellInfo.GetMaxRange(false);

            // SpellModOp.Range not applied in this place just because not existence range mods for attacking totems

            Unit victim = !_victimGuid.IsEmpty ? Global.ObjAccessor.GetUnit(me, _victimGuid) : null;

            // Search victim if no, not attackable, or out of range, or friendly (possible in case duel end)
            if (victim == null || !victim.IsTargetableForAttack() || !me.IsWithinDistInMap(victim, max_range) || me.IsFriendlyTo(victim) || !me.CanSeeOrDetect(victim))
            {
                float extraSearchRadius = max_range > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
                var u_check = new NearestAttackableUnitInObjectRangeCheck(me, me.CharmerOrOwnerOrSelf, max_range);
                var checker = new UnitLastSearcher(me, u_check, GridType.All);
                Cell.VisitGrid(me, checker, max_range + extraSearchRadius);
                victim = checker.GetTarget();
            }

            // If have target
            if (victim != null)
            {
                // remember
                _victimGuid = victim.GUID;

                // attack
                me.CastSpell(victim, me.ToTotem().GetSpell());
            }
            else
                _victimGuid.Clear();
        }

        public override void AttackStart(Unit victim) { }
    }
}
