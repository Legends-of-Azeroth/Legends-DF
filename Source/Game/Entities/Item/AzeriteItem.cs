﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities
{
    public class AzeriteItem : Item
    {
        private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            private readonly AzeriteItemData _azeriteItemMask = new();
            private readonly ItemData _itemMask = new();
            private readonly ObjectFieldData _objectMask = new();
            private readonly AzeriteItem _owner;

            public ValuesUpdateForPlayerWithMaskSender(AzeriteItem owner)
            {
                _owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(_owner.GetMapId());

                _owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _itemMask.GetUpdateMask(), _azeriteItemMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }

        public AzeriteItem()
        {
            AzeriteItemData = new AzeriteItemData();

            ObjectTypeMask |= TypeMask.AzeriteItem;
            ObjectTypeId = TypeId.AzeriteItem;

            SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.DEBUGknowledgeWeek), -1);
        }

        public AzeriteItemData AzeriteItemData { get; set; }

        public override bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
        {
            if (!base.Create(guidlow, itemId, context, owner))
                return false;

            SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Level), 1u);
            SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.KnowledgeLevel), GetCurrentKnowledgeLevel());
            UnlockDefaultMilestones();

            return true;
        }

        public override void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            switch (GetState())
            {
                case ItemUpdateState.New:
                case ItemUpdateState.Changed:
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, AzeriteItemData.Xp);
                    stmt.AddValue(2, AzeriteItemData.Level);
                    stmt.AddValue(3, AzeriteItemData.KnowledgeLevel);

                    int specIndex = 0;

                    for (; specIndex < AzeriteItemData.SelectedEssences.Size(); ++specIndex)
                    {
                        stmt.AddValue(4 + specIndex * 5, AzeriteItemData.SelectedEssences[specIndex].SpecializationID);

                        for (int j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                            stmt.AddValue(5 + specIndex * 5 + j, AzeriteItemData.SelectedEssences[specIndex].AzeriteEssenceID[j]);
                    }

                    for (; specIndex < 4; ++specIndex)
                    {
                        stmt.AddValue(4 + specIndex * 5, 0);

                        for (int j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                            stmt.AddValue(5 + specIndex * 5 + j, 0);
                    }

                    trans.Append(stmt);

                    foreach (uint azeriteItemMilestonePowerId in AzeriteItemData.UnlockedEssenceMilestones)
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, azeriteItemMilestonePowerId);
                        trans.Append(stmt);
                    }

                    foreach (var azeriteEssence in AzeriteItemData.UnlockedEssences)
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, azeriteEssence.AzeriteEssenceID);
                        stmt.AddValue(2, azeriteEssence.Rank);
                        trans.Append(stmt);
                    }

                    break;
            }

            base.SaveToDB(trans);
        }

        public void LoadAzeriteItemData(Player owner, AzeriteData azeriteData)
        {
            bool needSave = false;

            if (!CliDB.AzeriteLevelInfoStorage.ContainsKey(azeriteData.Level))
            {
                azeriteData.Xp = 0;
                azeriteData.Level = 1;
                azeriteData.KnowledgeLevel = GetCurrentKnowledgeLevel();
                needSave = true;
            }
            else if (azeriteData.Level > PlayerConst.MaxAzeriteItemLevel)
            {
                azeriteData.Xp = 0;
                azeriteData.Level = PlayerConst.MaxAzeriteItemLevel;
                needSave = true;
            }

            if (azeriteData.KnowledgeLevel != GetCurrentKnowledgeLevel())
            {
                // rescale XP to maintain same progress %
                ulong oldMax = CalcTotalXPToNextLevel(azeriteData.Level, azeriteData.KnowledgeLevel);
                azeriteData.KnowledgeLevel = GetCurrentKnowledgeLevel();
                ulong newMax = CalcTotalXPToNextLevel(azeriteData.Level, azeriteData.KnowledgeLevel);
                azeriteData.Xp = (ulong)(azeriteData.Xp / (double)oldMax * newMax);
                needSave = true;
            }
            else if (azeriteData.KnowledgeLevel > PlayerConst.MaxAzeriteItemKnowledgeLevel)
            {
                azeriteData.KnowledgeLevel = PlayerConst.MaxAzeriteItemKnowledgeLevel;
                needSave = true;
            }

            SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Xp), azeriteData.Xp);
            SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Level), azeriteData.Level);
            SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.KnowledgeLevel), azeriteData.KnowledgeLevel);

            foreach (uint azeriteItemMilestonePowerId in azeriteData.AzeriteItemMilestonePowers)
                AddUnlockedEssenceMilestone(azeriteItemMilestonePowerId);

            UnlockDefaultMilestones();

            foreach (AzeriteEssencePowerRecord unlockedAzeriteEssence in azeriteData.UnlockedAzeriteEssences)
                SetEssenceRank((uint)unlockedAzeriteEssence.AzeriteEssenceID, unlockedAzeriteEssence.Tier);

            foreach (AzeriteItemSelectedEssencesData selectedEssenceData in azeriteData.SelectedAzeriteEssences)
            {
                if (selectedEssenceData.SpecializationId == 0)
                    continue;

                var selectedEssences = new SelectedAzeriteEssences();
                selectedEssences.ModifyValue(selectedEssences.SpecializationID).SetValue(selectedEssenceData.SpecializationId);

                for (int i = 0; i < SharedConst.MaxAzeriteEssenceSlot; ++i)
                {
                    // Check if essence was unlocked
                    if (GetEssenceRank(selectedEssenceData.AzeriteEssenceId[i]) == 0)
                        continue;

                    selectedEssences.ModifyValue(selectedEssences.AzeriteEssenceID, i) = selectedEssenceData.AzeriteEssenceId[i];
                }

                if (owner != null &&
                    owner.GetPrimarySpecialization() == selectedEssenceData.SpecializationId)
                    selectedEssences.ModifyValue(selectedEssences.Enabled).SetValue(true);

                AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences), selectedEssences);
            }

            // add selected essences for current spec
            if (owner != null &&
                GetSelectedAzeriteEssences() == null)
                CreateSelectedAzeriteEssences(owner.GetPrimarySpecialization());

            if (needSave)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_ON_LOAD);
                stmt.AddValue(0, azeriteData.Xp);
                stmt.AddValue(1, azeriteData.KnowledgeLevel);
                stmt.AddValue(2, GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public override void DeleteFromDB(SQLTransaction trans)
        {
            DeleteFromDB(trans, GetGUID().GetCounter());
            base.DeleteFromDB(trans);
        }

        public static new void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public uint GetLevel()
        {
            return AzeriteItemData.Level;
        }

        public uint GetEffectiveLevel()
        {
            uint level = AzeriteItemData.AuraLevel;

            if (level == 0)
                level = AzeriteItemData.Level;

            return level;
        }

        public void GiveXP(ulong xp)
        {
            Player owner = GetOwner();
            uint level = AzeriteItemData.Level;

            if (level < PlayerConst.MaxAzeriteItemLevel)
            {
                ulong currentXP = AzeriteItemData.Xp;
                ulong remainingXP = xp;

                do
                {
                    ulong totalXp = CalcTotalXPToNextLevel(level, AzeriteItemData.KnowledgeLevel);

                    if (currentXP + remainingXP >= totalXp)
                    {
                        // advance to next level
                        ++level;
                        remainingXP -= totalXp - currentXP;
                        currentXP = 0;
                    }
                    else
                    {
                        currentXP += remainingXP;
                        remainingXP = 0;
                    }
                } while (remainingXP > 0 && level < PlayerConst.MaxAzeriteItemLevel);

                SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Xp), currentXP);

                owner.UpdateCriteria(CriteriaType.EarnArtifactXPForAzeriteItem, xp);

                // changing azerite level changes Item level, need to update Stats
                if (AzeriteItemData.Level != level)
                {
                    if (IsEquipped())
                        owner._ApplyItemBonuses(this, GetSlot(), false);

                    SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Level), level);
                    UnlockDefaultMilestones();
                    owner.UpdateCriteria(CriteriaType.AzeriteLevelReached, level);

                    if (IsEquipped())
                        owner._ApplyItemBonuses(this, GetSlot(), true);
                }

                SetState(ItemUpdateState.Changed, owner);
            }

            PlayerAzeriteItemGains xpGain = new();
            xpGain.ItemGUID = GetGUID();
            xpGain.XP = xp;
            owner.SendPacket(xpGain);
        }

        public static GameObject FindHeartForge(Player owner)
        {
            GameObject forge = owner.FindNearestGameObjectOfType(GameObjectTypes.ItemForge, 40.0f);

            if (forge != null)
                if (forge.GetGoInfo().ItemForge.ForgeType == 2)
                    return forge;

            return null;
        }

        public bool CanUseEssences()
        {
            PlayerConditionRecord condition = CliDB.PlayerConditionStorage.LookupByKey(PlayerConst.PlayerConditionIdUnlockedAzeriteEssences);

            if (condition != null)
                return ConditionManager.IsPlayerMeetingCondition(GetOwner(), condition);

            return false;
        }

        public bool HasUnlockedEssenceSlot(byte slot)
        {
            AzeriteItemMilestonePowerRecord milestone = Global.DB2Mgr.GetAzeriteItemMilestonePower(slot);

            return AzeriteItemData.UnlockedEssenceMilestones.FindIndex(milestone.Id) != -1;
        }

        public bool HasUnlockedEssenceMilestone(uint azeriteItemMilestonePowerId)
        {
            return AzeriteItemData.UnlockedEssenceMilestones.FindIndex(azeriteItemMilestonePowerId) != -1;
        }

        public void AddUnlockedEssenceMilestone(uint azeriteItemMilestonePowerId)
        {
            AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssenceMilestones), azeriteItemMilestonePowerId);
        }

        public uint GetEssenceRank(uint azeriteEssenceId)
        {
            int index = AzeriteItemData.UnlockedEssences.FindIndexIf(essence => { return essence.AzeriteEssenceID == azeriteEssenceId; });

            if (index < 0)
                return 0;

            return AzeriteItemData.UnlockedEssences[index].Rank;
        }

        public void SetEssenceRank(uint azeriteEssenceId, uint rank)
        {
            int index = AzeriteItemData.UnlockedEssences.FindIndexIf(essence => { return essence.AzeriteEssenceID == azeriteEssenceId; });

            if (rank == 0 &&
                index >= 0)
            {
                RemoveDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssences), index);

                return;
            }

            if (Global.DB2Mgr.GetAzeriteEssencePower(azeriteEssenceId, rank) == null)
                return;

            if (index < 0)
            {
                UnlockedAzeriteEssence unlockedEssence = new();
                unlockedEssence.AzeriteEssenceID = azeriteEssenceId;
                unlockedEssence.Rank = rank;
                AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssences), unlockedEssence);
            }
            else
            {
                UnlockedAzeriteEssence actorField = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssences, index);
                SetUpdateFieldValue(ref actorField.Rank, rank);
            }
        }

        public SelectedAzeriteEssences GetSelectedAzeriteEssences()
        {
            foreach (SelectedAzeriteEssences essences in AzeriteItemData.SelectedEssences)
                if (essences.Enabled)
                    return essences;

            return null;
        }

        public void CreateSelectedAzeriteEssences(uint specializationId)
        {
            SelectedAzeriteEssences selectedEssences = new();
            selectedEssences.ModifyValue(selectedEssences.SpecializationID).SetValue(specializationId);
            selectedEssences.ModifyValue(selectedEssences.Enabled).SetValue(true);
            AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences), selectedEssences);
        }

        public void SetSelectedAzeriteEssences(uint specializationId)
        {
            int index = AzeriteItemData.SelectedEssences.FindIndexIf(essences => { return essences.Enabled; });

            if (index >= 0)
            {
                SelectedAzeriteEssences selectedEssences = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences, index);
                SetUpdateFieldValue(selectedEssences.ModifyValue(selectedEssences.Enabled), false);
            }

            index = AzeriteItemData.SelectedEssences.FindIndexIf(essences => { return essences.SpecializationID == specializationId; });

            if (index >= 0)
            {
                SelectedAzeriteEssences selectedEssences = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences, index);
                SetUpdateFieldValue(selectedEssences.ModifyValue(selectedEssences.Enabled), true);
            }
            else
            {
                CreateSelectedAzeriteEssences(specializationId);
            }
        }

        public void SetSelectedAzeriteEssence(int slot, uint azeriteEssenceId)
        {
            //ASSERT(Slot < MAX_AZERITE_ESSENCE_SLOT);
            int index = AzeriteItemData.SelectedEssences.FindIndexIf(essences => { return essences.Enabled; });
            //ASSERT(index >= 0);
            SelectedAzeriteEssences selectedEssences = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences, index);
            SetUpdateFieldValue(ref selectedEssences.ModifyValue(selectedEssences.AzeriteEssenceID, slot), azeriteEssenceId);
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            ObjectData.WriteCreate(buffer, flags, this, target);
            _itemData.WriteCreate(buffer, flags, this, target);
            AzeriteItemData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            if (Values.HasChanged(TypeId.Object))
                ObjectData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.Item))
                _itemData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.AzeriteItem))
                AzeriteItemData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt32(Values.GetChangedObjectTypeMask());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new(14);
            valuesMask.Set((int)TypeId.Item);
            valuesMask.Set((int)TypeId.AzeriteItem);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            UpdateMask mask = new(40);
            _itemData.AppendAllowedFieldsMaskForFlag(mask, flags);
            _itemData.WriteUpdate(buffer, mask, true, this, target);

            UpdateMask mask2 = new(9);
            AzeriteItemData.AppendAllowedFieldsMaskForFlag(mask2, flags);
            AzeriteItemData.WriteUpdate(buffer, mask2, true, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void ClearUpdateMask(bool remove)
        {
            Values.ClearChangesMask(AzeriteItemData);
            base.ClearUpdateMask(remove);
        }

        private uint GetCurrentKnowledgeLevel()
        {
            // Count weeks from 14.01.2020
            DateTime now = GameTime.GetDateAndTime();
            DateTime beginDate = new(2020, 1, 14);
            uint knowledge = 0;

            while (beginDate < now && knowledge < PlayerConst.MaxAzeriteItemKnowledgeLevel)
            {
                ++knowledge;
                beginDate.AddDays(7);
            }

            return knowledge;
        }

        private ulong CalcTotalXPToNextLevel(uint level, uint knowledgeLevel)
        {
            AzeriteLevelInfoRecord levelInfo = CliDB.AzeriteLevelInfoStorage.LookupByKey(level);
            ulong totalXp = levelInfo.BaseExperienceToNextLevel * (ulong)CliDB.AzeriteKnowledgeMultiplierStorage.LookupByKey(knowledgeLevel).Multiplier;

            return Math.Max(totalXp, levelInfo.MinimumExperienceToNextLevel);
        }

        private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedAzeriteItemMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);

            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            _itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);

            if (requestedItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.Item);

            AzeriteItemData.FilterDisallowedFieldsMaskForFlag(requestedAzeriteItemMask, flags);

            if (requestedAzeriteItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.AzeriteItem);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Item])
                _itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

            if (valuesMask[(int)TypeId.AzeriteItem])
                AzeriteItemData.WriteUpdate(buffer, requestedAzeriteItemMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        private void UnlockDefaultMilestones()
        {
            bool hasPreviousMilestone = true;

            foreach (AzeriteItemMilestonePowerRecord milestone in Global.DB2Mgr.GetAzeriteItemMilestonePowers())
            {
                if (!hasPreviousMilestone)
                    break;

                if (milestone.RequiredLevel > GetLevel())
                    break;

                if (HasUnlockedEssenceMilestone(milestone.Id))
                    continue;

                if (milestone.AutoUnlock != 0)
                {
                    AddUnlockedEssenceMilestone(milestone.Id);
                    hasPreviousMilestone = true;
                }
                else
                {
                    hasPreviousMilestone = false;
                }
            }
        }
    }
}