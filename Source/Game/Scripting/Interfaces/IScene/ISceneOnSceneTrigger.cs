﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Interfaces.IScene
{
    public interface ISceneOnSceneTrigger : IScriptObject
    {
        void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName);
    }
}