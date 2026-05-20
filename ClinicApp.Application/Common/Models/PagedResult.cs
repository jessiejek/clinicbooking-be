using System.Text.Json.Serialization;

namespace ClinicApp.Application.Common.Models;

public sealed class PagedResult<T>
{
    public PagedResult(List<T> items, int totalCount, int page, int pageSize, int totalPages)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = totalPages;
    }

    public List<T> Items { get; }

    public int TotalCount { get; }

    [JsonIgnore]
    public int Total => TotalCount;

    public int Page { get; }

    public int PageSize { get; }

    public int TotalPages { get; }
}
