using System.Collections.Generic;

namespace Making.MiniApis.Analyzers.Constants
{
    internal static class MiniApiConstants
    {
        public const string MiniApiAttributeName = "MiniApiAttribute";
        public const string ServiceSuffix = "Service";
        public const string AsyncSuffix = "Async";
        public const string IgnoreRouteAttributeName = "IgnoreRouteAttribute";
        public const string FilterAttributeName = "FilterAttribute";
        
        public static readonly HashSet<string> HttpMethodAttributes = new()
        {
            "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute", 
            "HttpDeleteAttribute", "HttpPatchAttribute", "HttpHeadAttribute", "HttpOptionsAttribute"
        };
        
        public static readonly HashSet<string> SystemAssemblyPrefixes = new()
        {
            "System", "Microsoft.", "mscorlib", "netstandard", "runtime"
        };
        
        public static readonly Dictionary<string, string> MvcHttpMethodAttributes = new()
        {
            { "HttpGetAttribute", "Get" },
            { "HttpPostAttribute", "Post" },
            { "HttpPutAttribute", "Put" },
            { "HttpDeleteAttribute", "Delete" },
            { "HttpPatchAttribute", "Patch" },
            { "HttpHeadAttribute", "Head" },
            { "HttpOptionsAttribute", "Options" }
        };
    }
}