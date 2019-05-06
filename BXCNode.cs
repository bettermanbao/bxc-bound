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
		
		public BXCNode(string ip)
		{
			this.IsOK = false;
			this.IsSelected = false;
			this.IsBoundOK = false;
			
			try
			{
				MyWebClient wc = new MyWebClient();
				this.Discovery = new JavaScriptSerializer().Deserialize<api_discovery>(wc.DownloadString("http://" + ip + ":9017/discovery"));
				this.Status = new JavaScriptSerializer().Deserialize<api_status>(wc.DownloadString("http://" + ip + ":9017/status"));
				this.Version = new JavaScriptSerializer().Deserialize<api_version>(wc.DownloadString("http://" + ip + ":9017/version"));
				if(this.Discovery.cksum == "BonusCloud")
				{
					this.IsOK = true;
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
			return this.Discovery.ip + System.Environment.NewLine + this.Discovery.mac + "            " + this.Version.version ;
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
		
		private class MyWebClient : WebClient
		{
			protected override WebRequest GetWebRequest(Uri uri)
			{
				WebRequest w = base.GetWebRequest(uri);
				w.Timeout = 100;
				return w;
			}
		}
		
	}
}
