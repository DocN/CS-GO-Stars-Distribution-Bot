using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

namespace SteamBot
{
    public class TradeOfferUserHandler : UserHandler
    {
        private bool isTradeBot;

        public TradeOfferUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnNewTradeOffer(TradeOffer offer)
        {
            string tradeID = offer.TradeOfferId;

            //create condition if bot is listed 
            isTradeBot = checkBotList(offer.PartnerSteamId.ConvertToUInt64().ToString());

            var myItems = offer.Items.GetMyItems();
            var theirItems = offer.Items.GetTheirItems();

            //withdrawal request by admin
            if (IsAdmin && myItems.Count > 0)
            {
                if (offer.Accept())
                {
                    this.reportLog("items sent to admin " + offer.PartnerSteamId);
                }
            }
            //taking in an offer from one of the trade bots
            else if (theirItems.Count > 0 && myItems.Count <= 0 && isTradeBot)
            {
                if (offer.Accept())
                {
                    this.reportLog("accept trade offer " + tradeID);
                    starsDB database = new starsDB();
                    //move items to distro list in DB
                    database.distroRecievedTrade(tradeID);
                }
            }
            //reject all offers not associated to distro process 
            else
            {
                if(offer.Decline()) {
                    this.reportLog("Distro trade request attempted by " + offer.PartnerSteamId + " declined");
                }
            }
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            if (IsAdmin)
            {
            }
        }

        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() { }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeError(string error) { }

        public override void OnTradeTimeout() { }

        public override void OnTradeSuccess() { }

        public override void OnTradeInit() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) { }

        public override void OnTradeAccept() { }

        private bool DummyValidation(List<TradeAsset> myAssets, List<TradeAsset> theirAssets)
        {
            //compare items etc
            if (myAssets.Count == theirAssets.Count)
            {
                return true;
            }
            return false;
        }

        private bool reportLog(string msg)
        {
            Console.WriteLine(msg);
            Log.Success(msg);
            return true;
        }

        //checkBotList stores the list of bots and checks if the bot is on the list when requested to 
        private bool checkBotList(string traderSID)
        {
            bool isBot = false;
            List<string> botList = new List<string>();
            //list of bots
            botList.Add("76561197997042006");

            for (int i = 0; i < botList.Count; i++)
            {
                if (botList[i].Equals(traderSID))
                {
                    isBot = true;
                    return isBot;
                }
            }
            return isBot;
        }
    }
}
