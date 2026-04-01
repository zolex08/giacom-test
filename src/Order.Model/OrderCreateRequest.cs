using System;
using System.Collections.Generic;

namespace Order.Model
{
    /// <summary>
    /// A request for the Create order endpoint.
    /// </summary>
    public class OrderCreateRequest
    {
        /// <summary>
        /// The ID of the reseller.
        /// </summary>
        public required Guid ResellerId { get; set; }

        /// <summary>
        /// The ID of the customer.
        /// </summary>
        public required Guid CustomerId { get; set; }

        /// <summary>
        /// List of products to add.
        /// </summary>
        public required IEnumerable<OrderCreateRequestProduct> Products { get; set; }
    }
}
