using AuthService.Domain.SeedWork;

namespace AuthService.Domain.Entities
{
    public class Account : EntityBase<Guid>, ISoftDelete, IUpdateable
    {
        public string? Email { get; set; }
        public string? NewEmail { get; set; }
        public string? PasswordHashed { get; set; }

        public string? PreferredLanguage { get; set; }

        public bool IsEmailVerified { get; set; }
        public bool IsBanned { get; set; }
        public bool IsLocked { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTimeOffset? UnlockDate { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }

        // Relationships
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<Token> Tokens { get; set; } = new List<Token>();

        // Reserved for EF Core
        private Account() : base()
        {
        }

        public Account(
            string email,
            string passwordHashed)
            : base(Guid.CreateVersion7())
        {
            Email = email;
            PasswordHashed = passwordHashed;
        }

        public void SoftDelete()
        {
            Email = null;
            IsEmailVerified = false;
            NewEmail = null;
            PasswordHashed = null;
            PreferredLanguage = null;
            FailedLoginAttempts = 0;
            IsLocked = false;
            UnlockDate = null;
            UpdatedDate = null;
        }
    }
}
