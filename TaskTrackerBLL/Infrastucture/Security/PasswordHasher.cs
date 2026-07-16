using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerDAL.Models;
using Microsoft.AspNetCore.Identity;
namespace TaskTrackerBLL.Infrastucture.Security
{
    public class PasswordHasher:IPasswordHasher
    {
        private readonly PasswordHasher<User> _identityHasher = new();

        public string HashPassword(string password)
        {
            return _identityHasher.HashPassword(user: null!, password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _identityHasher.VerifyHashedPassword(user: null!, hashedPassword, providedPassword);

            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
