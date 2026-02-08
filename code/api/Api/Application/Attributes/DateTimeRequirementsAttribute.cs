namespace Api.Application.Attributes;

using System;
using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class DateTimeRequirementsAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not DateTime dateTime || dateTime == DateTime.MinValue)
        {
            ErrorMessage = "This field is required and must be in ISO 8601 format (e.g. 2025-07-01T00:00:00).";
            return false;
        }

        var utcNow = DateTime.UtcNow;
        var thirtyDaysAgo = utcNow.AddDays(-30);

        if (dateTime > utcNow)
        {
            ErrorMessage = "Date cannot be in the future.";
            return false;
        }

        if (dateTime < thirtyDaysAgo)
        {
            ErrorMessage = "Date cannot be more than 30 days in the past.";
            return false;
        }

        return true;
    }
}
