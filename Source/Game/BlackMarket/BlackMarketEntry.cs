﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Entities;

namespace Game.BlackMarket
{

    public class BlackMarketEntry
    {
        private ulong _bidder;
        private ulong _currentBid;
        private bool _mailSent;

        private uint _marketId;
        private uint _numBids;
        private uint _secondsRemaining;

        public void Initialize(uint marketId, uint duration)
        {
            _marketId = marketId;
            _secondsRemaining = duration;
        }

        public void Update(long newTimeOfUpdate)
        {
            _secondsRemaining = (uint)(_secondsRemaining - (newTimeOfUpdate - Global.BlackMarketMgr.GetLastUpdate()));
        }

        public BlackMarketTemplate GetTemplate()
        {
            return Global.BlackMarketMgr.GetTemplateByID(_marketId);
        }

        public uint GetSecondsRemaining()
        {
            return (uint)(_secondsRemaining - (GameTime.GetGameTime() - Global.BlackMarketMgr.GetLastUpdate()));
        }

        private long GetExpirationTime()
        {
            return GameTime.GetGameTime() + GetSecondsRemaining();
        }

        public bool IsCompleted()
        {
            return GetSecondsRemaining() <= 0;
        }

        public bool LoadFromDB(SQLFields fields)
        {
            _marketId = fields.Read<uint>(0);

            // Invalid MarketID
            BlackMarketTemplate templ = Global.BlackMarketMgr.GetTemplateByID(_marketId);

            if (templ == null)
            {
                Log.outError(LogFilter.Misc, "Black market auction {0} does not have a valid Id.", _marketId);

                return false;
            }

            _currentBid = fields.Read<ulong>(1);
            _secondsRemaining = (uint)(fields.Read<long>(2) - Global.BlackMarketMgr.GetLastUpdate());
            _numBids = fields.Read<uint>(3);
            _bidder = fields.Read<ulong>(4);

            // Either no bidder or existing player
            if (_bidder != 0 &&
                Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, _bidder)) == 0) // Probably a better way to check if player exists
            {
                Log.outError(LogFilter.Misc, "Black market auction {0} does not have a valid bidder (GUID: {1}).", _marketId, _bidder);

                return false;
            }

            return true;
        }

        public void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_BLACKMARKET_AUCTIONS);

            stmt.AddValue(0, _marketId);
            stmt.AddValue(1, _currentBid);
            stmt.AddValue(2, GetExpirationTime());
            stmt.AddValue(3, _numBids);
            stmt.AddValue(4, _bidder);

            trans.Append(stmt);
        }

        public void DeleteFromDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_BLACKMARKET_AUCTIONS);
            stmt.AddValue(0, _marketId);
            trans.Append(stmt);
        }

        public bool ValidateBid(ulong bid)
        {
            if (bid <= _currentBid)
                return false;

            if (bid < _currentBid + GetMinIncrement())
                return false;

            if (bid >= BlackMarketConst.MaxBid)
                return false;

            return true;
        }

        public void PlaceBid(ulong bid, Player player, SQLTransaction trans) //Updated
        {
            if (bid < _currentBid)
                return;

            _currentBid = bid;
            ++_numBids;

            if (GetSecondsRemaining() < 30 * Time.Minute)
                _secondsRemaining += 30 * Time.Minute;

            _bidder = player.GetGUID().GetCounter();

            player.ModifyMoney(-(long)bid);


            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_BLACKMARKET_AUCTIONS);

            stmt.AddValue(0, _currentBid);
            stmt.AddValue(1, GetExpirationTime());
            stmt.AddValue(2, _numBids);
            stmt.AddValue(3, _bidder);
            stmt.AddValue(4, _marketId);

            trans.Append(stmt);

            Global.BlackMarketMgr.Update(true);
        }

        public string BuildAuctionMailSubject(BMAHMailAuctionAnswers response)
        {
            return GetTemplate().Item.ItemID + ":0:" + response + ':' + GetMarketId() + ':' + GetTemplate().Quantity;
        }

        public string BuildAuctionMailBody()
        {
            return GetTemplate().SellerNPC + ":" + _currentBid;
        }


        public uint GetMarketId()
        {
            return _marketId;
        }

        public ulong GetCurrentBid()
        {
            return _currentBid;
        }

        private void SetCurrentBid(ulong bid)
        {
            _currentBid = bid;
        }

        public uint GetNumBids()
        {
            return _numBids;
        }

        private void SetNumBids(uint numBids)
        {
            _numBids = numBids;
        }

        public ulong GetBidder()
        {
            return _bidder;
        }

        private void SetBidder(ulong bidder)
        {
            _bidder = bidder;
        }

        public ulong GetMinIncrement()
        {
            return (_currentBid / 20) - ((_currentBid / 20) % MoneyConstants.Gold);
        } //5% increase every bid (has to be round gold value)

        public void MailSent()
        {
            _mailSent = true;
        } // Set when mail has been sent

        public bool GetMailSent()
        {
            return _mailSent;
        }
    }
}