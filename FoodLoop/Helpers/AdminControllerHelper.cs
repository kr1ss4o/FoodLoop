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
    public static string GetListAction(string entity)
    {
        return entity switch
        {
            "Category" => "Categories",
            "Tag" => "Tags",
            "Restaurant" => "Restaurants",
            "Offer" => "Offers",
            "Review" => "Reviews",
            "Reservation" => "Reservations",
            _ => $"{entity}s"
        };
    }
}