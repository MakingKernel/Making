namespace Mark.AspNetCore;

public class PagedResult<T>(PagingModel paging, IReadOnlyList<T> data)
{
    public PagingModel Paging => paging;

    public IReadOnlyList<T> Data => data;

    public static PagedResult<T> Create(PagingModel paging, IReadOnlyList<T> data)
    {
        return new PagedResult<T>(paging, data);
    }
}