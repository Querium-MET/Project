namespace Querim.Services
{
    public interface IAuthService
    {
        bool VerifyPassword(string inputPassword, string storedPassword);
    }

    public class AuthService : IAuthService
    {
        public bool VerifyPassword(string inputPassword, string storedPassword)
        {
            return inputPassword == storedPassword;
        }
    }
}
