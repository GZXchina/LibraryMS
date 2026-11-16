namespace LibraryMS.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string UserRole { get; set; }
        public string ReaderId { get; set; }

        public bool IsAdmin
        {
            get { return UserRole == "Admin"; }
        }
    }
}
