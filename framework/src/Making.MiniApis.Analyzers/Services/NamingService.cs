using Making.MiniApis.Analyzers.Constants;

namespace Making.MiniApis.Analyzers.Services
{
    internal static class NamingService
    {
        public static string GenerateMapMethodName(string className)
        {
            var baseName = className.EndsWith(MiniApiConstants.ServiceSuffix) ? className.Substring(0, className.Length - MiniApiConstants.ServiceSuffix.Length) : className;
            return baseName;
        }

        public static string GenerateInstanceName(string className)
        {
            var baseName = className.EndsWith(MiniApiConstants.ServiceSuffix) ? className.Substring(0, className.Length - MiniApiConstants.ServiceSuffix.Length) : className;
            return char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);
        }

        public static string NormalizeMethodName(string methodName)
        {
            var normalized = methodName;
            
            if (normalized.EndsWith(MiniApiConstants.AsyncSuffix, System.StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - MiniApiConstants.AsyncSuffix.Length);
            }

            if (normalized.EndsWith(MiniApiConstants.ServiceSuffix, System.StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - MiniApiConstants.ServiceSuffix.Length);
            }

            if (!normalized.StartsWith("/"))
                normalized = "/" + normalized;

            return normalized;
        }
    }
}