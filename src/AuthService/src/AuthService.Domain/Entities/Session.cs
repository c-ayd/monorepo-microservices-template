#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

using System.Net;
using AuthService.Domain.SeedWork;

namespace AuthService.Domain.Entities
{
    public class Session : EntityBase<Guid>, IUpdateable
    {
        public string RefreshTokenHashed { get; set; }
        public DateTimeOffset ExpirationDate { get; set; }
        public IPAddress? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        // Relationships
        public Guid AccountId { get; set; }
        public Account Account { get; set; }

        // Reserved for EF Core
        private Session() : base()
        {
        }

        public Session(
            Guid accountId,
            string refreshTokenHashed,
            DateTimeOffset expirationDate,
            IPAddress? ipAddress = null,
            string? deviceInfo = null)
            : base(Guid.CreateVersion7())
        {
            AccountId = accountId;

            RefreshTokenHashed = refreshTokenHashed;
            ExpirationDate = expirationDate;
            IpAddress = ipAddress;
            DeviceInfo = deviceInfo;
        }
    }
}
