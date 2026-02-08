using Api.Application.Models;
using Libs.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace Api.Application.Filters;

public class CustomExceptionFilter : IExceptionFilter
{
    private readonly ILogger<CustomExceptionFilter> _logger;

    public CustomExceptionFilter(ILogger<CustomExceptionFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(ILogger<CustomExceptionFilter>));
    }

    public void OnException(ExceptionContext context)
    {
        // 304
        if (context.Exception is ResourceNotModifiedException)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
        }
        // 400
        else if (context.Exception is RequirementException)
        {
            RequirementException? ex = context.Exception as RequirementException;

            _logger.LogError(ex, ex?.Message);

            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status400BadRequest,
                "The API encountered an identity related error.",
                nameof(IdentityException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                [context.Exception.Message]
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 400
        else if (context.Exception is IdentityException)
        {
            IdentityException? ex = context.Exception as IdentityException;

            _logger.LogError(ex, ex?.Message);

            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status400BadRequest,
                "The API encountered an identity related error.",
                nameof(IdentityException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                ex?.IdentityErrors?.Select(e => e.Description).ToArray() ?? []
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 401
        else if (context.Exception is UnauthorizedAccessException)
        {
            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status401Unauthorized,
                "Invalid Credentials.",
                nameof(UnauthorizedAccessException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                [context.Exception.Message]
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 401
        else if (context.Exception is SecurityTokenExpiredException)
        {
            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status401Unauthorized,
                context.Exception.Message,
                nameof(UnauthorizedAccessException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                new string[] { context.Exception.Message }
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 404
        else if (context.Exception is ResourceNotFoundException)
        {
            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status404NotFound,
                "The API could not find the resource requested.",
                nameof(ResourceNotFoundException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                new string[] { context.Exception.Message }
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 409
        else if (context.Exception is RouteIdMissmatchException)
        {
            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status422UnprocessableEntity,
                "There has been id missmatch between route id and resource id.",
                nameof(RouteIdMissmatchException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                new string[] { context.Exception.Message }
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 422
        else if (context.Exception is ResourceAlreadyExistsException)
        {
            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status422UnprocessableEntity,
                "The API could not process the requested entity.",
                nameof(ResourceAlreadyExistsException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                new string[] { context.Exception.Message }
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 500
        else if (context.Exception is LogicException)
        {
            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status500InternalServerError,
                "The API encountered an Logic related error.",
                nameof(LogicException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                new string[] { context.Exception.Message }
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 502 - External Service Error
        else if (context.Exception is ExternalServiceException)
        {
            ExternalServiceException? ex = context.Exception as ExternalServiceException;

            _logger.LogError(ex, "External service error: {Message}", ex?.Message);

            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status502BadGateway,
                "External data source is currently unavailable. Please try again later.",
                nameof(ExternalServiceException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                ["External data source is currently unavailable"]
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 500 - Service Error
        else if (context.Exception is ServiceException)
        {
            ServiceException? ex = context.Exception as ServiceException;

            _logger.LogError(ex, "External service error: {Message}", ex?.Message);

            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status500InternalServerError,
                ex!.GetErrors()[0],
                nameof(ServiceException),
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                ex.GetErrors()
            );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }
        // 500
        else
        {
            _logger.LogError(context.Exception, context.Exception.Message);

            var problemDetails = new ApplicationProblemDetailsModel(
                StatusCodes.Status500InternalServerError,
                "The API encountered an internal Error.",
                "Uncaught Exception",
                context.HttpContext.Request.Path,
                context.HttpContext.TraceIdentifier,
                [context.Exception.Message]
            );
            // var problemDetails = new ApplicationProblemDetailsModel(
            //     StatusCodes.Status500InternalServerError,
            //     "The API encountered an internal Error.",
            //     "Uncaught Exception",
            //     context.HttpContext.Request.Path,
            //     context.HttpContext.TraceIdentifier,
            //     new string[] { "Internal Error X" }
            // );

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
                StatusCode = problemDetails.Status
            };
        }

        context.ExceptionHandled = true;
    }
}
