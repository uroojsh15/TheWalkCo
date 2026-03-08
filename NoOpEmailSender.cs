namespace TheWalkco
{
    public class NoOpEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Do nothing
            return Task.CompletedTask;
        }
    }
}
