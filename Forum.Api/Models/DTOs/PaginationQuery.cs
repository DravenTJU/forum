using System.ComponentModel.DataAnnotations;

namespace Forum.Api.Models.DTOs;

public class PaginationQuery
{
    [Range(1, 100, ErrorMessage = "Limit must be between 1 and 100")]
    public int Limit { get; set; } = 20;
    
    public string? Cursor { get; set; }
}

public static class PaginationHelper
{
    public static string EncodeCursor(DateTime timestamp, string id)
    {
        var cursorData = $"{timestamp:O}|{id}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(cursorData);
        return Convert.ToBase64String(bytes);
    }
    
    public static (DateTime timestamp, string id)? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return null;
            
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var cursorData = System.Text.Encoding.UTF8.GetString(bytes);
            var parts = cursorData.Split('|', 2);
            
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out var timestamp))
            {
                return (timestamp, parts[1]);
            }
        }
        catch
        {
            // Invalid cursor, ignore
        }
        
        return null;
    }
}