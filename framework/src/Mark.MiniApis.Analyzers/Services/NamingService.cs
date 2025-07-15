using Mark.MiniApis.Analyzers.Constants;

namespace Mark.MiniApis.Analyzers.Services
{
    internal static class NamingService
    {
        public static string GenerateMapMethodName(string className)
        {
            var baseName = className.TrimEnd(MiniApiConstants.ServiceSuffix);
            return baseName;
        }

        public static string GenerateInstanceName(string className)
        {
            var baseName = className.TrimEnd(MiniApiConstants.ServiceSuffix);
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