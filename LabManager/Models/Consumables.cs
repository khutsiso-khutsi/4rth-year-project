namespace _4th_year_set_up.Models
{
    public class Consumable
    {
        public int ConsumableID { get; set; }

        public string ConsumableName { get; set; }

        public int SupplierID { get; set; }

        public string Supplier { get; set; }

        public int ReorderLevel { get; set; }

        public int OnHand { get; set; }

        public DateTime LastUpdated { get; set; }

        public string StockStatus
        {
            get
            {
                if (OnHand <= ReorderLevel)
                    return "Low";

                if (OnHand <= ReorderLevel + 20)
                    return "Medium";

                return "Good";
            }


        }


        public class ReceivedOrderItem
        {
            public string Name { get; set; }

            public int Quantity { get; set; }
        }

        public class ReceiveOrderRequest
        {
            public int OrderId { get; set; }

            public List<ReceivedOrderItem> ReceivedItems { get; set; }
        }


    }
}
