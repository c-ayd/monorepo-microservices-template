namespace Common.Http.Exceptions
{
    public class ValidatorNotFoundException : Exception
    {
        public ValidatorNotFoundException(string validatorTypeName)
            : base($"{validatorTypeName} is not registered in DI container.")
        {
        }
    }
}
