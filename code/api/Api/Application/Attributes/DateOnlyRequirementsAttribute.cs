namespace Api.Application.Attributes;

using System;
using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class DateOnlyRequirementsAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not DateOnly dateOnly || dateOnly == DateOnly.MinValue)
        {
            ErrorMessage = "This field is required and must be in ISO 8601 format (e.g. 2025-07-01).";
            return false;
        }

        var utcNow = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = utcNow.AddDays(-30);

        if (dateOnly > utcNow)
        {
            ErrorMessage = "Date cannot be in the future.";
            return false;
        }

        if (dateOnly < thirtyDaysAgo)
        {
            ErrorMessage = "Date cannot be more than 30 days in the past.";
            return false;
        }

        return true;
    }
}
