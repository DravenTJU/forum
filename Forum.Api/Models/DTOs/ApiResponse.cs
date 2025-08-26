using System.Text.Json.Serialization;

namespace Forum.Api.Models.DTOs;

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("error")]
    public ApiError? Error { get; set; }
    
    [JsonPropertyName("meta")]
    public ApiMeta? Meta { get; set; }

    public static ApiResponse<T> SuccessResult(T data, ApiMeta? meta = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Meta = meta
        };
    }

    public static ApiResponse<T> ErrorResult(string code, string message, Dictionary<string, string[]>? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }
}

public class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("error")]
    public ApiError? Error { get; set; }
    
    [JsonPropertyName("meta")]
    public ApiMeta? Meta { get; set; }

    public static ApiResponse SuccessResult(object? data = null, ApiMeta? meta = null)
    {
        return new ApiResponse
        {
            Success = true,
            Data = data,
            Meta = meta
        };
    }

    public static ApiResponse ErrorResult(string code, string message, Dictionary<string, string[]>? details = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }
}

public class ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("details")]
    public Dictionary<string, string[]>? Details { get; set; }
}

public class ApiMeta
{
    [JsonPropertyName("total")]
    public long? Total { get; set; }
    
    [JsonPropertyName("hasNext")]
    public bool? HasNext { get; set; }
    
    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }
}