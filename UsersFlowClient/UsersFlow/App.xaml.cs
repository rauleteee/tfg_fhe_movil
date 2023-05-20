using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using UsersFlow.Interfaces;
using UsersFlow.View;
using Plugin.FirebasePushNotification;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UsersFlow
{
    public partial class App : Application, ILoginManager, INotificationManager
    {
        static ILoginManager loginManager;
        public static App Current;
        public static int val;

        public event EventHandler NotificationReceived;

        public App ()
        {
            InitializeComponent();
            //Logout();
            Current = this;
            // Register Token Handler
            CrossFirebasePushNotification.Current.OnTokenRefresh += Current_OnTokenRefresh;

            var isLoggedIn = Properties.ContainsKey("IsLoggedIn") ? (bool)Properties["IsLoggedIn"] : false;
            if (isLoggedIn)
            {
                MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                MainPage = new NavigationPage(new LoginModalPage(this));
            }
        }
        private void Current_OnTokenRefresh(object source, FirebasePushNotificationTokenEventArgs e)
        {
            Debug.WriteLine($"Token: {e.Token}");
            
        }

        protected override void OnStart ()
        {
        }

        protected override void OnSleep ()
        {
        }

        protected override void OnResume ()
        {
        }

        public void ShowMainPage()
        {
            MainPage = new MainPage();
        }

        public void Logout()
        {
            Properties["IsLoggedIn"] = false;
            MainPage = new NavigationPage(new LoginModalPage(this));
        }

        public void ReceiveNotification(string title, string body)
        {
            throw new NotImplementedException();
        }
    }
}

