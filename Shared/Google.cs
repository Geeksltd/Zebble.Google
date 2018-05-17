namespace Zebble
{
    public partial class Google
    {
        public static readonly AsyncEvent<User> UserSignedIn = new AsyncEvent<User>();

        public class User
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string GivenName { get; set; }
            public string FamilyName { get; set; }
            public string Picture { get; set; }
            public string Email { get; internal set; }
            public string Token { get; set; }
        }
    }
}