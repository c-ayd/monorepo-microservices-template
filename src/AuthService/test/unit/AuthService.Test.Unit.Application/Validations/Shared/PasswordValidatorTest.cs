using System.Reflection;
using AuthService.Application.Validations;
using AuthService.Application.Validations.Constraints;
using AuthService.Application.Validations.Shared;
using Cayd.Test.Generators;

namespace AuthService.Test.Unit.Application.Validations.Shared
{
    public class PasswordValidatorTest
    {
        private readonly PasswordValidator _validator = new PasswordValidator();

        [Fact]
        public void Validate_WhenPasswordLengthIsWrong_ShouldReturnError()
        {
            // Arrange
            var passwordLengthCode = (string)typeof(PasswordValidator).GetField("_lengthCode", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

            var passwords = new List<string>()
            {
                PasswordGenerator.GenerateWithCustomRules(AccountConstraints.PasswordMinLength - 1, true, true, true, true),
                PasswordGenerator.GenerateWithCustomRules(AccountConstraints.PasswordMaxLength + 1, true, true, true, true)
            };

            // Act
            var errors = new List<ValidationError>();
            foreach (var password in passwords)
            {
                errors.AddRange(_validator.Validate(password));
            }

            // Assert
            Assert.NotEmpty(errors);

            var lengthErrors = errors.Where(e => e.Code == passwordLengthCode).ToList();
            Assert.Equal(passwords.Count, lengthErrors.Count);
        }

        [Fact]
        public void Validate_WhenFormatIsWrong_ShouldReturnError()
        {
            // Arrange
            var passwordFormatCode = (string)typeof(PasswordValidator).GetField("_formatCode", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

            var passwords = new List<string>()
            {
                "Abcdefghi-",     // No digit
                "ABCDEFGH1-",     // No lowercase
                "abcdefgh1-",     // No uppercase
                "Abcdefgh1j",     // No special character
            };

            // Act
            var errors = new List<ValidationError>();
            foreach (var password in passwords)
            {
                errors.AddRange(_validator.Validate(password));
            }

            // Assert
            Assert.NotEmpty(errors);

            var formatErrors = errors.Where(e => e.Code == passwordFormatCode).ToList();
            Assert.Equal(passwords.Count, formatErrors.Count);
        }

        [Fact]
        public void Validate_WhenPasswordIsInCorrectFormat_ShouldReturnNoError()
        {
            // Arrange
            var password = PasswordGenerator.GenerateWithCustomRules(10, true, true, true, true);

            // Act
            var errors = _validator.Validate(password);

            // Assert
            Assert.Empty(errors);
        }
    }
}
