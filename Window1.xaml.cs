/*
 * Created by SharpDevelop.
 * User: allen
 * Date: 2019/5/5
 * Time: 22:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace bxc_bound
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		List<BXCNode> bxcnodelist = new List<BXCNode>();
		string localip;
		TimeSpan scan_timeout;
		string str_email;
		string str_bcode;
		string str_result;
		string[] lst_bcode;
		win_startup startup;
		
		public Window1()
		{
			InitializeComponent();
			lookupNetInterface();
		}
		
		static readonly Regex _regex = new Regex("[^0-9]+");
		
		static bool IsTextAllowed(string text)
		{
			return !_regex.IsMatch(text);
		}
		
		void lookupNetInterface()
		{
			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
				{
					if(!ip.IsDnsEligible)
						continue;

					if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						cbx_ip.Items.Add(ip.Address.ToString());
					}
				}
			}
			cbx_ip.SelectedIndex = this.cbx_ip.Items.Count - 1;
		}

		void btn_scanIP_Click(object sender, RoutedEventArgs e)
		{
			btn_scanIP.IsEnabled = false;
			btn_scanIP.Content = "扫描中...";
			btn_bound.IsEnabled = false;
			lsb_ip.Items.Clear();
			bxcnodelist.Clear();
			
			startup = new win_startup();
			startup.Show();
			
			localip = this.cbx_ip.Text;
			int t;
			try
			{
				t = Convert.ToInt32(this.txb_timeout.Text);
			}
			catch
			{
				this.txb_timeout.Text = "100";
				t = 100;
			}
			scan_timeout = new TimeSpan(0,0,0,0,t);
			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += new DoWorkEventHandler(bw_queryBXCNode);
			bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_queryBXCNode_RunWorkerCompleted);
			bw.RunWorkerAsync();
		}
		
		void queryBXCNode(Object ip)
		{
			BXCNode bxcnode = new BXCNode((string)ip, scan_timeout);
			if(bxcnode.IsOK)
			{
				bxcnodelist.Add(bxcnode);
			}
		}
		
		void bw_queryBXCNode(object sender, DoWorkEventArgs e)
		{
			List<Task> queryTasks = new List<Task>();
			
			IPAddress address;
			if(IPAddress.TryParse(this.localip, out address))
			{
				string ipA = localip.Split('.')[0];
				string ipB = localip.Split('.')[1];
				string ipC = localip.Split('.')[2];
				for(int i=2; i<255; i++)
				{
					queryTasks.Add(Task.Factory.StartNew(queryBXCNode, ipA + "." + ipB + "." + ipC + "." + i));
				}
			}
			Task.WaitAll(queryTasks.ToArray());
		}
		
		void bw_queryBXCNode_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			foreach(BXCNode b in bxcnodelist)
			{
				ListBoxItem lbi_bxcnode = new ListBoxItem();
				lbi_bxcnode.Content = b.ToString();
				lbi_bxcnode.Foreground = b.ToBrush();
				lsb_ip.Items.Add(lbi_bxcnode);
			}
			btn_bound.IsEnabled = true;
			btn_scanIP.Content = "扫描节点";
			btn_scanIP.IsEnabled = true;
			startup.Close();
		}
		
		void btn_bound_Click(object sender, RoutedEventArgs e)
		{
			btn_bound.IsEnabled = false;
			btn_bound.Content = "绑定中...";
			btn_scanIP.IsEnabled = false;
			txb_email.IsEnabled = false;
			txb_bcode.IsEnabled = false;
			txb_result.Text = "";
			str_result = "";
			
			str_email = txb_email.Text;
			str_bcode = txb_bcode.Text;
			
			lst_bcode  = txb_bcode.Text.Split(new[] { System.Environment.NewLine }, StringSplitOptions.None);
			
			for(int i=0, j=0; i<lsb_ip.Items.Count && j<lst_bcode.Length; i++)
			{
				if(bxcnodelist[i].Status.status.bound)
				{
					continue;
				}
				
				if(((ListBoxItem)lsb_ip.Items[i]).IsSelected)
				{
					bxcnodelist[i].IsSelected = true;
					j++;
				}
			}
			
			BackgroundWorker bw = new BackgroundWorker();
			bw.WorkerReportsProgress = true;
			bw.DoWork += new DoWorkEventHandler(bw_bound_bcode);
			bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_bound_bcode_RunWorkerCompleted);
			bw.ProgressChanged += new ProgressChangedEventHandler(bw_bound_bcode_ProgressChanged);
			bw.RunWorkerAsync();
			
		}
		
		void bw_bound_bcode(object sender, DoWorkEventArgs e)
		{
			try{
				BackgroundWorker worker = sender as BackgroundWorker;
				
				for(int i=0, j=0; i<bxcnodelist.Count; i++)
				{
					if(bxcnodelist[i].IsSelected)
					{
						bound_bcode(i, bxcnodelist[i].Discovery.ip, str_email, lst_bcode[j++].Trim());
						worker.ReportProgress(i);
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
		
		void bound_bcode(int bxc_index, string ip, string email, string bcode)
		{
			try{
				WebRequest request = WebRequest.Create("http://" + ip + ":9017/bound");
				request.Method = "POST";
				
				string postData = "{\"bcode\":\""  + bcode + "\",\"email\":\"" + email + "\"}";
				byte[] byteArray = Encoding.UTF8.GetBytes(postData);
				
				request.ContentType = "application/json";
				request.ContentLength = byteArray.Length;
				
				Stream dataStream = request.GetRequestStream();
				dataStream.Write(byteArray, 0, byteArray.Length);
				dataStream.Close();
				
				WebResponse response = request.GetResponse();
				
				using (dataStream = response.GetResponseStream())
				{
					StreamReader reader = new StreamReader(dataStream);
					string responseFromServer = reader.ReadToEnd();
					str_result = ip + " " + responseFromServer;
					bxcnodelist[bxc_index].IsBoundOK = true;
				}

				response.Close();
			}
			catch(WebException wex)
			{
				if( wex.Response != null )
				{
					str_result = ip + " " + new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();
				}
				else
				{
					str_result = ip + " " + wex.Message;
				}
				bxcnodelist[bxc_index].IsBoundOK = false;
			}
			catch(Exception ex)
			{
				str_result = ip + " " + ex.Message;
				bxcnodelist[bxc_index].IsBoundOK = false;
			}
		}
		
		void bw_bound_bcode_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			for(int i=0; i<bxcnodelist.Count; i++)
			{
				if(bxcnodelist[i].IsSelected)
				{
					if(bxcnodelist[i].IsBoundOK)
					{
						((ListBoxItem)lsb_ip.Items[i]).Foreground = Brushes.Green;
					}
					else
					{
						((ListBoxItem)lsb_ip.Items[i]).Foreground = Brushes.Red;
					}
					bxcnodelist[i].IsSelected = false;
				}
			}
			
			lsb_ip.SelectedIndex = -1;
			btn_scanIP.IsEnabled = true;
			txb_email.IsEnabled = true;
			txb_bcode.IsEnabled = true;
			btn_bound.Content = "绑定";
			btn_bound.IsEnabled = true;
		}

		void bw_bound_bcode_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			txb_result.Text += str_result + System.Environment.NewLine;
			str_result = "";
		}
		void window1_Loaded(object sender, RoutedEventArgs e)
		{
			
		}
		void txb_timeout_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsTextAllowed(e.Text);
		}
		
	}
	
	static class JsonHelper
	{
		private const string INDENT_STRING = "    ";
		public static string FormatJson(string str)
		{
			var indent = 0;
			var quoted = false;
			var sb = new StringBuilder();
			for (var i = 0; i < str.Length; i++)
			{
				var ch = str[i];
				switch (ch)
				{
					case '{':
					case '[':
						sb.Append(ch);
						if (!quoted)
						{
							sb.AppendLine();
							Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
						}
						break;
					case '}':
					case ']':
						if (!quoted)
						{
							sb.AppendLine();
							Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
						}
						sb.Append(ch);
						break;
					case '"':
						sb.Append(ch);
						bool escaped = false;
						var index = i;
						while (index > 0 && str[--index] == '\\')
							escaped = !escaped;
						if (!escaped)
							quoted = !quoted;
						break;
					case ',':
						sb.Append(ch);
						if (!quoted)
						{
							sb.AppendLine();
							Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
						}
						break;
					case ':':
						sb.Append(ch);
						if (!quoted)
							sb.Append(" ");
						break;
					default:
						sb.Append(ch);
						break;
				}
			}
			return sb.ToString();
		}
	}

	static class Extensions
	{
		public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
		{
			foreach (var i in ie)
			{
				action(i);
			}
		}
	}
}