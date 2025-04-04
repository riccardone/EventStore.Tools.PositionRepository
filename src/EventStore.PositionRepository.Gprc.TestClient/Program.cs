﻿using System.Text;
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
        //SYNC TEST
        ConfigureLogging();
        var esConnection = new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false"));
        var positionRepo = new EventStore.PositionRepository.Gprc.PositionRepository($"EngagementsPositionStreamNameLocal1", "PositionSaved",
            esConnection, new NLogLogger(LogManager.GetCurrentClassLogger()));

        Log.Info("Position set to start");

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