# DotNetMinimalAPI

This is a project created for learning and practicing **.NET Minimal API** development. The main objective is to get hands-on experience with the .NET Core architecture, routing configuration, dependency injection, and the lightweight approach to building APIs.

## What is a Minimal API?

Minimal API is a lightweight pattern for building HTTP APIs introduced in .NET 6. Compared to the traditional Controllers-based architecture, Minimal API significantly reduces boilerplate code and complex configuration. Developers can use very little code, often within a single file (`Program.cs`), to quickly define routing and request handling logic.

### When is it typically used?

- **Microservices Architecture**: Perfect for building single-purpose, small, and lightweight independent microservices.
- **Rapid Prototyping**: When you need to quickly validate concepts or build API endpoints in the early stages of development, it saves time by skipping the full Controller architecture setup.
- **Single-Purpose APIs**: Such as an API solely responsible for receiving Webhooks, handling simple Serverless Functions, or providing a basic query interface.
- **Performance-Sensitive Lightweight Apps**: Because of its fast startup time and low resource consumption, it's suitable for deployment in resource-constrained environments (like edge computing or containers).
- **Learning for Beginners**: By removing complex directory structures and the concept of controllers, beginners can focus more directly on understanding the fundamentals of HTTP requests and routing.

## Project Environment
- Framework: .NET 10.0
- Language: C#

## How to Create a Minimal API Project

If you want to create a brand new Minimal API project from scratch, you can use the template provided by the .NET CLI. Open your terminal and run the following commands:

```bash
# Create a new web project named MyMinimalApi (which uses the Minimal API template by default)
dotnet new web -n MyMinimalApi

# Navigate into the project directory
cd MyMinimalApi
```

> [!NOTE]
> Starting from .NET 6, `dotnet new web` defaults to generating a Minimal API project structure (using top-level statements without a Controllers folder). If you need a traditional Web API project with controllers, you should use `dotnet new webapi` instead.

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
