using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Service
{
    /// <summary>
    /// Represents the Order service.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Gets the list of orders.
        /// </summary>
        /// <param name="status">An optional status ID of the order. If not specified or <c>null</c>, all orders are returned.</param>
        /// <returns>List of orders.</returns>
        Task<IEnumerable<OrderSummary>> GetOrdersAsync(Guid? status = null);
        
        /// <summary>
        /// Gets and order by it's ID.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <returns>The order matching the specified ID, or <c>null</c> if the order was not found.</returns>
        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

        /// <summary>
        /// Gets the list of all order statuses.
        /// </summary>
        /// <returns>List of all order statuses.</returns>
        Task<IEnumerable<OrderStatus>> GetOrderStatusesAsync();

        /// <summary>
        /// Updates the status of an order.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="newOrderStatusId">The ID of the new status.</param>
        /// <exception cref="ArgumentException">
        /// When an invalid order ID or status ID was specified.
        /// </exception>
        Task UpdateOrderStatus(Guid orderId, Guid newOrderStatusId);

        /// <summary>
        /// Creates a new order.
        /// </summary>
        /// <param name="orderCreateRequest">Order create request.</param>
        /// <returns>The ID of the newly created order.</returns>
        Task<Guid> CreateOrder(OrderCreateRequest orderCreateRequest);

        /// <summary>
        /// Calculates profit by month for all completed orders.
        /// </summary>
        /// <returns>A list of orders with a calculated monthly profit.</returns>
        Task<IEnumerable<OrderWithMonthlyProfit>> CalculateProfitsByMonth();
    }
}
