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
using System.IO;
using System.IO.IsolatedStorage;


namespace UnTappdWP7
{
    public partial class MainPage : PhoneApplicationPage
    {
        public static string ApiId {
            get
            {
                return GetKeyValue<string>("ApiId");
            }
            set
            {
                SetKeyValue("ApiId", value);
            }
        }
        public static string UserName
        {
            get
            {
                return GetKeyValue<string>("UserName");
            }
            set
            {
                SetKeyValue("UserName", value);
            }
        }
        public static string Password
        {
            get
            {
                return GetKeyValue<string>("Password");
            }
            set
            {
                SetKeyValue("Password", value);
            }
        }
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (string.IsNullOrEmpty(ApiId) || string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
            {
                NavigationService.Navigate(
                    new Uri("/Views/Logon.xaml", UriKind.Relative));
            }
        }

        private static T GetKeyValue<T>(string key)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains(key))
                return (T)IsolatedStorageSettings.ApplicationSettings[key];
            else
                return default(T);
        }

        private static void SetKeyValue<T>(string key, T value)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains(key))
                IsolatedStorageSettings.ApplicationSettings[key] = value;
            else
                IsolatedStorageSettings.ApplicationSettings.Add(key, value);

            IsolatedStorageSettings.ApplicationSettings.Save();
        }
    }
}