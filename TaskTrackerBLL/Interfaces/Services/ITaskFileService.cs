using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.TaskFile;
using Microsoft.AspNetCore.Http;
namespace TaskTrackerBLL.Interfaces.Services
{
    public interface ITaskFileService
    {
        Task<Result<IReadOnlyList<TaskFileDto>>> GetByTaskIdAsync(int taskId);

        Task<Result<TaskFileDto>> GetByIdAsync(int id);

        Task<Result<TaskFileDto>> UploadAsync(
            int taskId,
            IFormFile file,
            string uploadFolder);

        Task<Result<bool>> DeleteAsync(
            int id,
            string uploadFolder);

        Task<Result<bool>> DeleteByTaskIdAsync(int taskId);
    }
}
