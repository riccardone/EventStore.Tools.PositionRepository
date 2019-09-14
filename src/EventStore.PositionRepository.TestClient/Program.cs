using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace EventStore.PositionRepository.TestClient
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            ConfigureLogging();
            var connSettings = ConnectionSettings.Create().SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
            var connBuilder = new ConnectionBuilder(new Uri("tcp://localhost:1113"), connSettings, "testRepository");
            var positionRepo = new PositionRepository($"Position-{connBuilder.ConnectionName}", "PositionUpdated",
                connBuilder, new NLogLogger(LogManager.GetCurrentClassLogger()));
            positionRepo.Start().Wait();
            Log.Info($"Initial position is {positionRepo.Get()}");
            using (var connection = connBuilder.Build())
            {
                connection.ConnectAsync().Wait();
                var position = connection.AppendToStreamAsync("positionRepo-tests", ExpectedVersion.Any,
                        new List<EventData> { new EventData(Guid.NewGuid(), "EventTested", true, Encoding.ASCII.GetBytes("abc"), null) })
                    .Result.LogPosition;
                positionRepo.Set(position);
            }
            Thread.Sleep(1500);
            Log.Info($"Event saved. Current position is {positionRepo.Get()}");
            Log.Info("Press enter to exit the program");
            Console.ReadLine();
        }

        private static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget("target1")
            {
                Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception}"
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget); // all to console
            LogManager.Configuration = config;
        }
    }
}
