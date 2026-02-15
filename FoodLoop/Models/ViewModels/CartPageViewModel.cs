using System.Collections.Generic;
using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class CartPageViewModel
    {
        public List<CartItem> CartItems { get; set; } = new();
        public List<Reservation> Reservations { get; set; } = new();
    }

}
