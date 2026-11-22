using Microsoft.AspNetCore.Diagnostics;

namespace Etkinlikler.Hatalar;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "İşlenmeyen istek hatası. İstekKimliği: {IstekKimligi}", context.TraceIdentifier);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Sunucu hatası",
            detail: "İstek işlenirken beklenmeyen bir hata oluştu.",
            extensions: new Dictionary<string, object?> { ["istekKimligi"] = context.TraceIdentifier }
        ).ExecuteAsync(context);
        return true;
    }
}
