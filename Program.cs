using System.Text.Json.Serialization;
using DotNetMinimalAPI.CompiledModels;
using DotNetMinimalAPI.Data;
using DotNetMinimalAPI.Models;
using DotNetMinimalAPI.Models.Dtos;
using DotNetMinimalAPI.Routes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

// 配置 JSON 序列化（為了支援 AOT 編譯）
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 加入 OpenAPI 文件產生服務
builder.Services.AddOpenApi();

// 註冊 Oracle DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDemoConnection"))
           .UseModel(AppDbContextModel.Instance));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // 開啟 OpenAPI 路由
    app.MapOpenApi();
    
    // 開啟 Scalar UI 來呈現 API 文件 (可以取代 Swagger)
    // 啟動後在瀏覽器訪問 /scalar 即可看到漂亮的 API 測試畫面
    app.MapScalarApiReference();
}

// 建立一個在記憶體中的 List 當作暫時的資料庫
List<Todo> sampleTodos = new List<Todo>
{
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

RouteGroupBuilder todosApi = app.MapGroup("/todos");

// 註冊 Oracle 示範 API 路由
app.MapGroup("/api/oracle-demo")
   .MapOracleDemoRoutes();

// 註冊【不安全】的舊有寫法示範 API 路由 (僅供參考)
app.MapGroup("/api/legacy-oracle-demo")
   .MapLegacyOracleDemoRoutes();

// 1. GET: 取得所有 Todo
todosApi.MapGet("/", () => sampleTodos)
        .WithName("GetTodos")
        .WithSummary("取得所有代辦事項");

// 2. GET: 根據 ID 取得單一 Todo
todosApi.MapGet("/{id}", Results<Ok<Todo>, NotFound> (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? TypedResults.Ok(todo)
        : TypedResults.NotFound())
    .WithName("GetTodoById")
    .WithSummary("取得特定代辦事項");

// 3. POST: 建立新的 Todo
todosApi.MapPost("/", (Todo todo) =>
{
    // 自動產生新的 ID
    int newId = sampleTodos.Count > 0 ? sampleTodos.Max(t => t.Id) + 1 : 1;
    Todo newTodo = todo with { Id = newId }; // 替換掉傳入的 ID
    
    sampleTodos.Add(newTodo);
    return TypedResults.Created($"/todos/{newId}", newTodo);
})
.WithName("CreateTodo")
.WithSummary("新增代辦事項");

// 4. PUT: 完整更新特定 ID 的 Todo
todosApi.MapPut("/{id}", Results<NoContent, NotFound> (int id, Todo updatedTodo) =>
{
    int index = sampleTodos.FindIndex(t => t.Id == id);
    if (index == -1) return TypedResults.NotFound();

    // 替換為新的資料，但保留原本的 ID
    sampleTodos[index] = updatedTodo with { Id = id }; 
    return TypedResults.NoContent();
})
.WithName("UpdateTodo")
.WithSummary("完整更新代辦事項 (PUT)");

// 5. PATCH: 部分更新特定 ID 的 Todo
todosApi.MapPatch("/{id}", Results<NoContent, NotFound> (int id, TodoPatch patchTodo) =>
{
    int index = sampleTodos.FindIndex(t => t.Id == id);
    if (index == -1) return TypedResults.NotFound();

    Todo existing = sampleTodos[index];
    
    // 如果 patchTodo 中有值，就更新；否則保留舊值
    Todo updated = existing with 
    { 
        Title = patchTodo.Title ?? existing.Title,
        DueBy = patchTodo.DueBy ?? existing.DueBy,
        IsComplete = patchTodo.IsComplete ?? existing.IsComplete
    };

    sampleTodos[index] = updated;
    return TypedResults.NoContent();
})
.WithName("PatchTodo")
.WithSummary("部分更新代辦事項 (PATCH)");

// 6. DELETE: 刪除特定 ID 的 Todo
todosApi.MapDelete("/{id}", Results<NoContent, NotFound> (int id) =>
{
    int removedCount = sampleTodos.RemoveAll(t => t.Id == id);
    return removedCount > 0 ? TypedResults.NoContent() : TypedResults.NotFound();
})
.WithName("DeleteTodo")
.WithSummary("刪除代辦事項");

// 處理 404 (找不到網頁) 的 fallback 路由
app.MapFallback((HttpContext context) => 
{
    context.Response.StatusCode = 404;
    context.Response.ContentType = "text/html; charset=utf-8";
    return @"
        <!DOCTYPE html>
        <html lang='zh-TW'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>404 - 找不到頁面</title>
            <style>
                body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; padding: 50px; background-color: #f8f9fa; color: #333; }
                h1 { font-size: 4em; color: #dc3545; margin-bottom: 0; }
                h2 { margin-top: 10px; color: #6c757d; }
                p { font-size: 1.2em; line-height: 1.6; }
                a { color: #0d6efd; text-decoration: none; font-weight: bold; font-size: 1.1em; }
                a:hover { text-decoration: underline; color: #0a58ca; }
                .container { max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }
                .btn { display: inline-block; margin-top: 20px; padding: 10px 25px; background-color: #0d6efd; color: white; border-radius: 5px; transition: background-color 0.3s; }
                .btn:hover { background-color: #0b5ed7; color: white; text-decoration: none; }
            </style>
        </head>
        <body>
            <div class='container'>
                <h1>404</h1>
                <h2>找不到頁面 (Not Found)</h2>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                <p>您好！因為這是一個 <strong>.NET Minimal API</strong> 專案，主要作為後端資料服務，所以正常情況下是<strong>沒有提供首頁 (Homepage)</strong> 的。</p>
                <p>如果您想查看所有可用的 API 以及如何呼叫它們，請前往我們的 API 說明文件：</p>
                <a href='/scalar' class='btn'>👉 瀏覽 API 文件 (Scalar UI)</a>
            </div>
        </body>
        </html>
    ";
});

app.Run();

// 定義資料模型 (Models)
public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

// 定義給 PATCH 使用的資料模型 (所有欄位都是 Optional)
public record TodoPatch(string? Title, DateOnly? DueBy, bool? IsComplete);

// 設定 Json 序列化器支援的型別
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(TodoPatch))]
[JsonSerializable(typeof(OracleDemoItem))]
[JsonSerializable(typeof(OracleDemoCategory))]
[JsonSerializable(typeof(List<OracleDemoItem>))]
[JsonSerializable(typeof(List<OracleDemoCategory>))]
[JsonSerializable(typeof(OracleDemoItemResponse))]
[JsonSerializable(typeof(List<OracleDemoItemResponse>))]
[JsonSerializable(typeof(CreateOracleDemoItemRequest))]
[JsonSerializable(typeof(UpdateOracleDemoItemRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
