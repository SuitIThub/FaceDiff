using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FaceDiff.Services
{
    public static class TemplateInterpolation
    {
        public const string FolderPrefix = "FOLDER:";

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

        /// <summary>
        /// Finds the first parameter whose value is <c>FOLDER:&lt;path&gt;</c>.
        /// Additional FOLDER parameters are ignored.
        /// </summary>
        public static bool TryParseFolderMode(
            IReadOnlyDictionary<string, string> parameters,
            out string paramKey,
            out string rootPath)
        {
            paramKey = null;
            rootPath = null;
            if (parameters == null || parameters.Count == 0)
                return false;

            foreach (var kv in parameters)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                    continue;

                var value = kv.Value.Trim();
                if (!value.StartsWith(FolderPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                paramKey = kv.Key;
                rootPath = value.Substring(FolderPrefix.Length).Trim();
                return !string.IsNullOrWhiteSpace(rootPath);
            }

            return false;
        }

        /// <summary>Immediate child directory names under <paramref name="rootPath"/>, sorted.</summary>
        public static IReadOnlyList<string> GetCategoryNames(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                return Array.Empty<string>();

            return Directory.GetDirectories(rootPath)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Copy of <paramref name="parameters"/> with <paramref name="key"/> set to <paramref name="categoryName"/>.</summary>
        public static IReadOnlyDictionary<string, string> WithCategory(
            IReadOnlyDictionary<string, string> parameters,
            string key,
            string categoryName)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (parameters != null)
            {
                foreach (var kv in parameters)
                    result[kv.Key] = kv.Value;
            }

            if (!string.IsNullOrEmpty(key))
                result[key] = categoryName ?? "";

            return result;
        }

        /// <summary>
        /// Interpolates <paramref name="template"/> using folder-aware params when a FOLDER mode
        /// parameter exists and <paramref name="category"/> is set.
        /// </summary>
        public static string ApplyForCategory(
            string template,
            IReadOnlyDictionary<string, string> parameters,
            string category)
        {
            if (string.IsNullOrEmpty(category)
                || !TryParseFolderMode(parameters, out var key, out _))
            {
                return Apply(template ?? "", parameters);
            }

            return Apply(template ?? "", WithCategory(parameters, key, category));
        }

        /// <summary>
        /// Preview text for a template when folder mode is active.
        /// Shows a sample of resolved category paths, e.g. <c>D:\Sources\&lt;A|B|…&gt;</c>.
        /// </summary>
        public static string PreviewFolderAware(
            string template,
            IReadOnlyDictionary<string, string> parameters,
            int maxNames = 3)
        {
            if (!TryParseFolderMode(parameters, out var key, out var rootPath))
                return Apply(template ?? "", parameters);

            var names = GetCategoryNames(rootPath);
            if (names.Count == 0)
            {
                var placeholderParams = WithCategory(parameters, key, "<category>");
                return Apply(template ?? "", placeholderParams);
            }

            var shown = names.Take(maxNames).ToList();
            var joined = string.Join("|", shown);
            if (names.Count > maxNames)
                joined += "|…";

            return Apply(template ?? "", WithCategory(parameters, key, $"<{joined}>"));
        }
    }
}
