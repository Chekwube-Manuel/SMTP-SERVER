namespace EmailServer.Models
{
    public class DkimOptions
    {
        public bool Enabled { get; set; } = true;
        public bool RequireVerifiedDomain { get; set; } = true;
        public int SignatureExpirationHours { get; set; } = 24;
        public List<string> HeadersToSign { get; set; } =
        [
            "From",
            "To",
            "Subject",
            "Date",
            "Message-Id",
            "MIME-Version",
            "Content-Type"
        ];
    }
}
