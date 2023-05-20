using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Research.SEAL;
using Newtonsoft.Json;
using UsersFlow.Model;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace UsersFlow.ModelView
{
	public class FHEHandler
	{
        #region Variables
        /*
         * Encryption params with FHE (BFV)
         */

        public static EncryptionParameters parms;
        public static KeyGenerator keygen;
        public static ulong polyModulusDegree;
        public static SEALContext context;
        public static SecretKey secretKey;
        public static Encryptor encryptor;
        public static Evaluator evaluator;
        public static Decryptor decryptor;
        public static string userIdSended;
        public static string userIdReturned;
        #endregion
        public FHEHandler()
		{
            evaluator = new Evaluator(context);
        }

        #region FHE functions
        public async static Task<Schedule> cipherSchedule(Schedule schedule, UserModel user)
        {
            try
            {
                Schedule scheduleCiphered = new Schedule();
                /*************************************************************
                 * Loading encrypted parammeters
                 *************************************************************/
                scheduleCiphered.encParms = user.encParms;
                var bytesParms = Convert.FromBase64String(user.encParms);
                MemoryStream streamParms = new MemoryStream(bytesParms);

                EncryptionParameters encParms = new EncryptionParameters();
                encParms.Load(streamParms);

                /*************************************************************
                 * Context and decryptor, retrieving secret key from local storage
                 *************************************************************/
                // Retrieve the key from secure storage of the phone
                var bytesSecretKey = Convert.FromBase64String(await SecureStorage.GetAsync("SecretKey-" + user.username));
                MemoryStream streamSecretKey = new MemoryStream(bytesSecretKey);
                SecretKey retrievedSecretKey = new SecretKey();

                context = new SEALContext(encParms);
                retrievedSecretKey.Load(context, streamSecretKey);
                encryptor = new Encryptor(context, retrievedSecretKey);
                /****************************************
                * Encrypt date
                ***************************************/

                List<string> auxCiphDate = new List<string>();

                foreach (char c in schedule.date)
                {
                    string str = c.ToString();
                    byte Byte = Encoding.UTF32.GetBytes(str)[0];
                    ulong long_Byte = Convert.ToUInt64(Byte);
                    Ciphertext xEncrypt1 = new Ciphertext();
                    Plaintext xPlain1 = new Plaintext(ULongToString(long_Byte));
                    try
                    {
                        encryptor.EncryptSymmetric(xPlain1, xEncrypt1);
                        MemoryStream charStream = new MemoryStream();
                        xEncrypt1.Save(charStream);
                        var stringWord = Convert.ToBase64String((charStream as MemoryStream).ToArray());
                        auxCiphDate.Add(stringWord);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                // set the list of strings into a json (string) format
                scheduleCiphered.date = JsonConvert.SerializeObject(auxCiphDate);
                /****************************************
                * Encrypt entry_hour
                ***************************************/
                
                try
                {
                    Ciphertext xEncrypt2 = new Ciphertext();
                    Plaintext xPlain2 = new Plaintext(schedule.entry_hour_ciph);
                    encryptor.EncryptSymmetric(xPlain2, xEncrypt2);

                    // stream to string
                    MemoryStream streamEntry_Hour = new MemoryStream();
                    xEncrypt2.Save(streamEntry_Hour);
                    var stringCiphEntryHour = Convert.ToBase64String((streamEntry_Hour as MemoryStream).ToArray());
                    //byte[] binaryCiphEntryHour = streamEntry_Hour.ToArray();
                    Console.WriteLine($"[FHE] Entry_hour cihpered : {stringCiphEntryHour}");

                    // save the ciphered entry hour into the object
                    scheduleCiphered.entry_hour_ciph = stringCiphEntryHour;




                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                /****************************************
               * Encrypt leave_hour
               ***************************************/
                try
                {
                    Ciphertext xEncrypt3 = new Ciphertext();
                    Plaintext xPlain3 = new Plaintext(schedule.leave_hour_ciph);
                    encryptor.EncryptSymmetric(xPlain3, xEncrypt3);

                    // stream to string
                    MemoryStream streamLeave_Hour = new MemoryStream();
                    xEncrypt3.Save(streamLeave_Hour);
                    var stringCiphLeaveHour = Convert.ToBase64String((streamLeave_Hour as MemoryStream).ToArray());
                    Console.WriteLine($"[FHE] Leave_hour cihpered : {stringCiphLeaveHour}");

                    // save the ciphered entry hour into the object
                    scheduleCiphered.leave_hour_ciph = stringCiphLeaveHour;


                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


                //set the id associated to an user
                scheduleCiphered.entry_hour = 0;
                scheduleCiphered.leave_hour = 0;
                scheduleCiphered.balance = 0;
                scheduleCiphered.balance_ciph = "";
                scheduleCiphered.user_id = user._id;
                //scheduleCiphered._id = 0;
                //return the ciphered scheule
                return scheduleCiphered;

            }
            catch(Exception ex) { }

            return null;
            
        }
        public async static Task<Schedule> decryptSchedule(Schedule cipheredSchedule, UserModel user)
        {
            Schedule decryptedSchedule = new Schedule();
            try
            {

                /*************************************************************
                 * Loading encrypted parammeters
                 *************************************************************/

                var bytesParms = Convert.FromBase64String(user.encParms);
                MemoryStream streamParms = new MemoryStream(bytesParms);

                EncryptionParameters encParms = new EncryptionParameters();
                encParms.Load(streamParms);

                /*************************************************************
                 * Context and decryptor, retrieving secret key from local storage
                 *************************************************************/
                // Retrieve the key from secure storage of the phone
                var bytesSecretKey = Convert.FromBase64String(await SecureStorage.GetAsync("SecretKey-" + user.username));
                MemoryStream streamSecretKey = new MemoryStream(bytesSecretKey);
                SecretKey retrievedSecretKey = new SecretKey();

                context = new SEALContext(encParms);
                retrievedSecretKey.Load(context, streamSecretKey);
                decryptor = new Decryptor(context, retrievedSecretKey);

                /****************************************
                 * Date Number decryption
                 ***************************************/
                var CipheredDate = JsonConvert.DeserializeObject<List<string>>(cipheredSchedule.date);
                var decryptedMsg = new char[CipheredDate.Count + 1];

                int i = 0;
                foreach (var xEncrypted in CipheredDate)
                {
                    var ByteMsgWordStream = Convert.FromBase64String(xEncrypted);
                    Stream contentWordMsg = new MemoryStream(ByteMsgWordStream);

                    Ciphertext ld3 = new Ciphertext();
                    ld3.Load(context, contentWordMsg);

                    Plaintext pt3 = new Plaintext();
                    decryptor.Decrypt(ld3, pt3);

                    char decrypted = (char)short.Parse(pt3.ToString(),
                        NumberStyles.AllowHexSpecifier);

                    decryptedMsg[i] = decrypted;
                    i++;
                }
                string recovered_date = new string(decryptedMsg);
                // Save it to the retrieved user from database
                decryptedSchedule.date = recovered_date;
                /****************************************
                 * EntryHour Number decryption
                 ***************************************/
                var BytePostStream = Convert.FromBase64String(cipheredSchedule.entry_hour_ciph);
                Stream streamEntryHour = new MemoryStream(BytePostStream);

                Ciphertext ld = new Ciphertext();
                ld.Load(context, streamEntryHour);

                Plaintext pt2 = new Plaintext();
                decryptor.Decrypt(ld, pt2);

                decryptedSchedule.entry_hour_ciph = Convert.ToInt32(ULongToString(pt2[0]), 16).ToString();



                /****************************************
                 * LeaveHour Number decryption
                 ***************************************/
                var BytePostStream1 = Convert.FromBase64String(cipheredSchedule.leave_hour_ciph);
                Stream streamLeaveHour = new MemoryStream(BytePostStream1);

                Ciphertext cphLeave = new Ciphertext();
                cphLeave.Load(context, streamLeaveHour);

                Plaintext pt5 = new Plaintext();
                decryptor.Decrypt(cphLeave, pt5);

                decryptedSchedule.leave_hour_ciph = Convert.ToInt32(ULongToString(pt5[0]), 16).ToString();


                /****************************************
                 * Balance Number decryption
                 ***************************************/
                var BytePostStream2 = Convert.FromBase64String(cipheredSchedule.balance_ciph);
                Stream streamBalance = new MemoryStream(BytePostStream2);

                Ciphertext cphBal = new Ciphertext();
                cphBal.Load(context, streamBalance);

                Plaintext pt4 = new Plaintext();
                decryptor.Decrypt(cphBal, pt4);
                decryptedSchedule.balance_ciph = Convert.ToInt32(ULongToString(pt4[0]), 16).ToString();
               

                return decryptedSchedule;

            }
            catch (Exception ex)
            {

            }
            return null;
        }
        public async static Task<UserModel> encryptData(UserModel _userModel)
        {
            /******************************************************
             * Inicializacion de Objetos para esquema BFV con utilizacion de enteros 
             ******************************************************/
            parms = new EncryptionParameters(SchemeType.BFV);
            polyModulusDegree = 8192;
            parms.PolyModulusDegree = polyModulusDegree;
            parms.PlainModulus = PlainModulus.Batching(polyModulusDegree, 20);
            parms.CoeffModulus = CoeffModulus.BFVDefault(polyModulusDegree);
            context = new SEALContext(parms);
            keygen = new KeyGenerator(context);
            secretKey = keygen.SecretKey;
            encryptor = new Encryptor(context, secretKey);
            /*********************************
             * Save secret key into local storage of the phone
             *********************************/
            Console.WriteLine("[FHE] Secret key generated: " + secretKey);
            //todo asociar la secret key al token de seguridad en vez de al username
            MemoryStream secretKeyBytes = new MemoryStream();
            secretKey.Save(secretKeyBytes);
            var strSecretKey = Convert.ToBase64String((secretKeyBytes as MemoryStream).ToArray());
            await SecureStorage.SetAsync("SecretKey-" + _userModel.username, strSecretKey);
            /*
             *  STREAM -> STRING (PARMS)
             */
            MemoryStream streamParms = new MemoryStream();
            parms.Save(streamParms);
            var stringParms = Convert.ToBase64String((streamParms as MemoryStream).ToArray());
            _userModel.encParms = stringParms;

            /****************************************
             * Encrypt IBAN
             ***************************************/
            // Char-by-char encryption
            if (_userModel.IBAN.Length != 24)
            {
                //DisplayAlert("Incorrect IBAN's Prefix format", "Please, introduce a correct IBAN", "Ok");
                Console.WriteLine("Incorrect IBAN's Prefix format");
            }

            List<string> auxCiphIban = new List<string>();

            foreach (char c in _userModel.IBAN)
            {
                string str = c.ToString();
                byte Byte = Encoding.UTF32.GetBytes(str)[0];
                ulong long_Byte = Convert.ToUInt64(Byte);
                Ciphertext xEncrypt1 = new Ciphertext();
                Plaintext xPlain1 = new Plaintext(ULongToString(long_Byte));
                try
                {
                    encryptor.EncryptSymmetric(xPlain1, xEncrypt1);
                    MemoryStream charStream = new MemoryStream();
                    xEncrypt1.Save(charStream);
                    var stringWord = Convert.ToBase64String((charStream as MemoryStream).ToArray());
                    auxCiphIban.Add(stringWord);
                    //_userModel.cipherIban.Add(byteComp);
                    // Console.WriteLine("CIPHERED CHAR: " + stringWord);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            // set the list of strings into a json (string) format
            _userModel.cipherIban = JsonConvert.SerializeObject(auxCiphIban);
            /****************************************
             * Encrypt National Document Identity
             ***************************************/
            List<string> auxCiphDni = new List<string>();
            foreach (char c in _userModel.DNI)
            {
                string str = c.ToString();
                byte Byte = Encoding.UTF32.GetBytes(str)[0];
                ulong long_Byte = Convert.ToUInt64(Byte);
                Ciphertext xEncrypt1 = new Ciphertext();
                Plaintext xPlain1 = new Plaintext(ULongToString(long_Byte));
                try
                {
                    encryptor.EncryptSymmetric(xPlain1, xEncrypt1);
                    MemoryStream charStream = new MemoryStream();
                    xEncrypt1.Save(charStream);
                    var stringWord = Convert.ToBase64String((charStream as MemoryStream).ToArray());
                    auxCiphDni.Add(stringWord);
                    // Console.WriteLine("CIPHERED CHAR: " + stringWord);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            _userModel.cipherDNI = JsonConvert.SerializeObject(auxCiphDni);
            /****************************************
             * Encrypt Social's Secuty Number
             ***************************************/
            List<string> auxCiphSegSoc = new List<string>();

            foreach (char c in _userModel.SegSocialNumber)
            {
                string str = c.ToString();
                byte Byte = Encoding.UTF32.GetBytes(str)[0];
                ulong long_Byte = Convert.ToUInt64(Byte);
                Ciphertext xEncrypt1 = new Ciphertext();
                Plaintext xPlain1 = new Plaintext(ULongToString(long_Byte));
                try
                {
                    encryptor.EncryptSymmetric(xPlain1, xEncrypt1);
                    MemoryStream charStream = new MemoryStream();
                    xEncrypt1.Save(charStream);
                    var stringWord = Convert.ToBase64String((charStream as MemoryStream).ToArray());
                    auxCiphSegSoc.Add(stringWord);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            _userModel.cipherSegSocial = JsonConvert.SerializeObject(auxCiphSegSoc);
            
            /** remove seensible data */
            _userModel.DNI = null;
            _userModel.IBAN = null;
            _userModel.SegSocialNumber = null;
            
            return _userModel;

        }
        public async static Task<UserModel> decryptUserFromDB (UserModel CipheredUser)
        {
            SEALContext context;
            Decryptor decryptor;
            try
            {

                /*************************************************************
                 * Loading encrypted parammeters
                 *************************************************************/
                string CurrentUserJson1 = App.Current.Properties["CurrentUser"].ToString();
                UserModel CurrentUser = JsonConvert.DeserializeObject<UserModel>(CurrentUserJson1);

                var bytesParms = Convert.FromBase64String(CipheredUser.encParms);
                //var bytesParms = Convert.FromBase64String(CipheredUser.encParms as MemoryStream);
                MemoryStream streamParms = new MemoryStream(bytesParms);

                EncryptionParameters encParms = new EncryptionParameters();
                encParms.Load(streamParms);

                

                /*************************************************************
                 * Context and decryptor, retrieving secret key from local storage
                 *************************************************************/
                // Retrieve the key from secure storage of the phone


                var bytesSecretKey = Convert.FromBase64String(await SecureStorage.GetAsync("SecretKey-" + CipheredUser.username));
                //Console.WriteLine($"[FHE] CLIENT SECRET KEY RETRIEVED FROM SECURE STORAGE: {await SecureStorage.GetAsync("SecretKey-" + CurrentUser.username)}");
                MemoryStream streamSecretKey = new MemoryStream(bytesSecretKey);

                // We will ask to the manager to retrieve the secret Key
                SecretKey retrievedSecretKey = new SecretKey();
                
                context = new SEALContext(encParms);
                
                retrievedSecretKey.Load(context, streamSecretKey);
                decryptor = new Decryptor(context, retrievedSecretKey);

                /****************************************
                 * ciphered DNI decryption
                 ***************************************/
                var CipheredDNIFromUser = JsonConvert.DeserializeObject<List<string>>(CipheredUser.cipherDNI);
                var decryptedMsg = new char[CipheredDNIFromUser.Count + 1];
                int i = 0;
                foreach (var xEncrypted in CipheredDNIFromUser)
                {
                    var ByteMsgWordStream = Convert.FromBase64String(xEncrypted);
                    Stream contentWordMsg = new MemoryStream(ByteMsgWordStream);

                    Ciphertext ld3 = new Ciphertext();
                    ld3.Load(context, contentWordMsg);

                    Plaintext pt3 = new Plaintext();
                    decryptor.Decrypt(ld3, pt3);

                    char decrypted = (char)short.Parse(pt3.ToString(),
                       NumberStyles.AllowHexSpecifier);
                    //if (Int16.TryParse(pt3.ToString(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out short result))
                    //{
                       // char decrypted = (char)result;
                        // use decrypted char value
                   decryptedMsg[i] = decrypted;
                   // }
                    i++;
                }
                string recovered_dni = new string(decryptedMsg);
                // Save it to the retrieved user from database
                CipheredUser.DNI = recovered_dni;

                /****************************************
                 * ciphered Security Social's Number decryption
                 ***************************************/
                var CipheredSegSocialFromUser = JsonConvert.DeserializeObject<List<string>>(CipheredUser.cipherSegSocial);
                var decryptedMsg1 = new char[CipheredSegSocialFromUser.Count + 1];

                int j = 0;
                foreach (var xEncrypted in CipheredSegSocialFromUser)
                {
                    var ByteMsgWordStream = Convert.FromBase64String(xEncrypted);
                    Stream contentWordMsg = new MemoryStream(ByteMsgWordStream);

                    Ciphertext ld3 = new Ciphertext();
                    ld3.Load(context, contentWordMsg);

                    Plaintext pt3 = new Plaintext();
                    decryptor.Decrypt(ld3, pt3);

                    char decrypted = (char)short.Parse(pt3.ToString(),
                        NumberStyles.AllowHexSpecifier);

                    decryptedMsg1[j] = decrypted;
                    j++;
                }
                string recovered_segsocialnumb = new string(decryptedMsg1);
                // Save it to the retrieved user from database
                CipheredUser.SegSocialNumber = recovered_segsocialnumb;

                /****************************************
                 * ciphered IBAN's Number decryption
                 ***************************************/
                var CipheredIbanFromUser = JsonConvert.DeserializeObject<List<string>>(CipheredUser.cipherIban);
                var decryptedMsg2 = new char[CipheredIbanFromUser.Count + 1];

                int k = 0;
                foreach (var xEncrypted in CipheredIbanFromUser)
                {
                    var ByteMsgWordStream = Convert.FromBase64String(xEncrypted);
                    Stream contentWordMsg = new MemoryStream(ByteMsgWordStream);

                    Ciphertext ld3 = new Ciphertext();
                    ld3.Load(context, contentWordMsg);

                    Plaintext pt3 = new Plaintext();
                    decryptor.Decrypt(ld3, pt3);

                    char decrypted = (char)short.Parse(pt3.ToString(),
                        NumberStyles.AllowHexSpecifier);

                    decryptedMsg2[k] = decrypted;
                    k++;
                }
                string recovered_iban = new string(decryptedMsg2);
                // Save it to the retrieved user from database
                CipheredUser.IBAN = recovered_iban;
                /****************************************
                 * Ciphered Password
                 ***************************************/

                return CipheredUser;
            }
            catch(ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;

        }


        #endregion
        #region Auxiliar functions
        public static string ULongToString(ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToString(bytes).Replace("-", "");
        }
        public static Stream StringToStream(string src)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(src);
            return new MemoryStream(byteArray);
        }
        public static ulong StringToULong(string str)
        {
            byte[] bytes = new byte[8];
            byte[] strBytes = Encoding.UTF8.GetBytes(str);

            for (int i = 0; i < strBytes.Length && i < 8; i++)
            {
                bytes[i] = strBytes[i];
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt64(bytes, 0);
        }

        #endregion
    }
}

