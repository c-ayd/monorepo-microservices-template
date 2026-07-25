namespace AuthService.Application.Validations.Constraints
{
    public static class AccountConstraints
    {
        public const int EmailMaxLength = 255;

        public const int PasswordMinLength = 10;
        public const int PasswordMaxLength = 100;
        public const string PasswordSpecialCharacters = "!@#$%^&*()_+-=[]{};':\",./<>?\\|";
    }
}
