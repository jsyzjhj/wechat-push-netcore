using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeChatPushServer.Models
{
    public class AppSettings
    {
        public WeChatConfig WeChat { get; set; }
    }
    public class WeChatConfig
    {
        public string AppID { get; set; }
        public string AppSecret { get; set; }
        public string Token { get; set; }
        public string TemplateID { get; set; }
    }
}
