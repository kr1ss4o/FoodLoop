namespace FoodLoop.Helpers;

public static class AdminControllerHelper
{
    public static string Get(string entity)
    {
        return entity switch
        {
            "Category" => "AdminCategories",
            "Tag" => "AdminTags",
            "Restaurant" => "AdminRestaurants",
            "Offer" => "AdminOffers",
            "Review" => "AdminReviews",
            "Reservation" => "AdminReservations",
            _ => $"Admin{entity}s"
        };
    }
}