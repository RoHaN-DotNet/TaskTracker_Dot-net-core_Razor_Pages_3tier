using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerShared.Exceptions
{
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}
