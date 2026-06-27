namespace mediconyx_api.Swagger;

using Microsoft.OpenApi;

/// <summary>
/// Static helper that contains the schema-building decision logic for
/// <see cref="FileUploadOperationFilter"/>. Extracted from the filter so it
/// can be unit-tested without having to construct Swashbuckle's
/// <c>OperationFilterContext</c> (whose internals are heavy and version-sensitive).
/// </summary>
public static class FileUploadSchemaBuilder
{
    /// <summary>
    /// Lightweight DTO mirroring the subset of <c>ApiParameterDescription</c>
    /// fields that the filter cares about.
    /// </summary>
    public sealed record FormFileParam(string Name);

    /// <summary>
    /// Builds a multipart/form-data <see cref="OpenApiRequestBody"/> describing
    /// every supplied form-file parameter as a string/binary property and
    /// marking each one as required.
    /// </summary>
    /// <param name="formFileParams">The form-file parameters discovered on the action.</param>
    /// <returns>The constructed request body.</returns>
    public static OpenApiRequestBody BuildRequestBody(IEnumerable<FormFileParam> formFileParams)
    {
        var props = formFileParams.ToDictionary(
            p => p.Name,
            _ => (IOpenApiSchema)new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" });

        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = props,
            Required = new HashSet<string>(props.Keys),
        };

        return new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType { Schema = schema },
            },
        };
    }

    /// <summary>
    /// Projects a sequence of form-file parameters down to a hash set of
    /// their names — used by the filter to strip them out of
    /// <c>operation.Parameters</c> once they have been moved into the
    /// multipart request body.
    /// </summary>
    public static HashSet<string> FormFileParameterNames(IEnumerable<FormFileParam> formFileParams)
        => formFileParams.Select(p => p.Name).ToHashSet();
}