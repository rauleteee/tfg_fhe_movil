 using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UsersFlow.Model;
using UsersFlow.ModelView;
using Xamarin.Forms;
namespace UsersFlow.View
{	
	public partial class ScheduleHistory : ContentPage
	{
        #region Variables
        List<Schedule> _AllSchedules;
        public ObservableCollection<Schedule> AllSchedules { get; set; }
        private string _date;
        UserModel CurrentUser;
        public string date
        {
            get { return _date; }
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }
        private string _entry_hour_ciph;
        public string entry_hour_ciph
        {
            get { return _entry_hour_ciph; }
            set
            {
                _entry_hour_ciph = value;
                OnPropertyChanged();
            }
        }
        private string _leave_hour_ciph;
        public string leave_hour_ciph
        {
            get { return _leave_hour_ciph; }
            set
            {
                _leave_hour_ciph = value;
                OnPropertyChanged();
            }
        }
        private string _balance_ciph;
        public string balance_ciph
        {
            get { return _balance_ciph; }
            set
            {
                _balance_ciph = value;
                OnPropertyChanged();
            }
        }
        #endregion
        public ScheduleHistory ()
		{
			InitializeComponent ();
            _AllSchedules = new List<Schedule>();
            AllSchedules = new ObservableCollection<Schedule>();
            // Save the returned user in the login into the session storage
            string CurrentUserJson = App.Current.Properties["CurrentUser"].ToString();
            CurrentUser = JsonConvert.DeserializeObject<UserModel>(CurrentUserJson);
            /** Binding Context */
            BindingContext = this;
        }
        async void btn_rm_schedule(Object o, EventArgs e)
        {
            return;
        }
        async void btn_retrieve_db(Object o , EventArgs e)
        {
            try
            {
                spinner.IsVisible = true;
                spinner.IsRunning = true;
                AllSchedules.Clear();
                _AllSchedules.Clear();
                List<int> schedulesIds = await ApiConnection.GetAllSchedulesIds(CurrentUser._id);
                var getScheduleTasks = schedulesIds.Select(async id =>
                {
                    var schedule = await ApiConnection.GetSchedule(id);
                    _AllSchedules.Add(schedule);
                });
                await Task.WhenAll(getScheduleTasks);
                foreach (var schedule in _AllSchedules)
                {
                    var scheduleDecrypted = await FHEHandler.decryptSchedule(schedule, CurrentUser);
                    AllSchedules.Add(scheduleDecrypted);
                }
                spinner.IsVisible = false;
                spinner.IsRunning = false;
                // Indicator view
                //Numb = AllUsers.Count;
            }
            catch (Exception ex)
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
                Console.WriteLine(ex);
            }

        }

    }
}

