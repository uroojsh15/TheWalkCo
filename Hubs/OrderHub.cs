using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TheWalkco.Hubs
{
    [Authorize(Policy = "AdminAccess")]
    public class OrderHub : Hub
    {
        private const string AdminsGroup = "admins";

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;

            Console.WriteLine($"User connected: {user?.Identity?.Name}");
            Console.WriteLine($"Is authenticated: {user?.Identity?.IsAuthenticated}");

            var email =
                user?.FindFirst(ClaimTypes.Email)?.Value ??
                user?.Identity?.Name ??
                string.Empty;

            Console.WriteLine($"Email: {email}");

            // ✅ Claim / email-based admin check
            if (email.Contains("admin", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Adding admin to group: {Context.ConnectionId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var email =
                Context.User?.FindFirst(ClaimTypes.Email)?.Value ??
                Context.User?.Identity?.Name ??
                string.Empty;

            if (email.Contains("admin", StringComparison.OrdinalIgnoreCase))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminsGroup);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Admin-only notification
        public async Task NotifyAdmins(string message)
        {
            await Clients.Group(AdminsGroup)
                .SendAsync("ReceiveNotification", message);
        }
    }
}
