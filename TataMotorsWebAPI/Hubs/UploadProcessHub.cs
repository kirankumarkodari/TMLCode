using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace TataMotorsWebAPI.Hubs
{
    [HubName("ProcessUpdateHub")]
    public class UploadProcessHub : Hub
    {      
        private void Send(int StatusCode, string message)
        {
            Clients.All.updateProcessStatus(StatusCode, message);
        }
        
        //Server method to be called from client side
        public void sendDataOverwrite(int responseCode)
        {
            Debug.WriteLine("Overwrite response code is : " + responseCode);
        }
    }
}