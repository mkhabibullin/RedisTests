﻿using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisFTS.Core
{
    internal class RedisClient
    {
        protected IConfiguration Configuration { get; }

        protected ConnectionMultiplexer connection;

        public RedisClient(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task Connect()
        {
            var redisConfig = Configuration.GetValue<string>("RedisConfiguration");
            await this.Connect(redisConfig);
        }

        public async Task Connect(string config)
        {
            // Trying to connect to muster host
            try
            {
                connection = await ConnectionMultiplexer.ConnectAsync(config);
                return;
            }
            catch
            {
                Console.WriteLine($"Redis client failed to connect to a master host: {config}");
            }


            var sentinelOptions = ConfigurationOptions.Parse(config);
            sentinelOptions.CommandMap = CommandMap.Sentinel;
            sentinelOptions.TieBreaker = ""; // any master

            var sentinelConnection = ConnectionMultiplexer.Connect(sentinelOptions);

            var endpoints = sentinelConnection.GetEndPoints();
            if (endpoints != null && endpoints.Length > 0)
            {
                IServer server = null;
                foreach (var endpoint in endpoints)
                {
                    server = sentinelConnection.GetServer(endpoint);
                    if (server != null && server.IsConnected)
                    {
                        break;
                    }
                }

                if (server == null)
                {
                    throw new Exception($"Failed to get sentinel connection server by enpoint: {endpoints}");
                }

                var primary = server.SentinelGetMasterAddressByName("mymaster");
                var options = ConfigurationOptions.Parse(primary.ToString());
                options.DefaultDatabase = sentinelOptions.DefaultDatabase;
                connection = await ConnectionMultiplexer.ConnectAsync(options);

                Console.WriteLine($"Redis client connected to: {connection.Configuration}");
            }
            else
            {
                throw new Exception($"Failed to get sentinel connection endpoints");
            }
        }

        public async Task<IDictionary<string, string>> GetAll()
        {
            var result = new Dictionary<string, string>();

            if (connection == null) return result;

            var connections = connection.GetEndPoints();
            foreach (var conn in connections)
            {
                var server = connection.GetServer(conn);
                //if (server.IsReplica) continue; // TODO: x
                var database = connection.GetDatabase();

                var keys = server.Keys(pattern: $"*")
                    .ToArray();

                var values = new List<RedisValue>();
                foreach (var part in keys.Slice(1000))
                {
                    values.AddRange(await database.StringGetAsync(part));
                }

                ////var values = new List<RedisValue>();
                //var values = await Task.WhenAll(keys.Select(key => database.StringGetAsync(key))); // TODO: x
                ////values.AddRange(database.StringGet(keys));

                for (var index = 0; index < keys.Length; index++)
                {
                    var value = values[index];
                    var key = keys[index].ToString();
                    result.Add(
                        key,
                        value);
                }
            }

            return result;
        }
    }
}
