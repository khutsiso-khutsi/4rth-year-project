namespace _4th_year_set_up.Models
{
    public class Consumable
    {
        public int Id { get; set; }

        public string ConsumableName { get; set; }

        public string Supplier { get; set; }

        public int OnHand { get; set; }

        public int ReorderLevel { get; set; }

        public string StockStatus { get; set; }
    }
}
