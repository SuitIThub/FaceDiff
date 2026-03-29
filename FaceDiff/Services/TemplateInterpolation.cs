using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FaceDiff.Services
{
    public static class TemplateInterpolation
    {
        private static readonly Regex Placeholder = new Regex(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);

        /// <summary>Replaces <c>{name}</c> with values from <paramref name="parameters"/>. Unknown names are left unchanged.</summary>
        public static string Apply(string template, IReadOnlyDictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(template) || parameters == null || parameters.Count == 0)
                return template;

            return Placeholder.Replace(template, m =>
            {
                var key = m.Groups[1].Value;
                return parameters.TryGetValue(key, out var v) ? v ?? "" : m.Value;
            });
        }
    }
}
