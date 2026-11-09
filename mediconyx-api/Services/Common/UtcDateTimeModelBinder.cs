using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace mediconyx_api.Services.Common;

/// <summary>
/// Binds query/route DateTimes as UTC. Query strings bypass the JSON layer, so
/// UtcDateTimeJsonConverter never sees them: a date-only value like "2026-11-09"
/// binds as Kind=Unspecified and Npgsql rejects it for timestamptz columns.
/// Date-only and naive values are assumed UTC; values with an explicit offset
/// are adjusted to UTC.
/// </summary>
public class UtcDateTimeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None || string.IsNullOrWhiteSpace(value.FirstValue))
            return Task.CompletedTask;

        if (!DateTime.TryParse(
                value.FirstValue,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            bindingContext.ModelState.AddModelError(
                bindingContext.ModelName, $"Invalid date value '{value.FirstValue}'.");
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(parsed);
        return Task.CompletedTask;
    }
}
