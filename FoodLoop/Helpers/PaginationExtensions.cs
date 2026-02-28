using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Helpers
{
    public static class PaginationExtensions
    {
        public static async Task<(List<T> Items, int TotalPages)>
            ToPagedListAsync<T>(this IQueryable<T> query, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalPages);
        }
    }
}