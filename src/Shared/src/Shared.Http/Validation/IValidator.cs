using Shared.Http.Response.Structures;

namespace Shared.Http.Validation
{
    /// <summary>
    /// Provides a method to validate a value.
    /// </summary>
    /// <typeparam name="T">Value type to validate</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Validates a given value.
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <returns>Returns a list of errors if there any. Otherwise it returns an empty list.</returns>
        List<ErrorItem> Validate(T value);
    }
}
