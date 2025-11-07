using Microsoft.AspNetCore.Mvc;
using InvestmentApi.Models;

namespace InvestmentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvestmentController : ControllerBase
    {
        
        private static List<Security> _securities = new List<Security>
        {
            new Security { Id = 1, Ticker = "AAPL", Name = "Apple Inc.", CurrentPrice = 170.0m },
            new Security { Id = 2, Ticker = "GAZP", Name = "Газпром", CurrentPrice = 160.0m }
        };

        private static List<InvestmentOperation> _operations = new List<InvestmentOperation>();

        
        [HttpGet("securities")]
        public ActionResult<IEnumerable<Security>> GetSecurities()
        {
            return Ok(_securities);
        }

        
        [HttpGet("operations")]
        public ActionResult<IEnumerable<object>> GetOperations()
        {
            
            var operationsWithDetails = _operations.Select(op =>
            {
                var security = _securities.FirstOrDefault(s => s.Id == op.SecurityId);
                return new
                {
                    op.Id,
                    op.SecurityId,
                    SecurityTicker = security?.Ticker,
                    SecurityName = security?.Name,
                    op.Quantity,
                    op.PurchasePricePerShare,
                    op.Commission,
                    op.TotalCost,
                    op.TargetBuyPrice,
                    HasTrigger = op.TargetBuyPrice.HasValue
                };
            });
            return Ok(operationsWithDetails);
        }

        
        [HttpPost("calculate")]
        public ActionResult<object> CalculateOperation([FromBody] InvestmentOperation operation)
        {
            
            if (operation.Quantity <= 0 || operation.PurchasePricePerShare <= 0 || operation.Commission < 0)
            {
                return BadRequest("Некорректные данные для расчёта.");
            }

            
            var result = new
            {
                TotalCost = operation.TotalCost,
                HasTrigger = operation.TargetBuyPrice.HasValue,
                TriggerMessage = operation.TargetBuyPrice.HasValue ?
                    $"Триггер установлен на цену: ${operation.TargetBuyPrice}" :
                    "Триггер не установлен"
            };

            return Ok(result);
        }

        
        [HttpPost("operation")]
        public IActionResult AddOperation([FromBody] InvestmentOperation operation)
        {
            
            var securityExists = _securities.Any(s => s.Id == operation.SecurityId);
            if (!securityExists)
            {
                return BadRequest("Ценная бумага с указанным ID не найдена.");
            }

            
            operation.Id = _operations.Any() ? _operations.Max(op => op.Id) + 1 : 1;
            _operations.Add(operation);

            
            string triggerMessage = "";
            if (operation.TargetBuyPrice.HasValue)
            {
                var security = _securities.First(s => s.Id == operation.SecurityId);
                if (security.CurrentPrice <= operation.TargetBuyPrice.Value)
                {
                    triggerMessage = $" ВНИМАНИЕ: Триггер сработал сразу. Текущая цена ({security.CurrentPrice}) ниже целевой ({operation.TargetBuyPrice}). !";
                }
            }

            
            var response = new
            {
                Operation = operation,
                Message = "Операция успешно добавлена!" + triggerMessage,
                TriggerActivated = !string.IsNullOrEmpty(triggerMessage)
            };

            return CreatedAtAction(nameof(GetOperations), new { id = operation.Id }, response);
        }


        [HttpGet("check-triggers")]
        public ActionResult<IEnumerable<object>> CheckAllTriggers()
        {
            var activatedTriggers = new List<object>();

            foreach (var operation in _operations.Where(op => op.TargetBuyPrice.HasValue))
            {
                var security = _securities.FirstOrDefault(s => s.Id == operation.SecurityId);
                if (security != null && security.CurrentPrice <= operation.TargetBuyPrice.Value)
                {
                    activatedTriggers.Add(new
                    {
                        OperationId = operation.Id,
                        SecurityTicker = security.Ticker,
                        SecurityName = security.Name,
                        CurrentPrice = security.CurrentPrice,
                        TargetPrice = operation.TargetBuyPrice.Value,
                        Message = $"СРАБОТАЛ ТРИГГЕР! Пора докупать {security.Ticker}. Текущая цена ({security.CurrentPrice}) ниже или равна целевой ({operation.TargetBuyPrice})."
                    });
                }
            }

            return Ok(activatedTriggers);
        }

        [HttpGet("active-triggers")]
        public ActionResult<IEnumerable<object>> GetActiveTriggers()
        {
            var activeTriggers = new List<object>();

            foreach (var operation in _operations.Where(op => op.TargetBuyPrice.HasValue))
            {
                var security = _securities.FirstOrDefault(s => s.Id == operation.SecurityId);
                if (security != null)
                {
                    
                    bool isTriggerActive = security.CurrentPrice > operation.TargetBuyPrice.Value;

                    if (isTriggerActive)
                    {
                        activeTriggers.Add(new
                        {
                            OperationId = operation.Id,
                            SecurityTicker = security.Ticker,
                            SecurityName = security.Name,
                            CurrentPrice = security.CurrentPrice,
                            TargetPrice = operation.TargetBuyPrice.Value,
                            Message = $"Ждем падения {security.Ticker} до ${operation.TargetBuyPrice.Value}"
                        });
                    }
                }
            }

            return Ok(activeTriggers);
        }
    }
}