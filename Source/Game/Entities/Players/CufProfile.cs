﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections;
using Framework.Constants;

namespace Game.Entities;

public class CufProfile
{
	public string ProfileName { get; set; }
	public ushort FrameHeight { get; set; }
	public ushort FrameWidth { get; set; }
	public byte SortBy { get; set; }
	public byte HealthText { get; set; }

	// LeftAlign, TopAlight, BottomAlign
	public byte TopPoint { get; set; }
	public byte BottomPoint { get; set; }
	public byte LeftPoint { get; set; }

	// LeftOffset, TopOffset and BottomOffset
	public ushort TopOffset { get; set; }
	public ushort BottomOffset { get; set; }
	public ushort LeftOffset { get; set; }

	public BitSet BoolOptions { get; set; }

	public CufProfile()
	{
		BoolOptions = new BitSet((int)CUFBoolOptions.BoolOptionsCount);
	}

	public CufProfile(string name, ushort frameHeight, ushort frameWidth, byte sortBy, byte healthText, uint boolOptions,
					byte topPoint, byte bottomPoint, byte leftPoint, ushort topOffset, ushort bottomOffset, ushort leftOffset)
	{
		ProfileName = name;

		BoolOptions = new BitSet(new uint[]
		{
			boolOptions
		});

		FrameHeight = frameHeight;
		FrameWidth = frameWidth;
		SortBy = sortBy;
		HealthText = healthText;
		TopPoint = topPoint;
		BottomPoint = bottomPoint;
		LeftPoint = leftPoint;
		TopOffset = topOffset;
		BottomOffset = bottomOffset;
		LeftOffset = leftOffset;
	}

	public void SetOption(CUFBoolOptions opt, byte arg)
	{
		BoolOptions.Set((int)opt, arg != 0);
	}

	public bool GetOption(CUFBoolOptions opt)
	{
		return BoolOptions.Get((int)opt);
	}

	public ulong GetUlongOptionValue()
	{
		var array = new uint[1];
		BoolOptions.CopyTo(array, 0);

		return (ulong)array[0];
	}

	// More fields can be added to BoolOptions without changing DB schema (up to 32, currently 27)
}