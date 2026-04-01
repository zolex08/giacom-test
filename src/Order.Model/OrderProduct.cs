using System;

namespace Order.Model
{
    /// <summary>
    /// A product for an order.
    /// </summary>
    public class OrderProduct
    {
        /// <summary>
        /// Product ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Service ID.
        /// </summary>
        public Guid ServiceId { get; set; }

        /// <summary>
        /// Name of the product.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unit cost.
        /// </summary>
        public decimal UnitCost { get; set; }

        /// <summary>
        /// Unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }
    }
}
