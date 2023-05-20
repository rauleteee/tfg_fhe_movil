namespace ApiUsersFlow.Models
{
        public class Schedule
        {
        public int ?  _id { get; set; }
        public int ? user_id { get; set; }
       public ulong ? entry_hour { get; set; }
        public string ?entry_hour_ciph { get; set; }
        public ulong ? leave_hour { get; set; }
        public string ?leave_hour_ciph { get; set; }
        public string ?date { get; set; }
        public ulong ? balance { get; set; }
        public string ?balance_ciph { get; set; }
        /* 
		 * Encrypted encryption parms FHE
		 */
        public string? encParms { get; set; }
    }
}
