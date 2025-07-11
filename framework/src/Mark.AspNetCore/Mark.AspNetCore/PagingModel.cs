namespace Mark.AspNetCore;

public class PagingModel
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}