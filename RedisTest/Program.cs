using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTest
{
    class Program
    {
        private static SocketManager mgr;
        private static ConnectionMultiplexer connection;
        private static IDatabase db;

        static void Main(string[] args)
        {
            Connect("10.8.0.1:26379,10.8.0.2:26379,10.8.0.3:26379,defaultDatabase=1", out connection);

            //foreach (var i in Enumerable.Range(1, 10001))
            int idx2 = 0;
            while(true)
            {
                try
                {
                    idx2++;
                    Thread.Sleep(3000);
                    db = connection.GetDatabase();
                    Console.WriteLine(idx2.ToString() + "...");
                    db.StringSet(idx2.ToString(), idx2.ToString());
                    Console.WriteLine(idx2.ToString() + " !!!");
                }
                catch(RedisConnectionException exc)
                {
                    Console.WriteLine("Reconnecting...");
                    try
                    {
                        Connect("10.8.0.1:26379,10.8.0.2:26379,10.8.0.3:26379,defaultDatabase=1", out connection);
                    }
                    catch
                    {
                        Console.WriteLine("Not Connected");
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Unhandled exc: {exc.Message}");
                }
            }

            //db.StringSet("a1", "1");
            //db.StringSet("b1", "2");
            //db.StringSet("c1", "3");
            //db.StringSet("d1", "4");
            //db.StringSet("e1", "5");
            //db.StringSet("f1", "6");
            //db.StringSet("g1", "7");
            //db.StringSet("h1", "8");

            var allKeys = GetAll().Result;
            int idx = 0;
            foreach(var k in allKeys)
            {
                Console.WriteLine($"{++idx}: {k}");
            }

            //var options = ConfigurationOptions.Parse();
            //connection = ConnectionMultiplexer.Connect(options);
            //db = connection.GetDatabase();

            //var keys = db.SearchKeys("*");

            //Print("a");
            //Print("b");
            //Print("c");
            //Print("d");
            //Print("e");
            //Print("f");
            //Print("g");
            //Print("h");

            Console.WriteLine("Done!");
            Console.Read();
        }

        public static void Print(string key)
        {
            var v = db.StringGet(key);

            Console.WriteLine(v);
        }

        public async static Task<IDictionary<string, string>> GetAll()
        {
            var result = new Dictionary<string, string>();

            if (connection == null) return result;

            var connections = connection.GetEndPoints();
            foreach (var conn in connections)
            {
                var server = connection.GetServer(conn);
                //if (server.IsReplica) continue; // TODO: x
                var database = connection.GetDatabase();

                var keys = server
                    .Keys(pattern: $"*", pageSize: 1000)
                    .ToArray();

                var values = new List<RedisValue>();
                foreach (var part in keys.Slice(1000))
                {
                    values.AddRange(database.StringGet(part));
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

        static void Connect(string config, out ConnectionMultiplexer connection)
        {
            // Trying to connect to muster host
            try
            {
                connection = ConnectionMultiplexer.Connect(config);
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
                //options.ConfigCheckSeconds = 1;
                connection = ConnectionMultiplexer.Connect(options);

                Console.WriteLine($"Redis client connected to: {connection.Configuration}");
            }
            else
            {
                throw new Exception($"Failed to get sentinel connection endpoints");
            }
        }
    }

    internal static class Extensions
    {
        public static TimeSpan TrimMilliseconds(this TimeSpan timespan)
        {
            return new TimeSpan(timespan.Hours, timespan.Minutes, timespan.Seconds);
        }

        public static TimeSpan TrimSeconds(this TimeSpan timespan)
        {
            return new TimeSpan(timespan.Hours, timespan.Minutes, 0);
        }

        public static T[][] Slice<T>(this T[] source, int chunkSize)
        {
            int i = 0;
            return
                source
                .GroupBy(s => i++ / chunkSize)
                .Select(g => g.ToArray()).ToArray();
        }
    }
}
