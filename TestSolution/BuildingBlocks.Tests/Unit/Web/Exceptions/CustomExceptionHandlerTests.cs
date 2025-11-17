using System.Text.Json;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Web.Exceptions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Shouldly;

namespace BuildingBlocks.Tests.Unit.Web.Exceptions;

public class CustomExceptionHandlerTests
{
    [Test]
    public async Task Should_include_bad_request_reason_in_problem_details()
    {
        var handler = new CustomExceptionHandler(new NullLogger<CustomExceptionHandler>());
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Path = "/api/transactions";

        var exception = new BadHttpRequestException("El campo value es obligatorio.");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        document.RootElement.GetProperty("detail").GetString()
            .ShouldBe("El campo value es obligatorio.");
        document.RootElement.GetProperty("type").GetString()
            .ShouldBe("https://errors.example.com/validation/invalid-data");
    }

    [Test]
    public async Task Should_include_validation_failure_messages_in_problem_details_detail()
    {
        var handler = new CustomExceptionHandler(new NullLogger<CustomExceptionHandler>());
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Path = "/api/transactions";

        var failures = new[]
        {
            new ValidationFailure("Value", "El campo value es obligatorio."),
            new ValidationFailure("Description", "La descripción supera la longitud permitida.")
        };

        var exception = new ValidationException(failures);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        document.RootElement.GetProperty("detail").GetString()
            .ShouldBe("El campo value es obligatorio. La descripción supera la longitud permitida.");
        document.RootElement.GetProperty("type").GetString()
            .ShouldBe("https://errors.example.com/validation/invalid-data");

        var errors = document.RootElement.GetProperty("errors");
        errors.GetProperty("Value")[0].GetString()
            .ShouldBe("El campo value es obligatorio.");
        errors.GetProperty("Description")[0].GetString()
            .ShouldBe("La descripción supera la longitud permitida.");
    }
}
