using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UsersFlow.Model;

namespace UsersFlow.Interfaces
{
	public interface IHttp
	{
		Task<UserModel> GetUserFromDB(string Username);
		Task<HttpResponseMessage> PostUser(UserModel user);
		Task<HttpResponseMessage> UpdateUser(UserModel user);
		Task<string> UpdateToken(string token);

        Task<List<UserModel>> GetAllUsers();
		Task<HttpResponseMessage> DeleteUser(string username);

		Task<List<int>> GetAllSchedulesIds(int user_id);
		Task<Schedule> GetSchedule(int id);
		Task<HttpResponseMessage> PostSchedule(Schedule schedule, int user_id);

    }
}

