using System.Net.Mail;
using AuthService.Application.Validations.Constraints;

namespace AuthService.Application.Validations.Shared
{
    public class EmailValidator : IValidator<string>
    {
        private const string _invalidCode = "auth_error_email_invalid";
        private const string _maxLengthCode = "auth_error_email_max_length";

        public List<ValidationError> Validate(string value)
        {
            var errors = new List<ValidationError>();

            if (!MailAddress.TryCreate(value, out _))
            {
                errors.Add(new ValidationError(
                    Message: $"The email address is invalid.",
                    Code: _invalidCode
                ));
                
                return errors;
            }

            if (value.Length > AccountConstraints.EmailMaxLength)
            {
                errors.Add(new ValidationError(
                    Message: $"The email address cannot be longer than {AccountConstraints.EmailMaxLength} characters.",
                    Code: _maxLengthCode,
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
