namespace OpenAutomate.Core.IServices;

/// <summary>
/// Service for seeding the initial system administrator account
/// </summary>
public interface IAdminSeedService
{
    /// <summary>
    /// Seeds the system administrator account if it doesn't exist and seeding is enabled
    /// </summary>
    /// <returns>True if admin was seeded, false if already exists or seeding is disabled</returns>
    Task<bool> SeedSystemAdminAsync();
}