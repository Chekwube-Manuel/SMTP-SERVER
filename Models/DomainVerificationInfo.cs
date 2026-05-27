namespace EmailServer.Models
{
    public class DomainVerificationInfo
    {
        public string Domain { get; set; } = string.Empty;
        public bool Verified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string VerificationRecordName { get; set; } = string.Empty;
        public string VerificationRecordValue { get; set; } = string.Empty;
        public string SpfRecordName { get; set; } = string.Empty;
        public string SpfRecordValue { get; set; } = string.Empty;
        public string DkimRecordName { get; set; } = string.Empty;
        public string DkimRecordValue { get; set; } = string.Empty;
    }
}
