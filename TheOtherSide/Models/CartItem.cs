//es el modelo del item del carrito, o sea su estructura
namespace TheOtherSide.Models
{
    public class CartItem
    {
        public int Id { get; set; }        // solo el ID del producto
        public string Name { get; set; }   // nombre del producto
        public decimal Price { get; set; } // precio unitario
    }
}

