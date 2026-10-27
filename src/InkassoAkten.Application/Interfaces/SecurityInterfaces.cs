namespace InkassoAkten.Application.Interfaces;

public interface IOperatorContext
{
    bool IsOperator { get; }
    bool RequireOperatorAuth { get; }
    bool CanWrite { get; }
    bool PublicReadOnly { get; }
}
