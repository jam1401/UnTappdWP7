using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using WPUntappd.service;
using System.IO;
using System.Text;

namespace UnTappdWP7.Views.Feeds
{
    public partial class Pub : PhoneApplicationPage
    {
        public Pub()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            UntappdSvc svc = new UntappdSvc(MainPage.ApiId, MainPage.UserName, MainPage.Password);
            svc.SvcResponseHandler += new SvcResponseEventHandler(svc_SvcResponseHandler);
            svc.PublicFeed();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ContentPanel.Visibility = System.Windows.Visibility.Visible;
            ContentPanel2.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void svc_SvcResponseHandler(object sender, OpenReadCompletedEventArgs e)
        {
            // Debug case just send to the Console
            if (e.Error == null)
            {
                ContentPanel.Visibility = System.Windows.Visibility.Collapsed;
                ContentPanel2.Visibility = System.Windows.Visibility.Visible;
                Console.WriteLine("In svc_SvcResponseHandler Good Result");
                StreamReader reader = new StreamReader(e.Result);
                var sb = new StringBuilder();
                
                string str = reader.ReadLine();
                while (str != null)
                {
                    sb.AppendLine(str);
                    str = reader.ReadLine();
                }

                Output.Text = sb.ToString();
            }
            else
            {
                Console.WriteLine("svc_SvcResponseHandler Error");
                Console.WriteLine(e.Error.Message);
                try
                {
                    Console.WriteLine(e.Result);
                }
                catch (WebException ex)
                {
                    Console.WriteLine("Exception from remote host HTTP Status is " + ex.Status.ToString());
                    WebResponse resp = ex.Response;

                    StreamReader reader = new StreamReader(resp.GetResponseStream());
                    string str = reader.ReadLine();
                    while (str != null)
                    {
                        Console.WriteLine(str);
                        str = reader.ReadLine();
                    }
                }
            }
        }

    }
}