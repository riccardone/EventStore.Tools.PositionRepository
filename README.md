# EventStore.PositionRepository
To store current position and retrieve last used position on EventStore. This nuget is written in C# .Net Standard and it's compatible with both .Net Framwework and Core projects. It can be used whenever you need to retrieve the last processing position or save the current. The destination of the position is in an EventStore stream. The stream will contains only 1 event that is your position. 

You can reference this project using Nuget from within Visual Studio
```
PM> Install-Package EventStore.Tools.PositionRepository
```
From command line using Visual Studio Code or other editors
```
> dotnet add package EventStore.Tools.PositionRepository
```  

# Example use  
```c#
class Program
    {
        static void Main(string[] args)
        {   
            // I always prefer to use a custom Builder type to let me build and rebuild the connection within a service without passing all the params
            var connBuilder = new ConnectionBuilder(new Uri("tcp://localhost:1113"), ConnectionSettings.Create().SetDefaultUserCredentials(new UserCredentials("admin", "changeit")), "testRepository");
            var positionRepo = new PositionRepository($"Position-{connBuilder.ConnectionName}", "PositionUpdated", connBuilder);
            positionRepo.Start().Wait();
            // Run this program multiple times to retrieve the last processed position
            Console.WriteLine($"Initial position is {positionRepo.Get()}");
            using (var connection = conn.Build())
            {
                connection.ConnectAsync().Wait();
                var position = connection.AppendToStreamAsync("positionRepo-tests", ExpectedVersion.Any,
                        new List<EventData> { new EventData(Guid.NewGuid(), "EventTested", true, Encoding.ASCII.GetBytes("abc"), null) })
                    .Result.LogPosition;
                positionRepo.Set(position);
            }
            // The repository save the position every second to avoid overloading EventStore with unnecessary operations in case you are processing at speed. You can change the default setting passing the interval when you build the PositionRepository
            Thread.Sleep(1500);
            Console.WriteLine($"Event saved. Current position is {positionRepo.Get()}");
            Console.WriteLine("Press enter to exit the program");
            Console.ReadLine();
        }
```