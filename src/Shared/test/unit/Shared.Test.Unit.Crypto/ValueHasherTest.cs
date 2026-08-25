using System.Security.Cryptography;
using Shared.Crypto;
using Shared.Crypto.Exceptions;
using Shared.Crypto.Options;
using Shared.Test.Generators;

namespace Shared.Test.Unit.Crypto
{
    public class ValueHasherTest
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Hash_WhenValueIsInvalid_ShouldThrowException(string? value)
        {
            // Arrange
            var hashOptions = new HashOptions(SHA256.Create, 16);

            // Act
            var exception = Record.Exception(() =>
            {
                ValueHasher.Hash(value!, 1, () => hashOptions);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void Hash_WhenParametersAreValid_ShouldHashValue()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var hashOptions = new HashOptions(SHA256.Create, 16);

            // Act
            var valueHashed = ValueHasher.Hash(value, 1, () => hashOptions);

            // Assert
            Assert.NotNull(valueHashed);
            Assert.NotEmpty(valueHashed);
            Assert.NotEqual(value, valueHashed);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Verify_WhenHashedValueIsInvalid_ShouldThrowException(string? valueHashed)
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var hashOptions = new HashOptions(SHA256.Create, 16);

            // Act
            var exception = Record.Exception(() =>
            {
                ValueHasher.Verify(valueHashed!, value, (version) => hashOptions, out var version);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Verify_WhenPlainValueIsInvalid_ShouldThrowException(string? valuePlain)
        {
            // Arrange
            var valueHashed = StringGenerator.GeneratePrintableAscii();
            var hashOptions = new HashOptions(SHA256.Create, 16);

            // Act
            var exception = Record.Exception(() =>
            {
                ValueHasher.Verify(valueHashed, valuePlain!, (version) => hashOptions, out var version);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void Verify_WhenNumberOfBytesDoesNotMatchVersion_ShouldThrowException()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var hashOptions = new HashOptions(SHA256.Create, 16);
            var valueHashed = ValueHasher.Hash(value, 1, () => hashOptions);

            // Act
            var exception = Record.Exception(() =>
            {
                ValueHasher.Verify(valueHashed, value, (version) => new HashOptions(SHA256.Create, 32), out var version);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<HashLengthMismatchException>(exception);
        }

        [Fact]
        public void Verify_WhenValuesAreDifferent_ShouldReturnFalse()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var hashOptions = new HashOptions(SHA256.Create, 16);
            var valueHashed = ValueHasher.Hash(value, 1, () => hashOptions);

            // Act
            var result = ValueHasher.Verify(valueHashed, value + "a", (version) => hashOptions, out var version);

            // Assert
            Assert.False(result, "The verification returned true.");
        }

        [Fact]
        public void Verify_WhenValuesAreSame_ShouldReturnTrue()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var hashOptions = new HashOptions(SHA256.Create, 16);
            var valueHashed = ValueHasher.Hash(value, 1, () => hashOptions);

            // Act
            var result = ValueHasher.Verify(valueHashed, value, (version) => hashOptions, out var version);

            // Assert
            Assert.True(result, "The verification returned false.");
        }
    }
}
