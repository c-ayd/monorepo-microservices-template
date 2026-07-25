using System.Reflection;
using AuthService.Application.Validations.Constraints;
using AuthService.Application.Validations.Shared;
using Cayd.Test.Generators;

namespace AuthService.Test.Unit.Application.Validations.Shared
{
    public class EmailValidatorTest
    {
        private readonly EmailValidator _validator = new EmailValidator();

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("abc@")]
        public void Validate_WhenEmailIsInvalid_ShouldReturnError(string email)
        {
            // Arrange
            var invalidEmailCode = (string)typeof(EmailValidator).GetField("_invalidCode", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

            // Act
            var errors = _validator.Validate(email);

            // Assert
            Assert.NotEmpty(errors);

            var error = errors.FirstOrDefault(e => e.Code == invalidEmailCode);
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_WhenEmailIsLong_ShouldReturnError()
        {
            // Arrange
            var maxLengthCode = (string)typeof(EmailValidator).GetField("_maxLengthCode", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            
            var email = EmailGenerator.GenerateCustomLength(AccountConstraints.EmailMaxLength + 1, 3, 3);

            // Act
            var errors = _validator.Validate(email);

            // Assert
            Assert.NotEmpty(errors);

            var error = errors.FirstOrDefault(e => e.Code == maxLengthCode);
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_WhenEmailIsValid_ShouldReturnNoError()
        {
            // Arrange
            var email = EmailGenerator.Generate();

            // Act
            var errors = _validator.Validate(email);

            // Assert
            Assert.Empty(errors);
        }
    }
}
