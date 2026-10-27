using InkassoAkten.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace InkassoAkten.Web;

/// <summary>
/// Internal initializer surface. Not part of the public IOperatorContext so ordinary
/// web components/services cannot grant themselves operator status or enable a
/// write bypass. Used only by the trusted login flow and the explicit seed/test scope.
/// </summary>
internal interface IOperatorContextInitializer
{
    void SetOperator(bool value);
    bool BypassWrites { get; set; }
}

public class OperatorContext : IOperatorContext, IOperatorContextInitializer
{
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _config;
    private bool _isOperator;

    public OperatorContext(IHttpContextAccessor http, IConfiguration config)
    {
        _http = http;
        _config = config;
        // Fail-closed: missing configuration defaults to requiring operator auth.
        RequireOperatorAuth = _config.GetValue("Security:RequireOperatorAuth", true);
        PublicReadOnly = _config.GetValue("Security:PublicReadOnly", false);

        var user = _http.HttpContext?.User;
        if (user is not null)
        {
            _isOperator = user.Identity?.IsAuthenticated == true && user.IsInRole("Operator");
        }
    }

    public bool IsOperator => _isOperator;
    void IOperatorContextInitializer.SetOperator(bool value) => _isOperator = value;
    public bool BypassWrites { get; set; }
    public bool RequireOperatorAuth { get; }
    public bool PublicReadOnly { get; }
    public bool CanWrite => !PublicReadOnly && (BypassWrites || !RequireOperatorAuth || IsOperator);
}
