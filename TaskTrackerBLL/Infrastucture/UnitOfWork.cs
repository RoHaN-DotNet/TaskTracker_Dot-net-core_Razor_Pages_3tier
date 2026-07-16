using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Interfaces;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Repositories;

namespace TaskTrackerBLL.Infrastucture
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TaskTrackerDbContext _context;

        private ICompanyFeature? _companies;
        private IUserFeature? _users;
        private IRoleFeature? _roles;
        private IProjectFeature? _projects;
        private ITaskFeature? _tasks;

        public UnitOfWork(TaskTrackerDbContext context)
        {
            _context = context;
        }
        public ICompanyFeature Companies => _companies ??= new CompanyRepository(_context);

        public IUserFeature Users => _users ??= new UserRepository(_context);

        public IRoleFeature Roles => _roles ??= new RoleRepository(_context);

        public IProjectFeature Projects => _projects ??= new ProjectRepository(_context);

        public ITaskFeature Tasks => _tasks ??= new TaskRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
