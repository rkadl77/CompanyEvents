using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace HITS.Filters
{
    public class SwaggerDefaultValuesFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var methodAttributes = context.MethodInfo.GetCustomAttributes(true);
            var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? Array.Empty<object>();

            var hasAllowAnonymous = methodAttributes.OfType<AllowAnonymousAttribute>().Any() ||
                                   controllerAttributes.OfType<AllowAnonymousAttribute>().Any();

            var hasAuthorize = methodAttributes.OfType<AuthorizeAttribute>().Any() ||
                              controllerAttributes.OfType<AuthorizeAttribute>().Any();

            if (hasAllowAnonymous && !methodAttributes.OfType<AuthorizeAttribute>().Any())
            {
                operation.Security.Clear();
            }

            else if (!hasAuthorize && !hasAllowAnonymous)
            {
                operation.Security.Clear();
            }
        }
    }
}