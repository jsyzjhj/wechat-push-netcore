## 这是什么

一个用.Net Core简单实现的[Server酱](http://sc.ftqq.com/),通过API发送一个通知到微信上

## 如何使用

1. 首先，你需要一个公众号，如果没有到[这里](https://mp.weixin.qq.com/debug/cgi-bin/sandbox?t=sandbox/login)注册一个测试号

2. 配置一个消息模板，模板内容必须包含title,date,content三个参数

3. 到appsettings.json填写相关信息
```
{
    ......
	"WeChat": {
		//公众号AppID
		"AppID": "",
		//公众号AppSecret
		"AppSecret": "",
		//可随意填写，需要与公众号接口配置信息一致
		"Token": "",
		//消息模板ID
		"TemplateID": ""
	}
}
```

4. 部署程序，访问`https://你的网址/api/hello`看是否正常

5. 回到公众号后台填写接口配置信息，URL为`https://你的网址/api/wx`,Token同上配置文件填写的

6. 关注公众号，发送信息`绑定`到公众号获取一个与openid绑定的APIKEY

7. 配置完成，访问下面API试试吧

## 发送通知API

Url:`https://你的网址/api/send`

方法：`GET` or `POST`

参数:

- `apikey` 必须，获取的APIKEY

- `title` 必须，通知标题

- `content` 必须，通知内容

返回:

```
{
	"code":0,
	"msg":"ok"
}
```
