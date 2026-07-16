using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.Interfaces.Security
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);

        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}
