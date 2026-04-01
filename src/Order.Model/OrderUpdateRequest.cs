namespace Order.Model
{
    /// <summary>
    /// A request for the Update order endpoint.
    /// </summary>
    public class OrderUpdateRequest
    {
        /// <summary>
        /// The new status of the order.
        /// </summary>
        public required string Status { get; set; }
    }
}
