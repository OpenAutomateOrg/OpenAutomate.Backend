namespace OpenAutomate.Core.IServices;

/// <summary>
/// Service for seeding user accounts on startup
/// </summary>
public interface IUserSeedService
{
    /// <summary>
    /// Seeds user accounts if they don't exist and seeding is enabled
    /// </summary>
    /// <returns>Number of users that were successfully seeded</returns>
    Task<int> SeedUsersAsync();
}
