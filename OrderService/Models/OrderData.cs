namespace OrderService.Models
{
    public partial class OrderData
    {
        public int? Id { get; set; }
        public string? Code { get; set; }
        public int userId { get; set; }
        public int CourierId { get; set; }

        public List<OrderDetailData> Details { get; set; }
    }
}
