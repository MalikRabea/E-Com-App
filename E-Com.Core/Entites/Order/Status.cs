using StackExchange.Redis;

namespace E_Com.Core.Entites.Order
{
    public enum Status
    {
        Pending,
        PaymentReceived,
        PaymentFailed
    }
}