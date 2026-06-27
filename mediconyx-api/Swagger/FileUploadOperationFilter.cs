namespace mediconyx_api.Swagger;

using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Documents ASP.NET Core actions that bind <see cref="IFormFile"/> via <c>[FromForm]</c>.
/// Swashbuckle cannot model multipart file uploads out of the box; without this filter
/// <c>/swagger/v1/swagger.json</c> returns 500 for any upload action.
/// </summary>
/// <remarks>
/// Schema-building logic lives in <see cref="FileUploadSchemaBuilder"/> so it can be
/// unit-tested without a real <see cref="OperationFilterContext"/>. This filter is a
/// thin adapter that discovers the file parameters and delegates.
/// </remarks>
public sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile))
            .Select(p => new FileUploadSchemaBuilder.FormFileParam(p.Name))
            .ToList();

        if (formFileParams.Count == 0)
            return;

        operation.RequestBody = FileUploadSchemaBuilder.BuildRequestBody(formFileParams);

        var fileParamNames = FileUploadSchemaBuilder.FormFileParameterNames(formFileParams);

        // OpenApiOperation.Parameters is IList<OpenApiParameter> in Swashbuckle 10 /
        // Microsoft.OpenApi 2.3. The Where + ToList rebuild drops the file params that
        // have been folded into the multipart request body.
        operation.Parameters = operation.Parameters?
            .Where(p => p.Name != null && !fileParamNames.Contains(p.Name))
            .ToList();
    }
}