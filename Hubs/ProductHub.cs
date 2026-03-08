using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TheWalkco.Hubs
{
    public class ProductHub : Hub
    {
        // This hub will be used to broadcast new product notifications
        public async Task SendNewProductNotification(string productName)
        {
            await Clients.All.SendAsync("ReceiveNewProduct", productName);
        }
    }
}
