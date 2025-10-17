using System.Collections.Generic;

namespace TheOtherSide.Models
{
    public class Sale
    {
        public string Username { get; set; }
        public List<CartItem> Cart { get; set; } = new();
        public bool Confirmed { get; set; }
    }
}
