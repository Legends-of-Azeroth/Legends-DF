﻿using Game.Networking;

namespace Game.Entities;

public class ActivePlayerUnk901 : BaseUpdateData<Player>
{
	public UpdateField<ObjectGuid> Field_0 = new(0, 1);
	public UpdateField<int> Field_10 = new(0, 2);

	public ActivePlayerUnk901() : base(3) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WritePackedGuid(Field_0);
		data.WriteInt32(Field_10);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		UpdateMask changesMask = ChangesMask;
		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 3);

		data.FlushBits();
		if (changesMask[0])
		{
			if (changesMask[1])
			{
				data.WritePackedGuid(Field_0);
			}
			if (changesMask[2])
			{
				data.WriteInt32(Field_10);
			}
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Field_0);
		ClearChangesMask(Field_10);
		ChangesMask.ResetAll();
	}
}