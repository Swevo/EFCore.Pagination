using System.Text.Json;

namespace Swevo.EFCore.Pagination;

internal static class CursorTokenEncoder
{
    public static string Encode<T>(T value) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value));

    public static T Decode<T>(string cursor) =>
        JsonSerializer.Deserialize<T>(Convert.FromBase64String(cursor))!;
}
