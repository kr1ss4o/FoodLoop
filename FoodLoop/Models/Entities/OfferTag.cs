namespace FoodLoop.Models.Entities
{
    public class OfferTag
    {
        public Guid OfferId { get; set; }
        public Offer Offer { get; set; }

        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
    }
}