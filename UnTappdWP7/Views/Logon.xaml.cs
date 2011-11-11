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

namespace UnTappdWP7.Views
{
    public partial class Logon : PhoneApplicationPage
    {
        public Logon()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MainPage.UserName = UserNameText.Text;
            MainPage.Password = PasswordText.Password;
            MainPage.ApiId = ApiKeyText.Text;

            // Test the viability
            UntappdSvc svc = new UntappdSvc(MainPage.ApiId, MainPage.UserName, MainPage.Password);
            svc.SvcResponseHandler += new SvcResponseEventHandler(svc_SvcResponseHandler);
            svc.MyPendingFriends();

        }

        void svc_SvcResponseHandler(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // didn't work
                button1.Content = "Try Again";
            }
            else
            {
                NavigationService.GoBack();
            }
        }
    }
}