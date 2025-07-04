#!/bin/bash

# Docker Installation Script for Ubuntu Server
# This script installs Docker and Docker Compose on Ubuntu

set -e  # Exit on any error

echo "🐳 Installing Docker on Ubuntu Server..."

# Update package index
echo "📦 Updating package index..."
sudo apt-get update

# Install prerequisites
echo "🔧 Installing prerequisites..."
sudo apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

# Add Docker's official GPG key
echo "🔑 Adding Docker's GPG key..."
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

# Set up the repository
echo "📋 Setting up Docker repository..."
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Update package index again
sudo apt-get update

# Install Docker Engine
echo "🐳 Installing Docker Engine..."
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Start and enable Docker service
echo "🔄 Starting Docker service..."
sudo systemctl start docker
sudo systemctl enable docker

# Add current user to docker group
echo "👤 Adding current user to docker group..."
sudo usermod -aG docker $USER

# Test Docker installation
echo "🧪 Testing Docker installation..."
sudo docker run hello-world

echo ""
echo "✅ Docker installation completed successfully!"
echo ""
echo "📋 Installation Summary:"
echo "   - Docker Engine: $(docker --version)"
echo "   - Docker Compose: $(docker compose version)"
echo ""
echo "⚠️  IMPORTANT: You need to log out and log back in (or restart your session)"
echo "   for the docker group permissions to take effect."
echo ""
echo "🔧 Useful commands:"
echo "   - Check Docker status: sudo systemctl status docker"
echo "   - View Docker info: docker info"
echo "   - List running containers: docker ps"
echo "   - List all containers: docker ps -a"
echo ""
echo "🚀 You can now run the Redis setup script:"
echo "   ./setup-redis-ubuntu.sh"
