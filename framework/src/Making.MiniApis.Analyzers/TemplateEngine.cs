using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace Making.MiniApis.Analyzers
{
    public class TemplateEngine
    {
        private readonly Dictionary<string, string> _templates;
        private readonly string _templatesDirectory;

        public TemplateEngine(string templatesDirectory)
        {
            _templatesDirectory = templatesDirectory;
            _templates = new Dictionary<string, string>();
            // LoadTemplates() removed - not allowed in analyzers
        }

        // LoadTemplates method removed - file I/O not allowed in analyzers
        public void AddTemplate(string templateName, string template)
        {
            _templates[templateName] = template;
        }

        public string Render(string templateName, object model)
        {
            if (!_templates.TryGetValue(templateName, out var template))
                throw new FileNotFoundException($"Template '{templateName}' not found");

            return RenderTemplate(template, model);
        }

        private string RenderTemplate(string template, object model)
        {
            var result = template;
            
            // Handle simple property replacements
            result = Regex.Replace(result, @"\u003C#=\s*(\w+(?:\.\w+)*)\s*#\u003E", match =>
            {
                var propertyPath = match.Groups[1].Value;
                return GetPropertyValue(model, propertyPath)?.ToString() ?? string.Empty;
            });

            // Handle foreach loops
            result = Regex.Replace(result, 
                @"\u003C#\s*foreach\s*\(\s*(\w+)\s+in\s+(\w+(?:\.\w+)*)\s*\)\s*\{\s*#\u003E(.*?)\u003C#\s*}\s*#\u003E", 
                match =>
            {
                var itemName = match.Groups[1].Value;
                var collectionPath = match.Groups[2].Value;
                var innerTemplate = match.Groups[3].Value;
                
                var collection = GetPropertyValue(model, collectionPath) as IEnumerable;
                if (collection == null)
                    return string.Empty;

                var sb = new StringBuilder();
                foreach (var item in collection)
                {
                    var itemResult = innerTemplate;
                    itemResult = itemResult.Replace($"\u003C#= {itemName}.", "\u003C#= ITEM.");
                    itemResult = Regex.Replace(itemResult, @"\u003C#=\s*ITEM\.(\w+(?:\.\w+)*)\s*#\u003E", propMatch =>
                    {
                        var propPath = propMatch.Groups[1].Value;
                        return GetPropertyValue(item, propPath)?.ToString() ?? string.Empty;
                    });
                    sb.Append(itemResult);
                }
                return sb.ToString();
            }, RegexOptions.Singleline);

            // Handle if statements
            result = Regex.Replace(result,
                @"\u003C#\s*if\s*\(\s*!(\w+(?:\.\w+)*)\s*\)\s*\{\s*#\u003E(.*?)\u003C#\s*}\s*#\u003E",
                match =>
            {
                var conditionPath = match.Groups[1].Value;
                var innerTemplate = match.Groups[2].Value;
                
                var value = GetPropertyValue(model, conditionPath);
                var isNullOrEmpty = value == null || 
                    (value is string str && string.IsNullOrEmpty(str)) ||
                    (value is ICollection coll && coll.Count == 0);
                
                return isNullOrEmpty ? innerTemplate : string.Empty;
            }, RegexOptions.Singleline);

            return result;
        }

        private object? GetPropertyValue(object obj, string propertyPath)
        {
            if (obj == null) return null;

            var parts = propertyPath.Split('.');
            var current = obj;

            foreach (var part in parts)
            {
                if (current == null) return null;

                var property = current.GetType().GetProperty(part);
                if (property == null) return null;

                current = property.GetValue(current);
            }

            return current;
        }
    }
}