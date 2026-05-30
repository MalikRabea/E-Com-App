using Microsoft.AspNetCore.SignalR;

namespace E_Com.API.Hubs
{
    public class OrderTrackingHub : Hub
    {
        public async Task JoinOrder(string orderId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

        public async Task LeaveOrder(string orderId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
