using Models;

namespace Interfaces;

public interface IPersonMatchService
{
    string? FindExactNhsNumber(PersonSpecification input);
}

