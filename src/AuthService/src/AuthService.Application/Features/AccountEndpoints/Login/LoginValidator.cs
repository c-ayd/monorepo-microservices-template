using AuthService.Application.Validations.Shared;
using Shared.Http.Response.Structures;
using Shared.Http.Validation;

namespace AuthService.Application.Features.AccountEndpoints.Login
{
    public class LoginValidator : IValidator<LoginRequest>
    {
        public List<ErrorItem> Validate(LoginRequest value)
        {
            var errors = new List<ErrorItem>();

            if (value.Email == null)
            {
                errors.Add(new ErrorItem()
                {
                    Code = "auth_email_required",
                    Message = "The email address is required."
                });
            }
            else
            {
                errors.AddRange(new EmailValidator().Validate(value.Email));
            }
            
            if (value.Password == null)
            {
                errors.Add(new ErrorItem()
                {
                    Code = "auth_password_required",
                    Message = "The password is required."
                });
            }

            return errors;
        }
    }
}
