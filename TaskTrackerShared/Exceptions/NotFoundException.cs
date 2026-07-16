using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerShared.Exceptions
{
    public class NotFoundException:Exception
    {
        public NotFoundException(string entityName,object key):base($"{entityName} with identifier '{key} was not found.")
        {

        }
    }
}
