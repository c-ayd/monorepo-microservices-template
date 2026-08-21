using AuthService.Application.Validations.Constraints;
using Shared.Http.Response.Structures;
using Shared.Http.Validation;

namespace AuthService.Application.Validations.Shared
{
    public class PasswordValidator : IValidator<string>
    {
        private const string _lengthCode = "auth_error_password_length";
        private const string _formatCode = "auth_error_password_format";

        public List<ErrorItem> Validate(string value)
        {
            var errors = new List<ErrorItem>();

            if (value.Length < AccountConstraints.PasswordMinLength ||
                value.Length > AccountConstraints.PasswordMaxLength)
            {
                errors.Add(new ErrorItem(
                    Code: _lengthCode,
                    Message: $"The password must be between {AccountConstraints.PasswordMinLength} and {AccountConstraints.PasswordMaxLength} characters.",
                    Metadata: new
                    {
                        MinLength = AccountConstraints.PasswordMinLength,
                        MaxLength = AccountConstraints.PasswordMaxLength
                    }
                ));

                return errors;
            }

            if (!value.Any(c => char.IsLower(c)) ||
                !value.Any(c => char.IsUpper(c)) ||
                !value.Any(c => char.IsDigit(c)) ||
                !value.Any(c => AccountConstraints.PasswordSpecialCharacters.Contains(c)))
            {
                errors.Add(new ErrorItem(
                    Code: _formatCode,
                    Message: "The password must constain at least one lowercase, one uppercase, one digit and one special character.",
                    Metadata: new
                    {
                        SpecialCharacters = AccountConstraints.PasswordSpecialCharacters
                    }
                ));

                return errors;
            }

            return errors;
        }
    }
}
