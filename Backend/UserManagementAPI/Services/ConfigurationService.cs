using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public interface IConfigurationService
    {
        ChunkingConfiguration GetChunkingConfiguration();
        void UpdateChunkingConfiguration(ChunkingConfiguration config);
        List<string> ChunkDocumentText(string text, ChunkingConfiguration? config = null);
    }

    public class ConfigurationService : IConfigurationService
    {
        private ChunkingConfiguration _configuration;
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _configuration = new ChunkingConfiguration
            {
                ChunkSize = 500,
                ChunkOverlap = 50,
                AutoIndex = true,
                SplitStrategy = "sentence",
                PreserveLineBreaks = true
            };
        }

        public ChunkingConfiguration GetChunkingConfiguration()
        {
            return _configuration;
        }

        public void UpdateChunkingConfiguration(ChunkingConfiguration config)
        {
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            _configuration.LastUpdated = DateTime.UtcNow;

            // Validate configuration
            if (_configuration.ChunkSize < 100)
                _configuration.ChunkSize = 100;
            if (_configuration.ChunkSize > 2000)
                _configuration.ChunkSize = 2000;

            if (_configuration.ChunkOverlap < 0)
                _configuration.ChunkOverlap = 0;
            if (_configuration.ChunkOverlap > _configuration.ChunkSize / 2)
                _configuration.ChunkOverlap = _configuration.ChunkSize / 2;

            _logger.LogInformation($"Chunking configuration updated: Size={_configuration.ChunkSize}, Overlap={_configuration.ChunkOverlap}, Strategy={_configuration.SplitStrategy}");
        }

        public List<string> ChunkDocumentText(string text, ChunkingConfiguration? config = null)
        {
            try
            {
                config ??= _configuration;

                if (string.IsNullOrWhiteSpace(text))
                    return new List<string>();

                var chunks = ChunkingStrategy.ChunkText(text, config);

                _logger.LogInformation($"Document split into {chunks.Count} chunks using {config.SplitStrategy} strategy");

                return chunks;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error chunking document: {ex.Message}");
                throw;
            }
        }
    }
}
