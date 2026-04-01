using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqKit;

using Microsoft.EntityFrameworkCore;

using Order.Model;

namespace Order.Data
{
    /// <summary>
    /// Represents the Order repository.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderContext _orderContext;

        public OrderRepository(OrderContext orderContext)
        {
            _orderContext = orderContext;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync(Guid? statusId = null)
        {
            var orders = await _orderContext.Order
                .Where(x => statusId == null || x.StatusId == statusId.Value.ToByteArray())
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Select(x => new OrderSummary
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    ItemCount = x.Items.Count,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    CreatedDate = x.CreatedDate
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }

        /// <inheritdoc/>
        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var orderIdBytes = orderId.ToByteArray();

            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .Select(x => new OrderDetail
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    CreatedDate = x.CreatedDate,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    Items = x.Items.Select(i => new Model.OrderItem
                    {
                        Id = new Guid(i.Id),
                        OrderId = new Guid(i.OrderId),
                        ServiceId = new Guid(i.ServiceId),
                        ServiceName = i.Service.Name,
                        ProductId = new Guid(i.ProductId),
                        ProductName = i.Product.Name,
                        UnitCost = i.Product.UnitCost,
                        UnitPrice = i.Product.UnitPrice,
                        TotalCost = i.Product.UnitCost * i.Quantity.Value,
                        TotalPrice = i.Product.UnitPrice * i.Quantity.Value,
                        Quantity = i.Quantity.Value
                    })
                }).SingleOrDefaultAsync();
            
            return order;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Model.OrderStatus>> GetOrderStatuses()
        {
            var statuses = await _orderContext.OrderStatus
                .Select(x => new OrderStatus
                {
                    Id = new Guid(x.Id),
                    Name = x.Name
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return statuses;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<OrderProduct>> GetProductsByIds(IEnumerable<Guid> productIds)
        {
            var productIdsBytes = productIds.Select(x => x.ToByteArray()).ToList();

            var query = _orderContext.OrderProduct as IQueryable<Entities.OrderProduct>;

            if (_orderContext.Database.IsInMemory())
            {
                query = query.Where(x => productIdsBytes.Any(y => x.Id.SequenceEqual(y)));
            }
            else
            {
                // This is not ideal, but EF Core 8 cannot translate the Contains method when using with byte arrays, so we need to build the predicate manually.
                var predicate = PredicateBuilder.New<Entities.OrderProduct>();

                foreach (var productIdByte in productIdsBytes)
                {
                    predicate = predicate.Or(x => x.Id == productIdByte);
                }

                query = query.Where(predicate);
            }

            var products = await query
                .Select(x => new OrderProduct
                {
                    Id = new Guid(x.Id),
                    Name = x.Name,
                    ServiceId = new Guid(x.ServiceId),
                    UnitCost = x.UnitCost,
                    UnitPrice = x.UnitPrice
                })
                .ToListAsync();

            return products;
        }

        /// <inheritdoc/>
        public async Task<Guid> CreateOrder(Guid resellerId, Guid customerId)
        {
            var resellerIdBytes = resellerId.ToByteArray();
            var customerIdBytes = customerId.ToByteArray();
            var orderId = Guid.NewGuid();

            // We will assume that the Created status always exists, but this should be handled in real production code.
            var createdStatus = await _orderContext.OrderStatus.FirstAsync(x => x.Name == "Created");

            var order = new Entities.Order
            {
                Id = orderId.ToByteArray(),
                ResellerId = resellerIdBytes,
                CustomerId = customerIdBytes,
                StatusId = createdStatus.Id,
                CreatedDate = DateTime.UtcNow, // Assuming we are working with UTC dates. DateTimeOffset would be safer! Also, we should use something like IDateTimeProvider and inject it, for better testability in unit tests.
                Items = []
            };

            await _orderContext.AddAsync(order);
            await _orderContext.SaveChangesAsync();

            return orderId;
        }

        /// <inheritdoc/>
        public async Task AddProductToOrder(Guid orderId, Guid productId, int quantity)
        {
            var orderIdBytes = orderId.ToByteArray();
            var productIdBytes = productId.ToByteArray();

            var product = await _orderContext.OrderProduct
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(productIdBytes) : x.Id == productIdBytes)
                .SingleOrDefaultAsync();

            var orderItem = new Entities.OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ProductId = product.Id,
                ServiceId = product.ServiceId,
                Quantity = quantity
            };

            await _orderContext.AddAsync(orderItem);
            await _orderContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateOrderStatus(Guid orderId, Guid orderStatusId)
        {
            var orderIdBytes = orderId.ToByteArray();

            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .SingleOrDefaultAsync();

            order.StatusId = orderStatusId.ToByteArray();

            await _orderContext.SaveChangesAsync();
        }
    }
}
