namespace ClinicPos.Api.Exceptions;

public class DuplicatePhoneException : Exception
{
    public DuplicatePhoneException()
        : base("Phone number already exists for this tenant")
    {
    }
}
