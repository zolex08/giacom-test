using System;
using System.ComponentModel.DataAnnotations;

namespace Order.Model
{
    /// <summary>
    /// A product in the Create order endpoint request.
    /// </summary>
    public class OrderCreateRequestProduct
    {
        /// <summary>
        /// The ID of the product.
        /// </summary>
        public required Guid ProductId { get; set; }

        /// <summary>
        /// Quantity to add.
        /// </summary>

        [Range(1, int.MaxValue)]
        public required int Quantity { get; set; }
    }
}
