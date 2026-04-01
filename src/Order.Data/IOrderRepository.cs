using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Data
{
    /// <summary>
    /// Represents the Order repository.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Gets the list of orders.
        /// </summary>
        /// <param name="statusId">An optional status ID of the order. If not specified or <c>null</c>, all orders are returned.</param>
        /// <returns>List of orders.</returns>
        Task<IEnumerable<OrderSummary>> GetOrdersAsync(Guid? statusId = null);

        /// <summary>
        /// Gets an order by ID.
        /// </summary>
        /// <param name="orderId">Order ID.</param>
        /// <returns>The order matching the ID, or <c>null</c> if no order with that ID was found.</returns>
        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

        /// <summary>
        /// Gets a complate list of all order statuses.
        /// </summary>
        /// <returns>List of all order statuses.</returns>
        Task<IEnumerable<OrderStatus>> GetOrderStatuses();

        /// <summary>
        /// Gets all products matching any of the specified IDs.
        /// </summary>
        /// <param name="productIds">List of product IDs.</param>
        /// <returns>List of found products.</returns>
        Task<IEnumerable<OrderProduct>> GetProductsByIds(IEnumerable<Guid> productIds);

        /// <summary>
        /// Creates a new order.
        /// </summary>
        /// <param name="resellerId">The ID of the reseller.</param>
        /// <param name="customerId">The ID of the customer.</param>
        /// <returns>The ID of the newly created order.</returns>
        Task<Guid> CreateOrder(Guid resellerId, Guid customerId);

        /// <summary>
        /// Adds a product to an existing order.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="quantity">Quantity to add.</param>
        Task AddProductToOrder(Guid orderId, Guid productId, int quantity);

        /// <summary>
        /// Updates the status of an order.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="orderStatusId">The ID of the new status.</param>
        Task UpdateOrderStatus(Guid orderId, Guid orderStatusId);
    }
}
