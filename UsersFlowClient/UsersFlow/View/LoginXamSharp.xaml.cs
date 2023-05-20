using System;
using System.Collections.Generic;

using Xamarin.Forms;
using UsersFlow.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UsersFlow.Model;
using System.Linq;
using static Xamarin.Forms.Internals.GIFBitmap;
using UsersFlow.ModelView;
using Microsoft.AspNetCore.Identity;
using Plugin.FirebasePushNotification;

namespace UsersFlow.View
{
    public partial class LoginXamSharp : ContentPage
    {
        /** user information */
        public static string username = "";
        public static string password = "";
        /** login information */
        ILoginManager _ilm = null;
        UserModel userReturned;
        // Create an instance of the Argon2PasswordHasher class
        private readonly Argon2PasswordHasher passwordHasher = new Argon2PasswordHasher();
        private bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }
        private string _message;
        public string Message
        {
            get { return _message; }
            set {
                _message = value;
                OnPropertyChanged(); }
        }

        public LoginXamSharp(ILoginManager ilm)
        {
            InitializeComponent();
            _ilm = ilm;
            userReturned = new UserModel(null, null);
            BindingContext = this;
           
        }

        async void Button_ClickedAsync(object sender, EventArgs e)
        {
            Message = "Retrieving data from DB...";
            IsBusy = true;
            spinner.IsVisible = true;
            username = input_username.Text;
            password = input_password.Text;
            Message = "Downloading user's personal information...";
            await Task.Run(async() =>
            {
                
                userReturned = await ApiConnection.GetUserFromDB(username);
            });

            // Verify a password
            PasswordVerificationResult passwordMatches = PasswordVerificationResult.Failed;
            Message = "Verifying credentials...";
            if (userReturned != null)
            {
                await Task.Run(() =>
                {
                    passwordMatches = passwordHasher.VerifyHashedPassword(username, userReturned.password, password);
                });
            }

            
      
            if (userReturned != null)
            {
                    if (userReturned.username == username && passwordMatches == PasswordVerificationResult.Success)
                    {
                        // Session variables
                        App.Current.Properties["username"] = username;
                        App.Current.Properties["Name"] = userReturned.Name;
                        App.Current.Properties["CurrentUser"] = JsonConvert.SerializeObject(userReturned);
                        await App.Current.SavePropertiesAsync();
                        
                        App.Current.Properties["IsLoggedIn"] = true;
                    // Call RegisterForPushNotifications() to force a regeneration of the FCM token
                    CrossFirebasePushNotification.Current.RegisterForPushNotifications();
                    Message = "Updating token for push notifications...";
                    var token = await CrossFirebasePushNotification.Current.GetTokenAsync();
                    await Task.Run(async () =>
                    {
                        if (userReturned.username != "")
                        {
                            
                            string updatedUserToken = await ApiConnection.UpdateToken(token);
                            // Updated local storage
                            if (updatedUserToken != "")
                            {
                                Console.WriteLine("Updated user token correctly in the database");
                            }
                        }
                    });

                        _ilm.ShowMainPage();
                    }
                    else
                    {
                        DisplayAlert("Error", "Sorry, this user is not in our database", "OK");
                    }
            }
            else
            {
                DisplayAlert("Error", "Database of users is empty", "OK");
            }
            IsBusy = false;
            spinner.IsVisible = false;
            Message = "";

        }
        async void btn_add_user(object sender, EventArgs e)
        {
                await Navigation.PushAsync(new NewUser());
        }


    }
}

