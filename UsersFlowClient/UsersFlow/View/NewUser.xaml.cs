using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Research.SEAL;

using Newtonsoft.Json;
using UsersFlow.Model;
using UsersFlow.ModelView;
using Xamarin.Forms;

namespace UsersFlow.View
{
    
    public partial class NewUser : ContentPage
    {
        #region OBJECT
        public static UserModel newUser;
        // Create an instance of the Argon2PasswordHasher class
        private readonly Argon2PasswordHasher passwordHasher = new Argon2PasswordHasher();

        #endregion
        public NewUser()
        {
            InitializeComponent();
            newUser = new UserModel(null, null);

        }
        async void save_button(object o, EventArgs e)
        {
            spinner.IsVisible = true;
            spinner.IsRunning = true;
            newUser.username = input_username.Text;
            newUser.password = input_password.Text;
            newUser.Name = name.Text;
            newUser.Birth = birth.Text;
            newUser.DNI = dni.Text;
            newUser.IBAN = iban.Text;
            newUser.Privilege = privs.SelectedItem.ToString();
            newUser.SegSocialNumber = SegSocialNumb.Text;
            // Hash a password and store it
            await Task.Run(() =>
            {
                string hashedPassword = passwordHasher
                .HashPassword(newUser.username, newUser.password);
                newUser.password = hashedPassword;
            });
            

            //newUser.userID = todo meter id de la huella
            var userCiphered = await FHEHandler.encryptData(newUser);
            spinner.IsVisible = false;
            spinner.IsRunning = false;
            
            // campos cifrados: IBAN, seg social, dni => datos sensibles
            userCiphered.DNI = null;
            userCiphered.IBAN = null;
            userCiphered.SegSocialNumber = null;
            //POST new user to the database
            try
            {
                if(userCiphered.username != null &&
                    userCiphered.password != null &&
                    userCiphered.Name != null &&
                    userCiphered.cipherDNI != null &&
                    userCiphered.cipherIban != null &&
                    userCiphered.cipherSegSocial != null)
                {
                    spinner.IsVisible = true;
                    spinner.IsRunning = true;
                    HttpResponseMessage responseFromDB = null;
                    try
                    {

                        responseFromDB = await ApiConnection.PostUser(userCiphered);


                        spinner.IsVisible = false;
                        spinner.IsRunning = false;
                        if (!responseFromDB.IsSuccessStatusCode && responseFromDB != null)
                            await DisplayAlert("Oops!",
                                "Username is already in use. Response from server: \n"
                                + responseFromDB, "OK");
                        else
                        {
                            await DisplayAlert("Success",
                                "Username registered succesfully. Response from server:\n"
                                + responseFromDB.StatusCode, "OK");
                            await Navigation.PopAsync();
                        }
                    }catch(Exception ex) { Console.WriteLine(ex); }
                    

                }
                else
                {
                    await DisplayAlert("Error",
                        "There are some missing information fields",
                        "Try again");
                }
            }
            catch (HttpRequestException ex){
                await DisplayAlert("Error DB", ex.ToString(), "ok");
            }

        }
        
        

    }

}

