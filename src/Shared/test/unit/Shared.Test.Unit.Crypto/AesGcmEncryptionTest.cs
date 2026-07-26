using Shared.Crypto;
using Shared.Crypto.Exceptions;
using Shared.TestGenerators;

namespace Shared.Test.Unit.Crypto
{
    public class AesGcmEncryptionTest
    {
        private readonly string _validKey = "PtdqlngVTeZD5fMzyicfhQdq8Re8H9paNdgK8M7yDc4=";
        private readonly string _invalidKey = "tjGi11WLJxAEcclhs7q+R3JI7PQxVdvdtNKp+HfTQv/Y5tyTP9x+qCjoG6JhTSEp22P2VL4WwA4a8lBznyrtLA==";

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Encrypt_WhenValueIsInvalid_ShouldThrowException(string? value)
        {
            // Act
            var exception = Record.Exception(() =>
            {
                AesGcmEncryption.Encrypt(value!, 1, _validKey);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Encrypt_WhenKeyIsInvalid_ShouldThrowException(string? key)
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();

            // Act
            var exception = Record.Exception(() =>
            {
                AesGcmEncryption.Encrypt(value, 1, key!);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void Encrypt_WhenKeyLengthIsWrong_ShouldThrowException()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();

            // Act
            var exception = Record.Exception(() =>
            {
                AesGcmEncryption.Encrypt(value, 1, _invalidKey);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidEncryptionKeySizeException>(exception);
        }

        [Fact]
        public void Encrypt_WhenParametersAreValid_ShouldEncryptValue()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();

            // Act
            var valueEncrypted = AesGcmEncryption.Encrypt(value, 1, _validKey);

            // Assert
            Assert.NotNull(valueEncrypted);
            Assert.NotEmpty(valueEncrypted);
            Assert.NotEqual(value, valueEncrypted);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Decrypt_WhenEncrpytedValueIsInvalid_ShouldThrowException(string? value)
        {
            // Act
            var exception = Record.Exception(() =>
            {
                AesGcmEncryption.Decrypt(value!, (version) => _validKey, out var version);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void Decrypt_WhenKeyLengthIsWrong_ShouldThrowException()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var valueEncrypted = AesGcmEncryption.Encrypt(value, 1, _validKey);

            // Act
            var exception = Record.Exception(() =>
            {
                AesGcmEncryption.Decrypt(valueEncrypted, (version) => _invalidKey, out var version);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<InvalidEncryptionKeySizeException>(exception);
        }

        [Fact]
        public void Decrypt_WhenParametersAreValid_ShouldDecryptValue()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var valueEncrypted = AesGcmEncryption.Encrypt(value, 1, _validKey);

            // Act
            var valueDecrypted = AesGcmEncryption.Decrypt(valueEncrypted, (version) =>
            {
                return version switch
                {
                    1 => _validKey,
                    2 => _invalidKey,
                    _ => throw new ArgumentException("The version dos not exist")
                };
            }, out var version);

            // Assert
            Assert.NotNull(valueDecrypted);
            Assert.NotEmpty(valueDecrypted);
            Assert.Equal(value, valueDecrypted);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Compare_WhenEncryptedValueIsInvalid_ShouldThrowException(string? valueEncrypted)
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();

            // Act
            var exception = Record.Exception(() =>
            {
                AesGcmEncryption.Compare(valueEncrypted!, value, (version) => _validKey, out var version);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Compare_WhenPlainValueIsInvalid_ShouldThrowException(string? valuePlain)
        {
            // Arrange
            var valueEncrypted = StringGenerator.GeneratePrintableAscii();

            // Act
            var exception = Record.Exception(() =>
            {
                AesGcmEncryption.Compare(valueEncrypted, valuePlain!, (version) => _validKey, out var version);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void Compare_WhenValuesAreDifferent_ShouldReturnFalse()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var valueEncrypted = AesGcmEncryption.Encrypt(value, 1, _validKey);

            // Act
            var result = AesGcmEncryption.Compare(valueEncrypted, value + "a", (version) => _validKey, out var version);

            // Assert
            Assert.False(result, "The comparison returned true.");
        }

        [Fact]
        public void Compare_WhenValuesAreDifferent_ShouldReturnTrue()
        {
            // Arrange
            var value = StringGenerator.GeneratePrintableAscii();
            var valueEncrypted = AesGcmEncryption.Encrypt(value, 1, _validKey);

            // Act
            var result = AesGcmEncryption.Compare(valueEncrypted, value, (version) => _validKey, out var version);

            // Assert
            Assert.True(result, "The comparison returned false.");
        }
    }
}
