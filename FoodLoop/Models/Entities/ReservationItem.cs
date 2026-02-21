namespace FoodLoop.Models.Entities
{
    public class ReservationItem : BaseEntity
    {
        public Guid ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        public Guid OfferId { get; set; }
        public Offer Offer { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal PriceSnapshot { get; set; }
    }
}
