using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerDAL.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Developer = "Developer";
        public const string Tester = "Tester";
        public const string Debugger = "Debugger";
        public const string Designer = "Designer";
        public const string UiUxDesigner = "UI/UX";
        public const string TeamLead = "Team Lead";
        public const string Accountant = "Accountant";
        public const string HR = "HR";
        public const string ProjectManager = "Project Manger";
        public static readonly string[] EmployeeRoles =
        {
        Developer, Tester, Debugger, Designer, UiUxDesigner, TeamLead, Accountant, HR, ProjectManager
    };

        public static readonly string[] All =
        {
        Admin, Manager, Developer, Tester, Debugger, Designer, UiUxDesigner, TeamLead, Accountant,HR,ProjectManager
    };
    }
}
