using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.Configuration;

public class ApiDeprecationOptions
{
    /// <summary>
    /// Maps API version strings (e.g., "1.0") to their sunset dates.
    /// When a version is in this dictionary, the Sunset header (RFC 8594) will be added to responses.
    /// </summary>
    public Dictionary<string, DateTimeOffset> SunsetSchedule { get; set; } = new();

    /// <summary>
    /// Optional URL to migration/deprecation documentation.
    /// When set, a Link header with rel="sunset" will be included alongside the Sunset header.
    /// </summary>
    public string DeprecationDocUrl { get; set; }
}
