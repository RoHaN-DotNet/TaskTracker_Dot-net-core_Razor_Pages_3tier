using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Infrastucture;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Repositories;

namespace TaskTrackerBLL.Interfaces
{
    public interface IUnitOfWork:IDisposable
    {
        ICompanyFeature Companies { get; }

        IUserFeature Users { get; }

        IRoleFeature Roles { get; }

        IProjectFeature Projects { get; }

        ITaskFeature Tasks { get; }

        Task<int> SaveChangesAsync();
    }
}
