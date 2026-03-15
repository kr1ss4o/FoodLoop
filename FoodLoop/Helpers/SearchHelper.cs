namespace FoodLoop.Helpers;

public static class SearchHelper
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value.Trim().ToLower();
    }
}