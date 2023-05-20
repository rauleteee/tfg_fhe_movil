using System;
using System.Collections.Generic;
using Xamarin.Forms;
 
namespace UsersFlow.Model
{
    
	public class UserModel
	{

        public int _id { get; set; }
        public string username { get; set; }
		public string password { get; set; }
        public string Name { get; set; }
		public string Birth { get; set; }
		public string DNI { get; set; }
		public string SegSocialNumber { get; set; }
		public string IBAN { get; set; }
		public string cipherIban { get; set; }
		public string cipherDNI { get; set; }
		public string cipherSegSocial { get; set; }
		public string Privilege { get; set; }
        /** Token for push notifications */
        public string token { get; set; }
		/*
		 * Fingerprint user ID
		 */
		public ulong ? securityToken { get; set; }
        /* 
		 * Encrypted encryption parms
		 */
		public string encParms { get; set; }

        public UserModel(string _username, string _password)
        {

            if (Application.Current.Properties.ContainsKey("username") && Application.Current.Properties.ContainsKey("password"))
            {
                var _Username = Application.Current.Properties["username"];
                var _Password = Application.Current.Properties["password"];
                //var _Name = Application.Current.Properties["Name"];
                /*
                var _Birth = Application.Current.Properties["Birth"];
                var _DNI = Application.Current.Properties["DNI"];
                var _SegSocialNumber = Application.Current.Properties["SegSocialNumber"];
                var _IBAN = Application.Current.Properties["IBAN"];
                username = _Username.ToString();
                password = _Password.ToString();
                Name = _Name.ToString();
                
                Birth = _Birth.ToString();
                DNI = _DNI.ToString();
                SegSocialNumber = _SegSocialNumber.ToString();
                IBAN = _IBAN.ToString();*/
            }
        }
        public string GetUsername()
        {
            return username;
        }
        public string GetPassword()
        {
            return password;
        }
        /*
        public string GetName()
        {
            return Name;
        }
        
        public string GetBirth()
        {
            return Birth;
        }
        public string GetDNI()
        {
            return DNI;
        }
        public string GetSegSocialNumber()
        {
            return SegSocialNumber;
        }
        public string GetIBAN()
        {
            return IBAN;
        }*/

    }
}

