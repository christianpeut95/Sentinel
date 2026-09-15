using System.Text;

namespace Sentinel.Services;

/// <summary>
/// Shared validation for text-based uploads. Text and CSV formats do not have
/// a reliable binary signature, so Sentinel accepts only well-formed UTF-8
/// text and lets each import verify its required structure afterwards.
/// </summary>
public static class UploadContentValidator
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static async Task<string> ReadStrictUtf8TextAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var reader = new StreamReader(
            stream,
            StrictUtf8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 16 * 1024,
            leaveOpen: true);

        var content = await reader.ReadToEndAsync(cancellationToken);
        content = content.TrimStart('\uFEFF'); // Accept a UTF-8 BOM, but no alternate encoding.

        if (content.IndexOf('\0') >= 0)
        {
            throw new InvalidDataException("The uploaded text contains invalid binary data.");
        }

        return content;
    }

    public static async Task ValidateStrictUtf8TextAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        _ = await ReadStrictUtf8TextAsync(stream, cancellationToken);
    }
}
