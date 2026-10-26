namespace InkassoAkten.Application.Interfaces;

public interface IOperatorContext
{
    bool IsOperator { get; }
    void SetOperator(bool value);
    bool BypassWrites { get; set; }
    bool RequireOperatorAuth { get; }
    bool CanWrite { get; }
}
