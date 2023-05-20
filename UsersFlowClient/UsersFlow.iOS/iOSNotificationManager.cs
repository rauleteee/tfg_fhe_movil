using System;
using Foundation;
using UserNotifications;
using UsersFlow.Interfaces;

namespace UsersFlow.iOS
{
	public class iOSNotificationManager
	{
        #region Variables
        /** Notification Manager */
        public bool hasNotificationsPermission;
        public event EventHandler NotificationReceived;
        /**
          * Device token for ios
          */
        public static string deviceToken = "";
        #endregion


        public iOSNotificationManager()
		{
		}
        #region Start
        /**
		 * Permission requirements
		 */
        public void Start()
        {
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
        }
        #endregion
        #region Notification Receiver
        public void ReceiveNotification(string title, string description)
        {

            var args = new INotificationEventArgs()
            {
                Title = title,
                Message = description,
            };

            NotificationReceived?.Invoke(null, args);
        }


        #endregion

        #region Stop
        public void Stop() { }
        #endregion

    }
}

