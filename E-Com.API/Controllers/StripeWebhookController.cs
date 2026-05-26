using E_Com.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(IPaymentService paymentService, ILogger<StripeWebhookController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var webhookSecret = Environment.GetEnvironmentVariable("StripSetting__webhookSecret");
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError("Stripe webhook secret is not configured.");
                return StatusCode(500);
            }

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );
            }
            catch (StripeException ex)
            {
                _logger.LogWarning("Stripe webhook signature verification failed: {Message}", ex.Message);
                return BadRequest();
            }

            switch (stripeEvent.Type)
            {
                case EventTypes.PaymentIntentSucceeded:
                    var succeededIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (succeededIntent != null)
                    {
                        _logger.LogInformation("PaymentIntent succeeded: {Id}", succeededIntent.Id);
                        await _paymentService.UpdateOrderSuccess(succeededIntent.Id);
                    }
                    break;

                case EventTypes.PaymentIntentPaymentFailed:
                    var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (failedIntent != null)
                    {
                        _logger.LogInformation("PaymentIntent failed: {Id}", failedIntent.Id);
                        await _paymentService.UpdateOrderFaild(failedIntent.Id);
                    }
                    break;

                default:
                    _logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
    }
}
