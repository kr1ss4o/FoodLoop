namespace FoodLoop.Models.ViewModels
{
    public class ReviewFormViewModel
    {
        public Guid ReservationId { get; set; }

        public int Rating { get; set; } = 5;

        public string? Comment { get; set; }
    }
}