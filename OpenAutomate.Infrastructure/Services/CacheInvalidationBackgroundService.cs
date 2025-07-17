using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Background service that subscribes to Redis pub/sub messages for cache invalidation
/// </summary>
public class CacheInvalidationBackgroundService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheInvalidationBackgroundService> _logger;
    private readonly ISubscriber _subscriber;

    private static class LogMessages
    {
        public const string ServiceStarting = "Cache invalidation background service starting";
        public const string ServiceStopping = "Cache invalidation background service stopping";
        public const string ServiceStarted = "Cache invalidation background service started and subscribed to channel {Channel}";
        public const string MessageReceived = "Cache invalidation message received from channel {Channel}";
        public const string MessageProcessed = "Cache invalidation message processed successfully";
        public const string MessageProcessingError = "Error processing cache invalidation message";
        public const string SubscriptionError = "Error in cache invalidation subscription";
        public const string ServiceError = "Error in cache invalidation background service";
        public const string InvalidMessageFormat = "Invalid cache invalidation message format received";
    }

    public CacheInvalidationBackgroundService(
        IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        ILogger<CacheInvalidationBackgroundService> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _subscriber = _redis.GetSubscriber();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogMessages.ServiceStarting);

        try
        {
            const string channel = "cache:invalidate";
            
            // Subscribe to the cache invalidation channel
            await _subscriber.SubscribeAsync(channel, async (channelName, message) =>
            {
                try
                {
                    _logger.LogDebug(LogMessages.MessageReceived, channelName);
                    
                    if (message.IsNull)
                    {
                        return;
                    }

                    // Deserialize the message
                    var invalidationMessage = JsonSerializer.Deserialize<CacheInvalidationMessage>(message!, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (invalidationMessage == null)
                    {
                        _logger.LogWarning(LogMessages.InvalidMessageFormat);
                        return;
                    }

                    // Process the invalidation message using a scoped service
                    using var scope = _serviceProvider.CreateScope();
                    var cacheInvalidationService = scope.ServiceProvider.GetRequiredService<ICacheInvalidationService>();
                    
                    await cacheInvalidationService.ProcessInvalidationMessageAsync(invalidationMessage, stoppingToken);
                    
                    _logger.LogDebug(LogMessages.MessageProcessed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, LogMessages.MessageProcessingError);
                    // Don't rethrow - we don't want to crash the subscription
                }
            });

            _logger.LogInformation(LogMessages.ServiceStarted, channel);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the service is stopping
            _logger.LogInformation(LogMessages.ServiceStopping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.ServiceError);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.ServiceStopping);
        
        try
        {
            // Unsubscribe from all channels
            await _subscriber.UnsubscribeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.SubscriptionError);
        }

        await base.StopAsync(cancellationToken);
    }
} 