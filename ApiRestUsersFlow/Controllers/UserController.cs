
using ApiUsersFlow.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ApiUsersFlow.Repositories;
using ApiUsersFlow.Models;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Research.SEAL;
using Microsoft.Extensions.Hosting;
using MySqlX.XDevAPI.Common;

namespace ApiUsersFlow.Controllers
{
    // endpoint api/user
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        #region Constants and variables
        public IUserCollection db = new UserCollection();
        private const int PageSize = 10;
        private const int MaxHoursPerWeek = 40;
        #endregion
        // GET All users except the one sending the request with
        // @param username
        [HttpGet("all")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> GetAllUsers(int page = 1, string search = null)
        {
            List<UserModel> users = await db.GetAllUsers(page, PageSize, search);
            string json = JsonConvert.SerializeObject(users);
            return Content(json, "application/json");
        }
        // GET All users except the one sending the request with
        // @param username
        [HttpGet("all/usernames")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> GetAllUsernames(int page = 1, string search = null)
        {
            List<string> users = await db.GetListOfUsernames(page, PageSize, search);
            string json = JsonConvert.SerializeObject(users);
            return Content(json, "application/json");
        }

        // GET User by Id
        [HttpGet("{username}")]
        public async Task<UserModel> GetUserDetails(string username)
        {
            return await db.GetUserByUsername(username);
        }
        // GET User's token
        [HttpGet("token/{username}")]
        public async Task<string> GetUserToken(string username)
        {
            return await db.GetUserToken(username);
        }
        //PUT new user
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> CreateUser([FromBody] UserModel user)
        {
            if (user == null)
                return BadRequest();
            if (user.username == string.Empty || user.password == string.Empty)
            {
                ModelState.AddModelError("User information", "Username and password is required");
            }
            if(await db.PersonExists(user.username) == false){
                await db.InsertUser(user);
                return Created("Created user", true);
            }
             return NotFound("Username already exists in the database");
            

        }
        //PUT change user permission
        [HttpPut]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UpdateUser([FromBody] UserModel user)
        {
            if (await db.PersonExists(user.username) == false) { 
                return NotFound();
            }

            await db.UpdateUser(user);

            return Created("Updated correctly", true);
        }
        // PUT change user's data from console
        [HttpPut("token")]
        [DisableRequestSizeLimit]
        public async Task<string> UpdateUserToken([FromBody] UserModel user)
        {
            string tokenUpdated = await db.UpdateToken(user);
            return tokenUpdated;
        }

        // DELETE user
        [HttpDelete("{username}")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DeleteUser(string username)
            {
            if (await db.PersonExists(username) == false)
            {
                return NotFound();
            }
            await db.DeleteUser(username);
            return NoContent(); //success
        }
        #region HTTP SCHEDULE FUNCTIONS
        [HttpGet]
        [Route("schedule/user/{user_id}")]
        public async Task<List<int>> GetSchedulesIdsOfUserId(int user_id)
        {
            try
            {
                List<int> scheduleIds = await db.GetSchedulesIdsOfUserId(user_id);
                if (scheduleIds != null)
                {
                    return scheduleIds;
                }
                return new List<int>();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return new List<int>();
            }
        }
        // POST user time
        [HttpPost("schedule/addNewSchedule")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> PostSchedule([FromBody]Schedule schedule)
        {
            #region FHE
            /*************************************************************
             * FHE Encryption Parameters loading
             *************************************************************/

            SEALContext context;
            Evaluator evaluator;
            try
            {

                if (schedule.encParms != null)
                {
                    // Encryption params loading
                    var bytesParms = Convert.FromBase64String(schedule.encParms);
                    MemoryStream streamParms = new MemoryStream(bytesParms);

                    EncryptionParameters encParms = new EncryptionParameters();
                    encParms.Load(streamParms);

                    // creating context
                    context = new SEALContext(encParms);
                    //evaluator
                    evaluator = new Evaluator(context);
                    /*************************************************************
                     * Hours balancing
                     *************************************************************/

                    /** The following information will be working as follows
                     * We will sum all the hours that have been saved from the user 
                     * in each week, doing a balance. The employee has to work, usually,
                     * 40h per week. The server will do a calculation of a balance,
                     * in which we will be able to see from the Application, if the employee
                     * has worked the 40h, and the positive or negative balance. The server
                     * will never know what data is working with due to the fact that this information
                     * is given ciphered from the mobile application.
                     */

                    var bytesEncEntryHour = Convert.FromBase64String(schedule.entry_hour_ciph);
                    MemoryStream streamUsEntryHour = new MemoryStream(bytesEncEntryHour);
                    Ciphertext cipherEntryHour = new Ciphertext();
                    cipherEntryHour.Load(context, streamUsEntryHour);

                    var bytesEncLeaveHour = Convert.FromBase64String(schedule.leave_hour_ciph);
                    MemoryStream streamUsLeaveHour = new MemoryStream(bytesEncLeaveHour);
                    Ciphertext cipherLeaveHour = new Ciphertext();
                    cipherLeaveHour.Load(context, streamUsLeaveHour);

                    // FHE operations
                    //evaluator.NegateInplace(cipherEntryHour);
                    //evaluator.NegateInplace(cipherEntryHour);

                    Ciphertext cipheredBalance = new Ciphertext();
                    evaluator.Sub(cipherLeaveHour, cipherEntryHour, cipheredBalance);

                    // Save the balance operation into schedule
                    MemoryStream cipheredBalanceStream = new MemoryStream();
                    cipheredBalance.Save(cipheredBalanceStream);
                    var StringCipheredBalance = Convert.ToBase64String(cipheredBalanceStream.ToArray());

                    schedule.balance_ciph = StringCipheredBalance;

                }
            }catch(Exception ex) {Console.WriteLine(ex); }



            #endregion
            #region HttpPost handler
            await db.InsertDataEntry(schedule);
            return Created("Data entries saved", true);
            #endregion
        }
        [HttpGet]
        [Route("schedule/getId/{id}")]
        public async Task<Schedule> GetScheduleEntry(int id)
        {
            try
            {
                Console.WriteLine($"I tried to return a schedule with id: {id}");
                return await db.GetScheduleEntry(id);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return new Schedule();
            }
        }
        [HttpDelete]
        [Route("schedule/{id}")]
        public async Task<IActionResult> DeleteScheduleEntry(int id)
        {
            try
            {
                if (await db.EntryExistsForDate(id))
                {
                    await db.DeleteDataEntry(id);
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting schedule entry: " + ex.Message);
            }
        }
         

        #endregion

        
    }
}
