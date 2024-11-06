using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

namespace EventStore.PositionRepository.TestClient
{
    class Program
    {
        private static readonly Logger Log = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

        static void Main(string[] args)
        {
            ConfigureLogging();
            var positionRepo = new PositionRepository($"position-test", "PositionUpdated",
                BuildEsConnection, new NLogLogger(LogManager.GetCurrentClassLogger()), 3000, 10);
            positionRepo.Start().Wait();
            Log.Info($"Initial position is {positionRepo.Get()}");
            using (var connection = BuildEsConnection())
            {
                connection.ConnectAsync().Wait();
                for (int i = 0; i < 100; i++)
                {
                    var position = connection.AppendToStreamAsync("tests", ExpectedVersion.Any,
                            new List<EventData>
                                {new EventData(Guid.NewGuid(), "EventTested", true, Encoding.ASCII.GetBytes($"pos {i}"), null)})
                        .Result.LogPosition;
                    positionRepo.Set(position);
                    Thread.Sleep(1000);
                }
            }
            Thread.Sleep(1500);
            Log.Info($"Event saved. Current position is {positionRepo.Get()}");
            Log.Info("Press enter to exit the program");
            Console.ReadLine();

            IEventStoreConnection BuildEsConnection()
            {
                return EventStoreConnection.Create(
                    ConnectionSettings.Create().SetDefaultUserCredentials(new UserCredentials("admin", "changeit")),
                    new Uri("tcp://admin:changeit@localhost:1113"));
            }
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
