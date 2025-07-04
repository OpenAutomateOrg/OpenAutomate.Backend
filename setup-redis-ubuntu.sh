#!/bin/bash

# OpenAutomate Redis Setup Script for Ubuntu Server
# This script sets up Redis using Docker for the OpenAutomate backend

set -e  # Exit on any error

echo "🚀 Setting up Redis for OpenAutomate Backend..."

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    echo "Run: curl -fsSL https://get.docker.com -o get-docker.sh && sh get-docker.sh"
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    echo "Run: sudo apt-get update && sudo apt-get install docker-compose-plugin"
    exit 1
fi

# Create directory structure
BACKEND_DIR="/var/www/openautomate/backend"
echo "📁 Creating directory structure..."
sudo mkdir -p $BACKEND_DIR
cd $BACKEND_DIR

# Copy Redis configuration files (assuming they're already deployed)
if [ ! -f "docker-compose.redis.prod.yml" ]; then
    echo "❌ docker-compose.redis.prod.yml not found in $BACKEND_DIR"
    echo "Please ensure the deployment has copied the Redis configuration files."
    exit 1
fi

if [ ! -f "redis.conf" ]; then
    echo "❌ redis.conf not found in $BACKEND_DIR"
    echo "Please ensure the deployment has copied the Redis configuration files."
    exit 1
fi

# Stop existing Redis containers if any
echo "🛑 Stopping existing Redis containers..."
docker-compose -f docker-compose.redis.prod.yml down || true

# Start Redis
echo "🔄 Starting Redis..."
docker-compose -f docker-compose.redis.prod.yml up -d

# Wait for Redis to be ready
echo "⏳ Waiting for Redis to be ready..."
timeout 60 bash -c 'until docker exec openautomae-redis-prod redis-cli ping 2>/dev/null | grep -q PONG; do 
    echo "Waiting for Redis..."
    sleep 2
done'

# Test Redis connection
echo "🧪 Testing Redis connection..."
if docker exec openautomae-redis-prod redis-cli ping | grep -q PONG; then
    echo "✅ Redis is running and responding to ping"
else
    echo "❌ Redis is not responding"
    exit 1
fi

# Show Redis status
echo "📊 Redis container status:"
docker ps | grep redis

echo ""
echo "✅ Redis setup completed successfully!"
echo ""
echo "📋 Redis Information:"
echo "   - Container: openautomae-redis-prod"
echo "   - Port: 6379 (localhost only)"
echo "   - Data persistence: Enabled (RDB + AOF)"
echo "   - Redis Insight: Available at http://localhost:8001 (if enabled)"
echo ""
echo "🔧 Useful commands:"
echo "   - Check Redis logs: docker logs openautomae-redis-prod"
echo "   - Connect to Redis CLI: docker exec -it openautomae-redis-prod redis-cli"
echo "   - Stop Redis: docker-compose -f docker-compose.redis.prod.yml down"
echo "   - Restart Redis: docker-compose -f docker-compose.redis.prod.yml restart"
echo ""
echo "🔄 You can now restart your OpenAutomate backend service:"
echo "   sudo systemctl restart openautomate-backend"
