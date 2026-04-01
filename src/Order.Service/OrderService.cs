using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service
{
    /// <summary>
    /// Represents the Order service.
    /// </summary>
    public class OrderService : IOrderService
    {
        private const string ERROR_INVALID_ORDER_ID = "An invalid order ID was specified.";
        private const string ERROR_INVALID_STATUS_ID = "An invalid status ID was specified.";
        private const string ERROR_INVALID_PRODUCT_ID = "An invalid product ID was specified.";
        private const string ERROR_INVALID_QUANTITY = "All quantities must be larger than zero.";

        private readonly IOrderRepository _orderRepository;


        /// <summary>
        /// Initializes a new instance of the <see cref="OrderService"/> class.
        /// </summary>
        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync(Guid? statusId = null)
        {
            var orders = await _orderRepository.GetOrdersAsync(statusId);
            return orders;
        }

        /// <inheritdoc/>
        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            return order;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<OrderStatus>> GetOrderStatusesAsync()
        {
            var statuses = await _orderRepository.GetOrderStatuses();
            return statuses;
        }

        /// <inheritdoc/>
        public async Task UpdateOrderStatus(Guid orderId, Guid newOrderStatusId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId)
                ?? throw new ArgumentException(ERROR_INVALID_ORDER_ID, nameof(orderId));

            var statuses = await _orderRepository.GetOrderStatuses();
            
            if (!statuses.Any(x => x.Id == newOrderStatusId))
            {
                throw new ArgumentException(ERROR_INVALID_STATUS_ID, nameof(newOrderStatusId));
            }

            await _orderRepository.UpdateOrderStatus(orderId, newOrderStatusId);
        }

        /// <inheritdoc/>
        public async Task<Guid> CreateOrder(OrderCreateRequest orderCreateRequest)
        {
            if (orderCreateRequest.Products.Any(x => x.Quantity < 1))
            {
                throw new ArgumentException(ERROR_INVALID_QUANTITY, nameof(orderCreateRequest));
            }

            var products = (await _orderRepository.GetProductsByIds(orderCreateRequest.Products.Select(x => x.ProductId))).ToList();

            if (orderCreateRequest.Products.Count() != products.Count)
            {
                throw new ArgumentException(ERROR_INVALID_PRODUCT_ID, nameof(orderCreateRequest));
            }

            // In an ideal world, we would validate that the reseller and the customer really exists. But we don't have those database tables in this project.
            var orderId = await _orderRepository.CreateOrder(orderCreateRequest.ResellerId, orderCreateRequest.CustomerId);

            // Making DB request in a loop is not ideal. However, there should not be too much products in an order.
            foreach (var product in orderCreateRequest.Products)
            {
                await _orderRepository.AddProductToOrder(orderId, product.ProductId, product.Quantity);
            }

            return orderId;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<OrderWithMonthlyProfit>> CalculateProfitsByMonth()
        {
            // We should probably create a method to get a single status by name.
            var statuses = await _orderRepository.GetOrderStatuses();

            // We will assume that the completed status exists, but this should be handled in production code.
            var completedStatus = statuses.FirstOrDefault(x => x.Name.Equals("completed", StringComparison.OrdinalIgnoreCase));

            var completedOrders = await GetOrdersAsync(completedStatus.Id);

            return completedOrders.Select(x => new OrderWithMonthlyProfit
            {
                Id = x.Id,
                MonthlyProfit = x.TotalPrice - x.TotalCost
            });
        }
    }
}
