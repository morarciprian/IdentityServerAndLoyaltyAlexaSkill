using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServerHost.Interface;
using MongoDB.Driver;

namespace IdentityServerHost.Store
{
    public class CustomClientStore : IdentityServer4.Stores.IClientStore
    {
        protected IRepository _dbRepository;

        public CustomClientStore(IRepository repository)
        {
            _dbRepository = repository;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            return Task.Run(() =>
            {
                var client = _dbRepository.Single<Client>(c => c.ClientId == clientId);
                return client;
            });
        }
    }
}
