using System.Text.Json;
using SUI.FakeCustodians.Application.Contracts.Arbor;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Services
{
    public class ArborEventRecordProvider : IEventRecordProvider
    {
        private readonly string _basePath;
        private readonly IRecordMapper<ArborRecord> _mapper;
        
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        public ArborEventRecordProvider(IRecordMapper<ArborRecord> mapper)
        {
            // Path: <project_root>/SampleData/Arbor/
            _basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "SampleData",
                "Arbor"
            );

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        public EventResponse? GetEventRecordForSui(string sui)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui, nameof(sui));
            
            string filePath = Path.Combine(_basePath, $"{sui}.json");

            if (!File.Exists(filePath))
            {
                return null;
            }

            // Deserialize JSON into the provider-specific model
            var arborRecord = JsonSerializer.Deserialize<ArborRecord>(File.ReadAllText(filePath), _jsonSerializerOptions);
            
            if (arborRecord == null)
            {
                return null;
            }

            // Map the provider model to the unified event response
            return _mapper.Map(sui, arborRecord);
        }
    }
}