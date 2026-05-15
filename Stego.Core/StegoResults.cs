namespace Stego.Core;

public sealed record StegoEncodeResult(
    bool Success,
    StegoErrorCode ErrorCode,
    string? ErrorMessage,
    string? OutputPath)
{
    public static StegoEncodeResult Ok(string outputPath) =>
        new(true, StegoErrorCode.None, null, outputPath);

    public static StegoEncodeResult Fail(StegoErrorCode errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage, null);
}

public sealed record StegoDecodeResult(
    bool Success,
    StegoErrorCode ErrorCode,
    string? ErrorMessage,
    string? Message)
{
    public static StegoDecodeResult Ok(string message) =>
        new(true, StegoErrorCode.None, null, message);

    public static StegoDecodeResult Fail(StegoErrorCode errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage, null);
}