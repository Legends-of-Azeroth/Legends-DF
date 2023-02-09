﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellObjectAreaTargetSelect : ITargetHookHandler
    {
        void FilterTargets(List<WorldObject> targets);
    }

    public class ObjectAreaTargetSelectHandler : TargetHookHandler, ISpellObjectAreaTargetSelect
    {
        public delegate void SpellObjectAreaTargetSelectFnType(List<WorldObject> targets);

        private readonly SpellObjectAreaTargetSelectFnType _func;


        public ObjectAreaTargetSelectHandler(SpellObjectAreaTargetSelectFnType func, int effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.ObjectAreaTargetSelect) : base(effectIndex, targetType, true, hookType)
        {
            _func = func;
        }

        public void FilterTargets(List<WorldObject> targets)
        {
            _func(targets);
        }
    }
}