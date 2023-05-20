using System;
using System.Runtime.Serialization.Formatters.Binary;
using ApiUsersFlow.Interfaces;
using ApiUsersFlow.Models;
using MySql.Data.MySqlClient;
using NuGet.Protocol.Plugins;

namespace ApiUsersFlow.Repositories
{
	public class UserCollection : IUserCollection
	{
        
		static string connectionString = "SERVER=localhost;DATABASE=UsersInfo;User Id=root;Password=;";
		public static MySqlConnection cn;

		public UserCollection()
		{
            cn = new MySqlConnection(connectionString);
		}
		/**************************************************************
		* BASIC USER FUNCTIONS
		***************************************************************/
        public async Task<List<string>> GetListOfUsernames(int page, int pageSize, string search)
        {
            // Calculate the starting index of the page based on page number and page size
            int startIndex = (page - 1) * pageSize;

            List<string> users = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Create a MySqlCommand object to execute the SQL query
                using (MySqlCommand command = new MySqlCommand(
                    "SELECT username FROM Users " +
                    "WHERE username LIKE @search " +
                    "ORDER BY _id " +
                    "LIMIT @start, @pageSize", connection))
                {
                    command.Parameters.AddWithValue("@search", $"%{search}%");
                    command.Parameters.AddWithValue("@start", startIndex);
                    command.Parameters.AddWithValue("@pageSize", pageSize);

                    // Retrieve all users from database and add them to the list
                    using (MySqlDataReader reader = (MySqlDataReader) await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var username = reader.GetString("username");
                            // Populate other properties of User object as needed
                            users.Add(username);
                        }
                    }
                }
            }

            Console.WriteLine($"[GETALL] Returned {users.Count} users");
            // Return the list of users
            return users;

        }
        public async Task<List<UserModel>> GetAllUsers(int page, int pageSize, string search)
        {
            // Calculate the starting index of the page based on page number and page size
            int startIndex = (page - 1) * pageSize;

            List<UserModel> users = new List<UserModel>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Create a MySqlCommand object to execute the SQL query
                using (MySqlCommand command = new MySqlCommand(
                    "SELECT * FROM Users " +
                    "WHERE username LIKE @search " +
                    "ORDER BY _id " +
                    "LIMIT @start, @pageSize", connection))
                {
                    command.Parameters.AddWithValue("@search", $"%{search}%");
                    command.Parameters.AddWithValue("@start", startIndex);
                    command.Parameters.AddWithValue("@pageSize", pageSize);

                    // Retrieve all users from database and add them to the list
                    using (MySqlDataReader reader = (MySqlDataReader) await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            UserModel user = new UserModel()
                            {
                                _id = reader.GetInt32(reader.GetOrdinal("_id")),
                                username = reader.GetString("username"),
                                password = reader.GetString("password"),
                                Name = reader.GetString("Name"),
                                Birth = reader.GetString("Birth"),
                                cipherDNI = reader.GetString("cipherDNI"),
                                cipherIban = reader.GetString("cipherIban"),
                                cipherSegSocial = reader.GetString("cipherSegSocial"),
                                Privilege = reader.GetString("Privilege"),
                                encParms = reader.GetString("encParms")
                            };

                            // Populate other properties of User object as needed
                            users.Add(user);
                        }
                    }
                }
            }

            Console.WriteLine($"[GETALL] Returned {users.Count} users");
            // Return the list of users
            return users;
        }

        public async Task<string> GetUserToken(string username)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string selectQuery = "SELECT token FROM Users WHERE username = @username";

                using (MySqlCommand command = new MySqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string token = reader.GetString("token");
                            Console.WriteLine($"[GET] Returned token: {token}, of user: {username}");
                            return token;
                        }
                        else
                        {
                            return null;
                        }
                    }

                }
            }
            
        }
        public async Task DeleteUser(string username)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
               await connection.OpenAsync();

               // Delete associated recors in the user_time table
               using(MySqlCommand command = new MySqlCommand("DELETE FROM user_time WHERE _id = (SELECT _id FROM Users WHERE username = @username)", connection))
               {
                    command.Parameters.AddWithValue("@username", username);
                    await command.ExecuteNonQueryAsync();
                }

                string deleteQuery = "DELETE FROM Users WHERE username = @username";

                using (MySqlCommand command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    await command.ExecuteNonQueryAsync();
                }
                Console.WriteLine("[DELETE] Removed user with username:" + username);
            }


        }


        public async Task<UserModel> GetUserByUsername(string username)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string selectQuery = "SELECT * FROM Users WHERE username = @username";

                using (MySqlCommand command = new MySqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            UserModel user = new UserModel
                            {
                                _id = reader.GetInt32(reader.GetOrdinal("_id")),
                                username = reader.GetString("username"),
                                password = reader.GetString("password"),
                                Name = reader.GetString("Name"),
                                Birth = reader.GetString("Birth"),
                                cipherDNI = reader.GetString("cipherDNI"),
                                cipherIban = reader.GetString("cipherIban"),
                                cipherSegSocial = reader.GetString("cipherSegSocial"),
                                Privilege = reader.GetString("Privilege"),
                                encParms = reader.GetString("encParms")
                            };
                             Console.WriteLine("[GET] Returned user with username:" + username);

                            return user;
                        }
                        else
                        {
                            return null;
                        }
                    }

                }
            }
            
        }

        public async Task InsertUser(UserModel user)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                if(await PersonExists(user.username) == false){
                    using (MySqlCommand command = new MySqlCommand("INSERT INTO Users (username,password,Name,Birth,DNI,SegSocialNumber,IBAN" +
                ",cipherIban,cipherDNI,cipherSegSocial,Privilege,securityToken,encParms) VALUES (@username,@password,@Name,@Birth,@DNI,@SegSocialNumber,@IBAN" +
                ",@cipherIban,@cipherDNI,@cipherSegSocial,@Privilege,@securityToken,@encParms)", connection))
                {
                    
                    command.Parameters.AddWithValue("@username", user.username);
                    command.Parameters.AddWithValue("@password", user.password);
                    command.Parameters.AddWithValue("@Name", user.Name);
                    command.Parameters.AddWithValue("@Birth", user.Birth);
                    command.Parameters.AddWithValue("@DNI", user.DNI);
                    command.Parameters.AddWithValue("@SegSocialNumber", user.SegSocialNumber);
                    command.Parameters.AddWithValue("@IBAN", user.IBAN);
                    command.Parameters.AddWithValue("@cipherIban", user.cipherIban);
                    command.Parameters.AddWithValue("@cipherDNI", user.cipherDNI);
                    command.Parameters.AddWithValue("@cipherSegSocial", user.cipherSegSocial);
                    command.Parameters.AddWithValue("@Privilege", user.Privilege);
                    command.Parameters.AddWithValue("@securityToken", user.securityToken);
                    command.Parameters.AddWithValue("@encParms", user.encParms);
		    // Execute the query to insert the new row and retrieve the auto-generated ID
                    int _id = Convert.ToInt32(command.ExecuteScalar());
                    user._id = _id;
                    Console.WriteLine("[POST] Created user with username: " + user.username);
                }
            }
        }
                
            
        }

        public async Task UpdateUser(UserModel user)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string updateQuery = "UPDATE Users SET " +
                    "username = @username, " +
                    "password = @password, " +
                    "Name = @Name, " +
                    "Birth = @Birth, " +
                    "cipherIban = @cipherIban," +
                    "cipherDNI = @cipherDNI, " +
                    "cipherSegSocial = @cipherSegSocial, " +
                    "Privilege = @Privilege, " +
                    "securityToken = @securityToken, " +
                    "encParms = @encParms " +
                    "WHERE _id = @_id";

                using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@_id", user._id);
                    command.Parameters.AddWithValue("@username", user.username);
                    command.Parameters.AddWithValue("@password", user.password);
                    command.Parameters.AddWithValue("@Name", user.Name);
                    command.Parameters.AddWithValue("@Birth", user.Birth);
                    command.Parameters.AddWithValue("@cipherIban", user.cipherIban);
                    command.Parameters.AddWithValue("@cipherDNI", user.cipherDNI);
                    command.Parameters.AddWithValue("@cipherSegSocial", user.cipherSegSocial);
                    command.Parameters.AddWithValue("@Privilege", user.Privilege);
                    command.Parameters.AddWithValue("@securityToken", user.securityToken);
                    command.Parameters.AddWithValue("@encParms", user.encParms);
                    await command.ExecuteNonQueryAsync();
                }
                Console.WriteLine("[PUT] Updated user with username: " + user.username);
    }

        }
        public async Task<string> UpdateToken(UserModel user)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                  connection.Open();

                  Console.WriteLine($"Update from console user {user.username}: {user._id}: TOKEN : {user.token}");
                  string updateQuery = "UPDATE Users SET Token=@Token WHERE username=@username";
                  using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                  {
                      command.Parameters.AddWithValue("@username", user.username);
                      command.Parameters.AddWithValue("@token", user.token);
                      command.ExecuteNonQuery();
                  }
                  return user.token;
            }
        }

        /***********************************************************************
        * SCHEDULE USER FUNCTIONS
        ***********************************************************************/
        public async Task InsertDataEntry(Schedule schedule)
        {
             using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand("INSERT INTO user_time (user_id, date, entry_hour, leave_hour, balance, encParms) " +
                                                        "VALUES (@user_id, @date, @entry_hour, @leave_hour, @balance, @encParms)", connection))
                {
                    command.Parameters.AddWithValue("@user_id", schedule.user_id);
                    command.Parameters.AddWithValue("@date", schedule.date);
                    command.Parameters.AddWithValue("@entry_hour", schedule.entry_hour_ciph);
                    command.Parameters.AddWithValue("@leave_hour", schedule.leave_hour_ciph);
                    command.Parameters.AddWithValue("@balance", schedule.balance_ciph);
                    command.Parameters.AddWithValue("@encParms", schedule.encParms);
                    int _id = Convert.ToInt32(command.ExecuteScalar());
                    schedule._id = _id;
                    //await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"[POST] User schedule saved with new id: {schedule._id}");
                }
            }
        }
        public async Task DeleteDataEntry(int id)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand("DELETE FROM user_time WHERE _id = @_id", connection))
                {
                    command.Parameters.AddWithValue("@_id", id);

                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DELETE] User schedule deleted for user with ID: {id}");
                }
            }
        }
        public async Task UpdateDataEntry(string date, string entry_hour, string leave_hour, int id, string balance)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand("UPDATE user_time SET entry_hour = @entry_hour, leave_hour = @leave_hour, balance = @balance WHERE _id = @_id AND date = @date", connection))
                {
                    command.Parameters.AddWithValue("@_id", id);
                    command.Parameters.AddWithValue("@date", date);
                    command.Parameters.AddWithValue("@entry_hour", entry_hour);
                    command.Parameters.AddWithValue("@leave_hour", leave_hour);
                    command.Parameters.AddWithValue("@balance", balance);
                                                                                                    
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"[PUT] Updated schedule for date: {date}");
                 }
             }
        }

        public async Task<List<int>> GetSchedulesIdsOfUserId(int user_id)  
        {
            List<int> idList = new List<int>();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("SELECT _id FROM user_time WHERE user_id = @user_id", connection))
                {
                    command.Parameters.AddWithValue("@user_id", user_id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32("_id");
                            idList.Add(id);
                        }
                    }
                }
            }
            return idList;

        }                                     
        public async Task<Schedule> GetScheduleEntry(int _id)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string selectQuery = "SELECT * FROM user_time WHERE _id = @_id";

                using (MySqlCommand command = new MySqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@_id", _id);
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Schedule schd = new Schedule
                            {
                                _id = reader.GetInt32(reader.GetOrdinal("_id")),
                                user_id = reader.GetInt32(reader.GetOrdinal("user_id")),
                                date = reader.GetString("date"),
                                leave_hour = 0,
                                entry_hour = 0,
                                leave_hour_ciph = reader.GetString("leave_hour"),
                                entry_hour_ciph = reader.GetString("entry_hour"),
                                balance = 0,
                                balance_ciph = reader.GetString("balance"),
                                encParms = reader.GetString("encParms")
                            };
                             Console.WriteLine("[GET] Returned schedule with id:" + _id);

                            return schd;
                        }
                        else
                        {
                            return null;
                        }
                    }

                }
            }
        }
                                                                                                                                                                                                                                
                                                                                                                                                                                                                                                       
                                                                                                                                        
                                                                                                                                                                                                                                    
        #region AUXILIARY FUNCTIONS
        public async Task<bool> PersonExists(string username)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string selectQuery = "SELECT COUNT(*) FROM Users WHERE username = @username";

                using (MySqlCommand command = new MySqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    return (long)command.ExecuteScalar() > 0;
                }
            }
        }
        /*
        * Function that returns true when there is an existing entry of a date of a specific user
        */
        public async Task<bool> EntryExistsForDate(int id)
        {
            using(MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string selectQuery = "SELECT COUNT(*) FROM user_time WHERE _id=@_id";
                using (MySqlCommand command = new MySqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@_id", id);
                    return (long)command.ExecuteScalar() > 0;
                }
            }
        }
        
        #endregion

    }

}

