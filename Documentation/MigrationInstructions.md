# Database Migration Instructions

## Overview

We've refactored the entity configurations in the OpenAutomate application to use a more maintainable approach with separate configuration classes. This document provides instructions on how to create and apply migrations for these changes.

## Changes Made

1. Created separate entity configuration classes for all entities
2. Moved all entity configurations from ApplicationDbContext to dedicated configuration classes
3. Updated ApplicationDbContext to use ApplyConfiguration instead of direct entity configuration
4. Added new DbSet<AuthorityResource> in ApplicationDbContext
5. Fixed entity name issues (AuthorityID -> AuthorityId)

## Creating the Migration

To create a migration that captures these changes:

```powershell
# Navigate to the API project directory
cd OpenAutomate.Backend/OpenAutomate.API

# Create a migration with a descriptive name
dotnet ef migrations add SeparateEntityConfigurations --project ../OpenAutomate.Infrastructure
```

## Reviewing the Migration

Before applying the migration, it's important to review it to ensure it's making the expected changes:

1. Check the migration file created in the `OpenAutomate.Infrastructure/Migrations` folder
2. Verify that it creates the `AuthorityResources` table
3. Ensure that any other schema changes are as expected

## Applying the Migration

To apply the migration to your database:

```powershell
# Apply the migration
dotnet ef database update --project ../OpenAutomate.Infrastructure
```

## Rollback (If Needed)

If you need to rollback the migration:

```powershell
# Rollback to the previous migration
dotnet ef database update <previous-migration-name> --project ../OpenAutomate.Infrastructure
```

## Verification

After applying the migration, verify that:

1. All tables are created correctly
2. Relationships between tables are properly established
3. The application works as expected with the new database schema

## Additional Notes

- This migration approach maintains the existing table names to ensure backward compatibility
- The RBAC functionality requires the new `AuthorityResources` table
- Future migrations will be easier to understand with this more organized configuration approach 