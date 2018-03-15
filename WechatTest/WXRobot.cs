using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wechat;
namespace WechatTest
{
	class WXRobot
	{
		private WeChatClient wxServices;
		private bool init=false;
		private string xiaobinId;
		public WXRobot(WeChatClient wxServices)
		{
			this.wxServices = wxServices;
		}
		public  void Init(object sender,AddUserEvent e)
		{
			if(e.User.Signature== "我是人工智能微软小冰~~")
			{
				init = true;
				xiaobinId = e.User.UserName;
				Log("初始化成功");
			}
		}
		int notInitExceptionCount=5;
		private Queue<string> lastUserName=new Queue<string>();
		public  void MessageRecived(object sender,RecvMessageEvent e)
		{
			if (!init)
			{
				if (notInitExceptionCount++ > 5)
				{
					Log("小冰插件未加载成功");
					notInitExceptionCount = 0;
				}
				return;
			}
			if (e.Msg.FromUserName == xiaobinId)
				SendMsgToUser(e);
			else
			{
				if (e.Msg.FromUserName.Contains("@@")) {
					//Console.WriteLine("");
					return;
				}//屏蔽群消息
				SendMsgToXiaobin(e);
			}
		}

		int noUserRemainExceptionCount = 5;

		private void SendMsgToUser(RecvMessageEvent e)
		{
			if(lastUserName.Count == 0)
			{
				if (noUserRemainExceptionCount++ > 5)
				{
					Log("已无用户可回复");
					noUserRemainExceptionCount = 0;
				}
				return;
			}
			string user = lastUserName.Dequeue();
			if(e.Msg.MsgType!=1)
				wxServices.SendMsg(user, "[小冰自动回复]收到其他类型的消息" + e.Msg.MsgType,(x)=> { });
			else
				wxServices.SendMsg(user, "[小冰自动回复]" + e.Msg.Content, (x) => { });
		}
		private void SendMsgToXiaobin(RecvMessageEvent e)
		{
			if (e.Msg.MsgType != 1) return;
			lastUserName.Enqueue(e.Msg.FromUserName);
			wxServices.SendMsg(xiaobinId, e.Msg.Content, (x) => { });
		}
		private void Log(string message)
		{
			Console.WriteLine(message);
		}
	}
}
