namespace TheOtherSide.Models
{
    public class SaleDetailLine
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public decimal Subtotal { get; set; }

        public string? Size { get; set; }
    }

    public class SaleEntry
    {
        public string Username { get; set; }
        public decimal Total { get; set; }
        public bool Confirmed { get; set; }
        public DateTime DateUtc { get; set; }
        public List<SaleDetailLine> Items { get; set; } = new();
    }
}
