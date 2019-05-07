/*
 * Created by SharpDevelop.
 * User: allen
 * Date: 05/06/2019
 * Time: 09:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Net;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Net.Sockets;

namespace bxc_bound
{
	/// <summary>
	/// Description of BXCNode.
	/// </summary>
	public class BXCNode
	{
		public api_discovery Discovery;
		public api_status Status;
		public api_version Version;
		public bool IsOK;
		public bool IsSelected;
		public bool IsBoundOK;
		
		public BXCNode(string ip, TimeSpan timeout)
		{
			this.IsOK = false;
			this.IsSelected = false;
			this.IsBoundOK = false;
			
			try
			{
				if(BXCNode.IsPortOpen(ip, 9017, timeout))
				{
					WebClient wc = new WebClient();
					this.Discovery = new JavaScriptSerializer().Deserialize<api_discovery>(wc.DownloadString("http://" + ip + ":9017/discovery"));
					this.Status = new JavaScriptSerializer().Deserialize<api_status>(wc.DownloadString("http://" + ip + ":9017/status"));
					this.Version = new JavaScriptSerializer().Deserialize<api_version>(wc.DownloadString("http://" + ip + ":9017/version"));
					if(this.Discovery.cksum == "BonusCloud")
					{
						this.IsOK = true;
					}
				}
			}
			catch
			{
			}
		}
		
		public SolidColorBrush ToBrush()
		{
			if(this.IsOK)
			{
				if(this.Status.status.bound)
				{
					return Brushes.Green;
				}
				else
				{
					return Brushes.Black;
				}
			}
			else
			{
				return Brushes.Black;
			}
		}
		
		public override string ToString()
		{
			string process = "";
			if(this.Status.status.bound && !this.Status.status.process)
			{
				process = "            节点离线";
			}
			return this.Discovery.ip + process + System.Environment.NewLine + this.Discovery.mac + "            " + this.Version.version ;
		}
		
		public class api_discovery
		{
			public string cksum { get; set; }
			public string ip { get; set; }
			public string mac { get; set; }
		}
		
		public class key_status
		{
			public bool bound { get; set; }
			public bool process { get; set; }
		}
		
		public class api_status
		{
			public key_status status { get; set; }
		}
		
		public class api_version
		{
			public string version { get; set; }
		}
		
		static bool IsPortOpen(string host, int port, TimeSpan timeout)
		{
			try
			{
				using(var client = new TcpClient())
				{
					var result = client.BeginConnect(host, port, null, null);
					var success = result.AsyncWaitHandle.WaitOne(timeout);
					if (!success)
					{
						return false;
					}

					client.EndConnect(result);
				}

			}
			catch
			{
				return false;
			}
			return true;
		}
		
	}
}
