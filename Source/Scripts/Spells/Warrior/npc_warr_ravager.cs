﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Warrior
{
	[CreatureScript(76168)] // Ravager - 76168
	public class npc_warr_ravager : ScriptedAI
	{
		public npc_warr_ravager(Creature creature) : base(creature)
		{
		}


		public const uint RAVAGER_DISPLAYID = 55644;
		public const uint RAVAGER_VISUAL = 153709;

		public override void IsSummonedBy(WorldObject summoner)
		{
			me.SetDisplayId(RAVAGER_DISPLAYID);
			me.CastSpell(me, RAVAGER_VISUAL, true);
			me.SetReactState(ReactStates.Passive);
			me.AddUnitState(UnitState.Root);
			me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.CanSwim | UnitFlags.PlayerControlled);

			if (summoner == null || !summoner.IsPlayer())
				return;

			var player = summoner.ToPlayer();

			if (player != null)
			{
				var item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

				if (item != null)
				{
					var l_Proto = Global.ObjectMgr.GetItemTemplate(item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

					if (l_Proto != null)
						me.SetVirtualItem(0, l_Proto.GetId());
				}
				else
				{
					me.SetVirtualItem(0, item.GetTemplate().GetId());
				}

				item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

				if (item != null)
				{
					var l_Proto = Global.ObjectMgr.GetItemTemplate(item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

					if (l_Proto != null)
						me.SetVirtualItem(2, l_Proto.GetId());
				}
				else
				{
					me.SetVirtualItem(2, item.GetTemplate().GetId());
				}
			}
		}
	}
}