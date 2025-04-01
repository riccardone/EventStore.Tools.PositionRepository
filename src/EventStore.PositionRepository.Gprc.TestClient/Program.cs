using System.Text;
using EventStore.Client;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace EventStore.PositionRepository.Gprc.TestClient;

class Program
{
    private static readonly ILogger Log = new NLogLogger(LogManager.GetCurrentClassLogger());

    static void Main(string[] args)
    {
        ConfigureLogging();
        var esConnection = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));
        var positionRepo = new EventStore.PositionRepository.Gprc.PositionRepository($"EngagementsPositionStreamNameLocal", "PositionSaved",
            esConnection, new NLogLogger(LogManager.GetCurrentClassLogger()), 5000);
        Log.Info($"Initial position is {positionRepo.Get()}");
        var position = new Position(3000, 3000);
        positionRepo.Set(position);

        Thread.Sleep(5500);
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