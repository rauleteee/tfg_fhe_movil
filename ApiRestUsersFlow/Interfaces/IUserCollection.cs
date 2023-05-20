using System;
using ApiUsersFlow.Models;

namespace ApiUsersFlow.Interfaces
{
	public interface IUserCollection
	{
		Task InsertUser(UserModel user);
		Task UpdateUser(UserModel user);
		Task DeleteUser(string id);

		Task<List<UserModel>> GetAllUsers(int page, int pageSize, string search);
		Task<List<string>> GetListOfUsernames(int page, int pageSize, string search);
		Task<UserModel> GetUserByUsername(string username);
		Task<string> GetUserToken(string username);
		Task<string> UpdateToken(UserModel user);
        #region SCHEDULE FUNCTIONS
        Task InsertDataEntry(Schedule schedule);
        Task UpdateDataEntry(string date, string entry_hour, string leave_hour, int id, string balance);
        Task DeleteDataEntry(int id);
        Task<Schedule> GetScheduleEntry(int id);
		Task<List<int>> GetSchedulesIdsOfUserId(int user_id);
        
        #endregion
		#region AUXILIARY FUNCTIONS
		public Task<bool> PersonExists(string username);
		public Task<bool> EntryExistsForDate(int id);
        #endregion
    }
}

