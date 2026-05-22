using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ClinicApp.API.Swagger
{
    /// <summary>
    /// Operation filter that adds proper request body schema for endpoints that accept IFormFile via [FromForm].
    /// This resolves Swashbuckle errors related to "[FromForm] attribute used with IFormFile".
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Identify parameters of type IFormFile that are bound from form.
            var fileParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type == typeof(IFormFile) && p.Source.Id == "FromForm")
                .ToList();

            if (!fileParams.Any())
                return;

            // Ensure a request body exists.
            operation.RequestBody ??= new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>()
            };

            // Define schema for multipart/form-data.
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>()
            };

            foreach (var param in fileParams)
            {
                // For file parameters we use "binary" format.
                schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = param.ModelMetadata?.Description
                };
            }

            // Add or replace the multipart/form-data media type.
            operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = schema
            };
        }
    }
}
