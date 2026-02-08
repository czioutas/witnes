namespace Api.Application.Filters;

using System.Text;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

public class SnakeCaseSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema?.Properties == null) return;

        var newProperties = new Dictionary<string, IOpenApiSchema>();

        foreach (var kvp in schema.Properties)
        {
            var snakeCaseName = ToSnakeCase(kvp.Key);
            newProperties[snakeCaseName] = kvp.Value;
        }

        schema.Properties.Clear();
        foreach (var kvp in newProperties)
        {
            schema.Properties.Add(kvp.Key, kvp.Value);
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;

        var builder = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) builder.Append('_');
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }
}
