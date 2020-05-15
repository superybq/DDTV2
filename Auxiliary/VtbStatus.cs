using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Auxiliary
{
    public static class VtbStatus
    {
        private static Dictionary<string, VtbStatus.VtbStatusInfo> vtbStatusInfoDict = new Dictionary<string, VtbStatusInfo>();
        private static Dictionary<string, DateTime> expireTimeDict = new Dictionary<string, DateTime>();

        public static VtbStatus.VtbStatusInfo GetVtbStatus(string mid)
        {
            if (!vtbStatusInfoDict.ContainsKey(mid) || expireTimeDict[mid] < DateTime.Now)
            {
                VtbStatus.VtbStatusInfo info =
                    JsonConvert.DeserializeObject<VtbStatus.VtbStatusInfo>(
                        MMPU.返回网页内容_GET($"https://api.vtbs.moe/v1/detail/{mid}"));
                DateTime expireTime = DateTime.Now.AddMilliseconds(30000);
                vtbStatusInfoDict[mid] = info;
                expireTimeDict[mid] = expireTime;
                return info;
            }
            else
            {
                return vtbStatusInfoDict[mid];
            }
        }

        public class LastLive
        {
            public string online { get; set; }
            public string time { get; set; }
        }


        public class VtbStatusInfo
        {
            public long mid { get; set; }
            public string uuid { get; set; }
            public string uname { get; set; }
            public int video { get; set; }
            public string roomid { get; set; }
            public string sign { get; set; }
            public string notice { get; set; }
            public string face { get; set; }
            public string rise { get; set; }
            public string topPhoto { get; set; }
            public string archiveView { get; set; }
            public string follower { get; set; }
            public int liveStatus { get; set; }
            public string recordNum { get; set; }
            public string guardNum { get; set; }
            public LastLive lastLive { get; set; }
            public string guardChange { get; set; }
            public List<int> guardType { get; set; }
            public string areaRank { get; set; }
            public string online { get; set; }
            public string title { get; set; }
            public string time { get; set; }

        }
    }
}
