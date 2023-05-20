using System;
using UIKit;
using UserNotifications;
using UsersFlow.Interfaces;
using Xamarin.Forms;

namespace UsersFlow.iOS
{
    /** 
    * Handling App Notifications 
    * 
    * @extends UNUserNotificationCenterDelegate the app can take over responsibility for displaying the Notification
    */
    public class iOSNotificationReceiver : UNUserNotificationCenterDelegate, INotificationManager
    {

        void ProcessNotification(UNNotification notification)
        {
            try
            {
                if (notification.Request.Content.UserInfo["user"] != null)
                {
                    var userThatRequiresSecretKey = notification.Request.Content.UserInfo["user"].ToString();
                    MessagingCenter.Send<object, string>(this, "SecretKeyRequirement", userThatRequiresSecretKey);
                }
                else if ((notification.Request.Content.UserInfo["title"] != null))
                {
                    string title = notification.Request.Content.UserInfo["title"].ToString();
                    // Do something with the title
                    MessagingCenter.Send<object, string>(this, "SecretKeyRetrieval", title);
                }
            }catch(Exception ex) { Console.WriteLine(ex.Message); }
            
        }

        #region Override Methods
        /** Time when notification is visible */
        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {

            ProcessNotification(notification);
            completionHandler(UNNotificationPresentationOptions.Sound | UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Badge);
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
        }
        // Called if app is in the background, or killed state.
        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            ProcessNotification(response.Notification);

            completionHandler();
        }


        #endregion

    }

}

