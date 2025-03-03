// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World.NpcInnkeeper;

internal struct SpellIds
{
	public const uint TrickOrTreated = 24755;
	public const uint Treat = 24715;
}

internal struct Gossip
{
	public const uint MenuId = 9733;
	public const uint MenuEventId = 342;
}

[Script]
internal class npc_innkeeper : ScriptedAI
{
	public npc_innkeeper(Creature creature) : base(creature) { }

	public override bool OnGossipHello(Player player)
	{
		player.InitGossipMenu(Gossip.MenuId);

		if (Global.GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd) &&
			!player.HasAura(SpellIds.TrickOrTreated))
			player.AddGossipItem(Gossip.MenuEventId, 0, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);

		if (Me.IsQuestGiver)
			player.PrepareQuestMenu(Me.GUID);

		if (Me.IsVendor)
			player.AddGossipItem(Gossip.MenuId, 2, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_TRADE);

		if (Me.IsInnkeeper)
			player.AddGossipItem(Gossip.MenuId, 1, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INN);

		player.TalkedToCreature(Me.Entry, Me.GUID);
		player.SendGossipMenu(player.GetGossipTextId(Me), Me.GUID);

		return true;
	}

	public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
	{
		var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
		player.ClearGossipMenu();

		if (action == GossipAction.GOSSIP_ACTION_INFO_DEF + 1 &&
			Global.GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd) &&
			!player.HasAura(SpellIds.TrickOrTreated))
		{
			player.CastSpell(player, SpellIds.TrickOrTreated, true);

			if (RandomHelper.IRand(0, 1) != 0)
			{
				player.CastSpell(player, SpellIds.Treat, true);
			}
			else
			{
				uint trickspell = 0;

				switch (RandomHelper.IRand(0, 13))
				{
					case 0:
						trickspell = 24753;

						break; // cannot cast, random 30sec
					case 1:
						trickspell = 24713;

						break; // lepper gnome costume
					case 2:
						trickspell = 24735;

						break; // male ghost costume
					case 3:
						trickspell = 24736;

						break; // female ghostcostume
					case 4:
						trickspell = 24710;

						break; // male ninja costume
					case 5:
						trickspell = 24711;

						break; // female ninja costume
					case 6:
						trickspell = 24708;

						break; // male pirate costume
					case 7:
						trickspell = 24709;

						break; // female pirate costume
					case 8:
						trickspell = 24723;

						break; // skeleton costume
					case 9:
						trickspell = 24753;

						break; // Trick
					case 10:
						trickspell = 24924;

						break; // Hallow's End Candy
					case 11:
						trickspell = 24925;

						break; // Hallow's End Candy
					case 12:
						trickspell = 24926;

						break; // Hallow's End Candy
					case 13:
						trickspell = 24927;

						break; // Hallow's End Candy
				}

				player.CastSpell(player, trickspell, true);
			}

			player.CloseGossipMenu();

			return true;
		}

		player.CloseGossipMenu();

		switch (action)
		{
			case GossipAction.GOSSIP_ACTION_TRADE:
				player.Session.SendListInventory(Me.GUID);

				break;
			case GossipAction.GOSSIP_ACTION_INN:
				player.SetBindPoint(Me.GUID);

				break;
		}

		return true;
	}
}