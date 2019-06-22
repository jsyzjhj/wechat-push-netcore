using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WeChatPushServer.Models;

namespace WeChatPushServer
{
    [ApiController]
    public class WebApi : ControllerBase
    {
        private readonly AppSettings appSettings;
        private readonly WeChat weChat;
        private readonly WePushContext dbContext;
        public WebApi(IOptionsSnapshot<AppSettings> settings, WePushContext pushContext)
        {
            this.appSettings = settings.Value;
            this.dbContext = pushContext;
            this.weChat = new WeChat(
                appid: appSettings.WeChat.AppID,
                appsecret: appSettings.WeChat.AppSecret,
                templateid: appSettings.WeChat.TemplateID,
                token: appSettings.WeChat.Token);
        }
        [HttpGet]
        [Route("api/hello")]
        public JsonResult Hello()
        {
            return new JsonResult(new ApiModel()
            {
                msg = "Hello,World!"
            });
        }

        /// <summary>
        /// 接收公众号的消息
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="echostr"></param>
        /// <param name="timestamp"></param>
        /// <param name="nonce"></param>
        /// <param name="openid"></param>
        /// <returns></returns>
        [Route("api/wx")]
        public IActionResult ReceiveMessages(string signature, string echostr, string timestamp, string nonce, string openid)
        {
            //校验signature
            if (!weChat.CheckSignature(timestamp, nonce, signature))
            {
                return new JsonResult(new ApiModel
                {
                    code = -111,
                    msg = "签名校验错误"
                });
            }
            //绑定接口URL时Token验证
            if (echostr != null)
            {
                return new ContentResult()
                {
                    Content = echostr
                };
            }

            //处理消息
            if (Request.Body != null)
            {
                using (var reader = new StreamReader(Request.Body))
                {
                    var xml = reader.ReadToEnd();
                    var message = weChat.HandleMessage(xml);

                    var reply = new WeChatMessage()
                    {
                        FromUserName = message.ToUserName,
                        ToUserName = message.FromUserName,
                        MsgType = "text"
                    };
                    //关注公众号返回Apikey
                    //if (message.MsgType == "event")
                    //{
                    //    if (message.Event== "subscribe")
                    //    {
                    //        reply.Content = CreateApiKey(message.FromUserName);
                    //    }
                    //    else if(message.Event== "unsubscribe")
                    //    {
                    //        RemoveApiKey(message.FromUserName);
                    //        reply.Content = "已取消绑定";
                    //    }
                    //}

                    if (message.MsgType == "text")
                    {

                        switch (message.Content)
                        {
                            case "绑定":
                                reply.Content = $"APIKEY:{CreateApiKey(message.FromUserName)}";
                                break;
                            case "取消绑定":
                                RemoveApiKey(message.FromUserName);
                                reply.Content = "已取消绑定，APIKEY已经失效";
                                break;
                            default:
                                
                                reply.Content = "未知命令\n回复'绑定'获取一个APIKEY\n回复'取消绑定'移除已绑定的APIKEY";
                                break;
                        }
                        return new ContentResult()
                        {
                            Content = weChat.MessageToXml(reply)
                        };
                    }
                    else
                    {
                        reply.Content = "只支持文本消息";
                        return new ContentResult()
                        {
                            Content = weChat.MessageToXml(reply)
                        };

                    }
                }
            }
            else
            {
                return new ContentResult()
                {
                    Content = ""
                };
            }
        }
        /// <summary>
        /// 创建一个apikey
        /// </summary>
        /// <param name="openid"></param>
        /// <returns></returns>
        private string CreateApiKey(string openid)
        {
            var data = dbContext.UserApiKeys.FirstOrDefault(x => x.OpenId == openid);
            if (data != null)
            {
                return data.ApiKey;
            }
            else
            {
                var userkey = new UserApiKey()
                {
                    //用guid随机生成一个
                    ApiKey = Guid.NewGuid().ToString().Replace("-", ""),
                    OpenId = openid
                };
                dbContext.UserApiKeys.Add(userkey);
                dbContext.SaveChanges();
                return userkey.ApiKey;
            }
        }
        /// <summary>
        /// 移除apikey
        /// </summary>
        /// <param name="openid"></param>
        private void RemoveApiKey(string openid)
        {
            var data = dbContext.UserApiKeys.FirstOrDefault(x => x.OpenId == openid);
            if (data != null)
            {
                dbContext.UserApiKeys.Remove(data);
                dbContext.SaveChanges();
            }

        }
        /// <summary>
        /// 发送通知
        /// </summary>
        /// <param name="apikey">获取的apikey</param>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <returns></returns>
        [Route("api/Send")]
        public JsonResult Send(string apikey, string title, string content)
        {
            if (apikey == null || title == null || content == null || apikey.Length == 0 || title.Length == 0 || content.Length == 0)
            {
                return new JsonResult(new ApiModel()
                {
                    code = -101,
                    msg = "参数有误"
                });
            }
            //校验APIKEY
            var apikeyInfo = dbContext.UserApiKeys.FirstOrDefault(x => x.ApiKey == apikey);
            if (apikeyInfo == null)
            {
                return new JsonResult(new ApiModel()
                {
                    code = -102,
                    msg = "ApiKey校验失败,到公众号回复'绑定'获取一个apikey"
                });
            }
            //保存消息
            var message = new Message() {
                ApiKey=apikey,
                Content=content,
                SendTime=DateTime.Now,
                Title=title
            };
            dbContext.Messages.Add(message);
            dbContext.SaveChanges();
            //发送模板通知
            var url= $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/message/detail?id={message.Id}";
            var data = weChat.SendTemplateMessage(apikeyInfo.OpenId, title, content, message.SendTime, url);

            return new JsonResult(new ApiModel
            {
                code= data.errcode,
                msg = data.errmsg
            });
        }

        /// <summary>
        /// 通知详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("message/detail")]
        public IActionResult MessageDetail(int id)
        {
           
            ContentResult content = new ContentResult();
            content.ContentType = "text/html";
            var data = dbContext.Messages.FirstOrDefault(x => x.Id == id);
            if (data==null)
            {
                return NotFound();
            }
            content.Content = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>消息详情</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0, user-scalable=0"">
</head>
<body>
<h2>{data.Title}</h3>
<p>时间: {data.SendTime}</p>
<p>内容: {data.Content}</p>
</body>
</html>
";
            return content;
        }


    }


}
