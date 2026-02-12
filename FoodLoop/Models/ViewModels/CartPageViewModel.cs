using FoodLoop.Models.Entities;
using System.Collections.Generic;

namespace FoodLoop.Models.ViewModels
{
    public class CartPageViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public List<Reservation> Reservations { get; set; }
    }
}