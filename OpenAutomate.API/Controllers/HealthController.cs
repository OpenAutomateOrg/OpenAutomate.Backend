using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Health check controller to verify system components
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IDistributedCache cache, 
            IConnectionMultiplexer redis,
            ILogger<HealthController> logger)
        {
            _cache = cache;
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Redis health check with cache test
        /// </summary>
        [HttpGet("redis")]
        public async Task<IActionResult> CheckRedis()
        {
            try
            {
                // Test distributed cache
                var testKey = "health-check-" + DateTime.UtcNow.Ticks;
                var testValue = "Redis is working!";
                
                await _cache.SetStringAsync(testKey, testValue);
                var retrievedValue = await _cache.GetStringAsync(testKey);
                await _cache.RemoveAsync(testKey);

                // Test direct Redis connection
                var database = _redis.GetDatabase();
                var pingResult = await database.PingAsync();

                return Ok(new 
                { 
                    status = "healthy",
                    redis = new
                    {
                        connected = _redis.IsConnected,
                        ping = pingResult.TotalMilliseconds + "ms",
                        cacheTest = retrievedValue == testValue ? "passed" : "failed"
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return StatusCode(500, new 
                { 
                    status = "unhealthy", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Demo endpoint to create a persistent cache entry showing InstanceName prefix
        /// </summary>
        [HttpPost("redis/demo")]
        public async Task<IActionResult> CreateDemoCache()
        {
            try
            {
                var demoKey = "demo-key";
                var demoValue = $"Demo value created at {DateTime.UtcNow}";
                
                // Create with 5 minute expiration
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                
                await _cache.SetStringAsync(demoKey, demoValue, options);
                
                return Ok(new 
                { 
                    message = "Demo cache entry created",
                    key = demoKey,
                    value = demoValue,
                    actualRedisKey = "OpenAutomate_Dev:" + demoKey,
                    expiresIn = "5 minutes",
                    instructions = "Check RedisInsight at http://localhost:8001 to see the key with 'OpenAutomate_Dev:' prefix"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create demo cache");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
} 