using System.Collections.Generic;

namespace PropertyOS.Application.DTOs.Subscriptions;

/// <summary>
/// Generic paginated response wrapper.
/// </summary>
/// <typeparam name="T">Type of items in the page.</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Collection of items on current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Current page index (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items requested per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total count of matching records across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total calculated number of pages.
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / (PageSize > 0 ? PageSize : 1);

    /// <summary>
    /// Indicates whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates whether a next page exists.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
