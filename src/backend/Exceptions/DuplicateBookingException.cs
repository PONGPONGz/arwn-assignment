namespace ClinicPos.Api.Exceptions;

public class DuplicateBookingException : Exception
{
    public DuplicateBookingException()
        : base("An appointment already exists for this patient at the same branch and time")
    {
    }
}
