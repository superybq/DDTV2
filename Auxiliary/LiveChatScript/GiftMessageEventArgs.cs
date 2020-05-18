﻿//using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Auxiliary.LiveChatScript
{
    public class SendGiftEventArgs : MessageEventArgs
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public int GuardLevel { get; set; }

        public string GiftAction { get; set; }

        public int GiftId { get; set; }

        public int GiftType { get; set; }

        public string GiftName { get; set; }

        public float GiftPrice { get; set; }

        public int TotalCoin { get; set; }

        public int Amount { get; set; }

        public bool IsGoldGift { get; set; }

        public string CoinType { get; set; }

        public long Timestamp { get; set; }

        public string AvatarUrl { get; set; }

        internal SendGiftEventArgs(JObject obj) : base(obj)
        {
            UserId = (int)obj["data"]["uid"];
            UserName = (string)obj["data"]["uname"];
            TotalCoin = (int)obj["data"]["total_coin"];
            Amount = (int)obj["data"]["num"];
            GiftName = (string)obj["data"]["giftName"];
            GiftId = (int)obj["data"]["giftId"];
            GiftType = (int)obj["data"]["giftType"];
            GiftPrice = (float)obj["data"]["price"];
            GuardLevel = (int)obj["data"]["guard_level"];
            GiftAction = (string)obj["data"]["action"];
            CoinType = (string)obj["data"]["coin_type"];
            IsGoldGift = (string)obj["data"]["coin_type"] == "gold";
            Timestamp = (long)obj["data"]["timestamp"];
            AvatarUrl = (string)obj["data"]["face"];
        }
    }

    public class GuardBuyEventArgs : MessageEventArgs
    {
        /// <summary>
        /// 舰长等级：3-舰长 2-提督 1-总督
        /// </summary>
        public int GuardLevel { get; set; }

        public string GiftName { get; set; }

        /// <summary>
        /// 花费：单位金瓜子
        /// </summary>
        public int Price { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        //public string GiftId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Number { get; set; }

        internal GuardBuyEventArgs(JObject obj) : base(obj)
        {
            GuardLevel = (int)obj["data"]["guard_level"];
            UserId = (int)obj["data"]["uid"];
            UserName = (string)obj["data"]["username"];
            GiftName = (string)obj["data"]["gift_name"];
            Price = (int)obj["data"]["price"];
            Number = (int)obj["data"]["num"];
            //GiftId = obj["data"]["gift_id"].Value<string>();//舰长没有
        }

    }
}
