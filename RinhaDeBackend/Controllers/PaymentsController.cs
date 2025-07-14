using Microsoft.AspNetCore.Mvc;
using RinhaDeBackend.Models;
using RinhaDeBackend.Service;

namespace RinhaDeBackend.Controllers
{
    [ApiController]
    [Route("")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("payments")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var success = await _paymentService.ProcessPaymentAsync(request);

                if (success)
                {
                    return Ok(new { message = "Payment processed successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Payment processing failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment {CorrelationId}", request.CorrelationId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("payments-summary")]
        public async Task<IActionResult> GetPaymentsSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var summary = await _paymentService.GetPaymentsSummaryAsync(from, to);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments summary");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
