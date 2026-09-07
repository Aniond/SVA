using System.Buffers.Binary;

namespace SolaceWeather.Core;

public sealed class PortraitFailureException : InvalidOperationException
{
    public string Code { get; }
    public PortraitFailureException(string code) : base("Player portrait failed.") => Code = code;
}

/// <summary>Bounds PNG/JPEG allocations before handing provider bytes to the native image decoder.</summary>
public static class PortraitImageHeader
{
    public static (int Width, int Height) Read(byte[] bytes)
    {
        if (bytes.Length < 100 || bytes.Length > 8 * 1024 * 1024) throw new PortraitFailureException("image_byte_limit");
        byte[] png = { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.AsSpan(0, 8).SequenceEqual(png))
        {
            if (!bytes.AsSpan(12, 4).SequenceEqual(new byte[] { 73, 72, 68, 82 })) throw new PortraitFailureException("invalid_png_header");
            return Validate(BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        }
        if (bytes[0] == 255 && bytes[1] == 216)
        {
            int offset = 2;
            while (offset < bytes.Length)
            {
                if (bytes[offset++] != 255) break;
                while (offset < bytes.Length && bytes[offset] == 255) offset++;
                if (offset >= bytes.Length) break;
                byte marker = bytes[offset++];
                if (marker is 0xDA or 0xD9) break;
                if (marker is 0x01 or >= 0xD0 and <= 0xD8) continue;
                if (offset + 2 > bytes.Length) break;
                int length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
                if (length < 2 || offset + length > bytes.Length) break;
                if (marker is >= 0xC0 and <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC))
                {
                    if (length < 8) break;
                    int height = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 3, 2));
                    int width = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 5, 2));
                    return Validate(width, height);
                }
                offset += length;
            }
            throw new PortraitFailureException("invalid_jpeg_header");
        }
        throw new PortraitFailureException("unsupported_image_format");
    }
    private static (int Width, int Height) Validate(int width, int height)
    {
        if (width < 64 || height < 64 || width > 2048 || height > 2048 || width != height) throw new PortraitFailureException("image_dimensions");
        return (width, height);
    }
    public static string FailureCategory(Exception? error, string phase)
    {
        string category = error switch
        {
            PortraitFailureException known => known.Code,
            OperationCanceledException => "cancelled_or_timeout",
            System.Text.Json.JsonException => "invalid_response_json",
            FormatException => "invalid_image_encoding",
            IOException => "storage_or_stream_failure",
            HttpRequestException => "network_failure",
            _ => "unexpected_failure"
        };
        // Only explicit internal codes may reach logs. Never use Exception.Message.
        bool knownCode = new[] { "image_byte_limit", "invalid_png_header", "invalid_jpeg_header", "unsupported_image_format", "image_dimensions", "unsupported_image_mime", "no_image_returned", "response_byte_limit", "credential_missing", "native_decode_failed", "blank_image", "decoded_dimensions_mismatch", "capture_failed" }.Contains(category)
            || System.Text.RegularExpressions.Regex.IsMatch(category, "^http_[1-5][0-9]{2}$");
        if (error is PortraitFailureException && !knownCode) category = "unexpected_failure";
        if (phase is not ("service" or "capture" or "request" or "decode" or "cache" or "retry")) phase = "service";
        return phase + ":" + category;
    }
}
