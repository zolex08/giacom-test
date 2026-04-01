using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Order.Model;
using Order.Service;

namespace OrderService.WebAPI.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                return Ok(order);
            }
            else
            {
                return NotFound();
            }
        }


        [HttpGet("status/{status}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var allStatuses = await _orderService.GetOrderStatusesAsync();
            var orderStatus = allStatuses
                .FirstOrDefault(x => x.Name.Equals(status, StringComparison.OrdinalIgnoreCase));

            if (orderStatus == null)
            {
                return BadRequest();
            }

            var orders = await _orderService.GetOrdersAsync(orderStatus.Id);
            return Ok(orders);
        }


        [HttpPost("update/{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(Guid orderId, OrderUpdateRequest updateRequest)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var allStatuses = await _orderService.GetOrderStatusesAsync();
            var orderStatus = allStatuses
                .FirstOrDefault(x => x.Name.Equals(updateRequest.Status, StringComparison.OrdinalIgnoreCase));

            if (orderStatus == null)
            {
                return BadRequest();
            }

            await _orderService.UpdateOrderStatus(orderId, orderStatus.Id);

            return Ok();
        }


        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(OrderCreateRequest createRequest)
        {
            try
            {
                await _orderService.CreateOrder(createRequest);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpGet("profit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Profit()
        {
            var orders = await _orderService.CalculateProfitsByMonth();

            return Ok(orders);
        }
    }
}
