# DotNetMinimalAPI

This is a project created for learning and practicing **.NET Minimal API** development. The main objective is to get hands-on experience with the .NET Core architecture, routing configuration, dependency injection, and the lightweight approach to building APIs.

## Project Environment
- Framework: .NET 10.0
- Language: C#

## How to Start the Project

> [!NOTE]
> **Notice for Mac Users**: Microsoft has retired Visual Studio for Mac (support for Visual Studio 2022 for Mac has ended). Therefore, for developing modern .NET projects on a Mac, the officially recommended and mainstream approach is to use the **.NET CLI** (command line) paired with VS Code or Rider.

Please ensure you have the appropriate version of the [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.

### 1. Start using the Terminal (Command Line)

Navigate your terminal to the project directory and run the following command:

**Normal Run Mode:**
```bash
dotnet run
```

**Developer Mode (Hot Reload):**
(Recommended for development. When you modify and save the code, the API server will automatically reload without requiring a manual restart.)
```bash
dotnet watch run
```

### 2. Start using an IDE

- **VS Code**: 
  1. Open VS Code in the project root directory (`code .`).
  2. If the C# / C# Dev Kit extension is installed, press `F5` to start debugging.
- **Visual Studio (Windows) / Rider**:
  > [!TIP]
  > **Cross-Platform & IDE Note**: This project was initially created using the .NET CLI, so it may not contain a `.sln` (solution) file by default. If you are using Visual Studio on Windows, simply open the `.csproj` project file. After loading the project, Visual Studio will automatically prompt you to generate and save a `.sln` file upon your first save.

  1. Open the `.csproj` project file directly.
  2. Click the "Run" button at the top or press `F5` to start executing.

## Testing the API

Once the project successfully starts, the API will run on `http://localhost:5074` by default (based on the `Properties/launchSettings.json` configuration).

You can open a browser or use an API testing tool (such as Postman, Thunder Client, curl) to send a request for testing. For example:
```
http://localhost:5074/todos
```
