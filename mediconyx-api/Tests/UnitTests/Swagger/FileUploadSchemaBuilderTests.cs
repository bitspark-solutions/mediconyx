using FluentAssertions;
using mediconyx_api.Swagger;
using Microsoft.OpenApi;
using Xunit;

namespace mediconyx_api.Tests.UnitTests.Swagger;

public class FileUploadSchemaBuilderTests
{
    [Fact]
    public void BuildRequestBody_WithSingleFileParam_SetsMultipartBinaryProperty()
    {
        // Arrange
        var fileParams = new[] { new FileUploadSchemaBuilder.FormFileParam("file") };

        // Act
        var body = FileUploadSchemaBuilder.BuildRequestBody(fileParams);

        // Assert
        body.Content.Should().ContainKey("multipart/form-data");
        var mediaType = body.Content["multipart/form-data"];
        mediaType.Schema.Should().NotBeNull();
        mediaType.Schema!.Type.Should().Be(JsonSchemaType.Object);

        mediaType.Schema.Properties.Should().ContainKey("file");
        var fileProp = (OpenApiSchema)mediaType.Schema.Properties["file"];
        fileProp.Type.Should().Be(JsonSchemaType.String);
        fileProp.Format.Should().Be("binary");

        mediaType.Schema.Required.Should().BeEquivalentTo(new[] { "file" });
    }

    [Fact]
    public void BuildRequestBody_WithMultipleFileParams_ListsAllProperties()
    {
        // Arrange
        var fileParams = new[]
        {
            new FileUploadSchemaBuilder.FormFileParam("file"),
            new FileUploadSchemaBuilder.FormFileParam("attachment"),
        };

        // Act
        var body = FileUploadSchemaBuilder.BuildRequestBody(fileParams);

        // Assert
        var schema = (OpenApiSchema)body.Content["multipart/form-data"].Schema!;
        schema.Properties.Should().NotBeNull();
        schema.Properties!.Keys.Should().BeEquivalentTo(new[] { "file", "attachment" });
        schema.Required.Should().BeEquivalentTo(new[] { "file", "attachment" });
        schema.Type.Should().Be(JsonSchemaType.Object);
    }

    [Fact]
    public void BuildRequestBody_WithEmptyInput_ReturnsBodyWithNoProperties()
    {
        // Arrange
        var fileParams = Array.Empty<FileUploadSchemaBuilder.FormFileParam>();

        // Act
        var body = FileUploadSchemaBuilder.BuildRequestBody(fileParams);

        // Assert
        body.Content.Should().ContainKey("multipart/form-data");
        var schema = (OpenApiSchema)body.Content["multipart/form-data"].Schema!;
        schema.Properties.Should().NotBeNull().And.BeEmpty();
        schema.Required.Should().BeEmpty();
        schema.Type.Should().Be(JsonSchemaType.Object);
    }

    [Fact]
    public void BuildRequestBody_PreservesParameterOrderInRequiredSet()
    {
        // Arrange — caller's order must be reflected in Required.
        var fileParams = new[]
        {
            new FileUploadSchemaBuilder.FormFileParam("z_first"),
            new FileUploadSchemaBuilder.FormFileParam("a_second"),
        };

        // Act
        var body = FileUploadSchemaBuilder.BuildRequestBody(fileParams);

        // Assert
        var schema = (OpenApiSchema)body.Content["multipart/form-data"].Schema!;
        schema.Required.Should().Equal(new[] { "z_first", "a_second" });
    }

    [Fact]
    public void FormFileParameterNames_ProjectsNamesAsHashSet()
    {
        // Arrange
        var fileParams = new[]
        {
            new FileUploadSchemaBuilder.FormFileParam("a"),
            new FileUploadSchemaBuilder.FormFileParam("b"),
        };

        // Act
        var names = FileUploadSchemaBuilder.FormFileParameterNames(fileParams);

        // Assert
        names.Should().BeEquivalentTo(new HashSet<string> { "a", "b" });
        names.Should().BeOfType<HashSet<string>>();
    }

    [Fact]
    public void FormFileParameterNames_OnEmptyInput_ReturnsEmptyHashSet()
    {
        // Arrange
        var fileParams = Array.Empty<FileUploadSchemaBuilder.FormFileParam>();

        // Act
        var names = FileUploadSchemaBuilder.FormFileParameterNames(fileParams);

        // Assert
        names.Should().BeEmpty();
        names.Should().BeOfType<HashSet<string>>();
    }

    [Fact]
    public void FormFileParameterNames_DeduplicatesRepeatedNames()
    {
        // Arrange — defensive: hash-set semantics are part of the contract
        // because the filter uses this set to remove params from Parameters.
        var fileParams = new[]
        {
            new FileUploadSchemaBuilder.FormFileParam("file"),
            new FileUploadSchemaBuilder.FormFileParam("file"),
        };

        // Act
        var names = FileUploadSchemaBuilder.FormFileParameterNames(fileParams);

        // Assert
        names.Should().HaveCount(1);
        names.Should().Contain("file");
    }
}