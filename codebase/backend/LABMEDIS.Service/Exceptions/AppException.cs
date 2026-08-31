namespace LABMEDIS.Service.Exceptions;

/// <summary>
/// Expected business-rule failure carrying the HTTP status code and machine-readable error
/// code documented in contracts/*.md (e.g. 409 DESIGNATION_DUPLICATE, 422 OVERLAPPING_PRICE_PERIOD).
/// Controllers catch this before the generic Exception branch and translate it directly —
/// this is not the "explicit 500" forbidden by Principle VII, it is a known, anticipated
/// failure surfaced with its documented status code.
/// </summary>
public class AppException(int statusCode, string errorCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;
}
