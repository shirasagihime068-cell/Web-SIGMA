using System;

namespace PTK.SIGMA.Utilities.Base;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
