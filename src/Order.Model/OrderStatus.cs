using System;

namespace Order.Model
{
    /// <summary>
    /// Represents an order status.
    /// </summary>
    public class OrderStatus
    {
        /// <summary>
        /// Status identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Status name.
        /// </summary>
        public string Name { get; set; }
    }
}
