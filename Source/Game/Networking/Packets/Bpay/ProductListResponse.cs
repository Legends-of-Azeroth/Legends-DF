﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets.Bpay;

public class ProductListResponse : ServerPacket
{
	public uint Result { get; set; } = 0;
	public uint CurrencyID { get; set; } = 0;
	public List<BpayProductInfo> ProductInfos { get; set; } = new();
	public List<BpayProduct> Products { get; set; } = new();
	public List<BpayGroup> ProductGroups { get; set; } = new();
	public List<BpayShop> Shops { get; set; } = new();

	public ProductListResponse() : base(ServerOpcodes.BattlePayGetProductListResponse) { }

	public override void Write()
	{
		_worldPacket.Write(Result);
		_worldPacket.Write(CurrencyID);
		_worldPacket.WriteUInt32((uint)ProductInfos.Count);
		_worldPacket.WriteUInt32((uint)Products.Count);
		_worldPacket.WriteUInt32((uint)ProductGroups.Count);
		_worldPacket.WriteUInt32((uint)Shops.Count);

		foreach (var p in ProductInfos)
			p.Write(_worldPacket);

		foreach (var p in Products)
			p.Write(_worldPacket);

		foreach (var p in ProductGroups)
			p.Write(_worldPacket);

		foreach (var p in Shops)
			p.Write(_worldPacket);
	}
}