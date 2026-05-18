using System.Net;

namespace ClinicApp.Application.Common.Exceptions;

public sealed class ApiException : Exception
{
    public ApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
