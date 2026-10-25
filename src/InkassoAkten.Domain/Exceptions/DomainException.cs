namespace InkassoAkten.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class InvalidTransitionException : DomainException
{
    public InvalidTransitionException(string message) : base(message) { }
}
