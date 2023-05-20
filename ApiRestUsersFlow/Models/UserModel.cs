

namespace ApiUsersFlow.Models
{
	public class UserModel
	{
        public int ? _id { get; set; }
        public string ? username { get; set; }
        public string ? password { get; set; }
        public string ? Name { get; set; }
        public string ? Birth { get; set; }
        public string ? DNI { get; set; }
        public string ? SegSocialNumber { get; set; }
        public string ? IBAN { get; set; }
        public string ? cipherIban { get; set; }
        public string ? cipherDNI { get; set; }
        public string ? cipherSegSocial { get; set; }
        public string ? Privilege { get; set; }
        public string ? token {get; set;}
        /*
		 * Fingerprint securityToken for authorization
		 */
        public string ? securityToken { get; set; }
        /* 
		 * Encrypted encryption parms FHE
		 */
        public string ? encParms { get; set; }
    }
}

