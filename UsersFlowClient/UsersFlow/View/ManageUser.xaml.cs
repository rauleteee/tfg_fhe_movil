using System;
using System.Collections.Generic;
using UsersFlow.Model;
using Xamarin.Forms;
using UsersFlow.ModelView;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Xamarin.Essentials;
using System.Linq;

namespace UsersFlow.View
{	
	public partial class ManageUser : ContentPage
	{
		#region Variables
		List<UserModel> _AllUsers;
		public ObservableCollection<UserModel> AllUsers { get; set; }
        private int _NumberOfUsers;
        UserModel CurrentUser;
        public int NumberOfUsers
        {
            get { return _NumberOfUsers; }
            set
            {
                _NumberOfUsers = value;
                OnPropertyChanged();
            }
        }
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        private string _username;
        public string username
        {
            get { return _username; }
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }
        private string birth;
        public string Birth
        {
            get { return birth; }
            set
            {
                birth = value;
                OnPropertyChanged();
            }
        }
        private string segSocialNumber;
        public string SegSocialNumber
        {
            get { return segSocialNumber; }
            set
            {
                segSocialNumber = value;
                OnPropertyChanged();
            }
        }
        private string iban;
        public string IBAN
        {
            get { return iban; }
            set
            {
                iban = value;
                OnPropertyChanged();
            }
        }
        private string dni;
        public string DNI
        {
            get { return dni; }
            set
            {
                dni = value;
                OnPropertyChanged();
            }
        }
        private string priv;
        public string Privilege
        {
            get { return priv; }
            set
            {
                priv = value;
                OnPropertyChanged();
            }
        }
        
		
        #endregion
        public ManageUser ()
		{
			InitializeComponent ();
			/** List of all users */
			_AllUsers = new List<UserModel>();
			AllUsers = new ObservableCollection<UserModel>();
            // Save the returned user in the login into the session storage
            string CurrentUserJson = App.Current.Properties["CurrentUser"].ToString();
            CurrentUser = JsonConvert.DeserializeObject<UserModel>(CurrentUserJson);
            /** Binding Context */
            BindingContext = this;
		}
        async void btn_add_user(object sender, EventArgs e)
        {
            if (CurrentUser.Privilege == "Developer")
            {
                await DisplayAlert("Error",
                    "Sorry, you do not have permission for accessing user's information",
                    "Ok");
            }
            else
            {
                await Navigation.PushAsync(new NewUser());
            }

        }
        
        async void btn_retrieve_db(object sender, EventArgs e)
		{

            if (CurrentUser.Privilege == "Developer")
            {
                await DisplayAlert("Error",
                    "Sorry, you do not have permission for accessing user's information",
                    "Ok");
            }
            else
            {

                // Use Parallel.ForEach to execute requests for each username in parallel
                // Indicator view
                try
                {
                    spinner.IsVisible = true;
                    spinner.IsRunning = true;
                    AllUsers.Clear();
                    _AllUsers.Clear();
                    List<string> usernames = await ApiConnection.GetAllUsernames();
                    var getUserTasks = usernames.Select(async user =>
                    {
                        var userRetrieved = await ApiConnection.GetUserFromDB(user);
                        _AllUsers.Add(userRetrieved);
                    });
                    await Task.WhenAll(getUserTasks);
                    foreach (var user in _AllUsers)
                    {
                        var userDecrypted = await FHEHandler.decryptUserFromDB(user);
                        AllUsers.Add(userDecrypted);
                    }
                    spinner.IsVisible = false;
                    spinner.IsRunning = false;
                    // Indicator view
                    NumberOfUsers = AllUsers.Count;
                }
                catch(Exception ex)
                {
                    spinner.IsVisible = false;
                    spinner.IsRunning = false;
                    Console.WriteLine(ex);
                }
                    
 
            }
            
        }
        #region Remove User
        async void btn_remove_user(object o, EventArgs e)
        {

            /** Retrieve the new users */
            spinner.IsVisible = true;
            spinner.IsRunning = true;
            bool answer1 = false;
            var currentUser = carousel.CurrentItem as UserModel;
            bool answer = await DisplayAlert("Remove User",
                "Are you sure you want to delete the user?", "Yes", "No");
            if (answer)
            {
                answer1 = await DisplayAlert("Remove User",
                    "All the user data will be deleted", "Continue", "Cancel");

            }
            else
            {
                await Navigation.PopToRootAsync();
            }

            if (answer1)
            {
                var responseFromServer = await ApiConnection.DeleteUser(currentUser.username);
                if (responseFromServer.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success",
                        "User removed correctly.", "Ok");
                    // Remove the user secret key from secure storage
                    if (CurrentUser.Privilege == "Developer")
                    {
                        await SecureStorage.SetAsync("SecretKey-" + currentUser.username, "");
                    }

                    AllUsers.Clear();
                    _AllUsers.Clear();
                    List<string> usernames = await ApiConnection.GetAllUsernames();
                    var getUserTasks = usernames.Select(async user =>
                    {
                        var userRetrieved = await ApiConnection.GetUserFromDB(user);
                        _AllUsers.Add(userRetrieved);
                    });
                    await Task.WhenAll(getUserTasks);
                    foreach (var user in _AllUsers)
                    {
                        var userDecrypted = await FHEHandler.decryptUserFromDB(user);
                        AllUsers.Add(userDecrypted);
                    }

                    // Indicator view
                    NumberOfUsers = AllUsers.Count;
                    spinner.IsVisible = false;
                    spinner.IsRunning = false;
                    this.ForceLayout();
                }
                else
                {
                    await DisplayAlert("Error",
                        "User has not been removed. Response from server: \n" +
                        responseFromServer.StatusCode, "Ok");
                }
            }
        }
        #endregion
        async void btn_save_db(object sender, EventArgs e)
		{

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            var currentUser = carousel.CurrentItem as UserModel;
			// Creating the new user based on the entries of the page
			UserModel UserToSave = new UserModel(currentUser.username, currentUser.password)
			{
                _id = currentUser._id,
				Name = currentUser.Name,
				username = currentUser.username,
                password = currentUser.password,
				Birth = currentUser.Birth,
				DNI = currentUser.DNI,
				SegSocialNumber = currentUser.SegSocialNumber,
				IBAN = currentUser.IBAN,
				Privilege = currentUser.Privilege
            };
            UserModel CipheredUserToSave = await FHEHandler.encryptData(UserToSave);
            
            var responseFromDB = await ApiConnection.UpdateUser(CipheredUserToSave);
                spinner.IsVisible = false;
                spinner.IsRunning = false;
            Console.WriteLine("[SERVER] Response from server: " + responseFromDB.StatusCode.ToString());
            if (responseFromDB.StatusCode.ToString() == "Created")
            {
                await DisplayAlert("Success",
                           "User updated. Response from server:\n"
                           + responseFromDB.StatusCode, "OK");
            }
            else
            {
                await DisplayAlert("Oops!",
                            "User has not been updated. Response from server: \n"
                            + responseFromDB, "OK");
            }
            this.ForceLayout();

            /** Retrieve the new user */
            spinner.IsVisible = true;
            spinner.IsRunning = true;
            UserModel userUpdated = await ApiConnection.GetUserFromDB(currentUser.username);
            UserModel userUpdatedDecrypted = await FHEHandler.decryptUserFromDB(userUpdated);
            carousel.CurrentItem = userUpdatedDecrypted;
            spinner.IsVisible = false;
            spinner.IsRunning = false;
            this.ForceLayout();

        }

    }
    
}

