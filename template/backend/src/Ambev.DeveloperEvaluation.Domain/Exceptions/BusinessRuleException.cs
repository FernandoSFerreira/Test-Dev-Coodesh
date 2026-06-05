namespace Ambev.DeveloperEvaluation.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// Used for domain-specific validations such as quantity limits on sale items.
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
