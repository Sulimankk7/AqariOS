using System.Collections.Generic;

namespace PropertyOS.Application.Common.Models;

public class KeysetPage<T>
{
    public List<T> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }

    public KeysetPage() { }

    public KeysetPage(List<T> items, string? nextCursor, bool hasMore)
    {
        Items = items;
        NextCursor = nextCursor;
        HasMore = hasMore;
    }
}
