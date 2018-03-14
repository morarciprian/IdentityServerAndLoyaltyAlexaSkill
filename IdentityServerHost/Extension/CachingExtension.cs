using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServerHost.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IdentityServerHost.Extension
{

    public static class CachingExtensions
    {
        //public static async Task SetObjectAsync(this IDistributedCache cache, string key, SessionData value)
        //{
        //    await cache.SetStringAsync(key, JsonConvert.SerializeObject(value));
        //}

        //public static async Task GetObjectAsync(this IDistributedCache cache, string key)
        //{
        //    var value = await cache.GetStringAsync(key);
        //    return value == null ? default(SessionData) : JsonConvert.DeserializeObject(value);
        //}
    }
}
