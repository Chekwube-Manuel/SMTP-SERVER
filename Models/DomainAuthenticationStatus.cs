namespace EmailServer.Models
{
    public class DomainAuthenticationStatus
    {
        public string Domain { get; set; } = string.Empty;
        public bool DomainVerified { get; set; }
        public DateTime? DomainVerifiedAt { get; set; }
        public DnsRecordCheck Verification { get; set; } = new();
        public DnsRecordCheck Spf { get; set; } = new();
        public DnsRecordCheck Dkim { get; set; } = new();
        public DnsRecordCheck Dmarc { get; set; } = new();
        public bool ReadyToSendDirect { get; set; }
    }

    public class DnsRecordCheck
    {
        public string Name { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public bool Found { get; set; }
        public List<string> ActualValues { get; set; } = [];
        public string? Error { get; set; }
    }
}
