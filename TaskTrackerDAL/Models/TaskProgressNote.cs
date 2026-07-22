using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Common;

namespace TaskTrackerDAL.Models
{
    public class TaskProgressNote:BaseModel
    {
        public int TaskId {  get; set; }
        public int AuthorUserId {  get; set; }
        public string Note {  get; set; }=string.Empty;
        public bool IsCompletionComment {  get; set; }

        //Navigation properties

        public ProjectTask Task { get; set; } = null!;
        public User AuthorUser{ get; set; } = null!;
    }
}
