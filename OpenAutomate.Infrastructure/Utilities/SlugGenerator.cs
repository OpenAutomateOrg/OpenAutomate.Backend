using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenAutomate.Infrastructure.Utilities
{
    public static class SlugGenerator
    {
        // Define a constant for regex timeout
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Generates a URL-friendly slug from a string
        /// </summary>
        public static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove diacritics (accents)
            string normalizedString = input.Normalize(NormalizationForm.FormD);
            StringBuilder slug = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    slug.Append(c);
                }
            }

            // Convert to lowercase
            string result = slug.ToString().ToLowerInvariant();

            // Replace spaces with hyphens
            result = Regex.Replace(result, @"\s+", "-", RegexOptions.NonBacktracking, RegexTimeout);

            // Remove invalid characters
            result = Regex.Replace(result, @"[^a-z0-9\-]", string.Empty, RegexOptions.NonBacktracking, RegexTimeout);

            // Trim hyphens from beginning and end
            result = result.Trim('-');

            // Remove duplicate hyphens
            result = Regex.Replace(result, @"-+", "-", RegexOptions.NonBacktracking, RegexTimeout);

            return result;
        }

        /// <summary>
        /// Ensures a slug is unique by adding a suffix if needed
        /// </summary>
        public static string EnsureUniqueSlug(string baseSlug, Func<string, bool> slugExists)
        {
            if (!slugExists(baseSlug))
                return baseSlug;

            int counter = 2;
            string newSlug;

            do
            {
                newSlug = $"{baseSlug}-{counter}";
                counter++;
            } while (slugExists(newSlug));

            return newSlug;
        }
    }
} 