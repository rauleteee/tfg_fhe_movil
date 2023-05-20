using System;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UsersFlow.Model;
using UsersFlow.Interfaces;
using System.Collections.Generic;

namespace UsersFlow.ModelView
{
	public class ApiConnection 
	{
        // HttpRequest Response
        public static UserModel userReturned;
        public static string IpAdress = "192.168.1.34";
        public static string puerto = "5025";
        public ApiConnection()
		{
		}

        #region Api Rest Functions
        public static async Task<UserModel> GetUserFromDB(string username)
        {
            /******************************************************
             * GET request the user info for login
             ******************************************************/
            try
            {
                Uri requestUri = new Uri("http://"
                    + IpAdress
                    + ":" + puerto + 
                    "/api/user/" + username);
                var client = new HttpClient();
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await client.GetAsync(requestUri);
                Console.WriteLine("[SERVER]:" + response.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    userReturned = Newtonsoft.Json.JsonConvert.DeserializeObject<UserModel>(content);
                }
                return userReturned;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
                //return (ex.ToString());
            }

        }
        public static async Task<HttpResponseMessage> PostUser(UserModel user)
        {
            /*****************************************
             * POST request the user's info 
             *****************************************/
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user");
                var client = new HttpClient();
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var json = JsonConvert.SerializeObject(user);
                var contentJson = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUri, contentJson);
                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }

        }
        public static async Task<string> UpdateToken(string token)
        {
            /*****************************************
             * PUT (UPDATE) request the user's info 
             *****************************************/
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user/token");
                var client = new HttpClient();

                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Obtain current logged in user
                string CurrentUserJson = App.Current.Properties["CurrentUser"].ToString();
                UserModel user = JsonConvert.DeserializeObject<UserModel>(CurrentUserJson);
                user.token = token;
                //So as not to increment the time of the httpRequest, we onlly need to send the username and token
                UserModel userToBeSended = new UserModel(user.username, user.password)
                {
                    username = user.username,
                    token = user.token
                };

                var json = JsonConvert.SerializeObject(userToBeSended);
                var contentJson = new StringContent(json, Encoding.UTF8, "application/json");
                // RESPONSE FROM SERVER
                HttpResponseMessage response = await client.PutAsync(requestUri, contentJson);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SERVER] Token updated for user: {user.username} with token {user.token}");
                    await App.Current.SavePropertiesAsync();
                    var content = await response.Content.ReadAsStringAsync();
                    var tokenReturned = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(content);
                    return tokenReturned;
                }
                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public static async Task<HttpResponseMessage> UpdateUser(UserModel user)
        {
           /*****************************************
             * PUT (UPDATE) request the user's info 
             *****************************************/
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user");
                var client = new HttpClient();

                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var json = JsonConvert.SerializeObject(user);
                var contentJson = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(requestUri, contentJson);
                
                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public static async Task<List<UserModel>> GetAllUsers()
        {
            /*****************************************
             * GET a list of all users 
             *****************************************/
            List<UserModel> AllUsers = new List<UserModel>();
            try
            {
               Uri requestUri = new Uri("http://" +
               IpAdress + ":" +
               puerto + "/api/user/all");
                var client = new HttpClient();

                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await client.GetAsync(requestUri);
                Console.WriteLine("[SERVER]:" + response.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AllUsers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserModel>>(content);
                    Console.WriteLine($"[SERVER] Returned {AllUsers.Count} users");
                }
                return AllUsers;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
                
            }

        }
        public static async Task<List<string>> GetAllUsernames()
        {
            /*****************************************
             * GET a list of all users 
             *****************************************/
            List<string> AllUsers = new List<string>();
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user/all/usernames");
                var client = new HttpClient();

                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await client.GetAsync(requestUri);
                Console.WriteLine("[SERVER]:" + response.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AllUsers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(content);
                    Console.WriteLine($"[SERVER] Returned {AllUsers.Count} users");
                }
                return AllUsers;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;

            }

        }
        public static async Task<HttpResponseMessage> DeleteUser(string username)
        {
            /*****************************************
             * DELETE request for removing user
             *****************************************/
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user/" + username);
                var client = new HttpClient();
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.DeleteAsync(requestUri);

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }



        #endregion
        #region Api Rest functions schedule
        public static async Task<Schedule> GetSchedule(int id)
        {
            Schedule schedule;
            /******************************************************
             * GET request the user schedule
             ******************************************************/
            try
            {
                Uri requestUri = new Uri("http://"
                    + IpAdress
                    + ":" + puerto +
                    "/api/user/schedule/getId/" + id);
                var client = new HttpClient();
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await client.GetAsync(requestUri);
                Console.WriteLine("[SERVER]:" + response.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    schedule = Newtonsoft.Json.JsonConvert.DeserializeObject<Schedule>(content);
                    return schedule;
                }
                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }

        }
        public static async Task<List<int>> GetAllSchedulesIds(int user_id)
        {
            /*****************************************
             * GET a list of all users 
             *****************************************/
            List<int> AllIds = new List<int>();
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user/schedule/user/" + user_id);
                var client = new HttpClient();

                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await client.GetAsync(requestUri);
                //Console.WriteLine("[SERVER]:" + response.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AllIds = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(content);
                    Console.WriteLine($"[SERVER] Returned {AllIds.Count} SCHEDULES IDS");
                }
                return AllIds;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;

            }

        }
        public async Task<HttpResponseMessage> PostSchedule(Schedule schedule)
        {
            /*****************************************
             * POST request schedule
             *****************************************/
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user/schedule/addNewSchedule/");
                var client = new HttpClient();
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var json = JsonConvert.SerializeObject(schedule);
                var contentJson = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUri, contentJson);
                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }

        }
        public static async Task<HttpResponseMessage> UpdateSchedule(Schedule schedule)
        {
            /*****************************************
              * PUT (UPDATE) request the user's info 
              *****************************************/
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user/schedule/" + schedule._id + "/" + schedule.date);
                var client = new HttpClient();

                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                schedule.date = "";
                schedule._id = 0;
                var json = JsonConvert.SerializeObject(schedule);
                var contentJson = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(requestUri, contentJson);

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        
        public static async Task<HttpResponseMessage> DeleteSchedule(int id, string date)
        {
            /*****************************************
             * DELETE request for removing user
             *****************************************/
            try
            {
                Uri requestUri = new Uri("http://" +
                IpAdress + ":" +
                puerto + "/api/user/schedule/" + id + "/" + date);
                var client = new HttpClient();
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.DeleteAsync(requestUri);

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        #endregion
    }
}

