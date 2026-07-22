using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Repositories.Generic;

namespace TaskTrackerDAL.Repositories
{
    public class AuditLogRepository: GenericRepository<AuditLog>,IAuditLogFeature
    {
        public AuditLogRepository(TaskTrackerDbContext context) : base(context) 
        { 

        }
        public async Task<IReadOnlyList<AuditLog>> GetForEntityAsync(string modelName, int modelId)
        {
            return await _context.Set<AuditLog>()
                .Include(a => a.PerformedByUser)
                .AsNoTracking()
                .Where(a=>a.ModelName==modelName && a.ModelId==modelId)
                .OrderByDescending(a =>a.CreatedAt)
                .ToListAsync();
        }
        public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(int performedByUserId, int pageNumber, int pageSize)
        {
            return await _context.Set<AuditLog>()
                .Include(a => a.PerformedByUser)
                .AsNoTracking()
                .Where(a => a.PerformedByUserId == performedByUserId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task LogAsync(AuditLog entry)
        {
            await _context.Set<AuditLog>().AddAsync(entry);
        }
    }
}
