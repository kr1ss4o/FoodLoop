namespace FoodLoop.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public string Action { get; set; } = "Index";
        public string Controller { get; set; } = "";

        public string? Query { get; set; }
        public string? Sort { get; set; }
        public Guid? CategoryId { get; set; }

        // ако имаш multi-section pagination (breakfast/lunch/dinner)
        public string? Section { get; set; }
    }
}