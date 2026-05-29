namespace FoodLoop.Helpers;

public static class RatingHelper
{
    public static double Average<T>(IEnumerable<T> items, Func<T, int> ratingSelector)
    {
        return Average(items.Select(ratingSelector));
    }

    public static double Average(IEnumerable<int> ratings)
    {
        // Създаваме лист от оценките, за да можем да използваме Count и Average
        var ratingList = ratings.ToList();

        // Проверка дали ресторанта има оценки
        return ratingList.Count > 0
            ? ratingList.Average()
            : 0;
    }

    public static double SmartScore(double averageRating, int reviewsCount)
    {
        // Проверка дали ресторанта има оценки
        if (reviewsCount <= 0)
            return 0;

        // Връщаме стойността на алгоритъма - средната оценка на ресторант
        return averageRating * Math.Log(1 + reviewsCount);
    }

    public static double SmartScore(IEnumerable<int> ratings)
    {
        var ratingList = ratings.ToList();

        return SmartScore(Average(ratingList), ratingList.Count);
    }

    public static double SmartScore<T>(IEnumerable<T> items, Func<T, int> ratingSelector)
    {
        var ratings = items.Select(ratingSelector).ToList();

        return SmartScore(ratings);
    }

    public static double Round(double rating, int digits = 1)
    {
        return Math.Round(rating, digits);
    }
}