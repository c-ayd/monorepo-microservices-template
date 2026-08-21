using System.Net.Mail;
using AuthService.Application.Validations.Constraints;
using Shared.Http.Response.Structures;
using Shared.Http.Validation;

namespace AuthService.Application.Validations.Shared
{
    public class EmailValidator : IValidator<string>
    {
        private const string _invalidCode = "auth_error_email_invalid";
        private const string _maxLengthCode = "auth_error_email_max_length";

        public List<ErrorItem> Validate(string value)
        {
            var errors = new List<ErrorItem>();

            if (!MailAddress.TryCreate(value, out _))
            {
                errors.Add(new ErrorItem(
                    Code: _invalidCode,
                    Message: $"The email address is invalid."
                ));
                
                return errors;
            }

            if (value.Length > AccountConstraints.EmailMaxLength)
            {
                errors.Add(new ErrorItem(
                    Code: _maxLengthCode,
                    Message: $"The email address cannot be longer than {AccountConstraints.EmailMaxLength} characters.",
                    Metadata: new
                    {
                        MaxLength = AccountConstraints.EmailMaxLength
                    }
                ));

                return errors;
            }

            return errors;
        }
    }
}
