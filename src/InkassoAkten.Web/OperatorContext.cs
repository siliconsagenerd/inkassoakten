using InkassoAkten.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace InkassoAkten.Web;

public class OperatorContext : IOperatorContext
{
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _config;
    private bool _isOperator;

    public OperatorContext(IHttpContextAccessor http, IConfiguration config)
    {
        _http = http;
        _config = config;
        RequireOperatorAuth = _config.GetValue<bool>("Security:RequireOperatorAuth");

        var user = _http.HttpContext?.User;
        if (user is not null)
        {
            _isOperator = user.Identity?.IsAuthenticated == true && user.IsInRole("Operator");
        }
    }

    public bool IsOperator => _isOperator;
    public void SetOperator(bool value) => _isOperator = value;
    public bool BypassWrites { get; set; }
    public bool RequireOperatorAuth { get; }
    public bool CanWrite => BypassWrites || !RequireOperatorAuth || IsOperator;
}
