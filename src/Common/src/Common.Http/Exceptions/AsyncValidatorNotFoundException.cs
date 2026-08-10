namespace Common.Http.Exceptions
{
    public class AsyncValidatorNotFoundException : Exception
    {
        public AsyncValidatorNotFoundException(string validatorTypeName)
            : base($"{validatorTypeName} is not registered in DI container.")
        {
        }
    }
}
