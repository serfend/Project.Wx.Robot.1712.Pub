using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wechat;
using Wechat.API;

namespace WechatTest
{
	class WXSendMessageWithName:WxMsgHdl
	{
		public override void MsgRecived(object sender, RecvMessageEvent e)
		{
			if (e.Msg.Content.Contains("#SendAll"))
			{
				SendAll(e.Msg.Content.Substring(8));
			}
		}
		private void SendAll(string info)
		{
			foreach(var user in this.WxServices.Contacts)
			{
				string toThisUserInfo = SciMsgInfo(info, user);
				if (MessageBox.Show(user.ToString()+'\n'+ toThisUserInfo, "跳过", MessageBoxButtons.YesNo)==DialogResult.No){
					this.WxServices.SendMsg(user.UserName, toThisUserInfo, (x) => { });
				}
				
			}
		}
		private string SciMsgInfo(string initInfo,User target)
		{
			string tmp = initInfo.Replace("[Nick]" , GetUserNick(target.NickName));
			tmp = tmp.Replace("[Alias]", GetUserNick(target.RemarkName));
			return tmp;
		}
		private string GetUserNick(string initName)
		{
			//var sb = new StringBuilder();
			//foreach(var chr in initName)
			//{
			//	if ( Char.IsDigit(chr))
			//	{

			//	}
			//	else
			//	{
			//		sb.Append(chr);
			//	}
			//}
			return initName;
		}
	}
}
