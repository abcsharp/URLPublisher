using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;

namespace Reciever
{
	public partial class Form1 : Form
	{
		private TcpClient client;
		private const int port=8623;

		public Form1()
		{
			InitializeComponent();
		}

		private void timer1_Tick(object sender,EventArgs e)
		{
			if(client.Connected&&client.Available>0){
				var buffer=new byte[client.Available];
				client.GetStream().Read(buffer,0,buffer.Length);
				var message=Encoding.UTF8.GetString(buffer);
				if(message=="Hello."){
					MessageBox.Show("接続に成功しました。","",MessageBoxButtons.OK,MessageBoxIcon.Information);
				}else{
					Process.Start(message);
				}
			}
		}

		private void button1_Click(object sender,EventArgs e)
		{
			button1.Enabled=false;
			maskedTextBox1.Enabled=false;
			switch(button1.Text){
			case "接続":
				client=new TcpClient();
				client.ReceiveTimeout=5000;
				client.SendTimeout=5000;
				try{
					client.Connect(maskedTextBox1.Text.Replace(" ",""),port);
				}catch(Exception){
					MessageBox.Show("接続に失敗しました。","",MessageBoxButtons.OK,MessageBoxIcon.Error);
					client.Close();
				}
				maskedTextBox1.Enabled=!client.Connected;
				timer1.Enabled=client.Connected;
				if(client.Connected) button1.Text="切断";
				break;
			case "切断":
				client.Close();
				maskedTextBox1.Enabled=true;
				timer1.Enabled=false;
				button1.Text="接続";
				break;
			}
			button1.Enabled=true;
		}
	}
}
