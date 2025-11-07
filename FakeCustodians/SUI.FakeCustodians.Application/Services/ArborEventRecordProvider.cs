using System.Text.Json;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Services
{
    public class ArborEventRecordProvider : IEventRecordProvider
    {
        private readonly string _basePath;
        
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        public ArborEventRecordProvider()
        {
            // Path: <project_root>/SampleData/Arbor/
            _basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "SampleData",
                "Arbor"
            );
        }
        
        public EventResponse? GetEventRecordForSui(string sui)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui, nameof(sui));
            
            string filePath = Path.Combine(_basePath, $"{sui}.json");

            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<EventResponse>(json, _jsonSerializerOptions);
        }
    }
}