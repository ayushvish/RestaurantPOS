using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestaurantPOS.API.Tests.Helpers;

public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
