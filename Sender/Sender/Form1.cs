using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Text.RegularExpressions;

namespace Sender
{
	public partial class Form1 : Form
	{
		private TcpListener listener;
		private const int port=8623;
		private List<TcpClient> connectedClients;
		private bool commandMode;

		public Form1()
		{
			InitializeComponent();
			connectedClients=new List<TcpClient>();
			listener=new TcpListener(IPAddress.Parse("0.0.0.0"),port);
			listener.Start();
			commandMode=false;
		}

		private void Form1_Load(object sender,EventArgs e)
		{
			var unicastAddresses=from i in NetworkInterface.GetAllNetworkInterfaces()
								 where i.GetIPProperties().GatewayAddresses.Count>0
								 select i.GetIPProperties().UnicastAddresses;
			var localAddressBytes=new List<byte[]>();
			foreach(UnicastIPAddressInformationCollection ipList in unicastAddresses)
				foreach(UnicastIPAddressInformation info in ipList)
					localAddressBytes.Add(info.Address.GetAddressBytes());
			var localAddress=from bytes in localAddressBytes
							 where
							 bytes[0]==10||
							 (bytes[0]==172&&bytes[1]>=16&&bytes[1]<=31)||
							 (bytes[0]==192&&bytes[1]==168)
							 select bytes;
			if(localAddress.Count()==0){
				MessageBox.Show("ネットワークに接続していません。","",MessageBoxButtons.OK,MessageBoxIcon.Error);
				Close();
			}
			var addressString=string.Join(".",localAddress.ToList()[0]);
			this.Text=this.Text+"@"+addressString;
			timer1.Enabled=true;
		}

		private void button1_Click(object sender,EventArgs e)
		{
			textBox1.Enabled=false;
			Publish(textBox1.Text);
			textBox1.Text="";
			textBox1.Enabled=true;
			textBox1.Focus();
		}

		private void Form1_FormClosing(object sender,FormClosingEventArgs e)
		{
			foreach(TcpClient client in connectedClients) client.Close();
			timer1.Enabled=false;
			listener.Stop();
		}

		private void timer1_Tick(object sender,EventArgs e)
		{
			if(listener.Pending()){
				var client=listener.AcceptTcpClient();
				connectedClients.Add(client);
				var greeting=Encoding.UTF8.GetBytes("Hello.");
				client.GetStream().Write(greeting,0,greeting.Length);
			}
		}

		private void Form1_DragEnter(object sender,DragEventArgs e)
		{
			var checkResults=from format in new[]{DataFormats.Text,DataFormats.UnicodeText,DataFormats.OemText,DataFormats.FileDrop}
							 select e.Data.GetDataPresent(format);
			e.Effect=checkResults.Any(result=>result)?DragDropEffects.All:DragDropEffects.None;
		}

		private void Form1_DragDrop(object sender,DragEventArgs e)
		{
			if(e.Data.GetDataPresent(typeof(string))){
				textBox1.Text=e.Data.GetData(typeof(string)) as string;
			}else if(e.Data.GetDataPresent(DataFormats.FileDrop)){
				textBox1.Text=(e.Data.GetData(DataFormats.FileDrop) as string[])[0];
			}
			button1_Click(null,null);
		}

		private void Publish(string text)
		{
			if(text!=""&&!text.All(c=>c==' '||c=='\t')){
				if(commandMode) text=ParseCommand(text);
				var data=Encoding.UTF8.GetBytes(text);
				for(int i=0;i<connectedClients.Count;i++)
					if(connectedClients[i].Connected){
						try{
							connectedClients[i].GetStream().Write(data,0,data.Length);
						}catch(Exception){
							connectedClients[i].Close();
							connectedClients[i]=null;
						}
					}else{
						connectedClients[i].Close();
						connectedClients[i]=null;
					}
				connectedClients.RemoveAll(client=>client==null);
			}
			if(commandMode){
				this.Text=this.Text.Replace("(cmd)","");
				commandMode=false;
			}
			return;
		}

		private string ParseCommand(string text)
		{
			int index=0;
			for(;text[index]==' '||text[index]=='\t';index++) continue;
			int start=index;
			var delimiter=text[start]=='\"'?'\"':' ';
			for(index++;text[index]!=delimiter;index++) continue;
			index+=(delimiter=='\"'?1:0);
			var file=text.Substring(start,index-start);
			for(index++;index<text.Length&&(text[index]==' '||text[index]=='\t');index++) continue;
			string args="";
			if(index<text.Length){
				var str=text.Substring(index,text.Length-index);
				if(!str.All(c=>c==' '||c=='\t')) args=str;
			}
			return file+"\n"+args;
		}

		private void Form1_KeyDown(object sender,KeyEventArgs e)
		{
			if(e.Control&&e.KeyCode==Keys.E){
				if(!commandMode) this.Text=this.Text+"(cmd)";
				commandMode=true;
			}
		}
	}
}
