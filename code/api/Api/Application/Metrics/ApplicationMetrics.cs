using System.Diagnostics.Metrics;

namespace Api.Application.Metrics;

public static class ApplicationMetrics
{
    private static readonly Meter Meter = new("Witnes.Api", "1.0.0");

    // Metric name constants
    public const string ActivityEmissionsCalculationDurationMetricName = "activity_emissions_calculation_duration_milliseconds";
    public const string ActivityEmissionsCalculationDurationUnit = "ms";

    public static readonly Counter<long> ActivityEmissionsCalculationsTotal = Meter.CreateCounter<long>(
        "activity_emissions_calculations_total",
        description: "Total number of activity emissions calculations processed");

    public static readonly Histogram<double> ActivityEmissionsCalculationDuration = Meter.CreateHistogram<double>(
        ActivityEmissionsCalculationDurationMetricName,
        unit: ActivityEmissionsCalculationDurationUnit,
        description: "Duration of activity emissions calculations in milliseconds");
}
