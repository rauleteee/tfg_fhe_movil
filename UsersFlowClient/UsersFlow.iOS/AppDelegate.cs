using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Foundation;
using Plugin.FirebasePushNotification;
using UIKit;
using UserNotifications;
using Xamarin.Forms;
using static SystemConfiguration.NetworkReachability;

namespace UsersFlow.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());
            try
            {
                bool hasNotificationsPermission;
        /** Handle Reques for Notification Authorization */
                var optionsNoti = UNAuthorizationOptions.Alert |
                    UNAuthorizationOptions.Sound |
                    UNAuthorizationOptions.Badge |
                    UNAuthorizationOptions.ProvidesAppNotificationSettings;
                UNUserNotificationCenter.Current.RequestAuthorization(optionsNoti, (granted, error) =>
                {
                    if (granted)
                    {
                        hasNotificationsPermission = granted;
                    }
                    else
                    {
                        NSError nserror = error;
                    }
                });

                // FirebasePushNotificationManager fcm = new FirebasePushNotificationManager();
                FirebasePushNotificationManager.Initialize(options, true);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
                CrossFirebasePushNotification.Current.OnNotificationReceived += Current_OnNotificationReceived;
                // Initialize FirebasePushNotification plugin
                UNUserNotificationCenter.Current.Delegate = new iOSNotificationReceiver();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
            }

            return base.FinishedLaunching(app, options);
        }
        #region OVERRIDE FUNCTIONS: Remote notifications
        /**
         * Push and Local notifications handler, background
         */
        private void Current_OnNotificationReceived(object source, FirebasePushNotificationDataEventArgs e)
        {
            try
            {
                if (e.Data["user"] != null)
                {
                    var userThatRequiresSecretKey = e.Data["user"].ToString();
                    MessagingCenter.Send<object, string>(this, "SecretKeyRequirement", userThatRequiresSecretKey);
                }
                else if (e.Data["title"] !=null)
                {
                    string title = e.Data["title"].ToString();
                    // Do something with the title
                    MessagingCenter.Send<object, string>(this, "SecretKeyRetrieval", title);
                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            

        }


        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            /** Extracting deviceToken in a clear form */
            FirebasePushNotificationManager.DidRegisterRemoteNotifications(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            FirebasePushNotificationManager.RemoteNotificationRegistrationFailed(error);
            Console.WriteLine("Error registering for remote notifications: {0}", error);

        }

        /** Remote notifications event handler */

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            /* Whenever the device receives a remote notification
             * the aps content will be processed on ProcessNotification (same behaviour as local notifications).
             * This function will deserialize the aps content of the remote notification, and send the call to the messaging
             * center, in order to open the incoming call page
             */
            FirebasePushNotificationManager.DidReceiveMessage(userInfo);
            CrossFirebasePushNotification.Current.OnNotificationReceived += Current_OnNotificationReceived;

            completionHandler(UIBackgroundFetchResult.NoData);
        }

        #endregion

    }
}

