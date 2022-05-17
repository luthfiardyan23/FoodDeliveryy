using System;
using System.Collections.Generic;

namespace OrderService.Models
{
    public partial class Courier
    {
        public int Id { get; set; }
        public string CourierName { get; set; } = null!;
        public int Phone { get; set; }
    }
}
