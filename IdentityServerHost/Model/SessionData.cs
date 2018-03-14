using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceReference;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IdentityServerHost.Model
{
    public class SessionData
    {
        public string SessionId { get; set; }
        public List<RedeemableItem> RedeemableItems { get; set; }
        public List<PurchaseOrderItem> PurchaseItems { get; set; }
    }
}
