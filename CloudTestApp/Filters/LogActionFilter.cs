using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace CloudTestApp.Filters
{
    public class LogActionFilter : IActionFilter
    {
        private readonly ILogger<LogActionFilter> _logger;
        public LogActionFilter(ILogger<LogActionFilter> logger) => _logger = logger;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var action = context.ActionDescriptor.DisplayName;
            _logger.LogInformation("ENTER {Action} args={Args}", action, context.ActionArguments);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var action = context.ActionDescriptor.DisplayName;
            if (context.Exception != null)
            {
                _logger.LogError(context.Exception, "EXCEPTION {Action}", action);
            }
            _logger.LogInformation("EXIT {Action} status={StatusCode}",
                action, context.HttpContext.Response?.StatusCode);
        }
    }
}
