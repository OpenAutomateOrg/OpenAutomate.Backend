using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeZoneConverter;

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
                // First try direct lookup (works for IANA timezone IDs on Linux)
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    // Try using TimeZoneConverter for IANA timezone IDs
                    Console.WriteLine($"[TIMEZONE] Direct lookup failed for '{timeZoneId}', trying IANA conversion...");
                    var windowsTimeZoneId = TZConvert.IanaToWindows(timeZoneId);
                    Console.WriteLine($"[TIMEZONE] Converted IANA '{timeZoneId}' to Windows '{windowsTimeZoneId}'");
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TIMEZONE ERROR] IANA conversion failed for '{timeZoneId}': {ex.Message}");

                    // Create custom timezone for Linux containers
                    var customTimeZone = CreateCustomTimeZone(timeZoneId);
                    if (customTimeZone != null)
                    {
                        Console.WriteLine($"[TIMEZONE] Created custom timezone for '{timeZoneId}' with offset {customTimeZone.BaseUtcOffset}");
                        return customTimeZone;
                    }

                    // Final fallback to UTC
                    Console.WriteLine($"[TIMEZONE] All methods failed, falling back to UTC for timezone: {timeZoneId}");
                    return TimeZoneInfo.Utc;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TIMEZONE ERROR] Unexpected error for timezone '{timeZoneId}': {ex.Message}");
                return TimeZoneInfo.Utc;
            }
        }

        /// <summary>
        /// Creates a custom TimeZoneInfo for common timezones when system lookup fails
        /// This is essential for Linux containers that may not have full timezone data
        /// </summary>
        private static TimeZoneInfo? CreateCustomTimeZone(string? timeZoneId)
        {
            var timezoneData = GetTimezoneData(timeZoneId);
            if (timezoneData == null)
                return null;

            try
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    timezoneData.Id,
                    timezoneData.BaseOffset,
                    timezoneData.DisplayName,
                    timezoneData.StandardName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TIMEZONE ERROR] Failed to create custom timezone for '{timeZoneId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets timezone data for common IANA timezone IDs
        /// </summary>
        private static TimezoneData? GetTimezoneData(string? timeZoneId)
        {
            return timeZoneId switch
            {
                "Asia/Ho_Chi_Minh" => new TimezoneData
                {
                    Id = "Asia/Ho_Chi_Minh",
                    BaseOffset = TimeSpan.FromHours(7),
                    DisplayName = "(UTC+07:00) Ho Chi Minh City",
                    StandardName = "Indochina Time"
                },
                "Asia/Singapore" => new TimezoneData
                {
                    Id = "Asia/Singapore",
                    BaseOffset = TimeSpan.FromHours(8),
                    DisplayName = "(UTC+08:00) Singapore",
                    StandardName = "Singapore Standard Time"
                },
                "Asia/Tokyo" => new TimezoneData
                {
                    Id = "Asia/Tokyo",
                    BaseOffset = TimeSpan.FromHours(9),
                    DisplayName = "(UTC+09:00) Tokyo",
                    StandardName = "Japan Standard Time"
                },
                "America/New_York" => new TimezoneData
                {
                    Id = "America/New_York",
                    BaseOffset = TimeSpan.FromHours(-5),
                    DisplayName = "(UTC-05:00) New York",
                    StandardName = "Eastern Standard Time"
                },
                "Europe/London" => new TimezoneData
                {
                    Id = "Europe/London",
                    BaseOffset = TimeSpan.FromHours(0),
                    DisplayName = "(UTC+00:00) London",
                    StandardName = "Greenwich Mean Time"
                },
                "Australia/Sydney" => new TimezoneData
                {
                    Id = "Australia/Sydney",
                    BaseOffset = TimeSpan.FromHours(10),
                    DisplayName = "(UTC+10:00) Sydney",
                    StandardName = "Australian Eastern Standard Time"
                },
                _ => null
            };
        }

        /// <summary>
        /// Manual mapping for common IANA timezone IDs as final fallback
        /// </summary>
        private static string? MapIanaToWindows(string? ianaTimeZoneId)
        {
            return ianaTimeZoneId switch
            {
                "Asia/Ho_Chi_Minh" => "SE Asia Standard Time",
                "Asia/Singapore" => "Singapore Standard Time",
                "Asia/Tokyo" => "Tokyo Standard Time",
                "America/New_York" => "Eastern Standard Time",
                "Europe/London" => "GMT Standard Time",
                "Australia/Sydney" => "AUS Eastern Standard Time",
                _ => null
            };
        }

        /// <summary>
        /// Helper class for timezone data
        /// </summary>
        private class TimezoneData
        {
            public string Id { get; set; } = string.Empty;
            public TimeSpan BaseOffset { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string StandardName { get; set; } = string.Empty;
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