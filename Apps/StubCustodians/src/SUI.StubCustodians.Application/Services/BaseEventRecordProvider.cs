using System.Text.Json;
using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services
{
    public abstract class BaseEventRecordProvider<T> : IEventRecordProvider
        where T : class
    {
        private readonly string _basePath;
        private readonly IRecordMapper<T> _mapper;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        protected BaseEventRecordProvider(
            IRecordMapper<T> mapper,
            string providerName,
            string? basePath = null
        )
        {
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

            // Default: <project_root>/SampleData/<providerName>/
            _basePath =
                basePath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "SampleData", providerName);
            _mapper = mapper;
        }

        public EventResponse? GetEventRecordForSui(string sui)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui);

            string filePath = Path.Combine(_basePath, $"{sui}.json");

            if (!File.Exists(filePath))
            {
                return null;
            }

            // Deserialize JSON into the provider-specific model
            var record = JsonSerializer.Deserialize<T>(
                File.ReadAllText(filePath),
                _jsonSerializerOptions
            );

            if (record == null)
            {
                return null;
            }

            // Map the provider model to the unified event response
            return _mapper.Map(sui, record);
        }
    }
}
