using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for managing Quartz.NET database schema automatically
    /// </summary>
    public class QuartzSchemaService : IQuartzSchemaService
    {
        private readonly string _connectionString;
        private readonly ILogger<QuartzSchemaService> _logger;

        // Required Quartz.NET tables
        private readonly string[] _requiredTables = new[]
        {
            "QRTZ_JOB_DETAILS",
            "QRTZ_TRIGGERS", 
            "QRTZ_SIMPLE_TRIGGERS",
            "QRTZ_CRON_TRIGGERS",
            "QRTZ_SIMPROP_TRIGGERS",
            "QRTZ_BLOB_TRIGGERS",
            "QRTZ_CALENDARS",
            "QRTZ_PAUSED_TRIGGER_GRPS",
            "QRTZ_FIRED_TRIGGERS",
            "QRTZ_SCHEDULER_STATE",
            "QRTZ_LOCKS"
        };

        public QuartzSchemaService(IOptions<DatabaseSettings> databaseSettings, ILogger<QuartzSchemaService> logger)
        {
            _connectionString = databaseSettings.Value.DefaultConnection;
            _logger = logger;
        }

        public async Task<bool> EnsureSchemaExistsAsync()
        {
            try
            {
                _logger.LogInformation("Checking Quartz.NET database schema...");

                if (await SchemaExistsAsync())
                {
                    _logger.LogInformation("Quartz.NET schema already exists.");
                    return true;
                }

                _logger.LogInformation("Quartz.NET schema not found. Creating schema...");
                await CreateSchemaAsync();
                
                // Verify schema was created successfully
                if (await SchemaExistsAsync())
                {
                    _logger.LogInformation("Quartz.NET schema created successfully!");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to create Quartz.NET schema.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring Quartz.NET schema exists");
                return false;
            }
        }

        public async Task<bool> SchemaExistsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                foreach (var tableName in _requiredTables)
                {
                    var checkTableSql = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @TableName";

                    using var command = new SqlCommand(checkTableSql, connection);
                    command.Parameters.AddWithValue("@TableName", tableName);
                    
                    var tableCount = (int)await command.ExecuteScalarAsync();
                    if (tableCount == 0)
                    {
                        _logger.LogDebug("Quartz table {TableName} not found", tableName);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if Quartz.NET schema exists");
                return false;
            }
        }

        private async Task CreateSchemaAsync()
        {
            var createSchemaScript = GetCreateSchemaScript();
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(createSchemaScript, connection);
            command.CommandTimeout = 300; // 5 minutes timeout
            await command.ExecuteNonQueryAsync();
        }

        private string GetCreateSchemaScript()
        {
            return @"
-- Create the Quartz.NET tables if they don't exist

-- Table: QRTZ_JOB_DETAILS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_JOB_DETAILS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_JOB_DETAILS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [JOB_NAME] [nvarchar](200) NOT NULL,
        [JOB_GROUP] [nvarchar](200) NOT NULL,
        [DESCRIPTION] [nvarchar](250) NULL,
        [JOB_CLASS_NAME] [nvarchar](250) NOT NULL,
        [IS_DURABLE] [bit] NOT NULL,
        [IS_NONCONCURRENT] [bit] NOT NULL,
        [IS_UPDATE_DATA] [bit] NOT NULL,
        [REQUESTS_RECOVERY] [bit] NOT NULL,
        [JOB_DATA] [image] NULL,
        CONSTRAINT [PK_QRTZ_JOB_DETAILS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
    );
END;

-- Table: QRTZ_TRIGGERS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_TRIGGERS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_TRIGGERS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [TRIGGER_NAME] [nvarchar](200) NOT NULL,
        [TRIGGER_GROUP] [nvarchar](200) NOT NULL,
        [JOB_NAME] [nvarchar](200) NOT NULL,
        [JOB_GROUP] [nvarchar](200) NOT NULL,
        [DESCRIPTION] [nvarchar](250) NULL,
        [NEXT_FIRE_TIME] [bigint] NULL,
        [PREV_FIRE_TIME] [bigint] NULL,
        [PRIORITY] [int] NULL,
        [TRIGGER_STATE] [nvarchar](16) NOT NULL,
        [TRIGGER_TYPE] [nvarchar](8) NOT NULL,
        [START_TIME] [bigint] NOT NULL,
        [END_TIME] [bigint] NULL,
        [CALENDAR_NAME] [nvarchar](200) NULL,
        [MISFIRE_INSTR] [int] NULL,
        [JOB_DATA] [image] NULL,
        CONSTRAINT [PK_QRTZ_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
    );
END;

-- Table: QRTZ_SIMPLE_TRIGGERS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_SIMPLE_TRIGGERS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [TRIGGER_NAME] [nvarchar](200) NOT NULL,
        [TRIGGER_GROUP] [nvarchar](200) NOT NULL,
        [REPEAT_COUNT] [bigint] NOT NULL,
        [REPEAT_INTERVAL] [bigint] NOT NULL,
        [TIMES_TRIGGERED] [bigint] NOT NULL,
        CONSTRAINT [PK_QRTZ_SIMPLE_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
    );
END;

-- Table: QRTZ_CRON_TRIGGERS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_CRON_TRIGGERS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_CRON_TRIGGERS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [TRIGGER_NAME] [nvarchar](200) NOT NULL,
        [TRIGGER_GROUP] [nvarchar](200) NOT NULL,
        [CRON_EXPRESSION] [nvarchar](250) NOT NULL,
        [TIME_ZONE_ID] [nvarchar](80) NULL,
        CONSTRAINT [PK_QRTZ_CRON_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
    );
END;

-- Table: QRTZ_SIMPROP_TRIGGERS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_SIMPROP_TRIGGERS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [TRIGGER_NAME] [nvarchar](200) NOT NULL,
        [TRIGGER_GROUP] [nvarchar](200) NOT NULL,
        [STR_PROP_1] [nvarchar](512) NULL,
        [STR_PROP_2] [nvarchar](512) NULL,
        [STR_PROP_3] [nvarchar](512) NULL,
        [INT_PROP_1] [int] NULL,
        [INT_PROP_2] [int] NULL,
        [LONG_PROP_1] [bigint] NULL,
        [LONG_PROP_2] [bigint] NULL,
        [DEC_PROP_1] [numeric](13,4) NULL,
        [DEC_PROP_2] [numeric](13,4) NULL,
        [BOOL_PROP_1] [bit] NULL,
        [BOOL_PROP_2] [bit] NULL,
        CONSTRAINT [PK_QRTZ_SIMPROP_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
    );
END;

-- Table: QRTZ_BLOB_TRIGGERS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_BLOB_TRIGGERS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_BLOB_TRIGGERS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [TRIGGER_NAME] [nvarchar](200) NOT NULL,
        [TRIGGER_GROUP] [nvarchar](200) NOT NULL,
        [BLOB_DATA] [image] NULL,
        CONSTRAINT [PK_QRTZ_BLOB_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
    );
END;

-- Table: QRTZ_CALENDARS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_CALENDARS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_CALENDARS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [CALENDAR_NAME] [nvarchar](200) NOT NULL,
        [CALENDAR] [image] NOT NULL,
        CONSTRAINT [PK_QRTZ_CALENDARS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [CALENDAR_NAME])
    );
END;

-- Table: QRTZ_PAUSED_TRIGGER_GRPS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_PAUSED_TRIGGER_GRPS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_PAUSED_TRIGGER_GRPS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [TRIGGER_GROUP] [nvarchar](200) NOT NULL,
        CONSTRAINT [PK_QRTZ_PAUSED_TRIGGER_GRPS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_GROUP])
    );
END;

-- Table: QRTZ_FIRED_TRIGGERS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_FIRED_TRIGGERS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_FIRED_TRIGGERS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [ENTRY_ID] [nvarchar](140) NOT NULL,
        [TRIGGER_NAME] [nvarchar](200) NOT NULL,
        [TRIGGER_GROUP] [nvarchar](200) NOT NULL,
        [INSTANCE_NAME] [nvarchar](200) NOT NULL,
        [FIRED_TIME] [bigint] NOT NULL,
        [SCHED_TIME] [bigint] NOT NULL,
        [PRIORITY] [int] NOT NULL,
        [STATE] [nvarchar](16) NOT NULL,
        [JOB_NAME] [nvarchar](200) NULL,
        [JOB_GROUP] [nvarchar](200) NULL,
        [IS_NONCONCURRENT] [bit] NULL,
        [REQUESTS_RECOVERY] [bit] NULL,
        CONSTRAINT [PK_QRTZ_FIRED_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [ENTRY_ID])
    );
END;

-- Table: QRTZ_SCHEDULER_STATE
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_SCHEDULER_STATE')
BEGIN
    CREATE TABLE [dbo].[QRTZ_SCHEDULER_STATE] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [INSTANCE_NAME] [nvarchar](200) NOT NULL,
        [LAST_CHECKIN_TIME] [bigint] NOT NULL,
        [CHECKIN_INTERVAL] [bigint] NOT NULL,
        CONSTRAINT [PK_QRTZ_SCHEDULER_STATE] PRIMARY KEY CLUSTERED ([SCHED_NAME], [INSTANCE_NAME])
    );
END;

-- Table: QRTZ_LOCKS
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRTZ_LOCKS')
BEGIN
    CREATE TABLE [dbo].[QRTZ_LOCKS] (
        [SCHED_NAME] [nvarchar](120) NOT NULL,
        [LOCK_NAME] [nvarchar](40) NOT NULL,
        CONSTRAINT [PK_QRTZ_LOCKS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [LOCK_NAME])
    );
END;

-- Add foreign key constraints
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS')
BEGIN
    ALTER TABLE [dbo].[QRTZ_TRIGGERS]
    ADD CONSTRAINT [FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS] FOREIGN KEY ([SCHED_NAME], [JOB_NAME], [JOB_GROUP]) 
        REFERENCES [dbo].[QRTZ_JOB_DETAILS] ([SCHED_NAME], [JOB_NAME], [JOB_GROUP]);
END;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS')
BEGIN
    ALTER TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS]
    ADD CONSTRAINT [FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) 
        REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) ON DELETE CASCADE;
END;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS')
BEGIN
    ALTER TABLE [dbo].[QRTZ_CRON_TRIGGERS]
    ADD CONSTRAINT [FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) 
        REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) ON DELETE CASCADE;
END;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS')
BEGIN
    ALTER TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS]
    ADD CONSTRAINT [FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) 
        REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) ON DELETE CASCADE;
END;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_BLOB_TRIGGERS_QRTZ_TRIGGERS')
BEGIN
    ALTER TABLE [dbo].[QRTZ_BLOB_TRIGGERS]
    ADD CONSTRAINT [FK_QRTZ_BLOB_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) 
        REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) ON DELETE CASCADE;
END;

-- Insert default lock records for clustering (only if they don't exist)
IF NOT EXISTS (SELECT * FROM [dbo].[QRTZ_LOCKS] WHERE [SCHED_NAME] = 'OpenAutomate-Scheduler' AND [LOCK_NAME] = 'TRIGGER_ACCESS')
    INSERT INTO [dbo].[QRTZ_LOCKS] VALUES('OpenAutomate-Scheduler', 'TRIGGER_ACCESS');

IF NOT EXISTS (SELECT * FROM [dbo].[QRTZ_LOCKS] WHERE [SCHED_NAME] = 'OpenAutomate-Scheduler' AND [LOCK_NAME] = 'JOB_ACCESS')
    INSERT INTO [dbo].[QRTZ_LOCKS] VALUES('OpenAutomate-Scheduler', 'JOB_ACCESS');

IF NOT EXISTS (SELECT * FROM [dbo].[QRTZ_LOCKS] WHERE [SCHED_NAME] = 'OpenAutomate-Scheduler' AND [LOCK_NAME] = 'CALENDAR_ACCESS')
    INSERT INTO [dbo].[QRTZ_LOCKS] VALUES('OpenAutomate-Scheduler', 'CALENDAR_ACCESS');

IF NOT EXISTS (SELECT * FROM [dbo].[QRTZ_LOCKS] WHERE [SCHED_NAME] = 'OpenAutomate-Scheduler' AND [LOCK_NAME] = 'STATE_ACCESS')
    INSERT INTO [dbo].[QRTZ_LOCKS] VALUES('OpenAutomate-Scheduler', 'STATE_ACCESS');

IF NOT EXISTS (SELECT * FROM [dbo].[QRTZ_LOCKS] WHERE [SCHED_NAME] = 'OpenAutomate-Scheduler' AND [LOCK_NAME] = 'MISFIRE_ACCESS')
    INSERT INTO [dbo].[QRTZ_LOCKS] VALUES('OpenAutomate-Scheduler', 'MISFIRE_ACCESS');
";
        }
    }
} 