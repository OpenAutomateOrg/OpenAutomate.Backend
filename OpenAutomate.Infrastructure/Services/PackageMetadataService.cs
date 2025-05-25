using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for extracting metadata from automation package files
    /// </summary>
    public class PackageMetadataService : IPackageMetadataService
    {
        private readonly ILogger<PackageMetadataService> _logger;

        public PackageMetadataService(ILogger<PackageMetadataService> logger)
        {
            _logger = logger;
        }

        public async Task<PackageMetadata> ExtractMetadataAsync(Stream fileStream, string fileName)
        {
            var metadata = new PackageMetadata();

            try
            {
                // Reset stream position
                fileStream.Position = 0;

                // Check if it's a zip file
                if (!IsZipFile(fileName))
                {
                    metadata.ErrorMessage = "Package must be a ZIP file";
                    return metadata;
                }

                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
                
                // First priority: Look for bot.json file
                var botJsonEntry = archive.GetEntry("bot.json");
                if (botJsonEntry != null)
                {
                    await ExtractFromBotJson(botJsonEntry, metadata);
                }
                // Second priority: Look for config.ini file
                else
                {
                    var configEntry = archive.GetEntry("config/config.ini");
                    if (configEntry != null)
                    {
                        await ExtractFromConfigIni(configEntry, metadata);
                    }
                    else
                    {
                        // Fallback: look for other metadata files
                        await ExtractFromAlternativeSources(archive, metadata);
                    }
                }

                // If we still don't have basic info, use filename
                if (string.IsNullOrEmpty(metadata.Name))
                {
                    metadata.Name = Path.GetFileNameWithoutExtension(fileName);
                }

                // Set default version if not found
                if (string.IsNullOrEmpty(metadata.Version))
                {
                    metadata.Version = "1.0.0";
                }

                metadata.IsValid = !string.IsNullOrEmpty(metadata.Name);
                
                _logger.LogInformation("Extracted metadata from package: {Name} v{Version}", 
                    metadata.Name, metadata.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract metadata from package: {FileName}", fileName);
                metadata.ErrorMessage = $"Failed to extract metadata: {ex.Message}";
                metadata.IsValid = false;
            }

            return metadata;
        }

        public async Task<bool> IsValidPackageAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Reset stream position
                fileStream.Position = 0;

                // Must be a zip file
                if (!IsZipFile(fileName))
                {
                    return false;
                }

                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
                
                // Check for required files that indicate it's a bot package
                var hasMainBot = archive.GetEntry("bot.py") != null;
                var hasFramework = archive.GetEntry("framework/") != null || 
                                 archive.GetEntry("framework/base_bot.py") != null;
                var hasRequirements = archive.GetEntry("requirements.txt") != null;

                // At minimum, should have bot.py
                return hasMainBot;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating package: {FileName}", fileName);
                return false;
            }
        }

        private async Task ExtractFromConfigIni(ZipArchiveEntry configEntry, PackageMetadata metadata)
        {
            using var stream = configEntry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string currentSection = "";
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip comments and empty lines
                if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";") || string.IsNullOrEmpty(trimmedLine))
                    continue;

                // Check for section headers
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Trim('[', ']').ToLower();
                    continue;
                }

                // Parse key-value pairs
                var equalIndex = trimmedLine.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = trimmedLine.Substring(0, equalIndex).Trim().ToLower();
                    var value = trimmedLine.Substring(equalIndex + 1).Trim();

                    // Remove quotes if present
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Trim('"');
                    }

                    // Extract metadata based on section and key
                    if (currentSection == "bot")
                    {
                        switch (key)
                        {
                            case "name":
                                metadata.Name = value;
                                break;
                            case "description":
                                metadata.Description = value;
                                break;
                            case "version":
                                metadata.Version = value;
                                break;
                            case "author":
                                metadata.Author = value;
                                break;
                        }
                    }
                }
            }
        }

        private async Task ExtractFromAlternativeSources(ZipArchive archive, PackageMetadata metadata)
        {
            // Try to extract from README.md
            var readmeEntry = archive.GetEntry("README.md");
            if (readmeEntry != null)
            {
                using var stream = readmeEntry.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                
                // Simple extraction from README - look for title
                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("# ") && string.IsNullOrEmpty(metadata.Name))
                    {
                        metadata.Name = line.Substring(2).Trim();
                        break;
                    }
                }
            }

            // Try to extract from bot.py comments
            var botEntry = archive.GetEntry("bot.py");
            if (botEntry != null)
            {
                using var stream = botEntry.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                
                // Look for docstring or comments with metadata
                if (content.Contains("\"\"\"") && string.IsNullOrEmpty(metadata.Description))
                {
                    var startIndex = content.IndexOf("\"\"\"") + 3;
                    var endIndex = content.IndexOf("\"\"\"", startIndex);
                    if (endIndex > startIndex)
                    {
                        var docstring = content.Substring(startIndex, endIndex - startIndex).Trim();
                        if (!string.IsNullOrEmpty(docstring))
                        {
                            metadata.Description = docstring.Split('\n')[0].Trim();
                        }
                    }
                }
            }
        }

        private async Task ExtractFromBotJson(ZipArchiveEntry botJsonEntry, PackageMetadata metadata)
        {
            using var stream = botJsonEntry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            
            var content = await reader.ReadToEndAsync();
            
            try
            {
                // Parse JSON
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("name", out var nameElement))
                {
                    metadata.Name = nameElement.GetString() ?? "";
                }

                if (root.TryGetProperty("description", out var descElement))
                {
                    metadata.Description = descElement.GetString() ?? "";
                }

                if (root.TryGetProperty("version", out var versionElement))
                {
                    metadata.Version = versionElement.GetString() ?? "";
                }

                if (root.TryGetProperty("author", out var authorElement))
                {
                    metadata.Author = authorElement.GetString() ?? "";
                }

                _logger.LogInformation("Successfully extracted metadata from bot.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse bot.json, falling back to other sources");
                // Don't throw, let it fall back to other sources
            }
        }

        private bool IsZipFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension == ".zip";
        }
    }
} 