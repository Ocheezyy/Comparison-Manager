namespace DeathVerificationFW
{
    public class EnvMethods
    {
        public static string GetCurrentUser()
        {
            var name = System.Environment.UserName;
            return name;
        }
    }
}
