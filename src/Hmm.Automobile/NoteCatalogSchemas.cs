using System;
using System.IO;
using System.Reflection;

namespace Hmm.Automobile;

/// <summary>
/// JSON Schema (Draft-07) definitions for automobile entity NoteCatalogs.
/// Schemas are stored as embedded resources under the Schemas/ folder and
/// loaded once at startup via lazy initialization.
///
/// These schemas describe the serialized JSON structure stored in HmmNote.Content
/// and are used by <see cref="Hmm.Core.NoteSerializer.DefaultJsonNoteSerializer{T}"/>
/// for runtime validation via NJsonSchema.
///
/// Each schema follows the note envelope structure:
/// <code>{ "note": { "content": { "EntityName": { ... } } } }</code>
///
/// Schema constraints are derived from:
/// - Domain entity DataAnnotation attributes (StringLength, Range, Required)
/// - FluentValidation rules in the Validator folder
/// - Serialized property names from NoteSerialize classes
/// </summary>
public static class NoteCatalogSchemas
{
    private static readonly Lazy<string> _automobileInfoSchema = new(() => LoadSchema("AutomobileInfo.schema.json"));
    private static readonly Lazy<string> _gasLogSchema = new(() => LoadSchema("GasLog.schema.json"));
    private static readonly Lazy<string> _gasDiscountSchema = new(() => LoadSchema("GasDiscount.schema.json"));
    private static readonly Lazy<string> _gasStationSchema = new(() => LoadSchema("GasStation.schema.json"));
    private static readonly Lazy<string> _autoInsurancePolicySchema = new(() => LoadSchema("AutoInsurancePolicy.schema.json"));
    private static readonly Lazy<string> _serviceRecordSchema = new(() => LoadSchema("ServiceRecord.schema.json"));
    private static readonly Lazy<string> _autoScheduledServiceSchema = new(() => LoadSchema("AutoScheduledService.schema.json"));

    /// <summary>
    /// JSON Schema for AutomobileInfo entities.
    /// Validates: VIN (exactly 17 chars), Maker, Brand, Model, Year (1900-2100),
    /// Plate, EngineType/FuelType enums, MeterReading (&gt;0), and optional ownership/insurance/maintenance fields.
    /// </summary>
    public static string AutomobileInfoSchema => _automobileInfoSchema.Value;

    /// <summary>
    /// JSON Schema for GasLog entities.
    /// Validates: Date, AutomobileId (&gt;0), Distance/Odometer (Dimension objects),
    /// Fuel (Volume object), TotalPrice/UnitPrice (Money objects), Station reference,
    /// FuelGrade enum, driving context percentages (0-100), and discount array.
    /// </summary>
    public static string GasLogSchema => _gasLogSchema.Value;

    /// <summary>
    /// JSON Schema for GasDiscount entities.
    /// Validates: Program (non-empty), Amount (Money object with non-negative value),
    /// DiscountType enum (Flat/PerLiter), IsActive flag, and optional Comment.
    /// </summary>
    public static string GasDiscountSchema => _gasDiscountSchema.Value;

    /// <summary>
    /// JSON Schema for GasStation entities.
    /// Validates: Name (required, max 100), Address (max 200), City/State (max 50),
    /// ZipCode (max 20), Description (max 500), and IsActive flag.
    /// </summary>
    public static string GasStationSchema => _gasStationSchema.Value;

    /// <summary>
    /// JSON Schema for AutoInsurancePolicy entities. Validates required policy
    /// fields, Money premium, optional deductible, and the embedded coverage list.
    /// </summary>
    public static string AutoInsurancePolicySchema => _autoInsurancePolicySchema.Value;

    /// <summary>
    /// JSON Schema for ServiceRecord entities. Validates required automobile/date/mileage/type
    /// fields, optional Money cost, and the embedded parts list.
    /// </summary>
    public static string ServiceRecordSchema => _serviceRecordSchema.Value;

    /// <summary>
    /// JSON Schema for AutoScheduledService entities. Validates required name/type
    /// and optional interval / next-due fields.
    /// </summary>
    public static string AutoScheduledServiceSchema => _autoScheduledServiceSchema.Value;

    private static string LoadSchema(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Hmm.Automobile.Schemas.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
