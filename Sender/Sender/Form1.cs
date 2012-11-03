using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Sender
{
	public partial class Form1 : Form
	{
		private TcpListener listener;
		private const int port=8623;
		private List<TcpClient> connectedClients;

		public Form1()
		{
			InitializeComponent();
			connectedClients=new List<TcpClient>();
			listener=new TcpListener(IPAddress.Parse("127.0.0.1"),port);
			listener.Start();
		}

		private void Form1_Load(object sender,EventArgs e)
		{
			var ipv4AddressBytes=from ip in Dns.GetHostAddresses(Dns.GetHostName())
								 where ip.GetAddressBytes().Length==4 select ip.GetAddressBytes();
			var localAddressBytes=from bytes in ipv4AddressBytes
								  where
								  bytes[0]==10||
								  (bytes[0]==172&&bytes[1]>=16&&bytes[1]<=31)||
								  (bytes[0]==192&&bytes[1]==168)
								  select bytes;
			var localAddressString=string.Join(".",localAddressBytes.ToList()[0]);
			this.Text=this.Text+"@"+localAddressString;
			timer1.Enabled=true;
		}

		private void button1_Click(object sender,EventArgs e)
		{
			textBox1.Enabled=false;
			Publish(textBox1.Text);
			textBox1.Text="";
			textBox1.Enabled=true;
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

		private void Publish(string url)
		{
			var data=Encoding.UTF8.GetBytes(url);
			for(int i=0;i<connectedClients.Count;i++)
				if(connectedClients[i].Connected){
					try{
						connectedClients[i].GetStream().Write(data,0,data.Length);
					}catch(IOException){
						connectedClients[i].Close();
						connectedClients[i]=null;
					}
				}else{
					connectedClients[i].Close();
					connectedClients[i]=null;
				}
			connectedClients.RemoveAll(client=>client==null);
			return;
		}
	}
}
