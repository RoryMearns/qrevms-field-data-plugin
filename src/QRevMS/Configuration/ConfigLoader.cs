using System;
using System.Collections.Generic;
using System.Text.Json;

namespace QRevMS.Configuration
{
    public static class ConfigLoader
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        public static Config Load(Dictionary<string, string> settings)
        {
            if (!settings.TryGetValue(nameof(Config), out var jsonText) || string.IsNullOrWhiteSpace(jsonText))
                return new Config();

            try
            {
                return JsonSerializer.Deserialize<Config>(jsonText, Options) ?? new Config();
            }
            catch (JsonException exception)
            {
                throw new ArgumentException($"Invalid Config JSON: {jsonText}", exception);
            }
        }
    }
}