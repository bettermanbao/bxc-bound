/*
 * Created by SharpDevelop.
 * User: allen
 * Date: 2019/4/29
 * Time: 8:30
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
using System.IO;
using System.Net;
using System.Linq;

namespace bxc_bound
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();
		}
		void button1_Click(object sender, RoutedEventArgs e)
		{
			try{
				this.txb_result.Text = "";
				WebRequest request = WebRequest.Create("http://" + this.txb_ip.Text + ":9017/bound");
				request.Method = "POST";
				
				string postData = "{\"bcode\":\""  + this.txb_bcode.Text + "\",\"email\":\"" + this.txb_email.Text + "\"}";
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
					this.txb_result.Text = JsonHelper.FormatJson(responseFromServer);
				}

				response.Close();
			}
			catch(WebException wex)
			{
				if( wex.Response != null )
				{
					this.txb_result.Text = JsonHelper.FormatJson(new StreamReader(wex.Response.GetResponseStream()).ReadToEnd());
				}
				else
				{
					this.txb_result.Text = wex.Message;
				}
			}
			catch(Exception ex)
			{
				this.txb_result.Text = ex.Message;
			}
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