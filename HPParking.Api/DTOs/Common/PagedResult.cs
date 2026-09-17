using System.Collections.Generic;

namespace HPParking.Api.DTOs.Common
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public PaginationMetadata Pagination { get; set; } = new();

        public PagedResult() { }

        public PagedResult(IReadOnlyList<T> items, int pageIndex, int pageSize, long totalCount)
        {
            Items = items ?? new List<T>();
            Pagination = new PaginationMetadata(pageIndex, pageSize, totalCount);
        }
    }
}
