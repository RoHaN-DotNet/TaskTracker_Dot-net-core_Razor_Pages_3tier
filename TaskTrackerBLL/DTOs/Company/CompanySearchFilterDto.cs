using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Company
{
    public class CompanySearchFilterDto
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive {  get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
