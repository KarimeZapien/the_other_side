//es el modelo del item del carrito, o sea su estructura
namespace TheOtherSide.Models
{
    public class CartItem
    {
        public int Id { get; set; }      
        public string Name { get; set; }   
        public decimal Price { get; set; } 

        public string? Size { get; set; }
    }
}

