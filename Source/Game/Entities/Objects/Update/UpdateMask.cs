﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq;

namespace Game.Entities;

public class UpdateMask
{
	readonly int _blockCount;
	readonly int _blocksMaskCount;
	readonly uint[] _blocks;
	readonly uint[] _blocksMask;

	public bool this[int index]
	{
		get { return (_blocks[index / 32] & (1 << (index % 32))) != 0; }
	}

	public UpdateMask(int bits, uint[] input = null)
	{
		_blockCount = (bits + 31) / 32;
		_blocksMaskCount = (_blockCount + 31) / 32;

		_blocks = new uint[_blockCount];
		_blocksMask = new uint[_blocksMaskCount];

		if (input != null)
		{
			var block = 0;

			for (; block < input.Length; ++block)
				if ((_blocks[block] = input[block]) != 0)
					_blocksMask[GetBlockIndex(block)] |= (uint)GetBlockFlag(block);

			for (; block < _blockCount; ++block)
				_blocks[block] = 0;
		}
	}

	public uint GetBlocksMask(uint index)
	{
		return _blocksMask[index];
	}

	public uint GetBlock(uint index)
	{
		return _blocks[index];
	}

	public bool IsAnySet()
	{
		return _blocksMask.Any(blockMask => blockMask != 0);
	}

	public void Reset(int index)
	{
		var blockIndex = GetBlockIndex(index);

		if ((_blocks[blockIndex] &= ~(uint)GetBlockFlag(index)) == 0)
			_blocksMask[GetBlockIndex(blockIndex)] &= ~(uint)GetBlockFlag(blockIndex);
	}

	public void ResetAll()
	{
		Array.Clear(_blocks, 0, _blocks.Length);
		Array.Clear(_blocksMask, 0, _blocksMask.Length);
	}

	public void Set(int index)
	{
		var blockIndex = GetBlockIndex(index);
		_blocks[blockIndex] |= (uint)GetBlockFlag(index);
		_blocksMask[GetBlockIndex(blockIndex)] |= (uint)GetBlockFlag(blockIndex);
	}

	public void SetAll()
	{
		for (var i = 0; i < _blocksMaskCount; ++i)
			_blocksMask[i] = 0xFFFFFFFF;

		for (var i = 0; i < _blockCount; ++i)
			_blocks[i] = 0xFFFFFFFF;

		if ((_blocksMaskCount % 32) != 0)
		{
			var unused = 32 - (_blocksMaskCount % 32);
			_blocksMask[_blocksMaskCount - 1] &= (0xFFFFFFFF >> unused);
		}

		if ((_blockCount % 32) != 0)
		{
			var unused = 32 - (_blockCount % 32);
			_blocks[_blockCount - 1] &= (0xFFFFFFFF >> unused);
		}
	}

	public void AND(UpdateMask right)
	{
		for (var i = 0; i < _blocksMaskCount; ++i)
			_blocksMask[i] &= right._blocksMask[i];

		for (var i = 0; i < _blockCount; ++i)
			if (!Convert.ToBoolean(_blocks[i] &= right._blocks[i]))
				_blocksMask[GetBlockIndex(i)] &= ~(uint)GetBlockFlag(i);
	}

	public void OR(UpdateMask right)
	{
		for (var i = 0; i < _blocksMaskCount; ++i)
			_blocksMask[i] |= right._blocksMask[i];

		for (var i = 0; i < _blockCount; ++i)
			_blocks[i] |= right._blocks[i];
	}

	public static UpdateMask operator &(UpdateMask left, UpdateMask right)
	{
		var result = left;
		result.AND(right);

		return result;
	}

	public static UpdateMask operator |(UpdateMask left, UpdateMask right)
	{
		var result = left;
		result.OR(right);

		return result;
	}

	//helpers
	public static int GetBlockIndex(int bit)
	{
		return bit / 32;
	}

	public static int GetBlockFlag(int bit)
	{
		return 1 << (bit % 32);
	}
}