using System.Runtime.Serialization;

namespace Libs.Domain;

/// <summary>
/// Territory codes including world, continents, and ISO 3166-1 alpha-2 country codes
/// </summary>
public enum TerritoryCode
{
    // Global level
    /// <summary>World / Global</summary>
    [EnumMember(Value = "WORLD")]
    WORLD = 1,

    // Continents
    /// <summary>Africa</summary>
    [EnumMember(Value = "AF")]
    Africa = 100,

    /// <summary>Asia</summary>
    [EnumMember(Value = "AS")]
    Asia = 101,

    /// <summary>Europe</summary>
    [EnumMember(Value = "EU")]
    Europe = 102,

    /// <summary>North America</summary>
    [EnumMember(Value = "NA")]
    NorthAmerica = 103,

    /// <summary>South America</summary>
    [EnumMember(Value = "SA")]
    SouthAmerica = 104,

    /// <summary>Oceania</summary>
    [EnumMember(Value = "OC")]
    Oceania = 105,

    /// <summary>Antarctica</summary>
    [EnumMember(Value = "AN")]
    Antarctica = 106,

    // European Countries (starting from 1000)
    /// <summary>Austria</summary>
    [EnumMember(Value = "AT")]
    AT = 1000,

    /// <summary>Belgium</summary>
    [EnumMember(Value = "BE")]
    BE = 1001,

    /// <summary>Bulgaria</summary>
    [EnumMember(Value = "BG")]
    BG = 1002,

    /// <summary>Croatia</summary>
    [EnumMember(Value = "HR")]
    HR = 1003,

    /// <summary>Cyprus</summary>
    [EnumMember(Value = "CY")]
    CY = 1004,

    /// <summary>Czech Republic</summary>
    [EnumMember(Value = "CZ")]
    CZ = 1005,

    /// <summary>Denmark</summary>
    [EnumMember(Value = "DK")]
    DK = 1006,

    /// <summary>Estonia</summary>
    [EnumMember(Value = "EE")]
    EE = 1007,

    /// <summary>Finland</summary>
    [EnumMember(Value = "FI")]
    FI = 1008,

    /// <summary>France</summary>
    [EnumMember(Value = "FR")]
    FR = 1009,

    /// <summary>Germany</summary>
    [EnumMember(Value = "DE")]
    DE = 1010,

    /// <summary>Greece</summary>
    [EnumMember(Value = "GR")]
    GR = 1011,

    /// <summary>Hungary</summary>
    [EnumMember(Value = "HU")]
    HU = 1012,

    /// <summary>Ireland</summary>
    [EnumMember(Value = "IE")]
    IE = 1013,

    /// <summary>Italy</summary>
    [EnumMember(Value = "IT")]
    IT = 1014,

    /// <summary>Latvia</summary>
    [EnumMember(Value = "LV")]
    LV = 1015,

    /// <summary>Lithuania</summary>
    [EnumMember(Value = "LT")]
    LT = 1016,

    /// <summary>Luxembourg</summary>
    [EnumMember(Value = "LU")]
    LU = 1017,

    /// <summary>Malta</summary>
    [EnumMember(Value = "MT")]
    MT = 1018,

    /// <summary>Netherlands</summary>
    [EnumMember(Value = "NL")]
    NL = 1019,

    /// <summary>Poland</summary>
    [EnumMember(Value = "PL")]
    PL = 1020,

    /// <summary>Portugal</summary>
    [EnumMember(Value = "PT")]
    PT = 1021,

    /// <summary>Romania</summary>
    [EnumMember(Value = "RO")]
    RO = 1022,

    /// <summary>Slovakia</summary>
    [EnumMember(Value = "SK")]
    SK = 1023,

    /// <summary>Slovenia</summary>
    [EnumMember(Value = "SI")]
    SI = 1024,

    /// <summary>Spain</summary>
    [EnumMember(Value = "ES")]
    ES = 1025,

    /// <summary>Sweden</summary>
    [EnumMember(Value = "SE")]
    SE = 1026,

    /// <summary>United Kingdom</summary>
    [EnumMember(Value = "GB")]
    GB = 1027,

    // Add more countries as needed
}
