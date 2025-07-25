using Making.AspNetCore;

namespace Making.MiniApis.SimpleTest;

[MiniApi(Route = "/api/v1/test")]
public class TestService
{
    public string GetDemo(GetTestInput input)
    {
        return "您好";
    }

    public class GetTestInput : PageInput
    {
    }

    public class PageInput
    {
        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}