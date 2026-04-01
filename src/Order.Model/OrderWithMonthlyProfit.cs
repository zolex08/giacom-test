using System;

namespace Order.Model
{
    /// <summary>
    /// Order info with monthly profit.
    /// </summary>
    public class OrderWithMonthlyProfit
    {
        /// <summary>
        /// The ID of the order.
        /// </summary>
        public Guid Id { get; set; }


        /// <summary>
        /// The calculated monthly profit.
        /// </summary>
        public decimal MonthlyProfit { get; set; }
    }
}
