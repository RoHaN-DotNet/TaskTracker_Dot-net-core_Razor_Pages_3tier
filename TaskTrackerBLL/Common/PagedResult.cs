using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.Common
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount {  get; }

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalCount;

        public PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber=pageNumber;
            PageSize=pageSize;
            TotalCount= totalCount;
        }

    }
}
