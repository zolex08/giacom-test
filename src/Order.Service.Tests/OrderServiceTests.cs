using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;
using Order.Data;
using Order.Data.Entities;

using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests
{
    public class OrderServiceTests
    {
        private const string ORDER_STATUS_CREATED_ID = "89db2ded-8f34-4ecf-aebf-6c04453b4d5c";
        private const string ORDER_STATUS_INPROGRESS_ID = "ee7c0d92-20e3-407b-b79e-a94e26f56a2c";
        private const string ORDER_STATUS_COMPLETED_ID = "c059ee1d-6bb2-422a-a323-0c13858b9e22";

        private IOrderService _orderService;
        private IOrderRepository _orderRepository;
        private OrderContext _orderContext;
        private DbConnection _connection;

        private readonly byte[] _orderServiceEmailId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderProductEmailId = Guid.NewGuid().ToByteArray();


        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<OrderContext>()
                .UseSqlite(CreateInMemoryDatabase())
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .Options;

            _connection = RelationalOptionsExtension.Extract(options).Connection;

            _orderContext = new OrderContext(options);
            _orderContext.Database.EnsureDeleted();
            _orderContext.Database.EnsureCreated();

            _orderRepository = new OrderRepository(_orderContext);
            _orderService = new OrderService(_orderRepository);

            await AddReferenceDataAsync(_orderContext);
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
            _orderContext.Dispose();
        }


        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            return connection;
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsCorrectNumberOfOrders()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, ORDER_STATUS_CREATED_ID, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, ORDER_STATUS_CREATED_ID, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            Assert.AreEqual(3, orders.Count());
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsOrdersWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, ORDER_STATUS_CREATED_ID, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, ORDER_STATUS_CREATED_ID, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            var order1 = orders.SingleOrDefault(x => x.Id == orderId1);
            var order2 = orders.SingleOrDefault(x => x.Id == orderId2);
            var order3 = orders.SingleOrDefault(x => x.Id == orderId3);

            Assert.AreEqual(0.8m, order1.TotalCost);
            Assert.AreEqual(0.9m, order1.TotalPrice);

            Assert.AreEqual(1.6m, order2.TotalCost);
            Assert.AreEqual(1.8m, order2.TotalPrice);

            Assert.AreEqual(2.4m, order3.TotalCost);
            Assert.AreEqual(2.7m, order3.TotalPrice);
        }

        [TestCase(ORDER_STATUS_CREATED_ID)]
        [TestCase(ORDER_STATUS_INPROGRESS_ID)]
        public async Task GetOrdersAsync_StatusSpecified_FiltersOrders(string orderStatusId)
        {
            // Arrange
            var orderStatusGuid = Guid.Parse(orderStatusId);

            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, ORDER_STATUS_INPROGRESS_ID, 1);

            // Act
            var orders = await _orderService.GetOrdersAsync(orderStatusGuid);

            // Assert
            Assert.AreEqual(1, orders.Count());
            Assert.AreEqual(orderStatusGuid, orders.First().StatusId);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrder()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(orderId1, order.Id);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrderItemCount()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1, order.Items.Count());
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsOrderWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 2);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1.6m, order.TotalCost);
            Assert.AreEqual(1.8m, order.TotalPrice);
        }

        [Test]
        public async Task GetOrderStatusesAsync_ReturnsAllStatuses()
        {
            // Act
            var statuses = await _orderService.GetOrderStatusesAsync();

            // Assert
            CollectionAssert.AreEquivalent(new[] { "Created", "InProgress", "Completed" }, statuses.Select(x => x.Name));
        }

        [Test]
        public async Task CreateOrder_CreatesOrder()
        {
            // Arrange
            var request = new Model.OrderCreateRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Products = []
            };

            // Act
            var orderId = await _orderService.CreateOrder(request);

            // Assert
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            
            Assert.IsNotNull(order);
            Assert.AreEqual(request.ResellerId, order.ResellerId);
            Assert.AreEqual(request.CustomerId, order.CustomerId);
        }

        [Test]
        public async Task CreateOrder_InvalidProduct_ThrowsException()
        {
            // Arrange
            var request = new Model.OrderCreateRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Products =
                [
                    new() { ProductId = Guid.Empty, Quantity = 1}
                ]
            };

            // Act
            var exception = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrder(request));
            StringAssert.StartsWith("An invalid product ID was specified.", exception.Message);
        }


        [TestCase(0)]
        [TestCase(-1)]
        public async Task CreateOrder_InvalidQuantity_ThrowsException(int quantity)
        {
            // Arrange
            var request = new Model.OrderCreateRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Products =
                [
                    new() { ProductId = new Guid(_orderProductEmailId), Quantity = quantity}
                ]
            };

            // Act
            var exception = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrder(request));
            StringAssert.StartsWith("All quantities must be larger than zero.", exception.Message);
        }

        [Test]
        public async Task CreateOrder_ValidProducts_AddsProducts()
        {
            // Arrange
            var request = new Model.OrderCreateRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Products =
                [
                    new() { ProductId = new Guid(_orderProductEmailId), Quantity = 3}
                ]
            };

            // Act
            var orderId = await _orderService.CreateOrder(request);

            // Assert
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            Assert.IsNotNull(order);
            Assert.AreEqual(1, order.Items.Count());
            Assert.AreEqual(_orderServiceEmailId, order.Items.First().ServiceId.ToByteArray());
            Assert.AreEqual(_orderProductEmailId, order.Items.First().ProductId.ToByteArray());
            Assert.AreEqual(3, order.Items.First().Quantity);
        }

        [Test]
        public async Task UpdateOrderStatus_InvalidOrderId_ThrowsException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() => _orderService.UpdateOrderStatus(Guid.Empty, Guid.Parse(ORDER_STATUS_INPROGRESS_ID)));
            StringAssert.StartsWith("An invalid order ID was specified.", exception.Message);
        }

        [Test]
        public async Task UpdateOrderStatus_InvalidStatusId_ThrowsException()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 0);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() => _orderService.UpdateOrderStatus(orderId1, Guid.Empty));
            StringAssert.StartsWith("An invalid status ID was specified.", exception.Message);
        }

        [Test]
        public async Task UpdateOrderStatus_ValidParameters_UpdatesOrder()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            var order = await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, 0);

            // Act
            await _orderService.UpdateOrderStatus(orderId1, Guid.Parse(ORDER_STATUS_INPROGRESS_ID));

            // Assert
            Assert.AreEqual(Guid.Parse(ORDER_STATUS_INPROGRESS_ID).ToByteArray(), order.StatusId);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public async Task CalculateProfitsByMonth_CalculatesProfit(int itemCount)
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, ORDER_STATUS_CREATED_ID, itemCount);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, ORDER_STATUS_COMPLETED_ID, itemCount);

            // Act
            var orders = await _orderService.CalculateProfitsByMonth();

            // Assert
            Assert.AreEqual(1, orders.Count());
            Assert.AreEqual(orderId2, orders.First().Id);
            Assert.AreEqual(itemCount * 0.1m, orders.First().MonthlyProfit);
        }

        private async Task<Data.Entities.Order> AddOrder(Guid orderId, string statusId, int quantity)
        {
            var orderIdBytes = orderId.ToByteArray();

            var order = new Data.Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = Guid.NewGuid().ToByteArray(),
                CustomerId = Guid.NewGuid().ToByteArray(),
                CreatedDate = DateTime.Now,
                StatusId = Guid.Parse(statusId).ToByteArray(),
            };

            _orderContext.Order.Add(order);

            _orderContext.OrderItem.Add(new OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ServiceId = _orderServiceEmailId,
                ProductId = _orderProductEmailId,
                Quantity = quantity
            });

            await _orderContext.SaveChangesAsync();

            return order;
        }

        private async Task AddReferenceDataAsync(OrderContext orderContext)
        {
            orderContext.OrderStatus.AddRange(
            [
                new OrderStatus
                {
                    Id = Guid.Parse(ORDER_STATUS_CREATED_ID).ToByteArray(),
                    Name = "Created",
                },
                new OrderStatus
                {
                    Id = Guid.Parse(ORDER_STATUS_INPROGRESS_ID).ToByteArray(),
                    Name = "InProgress",
                },
                new OrderStatus
                {
                    Id = Guid.Parse(ORDER_STATUS_COMPLETED_ID).ToByteArray(),
                    Name = "Completed",
                }
            ]);

            orderContext.OrderService.Add(new Data.Entities.OrderService
            {
                Id = _orderServiceEmailId,
                Name = "Email"
            });

            orderContext.OrderProduct.Add(new OrderProduct
            {
                Id = _orderProductEmailId,
                Name = "100GB Mailbox",
                UnitCost = 0.8m,
                UnitPrice = 0.9m,
                ServiceId = _orderServiceEmailId
            });

            await orderContext.SaveChangesAsync();
        }
    }
}
