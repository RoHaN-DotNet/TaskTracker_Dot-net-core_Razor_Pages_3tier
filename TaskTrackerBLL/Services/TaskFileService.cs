using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.TaskFile;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models;
using Microsoft.AspNetCore.Http;

namespace TaskTrackerBLL.Services
{
    public class TaskFileService:ITaskFileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TaskFileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IReadOnlyList<TaskFileDto>>> GetByTaskIdAsync(
            int taskId)
        {
            var files = await _unitOfWork.TaskFiles
                .GetByTaskIdAsync(taskId);

            var dtos = files.Select(f => new TaskFileDto
            {
                Id = f.Id,
                TaskId = f.TaskId,
                FileName = f.FileName,
                StoredFileName = f.StoredFileName,
                FilePath = f.FilePath,
                FileSize = f.FileSize,
                UploadedAt = f.UploadedAt
            }).ToList();

            return Result<IReadOnlyList<TaskFileDto>>
                .Success(dtos);
        }

        public async Task<Result<TaskFileDto>> GetByIdAsync(int id)
        {
            var file = await _unitOfWork.TaskFiles
                .GetByIdWithTaskAsync(id);

            if (file == null)
            {
                return Result<TaskFileDto>
                    .Failure("File not found.");
            }

            var dto = new TaskFileDto
            {
                Id = file.Id,
                TaskId = file.TaskId,
                FileName = file.FileName,
                StoredFileName = file.StoredFileName,
                FilePath = file.FilePath,
                FileSize = file.FileSize,
                UploadedAt = file.UploadedAt
            };

            return Result<TaskFileDto>.Success(dto);
        }

        public async Task<Result<TaskFileDto>> UploadAsync(
    int taskId,
    IFormFile file,
    string uploadFolder)
        {
            if (file == null || file.Length == 0)
            {
                return Result<TaskFileDto>
                    .Failure("Please select a file.");
            }

            var task = await _unitOfWork.Tasks
                .GetByIdAsync(taskId);

            if (task == null)
            {
                return Result<TaskFileDto>
                    .Failure("Task not found.");
            }

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var extension = Path.GetExtension(file.FileName);

            var storedFileName =
                $"{Guid.NewGuid()}{extension}";

            // Physical path — only used to save the actual file
            var filePath = Path.Combine(
                uploadFolder,
                storedFileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var taskFile = new TaskFile
            {
                TaskId = taskId,
                FileName = file.FileName,
                StoredFileName = storedFileName,

                // Save ONLY filename in database
                FilePath = "\\" + storedFileName,

                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            await _unitOfWork.TaskFiles
                .AddAsync(taskFile);

            await _unitOfWork.SaveChangesAsync();

            var dto = new TaskFileDto
            {
                Id = taskFile.Id,
                TaskId = taskFile.TaskId,
                FileName = taskFile.FileName,
                StoredFileName = taskFile.StoredFileName,

                // DTO also gets only filename
                FilePath = storedFileName,

                FileSize = taskFile.FileSize,
                UploadedAt = taskFile.UploadedAt
            };

            return Result<TaskFileDto>.Success(dto);
        }

        public async Task<Result<bool>> DeleteAsync(
            int id,
            string uploadFolder)
        {
            var file = await _unitOfWork.TaskFiles.GetByIdAsync(id);

            if (file == null)
            {
                return Result<bool>
                    .Failure("File not found.");
            }

            var physicalPath = Path.Combine(
                uploadFolder,
                file.StoredFileName);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            _unitOfWork.TaskFiles.Remove(file);

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteByTaskIdAsync(int taskId)
        {
            await _unitOfWork.TaskFiles
                .DeleteByTaskIdAsync(taskId);

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
