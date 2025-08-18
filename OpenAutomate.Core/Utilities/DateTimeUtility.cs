using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Utilities
{
    /// <summary>
    /// Utility class for consistent DateTime handling across the application
    /// All DateTime values should be handled as UTC in the backend
    /// </summary>
    public static class DateTimeUtility
    {
        /// <summary>
        /// Gets the current UTC time with Kind set to Utc
        /// </summary>
        public static DateTime UtcNow => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        /// <summary>
        /// Ensures a DateTime is converted to UTC regardless of its current Kind
        /// </summary>
        /// <param name="dateTime">The DateTime to convert</param>
        /// <param name="sourceTimeZone">The source timezone if DateTime.Kind is Unspecified</param>
        /// <returns>DateTime in UTC with Kind set to Utc</returns>
        public static DateTime EnsureUtc(DateTime dateTime, TimeZoneInfo? sourceTimeZone = null)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => sourceTimeZone != null 
                    ? TimeZoneInfo.ConvertTimeToUtc(dateTime, sourceTimeZone)
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), // Assume UTC if no timezone provided
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            };
        }

        /// <summary>
        /// Converts UTC DateTime to a specific timezone
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime</param>
        /// <param name="targetTimeZone">Target timezone</param>
        /// <returns>DateTime in target timezone</returns>
        public static DateTime ConvertFromUtc(DateTime utcDateTime, TimeZoneInfo targetTimeZone)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = EnsureUtc(utcDateTime);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, targetTimeZone);
        }

        /// <summary>
        /// Gets a TimeZoneInfo by ID with fallback to UTC
        /// </summary>
        /// <param name="timeZoneId">IANA timezone ID</param>
        /// <returns>TimeZoneInfo or UTC if invalid</returns>
        public static TimeZoneInfo GetTimeZoneInfo(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return TimeZoneInfo.Utc;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                // Fallback to UTC if timezone is invalid
                return TimeZoneInfo.Utc;
            }
        }

        /// <summary>
        /// Validates if a timezone ID is valid
        /// </summary>
        /// <param name="timeZoneId">IANA timezone ID</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidTimeZone(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return false;

            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Formats a UTC DateTime for display in a specific timezone
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime</param>
        /// <param name="timeZoneId">Target timezone ID</param>
        /// <param name="format">Date format string</param>
        /// <returns>Formatted date string</returns>
        public static string FormatInTimeZone(DateTime utcDateTime, string? timeZoneId, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var timeZone = GetTimeZoneInfo(timeZoneId);
            var localTime = ConvertFromUtc(utcDateTime, timeZone);
            return localTime.ToString(format);
        }
    }

    /// <summary>
    /// Custom JSON converter to ensure all DateTime values are serialized as UTC with 'Z' suffix
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (DateTime.TryParse(value, out var dateTime))
            {
                return DateTimeUtility.EnsureUtc(dateTime);
            }
            throw new JsonException($"Unable to parse DateTime from '{value}'");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utcValue = DateTimeUtility.EnsureUtc(value);
            writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }

    /// <summary>
    /// Custom JSON converter for nullable DateTime values
    /// </summary>
    public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return null;
                
            if (DateTime.TryParse(value, out var dateTime))
            {
                return DateTimeUtility.EnsureUtc(dateTime);
            }
            throw new JsonException($"Unable to parse DateTime from '{value}'");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                var utcValue = DateTimeUtility.EnsureUtc(value.Value);
                writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}