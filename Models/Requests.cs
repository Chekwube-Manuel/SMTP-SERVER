namespace EmailServer.Models
{
    public record TenantCreateRequest(string Name, string Domain, int MaxMessagesPerDay);
    public record EmailSendRequest(string From, List<string> To, string Subject, string Body);
}
