using System;
using OpenAutomate.Core.Utilities;

// Quick test to verify timezone calculation
public class TimezoneTest
{
    public static void Main()
    {
        Console.WriteLine("=== Backend Timezone Test ===");
        
        // Your scenario: Schedule created at 01:28 AM Vietnam, target 01:30 AM
        var scheduleCreatedUtc = DateTime.Parse("2025-08-17T18:28:36.950Z").ToUniversalTime();
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        
        Console.WriteLine($"Schedule created (UTC): {scheduleCreatedUtc:yyyy-MM-ddTHH:mm:ss.fffZ}");
        
        // Convert to Vietnam time (this is what localNow would be)
        var localNow = DateTimeUtility.ConvertFromUtc(scheduleCreatedUtc, vietnamTimeZone);
        Console.WriteLine($"Local time (Vietnam): {localNow:yyyy-MM-dd HH:mm:ss}");
        
        // Parse cron expression "0 30 01 * * *"
        int hour = 1, minute = 30, second = 0;
        
        // Calculate todayAtTime
        var todayAtTime = localNow.Date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
        Console.WriteLine($"Today at target time: {todayAtTime:yyyy-MM-dd HH:mm:ss}");
        
        // Calculate time difference
        var timeDifference = todayAtTime.Subtract(localNow).TotalSeconds;
        Console.WriteLine($"Time difference: {timeDifference} seconds");
        
        // Apply the corrected logic
        var nextRun = timeDifference >= 10 ? todayAtTime : todayAtTime.AddDays(1);
        Console.WriteLine($"Next run (local): {nextRun:yyyy-MM-dd HH:mm:ss}");
        
        // Convert back to UTC
        var nextRunUtc = DateTimeUtility.EnsureUtc(nextRun, vietnamTimeZone);
        Console.WriteLine($"Next run (UTC): {nextRunUtc:yyyy-MM-ddTHH:mm:ss.fffZ}");
        
        Console.WriteLine("\n=== Current Time Test ===");
        var nowUtc = DateTimeUtility.UtcNow;
        var nowLocal = DateTimeUtility.ConvertFromUtc(nowUtc, vietnamTimeZone);
        Console.WriteLine($"Current UTC: {nowUtc:yyyy-MM-ddTHH:mm:ss.fffZ}");
        Console.WriteLine($"Current Vietnam: {nowLocal:yyyy-MM-dd HH:mm:ss}");
        
        var todayAtTime2 = nowLocal.Date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
        var timeDiff2 = todayAtTime2.Subtract(nowLocal).TotalSeconds;
        var nextRun2 = timeDiff2 >= 10 ? todayAtTime2 : todayAtTime2.AddDays(1);
        var nextRunUtc2 = DateTimeUtility.EnsureUtc(nextRun2, vietnamTimeZone);
        
        Console.WriteLine($"Next 01:30 AM run would be: {nextRunUtc2:yyyy-MM-ddTHH:mm:ss.fffZ}");
    }
}