using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.Entities
{
    public class Tag : BaseEntity
    {
        [Display(Name = "Име")]
        public string Name { get; set; }
        public ICollection<OfferTag>? OfferTags { get; set; }
    }
}