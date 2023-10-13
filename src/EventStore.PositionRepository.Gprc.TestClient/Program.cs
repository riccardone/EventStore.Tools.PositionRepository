using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using EventStore.Client;
using EventStore.PositionRepository.Gprc;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

namespace EventStore.PositionRepository.TestClient.Gprc
{
    class Program
    {
        private static readonly Logger Log = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

        static void Main(string[] args)
        {
            ConfigureLogging();
            var esConnection = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));
            var positionRepo = new PositionRepository.Gprc.PositionRepository($"position-test", "PositionUpdated",
                esConnection, new NLogLogger(LogManager.GetCurrentClassLogger()));
            Log.Info($"Initial position is {positionRepo.Get()}");
            var position = esConnection.AppendToStreamAsync("tests", StreamState.Any,
                    new List<EventData>
                        {new EventData(Uuid.FromGuid(Guid.NewGuid()), "EventTested", Encoding.ASCII.GetBytes("abc"), null, "application/json")})
                .Result.LogPosition;
            positionRepo.Set(position);

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
