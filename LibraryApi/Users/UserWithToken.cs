namespace LibraryApi.Users
{
    using LibraryApi.Controllers;

    public class UserWithToken : UserInfo
    {
        public string Token { get; set; }
    }
}