using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.TaskFile
{
    public class TaskFileDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string StoredFileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; }

    }
}
