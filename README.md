# Position Repository for EventStore
To store current position and retrieve last used position on EventStore. This nuget is written in C# .Net Standard and it's compatible with both .Net Framwework and Core projects. It can be used whenever you need to retrieve the last processing position or save the current. The destination of the position is in an EventStore stream. The stream will contains only 1 event that is your position. 
The repository save the position every second to avoid overloading EventStore with unnecessary operations in case you are processing at speed. You can change the default setting passing the interval when you build the PositionRepository

You can reference this project using Nuget from within Visual Studio
```
PM> Install-Package EventStore.Tools.PositionRepository
```
From command line using Visual Studio Code or other editors
```
> dotnet add package EventStore.Tools.PositionRepository
```  
## Set the current position
```
positionRepo.Set(resolvedEvent.OriginalPosition);
```  
## Get the last processed position
```
connection.SubscribeToAllFrom(positionRepo.Get(), ...other params)
```