﻿//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using GameDevWare.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;
using JsonException = Newtonsoft.Json.JsonException;

namespace Auxiliary.LiveChatScript
{
    public class LiveChatListener
    {
        private ClientWebSocket m_client;

        public event EventHandler<MessageEventArgs> MessageReceived;

        private readonly byte[] m_ReceiveBuffer;

        private CancellationTokenSource m_innerRts;
        private int TroomId = 0;

        public LiveChatListener()
        {
            m_ReceiveBuffer = new byte[8192*1024];
        }

        public void Connect(int roomId)
        {
            TroomId = roomId;
            ConnectAsync(roomId, null).Wait();
        }

        public async Task ConnectAsync(int roomId, CancellationToken? cancellationToken = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("");
            }

            m_client = new ClientWebSocket();
            m_innerRts = new CancellationTokenSource();
            try
            {
                await m_client.ConnectAsync(new Uri("wss://broadcastlv.chat.bilibili.com/sub"), cancellationToken ?? new CancellationTokenSource(5000).Token);
            }
            catch (Exception e)
            {
                throw e;
            }

            int realRoomId = roomId;//await _getRealRoomId(roomId);

            await _sendObject(7, new
            {
                uid = 0,
                roomid = realRoomId,
                protover = 2,
                platform = "web",
                clientver = "1.7.3"
            });

            _ = _innerLoop().ContinueWith((t) =>
           {
               if (t.IsFaulted)
               {
                   //UnityEngine.Debug.LogError(t.Exception.);
                   if (!m_innerRts.IsCancellationRequested)
                   {
                       MessageReceived(this, new ExceptionEventArgs(t.Exception.InnerException));
                       m_innerRts.Cancel();
                   }
               }
               else
               {
                   //POST-CANCEL
#if DEBUG
                   Console.WriteLine("LiveChatListender cancelled.");
#endif
               }
               m_client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).Wait();
               m_client.Dispose();
               m_innerRts.Dispose();
           });
            _=_innerHeartbeat();
        }

        public void Close()
        {
            m_innerRts.Cancel();
        }

        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
            }
            _disposed = true;
        }

        private async Task _innerLoop()
        {
#if DEBUG
            Console.WriteLine("LiveChatListender start.");
#endif
            while (!m_innerRts.IsCancellationRequested)
            {
                try
                {
                    WebSocketReceiveResult result;
                    int length = 0;
                    do
                    {
                         result = await m_client.ReceiveAsync(
                             new ArraySegment<byte>(m_ReceiveBuffer, length, m_ReceiveBuffer.Length - length), 
                             m_innerRts.Token);
                        length += result.Count;
                    }
                    while(!result.EndOfMessage);

                    //=========fuckbilibili==========
                    DepackDanmakuData(m_ReceiveBuffer);
                    //===============================

                    #region 已失效[弃用]
                    //var segments = _depack(new ArraySegment<byte>(m_ReceiveBuffer, 0, length));
                    //foreach (var segment in segments)
                    //{
                    //    string jsonBody = System.Text.Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
                    //    _parse(jsonBody);
                    //}
                    #endregion
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    continue;
                }
                catch (WebSocketException we)
                {
                    throw we;
                }
                catch (JsonException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    //UnityEngine.Debug.LogException(e);
                    throw e;
                }
            }
        }

        private IEnumerable<ArraySegment<byte>> _depack(ArraySegment<byte> array)
        {
            int ioffset = 0;
            while (ioffset < array.Count)
            {
                int totalPackageCount = BEBitConverter.ToInt32(array.Array, array.Offset + ioffset);
                ushort protocol = BEBitConverter.ToUInt16(array.Array, array.Offset + ioffset + 6);
                int type = BEBitConverter.ToInt32(array.Array, array.Offset + ioffset + 8);
                if (type == 5)
                {
                    ushort headerCount = BEBitConverter.ToUInt16(m_ReceiveBuffer, array.Offset + ioffset + 4);
                    ArraySegment<byte> packageSegment = new ArraySegment<byte>(
                        array.Array,
                        ioffset + headerCount, totalPackageCount - headerCount);
                    if (protocol == 0)
                    {
                        yield return packageSegment;
                    }
                    else if (protocol == 2)
                    {
                        throw new NotSupportedException("Gzip not supported yet.");
                        //_depack(segment.)
                        //foreach (var i in _depack(packageSegment)) yield return i;
                    }
                }
                ioffset += totalPackageCount;
            }
        }

        private void _parse(string jsonBody)
        {
            var obj = JObject.Parse(jsonBody); ///JsonMapper.ToObject(jsonBody);
            //Debug.Log(jsonBody);
            string cmd = (string)obj["cmd"];
            Console.WriteLine(cmd);
            switch (cmd)
            {
                case "DANMU_MSG":
                    MessageReceived(this, new DanmuMessageEventArgs(obj));
                    break;
                case "SEND_GIFT":
                    MessageReceived(this, new SendGiftEventArgs(obj));
                    break;
                case "GUARD_BUY":
                    //Debug.Log("guraddd\n"+obj);
                    MessageReceived(this, new GuardBuyEventArgs(obj));
                    break;
                case "WELCOME":
                    MessageReceived(this, new WelcomeEventArgs(obj));
                    break;
                case "ACTIVITY_BANNER_UPDATE_V2":
                    MessageReceived(this, new ActivityBannerEventArgs(obj));
                    break;
                default:
                    //Debug.Log("Unknow\n"+obj);
                    MessageReceived(this, new MessageEventArgs(obj));
                    break;
            }
        }

        private async Task _innerHeartbeat()
        {
            while (!m_innerRts.IsCancellationRequested)
            {
                try
                {
                    //UnityEngine.Debug.Log("heartbeat");
                    await _sendBinary(2, System.Text.Encoding.UTF8.GetBytes("[object Object]"));
                    await Task.Delay(30 * 1000, m_innerRts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        private async Task _sendBinary(int type, byte[] body)
        {
            byte[] head = new byte[16];
            using (MemoryStream ms = new MemoryStream(head))
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.WriteBE(16 + body.Length);
                    bw.WriteBE((ushort)16);
                    bw.WriteBE((ushort)1);
                    bw.WriteBE(type);
                    bw.WriteBE(1);
                }
            }
            await m_client.SendAsync(new ArraySegment<byte>(head), WebSocketMessageType.Binary, false, CancellationToken.None);
            await m_client.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task _sendObject(int type, object obj)
        {

            //string jsonBody = JsonConvert.SerializeObject(obj, Formatting.None);
            string jsonBody = JsonMapper.ToJson(obj);
            await _sendBinary(type, System.Text.Encoding.UTF8.GetBytes(jsonBody));
        }

        private async Task<int> _getRealRoomId(int roomId)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage ret = await client.PostAsync("https://api.live.bilibili.com/room/v1/Room/room_init?id=" + roomId, new StringContent(""));
                string k = await ret.Content.ReadAsStringAsync();
                //dynamicClass cc = JsonConvert.DeserializeObject<dynamicClass>(k);
                dynamicClass cc = JsonMapper.ToObject<dynamicClass>(k);
                return cc.data.room_id;
            }
        }

        private class dynamicClass
        {
            public class d2
            {
                public int room_id { get; set; }
            }

            public d2 data { get; set; }
        }


        //FUCKBILIBILI
        #region 新协议解析方法

        /// <summary>
        /// 消息协议
        /// </summary>
        public class DanmakuProtocol
        {
            /// <summary>
            /// 消息总长度 (协议头 + 数据长度)
            /// </summary>
            public int PacketLength;
            /// <summary>
            /// 消息头长度 (固定为16[sizeof(DanmakuProtocol)])
            /// </summary>
            public short HeaderLength;
            /// <summary>
            /// 消息版本号
            /// </summary>
            public short Version;
            /// <summary>
            /// 消息类型
            /// </summary>
            public int Operation;
            /// <summary>
            /// 参数, 固定为1
            /// </summary>
            public int Parameter;

            /// <summary>
            /// 转为本机字节序
            /// </summary>
            public DanmakuProtocol(byte[] buff)
            {
                PacketLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buff, 0));
                HeaderLength = IPAddress.HostToNetworkOrder(BitConverter.ToInt16(buff, 4));
                Version = IPAddress.HostToNetworkOrder(BitConverter.ToInt16(buff, 6));
                Operation = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(buff, 8));
                Parameter = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(buff, 12));
            }
        }

        /// <summary>
        /// 消息处理
        /// </summary>
        private void ProcessDanmakuData(int opt, byte[] buffer, int length)
        {
            switch (opt)
            {
                case 3:
                    {
                        if (length == 4)
                        {
                            int 人气值 = buffer[3] + buffer[2] * 255 + buffer[1] * 255 * 255 + buffer[0] * 255 * 255 * 255;
                        }
                        break;
                    }
                case 5:
                    {
                        try
                        {
                            string jsonBody = Encoding.UTF8.GetString(buffer, 0, length);
                            jsonBody = Regex.Unescape(jsonBody);
                            _parse(jsonBody);
                            //Debug.Log(jsonBody);
                            //ReceivedDanmaku?.Invoke(this, new ReceivedDanmakuArgs { Danmaku = new Danmaku(json) });
                        }
                        catch (Exception ex)
                        {
                            if (ex is JsonException || ex is KeyNotFoundException)
                            {
                                //LogEvent?.Invoke(this, new LogEventArgs { Log = $@"[{_roomId}] 弹幕识别错误 {json}" });
                            }
                            else
                            {
                                //LogEvent?.Invoke(this, new LogEventArgs { Log = $@"[{_roomId}] {ex}" });
                            }
                        }
                        break;
                    }
                default:
                    ;
                    break;
            }
        }

        /// <summary>
        /// 消息拆包
        /// </summary>
        private void DepackDanmakuData(byte[] messages)
        {
            var headerBuffer = new byte[16];
            //for (int i = 0; i < 16; i++)
            //{
            //    headerBuffer[i] = messages[i];
            //}
            Array.Copy(messages, 0, headerBuffer, 0, 16);
            var protocol = new DanmakuProtocol(headerBuffer);

            //Debug.LogError(protocol.Version + "\\" + protocol.Operation);
            //
            if (protocol.PacketLength < 16)
            {
                throw new NotSupportedException($@"协议失败: (L:{protocol.PacketLength})");
            }
            var bodyLength = protocol.PacketLength - 16;
            if (bodyLength == 0)
            {
                //continue;
                return;
            }
            var buffer = new byte[bodyLength];
            //for (int i = 0; i < bodyLength; i++)
            //{
            //    buffer[i] = messages[i + 16];
            //}
            Array.Copy(messages, 16, buffer, 0, bodyLength);
            
            switch (protocol.Version)
            {
                case 1:
                    ProcessDanmakuData(protocol.Operation, buffer, bodyLength);
                    break;
                case 2:
                    {
                        var ms = new MemoryStream(buffer, 2, bodyLength - 2);
                        var deflate = new DeflateStream(ms, CompressionMode.Decompress);
                        while (deflate.Read(headerBuffer, 0, 16) > 0)
                        {
                            protocol = new DanmakuProtocol(headerBuffer);
                            bodyLength = protocol.PacketLength - 16;
                            if (bodyLength == 0)
                            {
                                continue; // 没有内容了
                            }
                            if (buffer.Length < bodyLength) // 不够长再申请
                            {
                                buffer = new byte[bodyLength];
                            }
                            deflate.Read(buffer, 0, bodyLength);
                            ProcessDanmakuData(protocol.Operation, buffer, bodyLength);
                        }
                        ms.Dispose();
                        deflate.Dispose();
                        break;
                    }
                default:
                    ;
                    break;
            }
        }
        #endregion
    }
}
