namespace WechatTest
{
    partial class LoginForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
			this.PictureBox_qr = new System.Windows.Forms.PictureBox();
			this.ComboBox_users = new System.Windows.Forms.ComboBox();
			this.Button_send_msg_txt = new System.Windows.Forms.Button();
			this.TextBox_msg_text = new System.Windows.Forms.TextBox();
			this.Button_send_msg_image = new System.Windows.Forms.Button();
			this.Label_status = new System.Windows.Forms.Label();
			this.Button_refreshGroupMemberInfo = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.PictureBox_qr)).BeginInit();
			this.SuspendLayout();
			// 
			// PictureBox_qr
			// 
			this.PictureBox_qr.Location = new System.Drawing.Point(115, 13);
			this.PictureBox_qr.Margin = new System.Windows.Forms.Padding(4);
			this.PictureBox_qr.Name = "PictureBox_qr";
			this.PictureBox_qr.Size = new System.Drawing.Size(313, 301);
			this.PictureBox_qr.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.PictureBox_qr.TabIndex = 0;
			this.PictureBox_qr.TabStop = false;
			// 
			// ComboBox_users
			// 
			this.ComboBox_users.FormattingEnabled = true;
			this.ComboBox_users.Location = new System.Drawing.Point(32, 388);
			this.ComboBox_users.Margin = new System.Windows.Forms.Padding(4);
			this.ComboBox_users.Name = "ComboBox_users";
			this.ComboBox_users.Size = new System.Drawing.Size(485, 23);
			this.ComboBox_users.TabIndex = 1;
			this.ComboBox_users.SelectedIndexChanged += new System.EventHandler(this.ComboBox_users_SelectedIndexChanged);
			// 
			// Button_send_msg_txt
			// 
			this.Button_send_msg_txt.Location = new System.Drawing.Point(204, 440);
			this.Button_send_msg_txt.Margin = new System.Windows.Forms.Padding(4);
			this.Button_send_msg_txt.Name = "Button_send_msg_txt";
			this.Button_send_msg_txt.Size = new System.Drawing.Size(132, 29);
			this.Button_send_msg_txt.TabIndex = 2;
			this.Button_send_msg_txt.Text = "发送文字消息";
			this.Button_send_msg_txt.UseVisualStyleBackColor = true;
			this.Button_send_msg_txt.Click += new System.EventHandler(this.Button_send_msg_txt_Click);
			// 
			// TextBox_msg_text
			// 
			this.TextBox_msg_text.Location = new System.Drawing.Point(32, 441);
			this.TextBox_msg_text.Margin = new System.Windows.Forms.Padding(4);
			this.TextBox_msg_text.Name = "TextBox_msg_text";
			this.TextBox_msg_text.Size = new System.Drawing.Size(163, 25);
			this.TextBox_msg_text.TabIndex = 3;
			// 
			// Button_send_msg_image
			// 
			this.Button_send_msg_image.Location = new System.Drawing.Point(344, 440);
			this.Button_send_msg_image.Margin = new System.Windows.Forms.Padding(4);
			this.Button_send_msg_image.Name = "Button_send_msg_image";
			this.Button_send_msg_image.Size = new System.Drawing.Size(159, 29);
			this.Button_send_msg_image.TabIndex = 4;
			this.Button_send_msg_image.Text = "发送图片消息";
			this.Button_send_msg_image.UseVisualStyleBackColor = true;
			this.Button_send_msg_image.Click += new System.EventHandler(this.Button_send_msg_image_Click);
			// 
			// Label_status
			// 
			this.Label_status.Location = new System.Drawing.Point(29, 318);
			this.Label_status.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.Label_status.Name = "Label_status";
			this.Label_status.Size = new System.Drawing.Size(467, 66);
			this.Label_status.TabIndex = 5;
			this.Label_status.Text = "扫描二维码登陆微信";
			this.Label_status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// Button_refreshGroupMemberInfo
			// 
			this.Button_refreshGroupMemberInfo.Location = new System.Drawing.Point(32, 476);
			this.Button_refreshGroupMemberInfo.Margin = new System.Windows.Forms.Padding(4);
			this.Button_refreshGroupMemberInfo.Name = "Button_refreshGroupMemberInfo";
			this.Button_refreshGroupMemberInfo.Size = new System.Drawing.Size(143, 29);
			this.Button_refreshGroupMemberInfo.TabIndex = 6;
			this.Button_refreshGroupMemberInfo.Text = "设置备注名";
			this.Button_refreshGroupMemberInfo.UseVisualStyleBackColor = true;
			this.Button_refreshGroupMemberInfo.Click += new System.EventHandler(this.Button_refreshGroupMemberInfo_Click);
			// 
			// LoginForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(548, 576);
			this.Controls.Add(this.Button_refreshGroupMemberInfo);
			this.Controls.Add(this.Label_status);
			this.Controls.Add(this.Button_send_msg_image);
			this.Controls.Add(this.TextBox_msg_text);
			this.Controls.Add(this.Button_send_msg_txt);
			this.Controls.Add(this.ComboBox_users);
			this.Controls.Add(this.PictureBox_qr);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "LoginForm";
			this.Text = "WeChat";
			this.Load += new System.EventHandler(this.Form1_Load);
			((System.ComponentModel.ISupportInitialize)(this.PictureBox_qr)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox PictureBox_qr;
        private System.Windows.Forms.ComboBox ComboBox_users;
        private System.Windows.Forms.Button Button_send_msg_txt;
        private System.Windows.Forms.TextBox TextBox_msg_text;
        private System.Windows.Forms.Button Button_send_msg_image;
        private System.Windows.Forms.Label Label_status;
        private System.Windows.Forms.Button Button_refreshGroupMemberInfo;
    }
}

