using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace mediconyx_api.Services.Common;

/// <summary>
/// Supplies <see cref="UtcDateTimeModelBinder"/> for DateTime query/route
/// parameters. Body-bound DateTimes are excluded — those are deserialized by
/// the JSON input formatter, which already applies UtcDateTimeJsonConverter.
/// </summary>
public class UtcDateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var modelType = context.Metadata.ModelType;
        var isDate = modelType == typeof(DateTime) || modelType == typeof(DateTime?);
        if (!isDate)
            return null;

        var source = context.BindingInfo?.BindingSource;
        if (source == BindingSource.Body || source == BindingSource.Form || source == BindingSource.FormFile)
            return null;

        return new UtcDateTimeModelBinder();
    }
}
