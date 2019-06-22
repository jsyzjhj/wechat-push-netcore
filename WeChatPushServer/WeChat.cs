using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WeChatPushServer
{
    public class WeChat
    {
        /// <summary>
        /// 公众号的全局唯一接口调用凭据
        /// https://mp.weixin.qq.com/wiki?t=resource/res_main&id=mp1421140183
        /// </summary>
        public static string access_token { get; set; } = "";
        /// <summary>
        /// access_token的过期时间
        /// </summary>
        public static DateTime expires_in { get; set; }

        private string appId, appSecret, templateId, token;
        public WeChat(string appid, string appsecret, string templateid, string token)
        {
            this.appId = appid;
            this.appSecret = appsecret;
            this.templateId = templateid;
            this.token = token;
        }
        /// <summary>
        /// 校验签名
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="nonce"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public bool CheckSignature(string timestamp, string nonce, string signature)
        {
            var list = new List<string>() { token, timestamp, nonce };
            //参数排序
            list.Sort();
            var par = new StringBuilder();
            list.ForEach(x => par.Append(x));
            var parBytes = Encoding.UTF8.GetBytes(par.ToString());
            SHA1 sha1 = SHA1.Create();
            var hash = new StringBuilder();
            foreach (var item in sha1.ComputeHash(parBytes))
            {
                hash.Append(item.ToString("x2"));
            }
            return hash.ToString() == signature;
        }

        /// <summary>
        /// 获取AccessToken
        /// </summary>
        /// <returns></returns>
        private bool GetAccessToken()
        {
            try
            {
                var url = $"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={appId}&secret={appSecret}";
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = httpClient.GetAsync(url).Result;
                    response.EnsureSuccessStatusCode();
                    var content = response.Content.ReadAsStringAsync().Result;
                    var result = JObject.Parse(content);
                    if (result["access_token"] !=null)
                    {
                        access_token = result["access_token"].ToString();
                        expires_in=DateTime.Now.AddSeconds( Convert.ToInt32(result["expires_in"]));
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
               
            }
            catch (Exception)
            {
                return false;
            }

        }

        /// <summary>
        /// 发送模板通知
        /// </summary>
        /// <param name="openid">接收用户的ID</param>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <param name="dateTime">时间</param>
        /// <param name="openUrl">点击打开链接</param>
        /// <returns></returns>
        public WechatResult SendTemplateMessage(string openid,string title,string content,DateTime dateTime,string openUrl="")
        {
            //检查access_token
            if (access_token==""||expires_in<=DateTime.Now)
            {
                if (!GetAccessToken())
                {
                    return new WechatResult() {
                        errcode=-999,
                        errmsg="设置access_token失败"
                    };
                }
            }
            try
            {
                var url = $"https://api.weixin.qq.com/cgi-bin/message/template/send?access_token={access_token}";
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,url);
                    var body = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        touser = openid,
                        template_id = templateId,
                        url = openUrl,
                        data = new
                        {
                            title = new
                            {
                                value = title
                            },
                            date = new
                            {
                                value = dateTime.ToString("yyyy-MM-dd HH:mm:ss")
                            },
                            content = new
                            {
                                value = content
                            }
                        }
                    });
                    HttpContent httpContent = new StringContent(body);
                    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var response = httpClient.PostAsync(url, httpContent).Result;
                    response.EnsureSuccessStatusCode();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<WechatResult>(response.Content.ReadAsStringAsync().Result);
                }

            }
            catch (Exception)
            {
                return new WechatResult() {
                    errcode=-998,
                    errmsg="发送出现错误"
                };
            }
        }


        public WeChatMessage HandleMessage(string xml)
        {

            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            return new WeChatMessage()
            {
                MsgType = document.SelectSingleNode("//MsgType")?.InnerText,
                Content = document.SelectSingleNode("//Content")?.InnerText,
                CreateTime = document.SelectSingleNode("//CreateTime")?.InnerText,
                FromUserName = document.SelectSingleNode("//FromUserName")?.InnerText,
                MsgId =document.SelectSingleNode("//MsgId")?.InnerText,
                ToUserName = document.SelectSingleNode("//ToUserName")?.InnerText,
                Event= document.SelectSingleNode("//Event")?.InnerText,
            };

        }
        public string MessageToXml(WeChatMessage data)
        {
          return $@"<xml>
<ToUserName><![CDATA[{data.ToUserName}]]></ToUserName>
<FromUserName><![CDATA[{data.FromUserName}]]></FromUserName>
<CreateTime>{DateTimeOffset.Now.ToUnixTimeSeconds()}</CreateTime>
<MsgType><![CDATA[text]]></MsgType>
<Content><![CDATA[{data.Content}]]></Content>
</xml>";

        }


    }

    /// <summary>
    /// 接收微信消息类型
    /// 只处理text类型
    /// https://mp.weixin.qq.com/wiki?t=resource/res_main&id=mp1421140453
    /// </summary>
    public class WeChatMessage
    {
        /// <summary>
        /// 开发者微信号
        /// </summary>
        public string ToUserName { get; set; }
        /// <summary>
        /// 发送方帐号（一个OpenID）
        /// </summary>
        public string FromUserName { get; set; }
        /// <summary>
        ///消息创建时间
        /// </summary>
        public string CreateTime { get; set; }
        /// <summary>
        /// 消息类型，文本为text,事件event
        /// </summary>
        public string MsgType { get; set; }
        /// <summary>
        /// 文本消息内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 消息id，64位整型
        /// </summary>
        public string MsgId { get; set; }
        /// <summary>
        /// 事件类型，subscribe(订阅)、unsubscribe(取消订阅)
        /// </summary>
        public string Event { get; set; }
    }

    public class WechatResult
    {
        public int errcode { get; set; }
        public string errmsg { get; set; }
        public string msgid { get; set; }
    }


}
