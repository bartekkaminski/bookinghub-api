namespace BookingHub.Api.Repositories.Common;

/// <summary>
/// Wynik stronicowanego zapytania zawierający dane oraz metadane paginacji.
/// </summary>
/// <typeparam name="T">Typ elementów w kolekcji wynikowej.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Elementy bieżącej strony.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Numer bieżącej strony (licząc od 1).</summary>
    public int Page { get; }

    /// <summary>Maksymalna liczba elementów na stronie.</summary>
    public int PageSize { get; }

    /// <summary>Łączna liczba elementów spełniających kryteria filtrowania.</summary>
    public int TotalCount { get; }

    /// <summary>Łączna liczba stron.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Czy istnieje poprzednia strona.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Czy istnieje następna strona.</summary>
    public bool HasNextPage => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>Tworzy pusty wynik paginacji.</summary>
    public static PagedResult<T> Empty(int page, int pageSize)
        => new([], page, pageSize, 0);

    /// <summary>Projektuje elementy na inny typ za pomocą selektora.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector)
        => new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
