using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wechat;

namespace WechatTest
{
	class WxMsgHdl
	{
		private WeChatClient wxServices;
		private bool init;

		public WeChatClient WxServices { get => wxServices; set => wxServices = value; }
		public bool Init { get => init; set => init = value; }

		public virtual void MsgRecived(object sender, RecvMessageEvent e)
		{

		}
		public virtual void RobotInit()
		{

		}
		public virtual void SendMsg(RecvMessageEvent e)
		{

		}
	}
}
