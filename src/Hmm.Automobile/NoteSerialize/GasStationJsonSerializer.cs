using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile.NoteSerialize
{
    /// <summary>
    /// Helper class for parsing GasStation references in GasLog JSON.
    /// Supports: station ID (for lookup), string name, or full station object.
    /// This is used by GasLogJsonNoteSerialize to handle station references.
    /// </summary>
    public class GasStationXRefSerializer
    {
        private readonly IAutoEntityManager<GasStation> _stationManager;
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public GasStationXRefSerializer(
            IAutoEntityManager<GasStation> stationManager,
            ILogger logger,
            JsonSerializerOptions jsonOptions = null)
        {
            ArgumentNullException.ThrowIfNull(stationManager);
            ArgumentNullException.ThrowIfNull(logger);

            _stationManager = stationManager;
            _logger = logger;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Parses GasStation from JSON element.
        /// Supports: station ID (for lookup), string name, or full station object.
        /// </summary>
        /// <param name="stationElement">The JSON element containing station information.</param>
        /// <returns>ProcessingResult containing GasStation object.</returns>
        public async Task<ProcessingResult<GasStation>> DeserializeAsync(JsonElement stationElement)
        {
            try
            {
                // Case 1: Station ID (integer) - lookup from database
                if (stationElement.ValueKind == JsonValueKind.Number)
                {
                    var stationId = stationElement.GetInt32();
                    return await GetStationByIdAsync(stationId);
                }

                // Case 2: Station name (string) - create transient station
                if (stationElement.ValueKind == JsonValueKind.String)
                {
                    var stationName = stationElement.GetString();
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        return ProcessingResult<GasStation>.Ok(null, "Empty station name");
                    }

                    return ProcessingResult<GasStation>.Ok(new GasStation
                    {
                        Name = stationName,
                        Description = $"Gas station: {stationName}"
                    });
                }

                // Case 3: Full station object
                if (stationElement.ValueKind == JsonValueKind.Object)
                {
                    // Check if it has an ID - if so, look it up
                    if (stationElement.TryGetProperty("id", out var idElement) &&
                        idElement.TryGetInt32(out var stationId) &&
                        stationId > 0)
                    {
                        return await GetStationByIdAsync(stationId);
                    }

                    // Otherwise, deserialize as inline station object
                    var station = new GasStation
                    {
                        Name = GetStringProperty(stationElement, "name"),
                        Address = GetStringProperty(stationElement, "address"),
                        City = GetStringProperty(stationElement, "city"),
                        State = GetStringProperty(stationElement, "state"),
                        ZipCode = GetStringProperty(stationElement, "zipCode"),
                        Description = GetStringProperty(stationElement, "description")
                    };

                    return ProcessingResult<GasStation>.Ok(station);
                }

                return ProcessingResult<GasStation>.Ok(null, "No valid station data found");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while deserializing GasStation");
                return ProcessingResult<GasStation>.Fail(
                    $"Invalid JSON format for station: {ex.Message}",
                    ErrorCategory.MappingError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing GasStation from JSON");
                return ProcessingResult<GasStation>.FromException(ex);
            }
        }

        /// <summary>
        /// Serializes GasStation to appropriate JSON representation.
        /// If station has ID, stores ID reference; otherwise stores full object.
        /// </summary>
        /// <param name="station">The GasStation to serialize.</param>
        /// <returns>ProcessingResult containing serializable object.</returns>
        public ProcessingResult<object> Serialize(GasStation station)
        {
            try
            {
                if (station == null)
                {
                    return ProcessingResult<object>.Ok(null);
                }

                // If station has an ID, just store the ID for reference
                if (station.Id > 0)
                {
                    return ProcessingResult<object>.Ok(station.Id);
                }

                // Otherwise, store the station details inline
                var stationData = new
                {
                    name = station.Name ?? string.Empty,
                    address = station.Address ?? string.Empty,
                    city = station.City ?? string.Empty,
                    state = station.State ?? string.Empty,
                    zipCode = station.ZipCode ?? string.Empty,
                    description = station.Description ?? string.Empty
                };

                return ProcessingResult<object>.Ok(stationData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serializing GasStation to JSON");
                return ProcessingResult<object>.FromException(ex);
            }
        }

        /// <summary>
        /// Serializes GasStation and returns JSON string.
        /// </summary>
        /// <param name="station">The GasStation to serialize.</param>
        /// <returns>ProcessingResult containing JSON string.</returns>
        public ProcessingResult<string> SerializeToJson(GasStation station)
        {
            try
            {
                var serializeResult = Serialize(station);
                if (!serializeResult.Success)
                {
                    return ProcessingResult<string>.Fail(
                        serializeResult.ErrorMessage,
                        serializeResult.ErrorType);
                }

                if (serializeResult.Value == null)
                {
                    return ProcessingResult<string>.Ok(null);
                }

                var json = JsonSerializer.Serialize(serializeResult.Value, _jsonOptions);
                return ProcessingResult<string>.Ok(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serializing GasStation to JSON string");
                return ProcessingResult<string>.FromException(ex);
            }
        }

        /// <summary>
        /// Gets station by ID with error handling.
        /// </summary>
        private async Task<ProcessingResult<GasStation>> GetStationByIdAsync(int stationId)
        {
            if (stationId <= 0)
            {
                return ProcessingResult<GasStation>.Invalid("Invalid station ID");
            }

            var result = await _stationManager.GetEntityByIdAsync(stationId);

            if (!result.Success)
            {
                _logger.LogWarning("Cannot find gas station with ID: {StationId}", stationId);
                return ProcessingResult<GasStation>.NotFound(
                    $"Gas station with ID {stationId} not found");
            }

            return result;
        }

        /// <summary>
        /// Attempts to find or create a station by name.
        /// </summary>
        /// <param name="stationName">The station name to search for.</param>
        /// <returns>ProcessingResult containing GasStation.</returns>
        public async Task<ProcessingResult<GasStation>> FindOrCreateByNameAsync(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return ProcessingResult<GasStation>.Invalid("Station name cannot be empty");
            }

            try
            {
                // Try to find existing station by name
                var stationsResult = await _stationManager.GetEntitiesAsync();
                if (!stationsResult.Success || stationsResult.Value == null)
                {
                    return ProcessingResult<GasStation>.Ok(new GasStation
                    {
                        Name = stationName,
                        Description = $"Gas station: {stationName}"
                    });
                }

                var existingStation = stationsResult.Value.FirstOrDefault(s =>
                    s.Name.Equals(stationName, StringComparison.OrdinalIgnoreCase));

                if (existingStation != null)
                {
                    return ProcessingResult<GasStation>.Ok(existingStation);
                }

                // Create new transient station (not persisted)
                var newStation = new GasStation
                {
                    Name = stationName,
                    Description = $"Gas station: {stationName}"
                };

                return ProcessingResult<GasStation>.Ok(newStation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding or creating station by name: {StationName}", stationName);
                return ProcessingResult<GasStation>.FromException(ex);
            }
        }

        private static string GetStringProperty(JsonElement element, string propertyName, string defaultValue = null)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.GetString() ?? defaultValue;
            }
            return defaultValue;
        }
    }
}