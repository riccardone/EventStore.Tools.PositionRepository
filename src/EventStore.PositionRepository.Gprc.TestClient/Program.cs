using System.Text;
using KurrentDB.Client;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace EventStore.PositionRepository.Gprc.TestClient;

class Program
{
    private static readonly ILogger Log = new NLogLogger(LogManager.GetCurrentClassLogger());

    static void Main(string[] args)
    {
        //SYNC TEST
        ConfigureLogging();
        var esConnection = new KurrentDBClient(KurrentDBClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));
        var positionRepo = new PositionRepository($"EngagementsPositionStreamNameLocal1", "PositionSaved",
            esConnection, new NLogLogger(LogManager.GetCurrentClassLogger()), 0);

        positionRepo.Set(Position.Start);
        Log.Info($"Position set to start {positionRepo.Get()}");

        Position lastSavedPosition = Position.Start;
        Task.Run(() => esConnection.SubscribeToAllAsync(FromAll.Start, async (arg1, arg2, ct) =>
        {
            if (lastSavedPosition != Position.Start) return;
            lastSavedPosition = (Position)arg2.OriginalPosition;
            positionRepo.Set(lastSavedPosition);
            Log.Info($"Position set to {lastSavedPosition}");
        }));

        Thread.Sleep(1500);

        var updatedPosition = positionRepo.Get();
        Log.Info($"Updated position is {updatedPosition}");
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