﻿using System;

namespace EventStore.PositionRepository.Gprc;

public class SimpleConsoleLogger : ILogger
{
    private readonly string _moduleName;

    public SimpleConsoleLogger(string moduleName)
    {
        _moduleName = moduleName;
    }

    public void Info(string message)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}");
    }

    public void Warn(string message)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}");
    }

    public void Warn(string message, Exception ex)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}");
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{ex.GetBaseException().Message}");
    }

    public void Warn(string message, string warnMessage)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}|{warnMessage}");
    }

    public void Error(string message)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}");
    }

    public void Error(string message, Exception ex)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}");
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{ex.GetBaseException().Message}");
    }

    public void Error(string message, string errorMessage)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}|{errorMessage}");
    }

    public void Debug(string message)
    {
        Console.WriteLine($"{DateTime.Now:F}|{_moduleName}|{message}");
    }
}