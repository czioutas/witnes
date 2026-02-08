namespace Api.Application.Attributes;

using System;
using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class DateTimeOffsetRequirementsAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not DateTimeOffset dateTimeOffset || dateTimeOffset == DateTimeOffset.MinValue)
        {
            ErrorMessage = "This field is required and must be in ISO 8601 format with timezone (e.g. 2025-07-01T00:00:00+01:00).";
            return false;
        }

        // Convert both to UTC for proper comparison
        var dateTimeOffsetUtc = dateTimeOffset.ToUniversalTime();
        var utcNow = DateTimeOffset.UtcNow;
        var thirtyDaysAgo = utcNow.AddDays(-30);

        if (dateTimeOffsetUtc > utcNow)
        {
            ErrorMessage = "Date cannot be in the future.";
            return false;
        }

        if (dateTimeOffsetUtc < thirtyDaysAgo)
        {
            ErrorMessage = "Date cannot be more than 30 days in the past.";
            return false;
        }

        return true;
    }
}
