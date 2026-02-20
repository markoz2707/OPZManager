using Microsoft.EntityFrameworkCore;
using OPZManager.API.DTOs.Common;

namespace OPZManager.API.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PaginatedResponseDto<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> source,
            PaginationParams paginationParams)
        {
            var totalCount = await source.CountAsync();
            var items = await source
                .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PaginatedResponseDto<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = paginationParams.Page,
                PageSize = paginationParams.PageSize
            };
        }
    }
}
