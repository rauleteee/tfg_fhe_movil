using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UsersFlow.Model;
using UsersFlow.ModelView;
using Xamarin.Forms;

namespace UsersFlow.View
{	
	public partial class ScheduleView : ContentPage
	{
		public static Schedule schedule;
        private DateTime _selectedDate;
        public ApiConnection http;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
            }
        }
        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }
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
        public ScheduleView()
		{
			InitializeComponent();
            http = new ApiConnection();
            SelectedDate = DateTime.Now;
            BindingContext = this;
		}
        async void Button_Clicked_Schedule(Object o, EventArgs e)
        {
            await Navigation.PushAsync(new ScheduleHistory());
        }

        async void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            Message = "Ciphering your information with FHE...";
            Thread.Sleep(1000);
            IsBusy = true;
            spinner.IsVisible = true;

            var selectedDateString = SelectedDate.Date.ToString("dd-MM-yyyy");

            var entry_hour_ciph = input_entry.Text;
            var leave_hour_ciph = input_leave.Text;

           

            Schedule scheduleToBeCiphered = new Schedule();
            scheduleToBeCiphered.date = selectedDateString;
            //scheduleToBeCiphered.entry_hour = entry_hour;
            // scheduleToBeCiphered.leave_hour = leave_hour;
            scheduleToBeCiphered.entry_hour_ciph = FHEHandler.ULongToString(Convert.ToUInt64(entry_hour_ciph));
            scheduleToBeCiphered.leave_hour_ciph = FHEHandler.ULongToString(Convert.ToUInt64(leave_hour_ciph));
          

            //Retrieve the current user from local storage
            UserModel currentUser = null;
            // Save the returned user in the login into the session storage
            string CurrentUserJson = App.Current.Properties["CurrentUser"].ToString();
            currentUser = JsonConvert.DeserializeObject<UserModel>(CurrentUserJson);
            var cipheredSchedule = await FHEHandler.cipherSchedule(scheduleToBeCiphered, currentUser);

            //Send the data to the database
            Message = "Saving your information to the database...";
            HttpResponseMessage responseFromServer = null;
            await Task.Run(async () =>
            {
                 responseFromServer = await http.PostSchedule(cipheredSchedule);
            });

            Console.WriteLine($"[SERVER] Post new schedule of user: {currentUser.username}" +
                $"with response: {responseFromServer.ToString()}");
            IsBusy = false;
            spinner.IsVisible = false;
            if (!responseFromServer.IsSuccessStatusCode && responseFromServer != null)
                await DisplayAlert("Oops!",
                    "Schedule has not been registered. Response from server: \n"
                    + responseFromServer, "OK");
            else
            {
                await DisplayAlert("Success",
                    "Schedule registered succesfully. Response from server:\n"
                    + responseFromServer.StatusCode, "OK");
                input_entry.Text = "";
                input_leave.Text = "";


            }

            Message = "";
        }
    }
}

