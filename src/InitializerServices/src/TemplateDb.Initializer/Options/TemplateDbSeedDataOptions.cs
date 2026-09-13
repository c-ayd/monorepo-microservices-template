namespace TemplateDb.Initializer.Options
{
    public class TemplateDbSeedDataOptions
    {
        public required List<TemplateDetails> Email { get; set; }

        public class TemplateDetails
        {
            public required string TemplateId { get; set; }
            public required string Language { get; set; }
            public required string Subject { get; set; }
            public required string Body { get; set; }
            public required bool IsBodyHtml { get; set; }
        }
    }
}
