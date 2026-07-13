# TypeScript vs C# (DotNetMinimalAPI 專案語法差異筆記)

這份筆記旨在幫助熟悉 TypeScript (Node.js/前端) 的開發者，快速適應本專案 (.NET 10, C# 14/13) 的嚴格撰寫規範與語法差異。

C# 與 TypeScript 都是由 Anders Hejlsberg 主導設計，因此在語法上有許多相似之處（例如 `async/await`、泛型）。但 C# 屬於**名義型別 (Nominal Typing)**，而 TypeScript 是**結構型別 (Structural Typing)**。此外，本專案實施了**非常嚴格的開發規範**。

---

## 1. 變數宣告與基礎型別 (Variables & Types)

在 TypeScript 中，你可能習慣使用 `let` 或 `const`。但在本專案的 C# 中，規範要求**明確宣告型別**，**避免使用隱含型別 `var`**（除非等號右側的型別顯而易見）。

| 功能 | TypeScript | C# (本專案規範) |
| :--- | :--- | :--- |
| **字串** | `let name: string = "Alice";` | `string name = "Alice";` |
| **整數** | `let age: number = 25;` | `int age = 25;` |
| **布林** | `let isActive: boolean = true;` | `bool isActive = true;` |
| **常數** | `const max: number = 100;` | `const int max = 100;` |
| **自動推斷** | `let list = new Array<string>();` | `var list = new List<string>();` (⚠️ 僅在右側型別明顯時允許使用 `var`) |

### ⚠️ 嚴格禁止的動態型別
在 TS 中，`any` 或 `unknown` 是處理未知結構的常見方式。但在本專案中：
- **絕對禁止**使用 `dynamic` 或 `object`（除非在處理 Reflection 或未定型的 JSON）。
- 請一律定義強型別的 DTO 或 Model 來接資料。

---

## 2. Nullable 參考型別與空值檢查 (Nullability)

本專案啟用了 `<Nullable>enable</Nullable>`，這表示參考型別預設是不可為 null 的（類似 TypeScript 的 `strictNullChecks`）。

### 宣告 Nullable
- **TS**: `let name: string | null = null;`
- **C#**: `string? name = null;`

### 專案規範：嚴格的 Null 檢查
在處理字串與物件時，本專案有特定的檢查習慣：

```csharp
// ❌ 壞習慣 (舊式 C# 或 TS 思維)
if (name == null || name == "") { }
if (obj != null) { }

// ✅ 好習慣 (本專案規範)
if (string.IsNullOrEmpty(name)) { }
if (string.IsNullOrWhiteSpace(name)) { }

// 物件檢查 (使用 pattern matching)
if (obj is not null) { }
if (obj is null) { }

// 方法開頭的防呆機制 (Guard Clauses)
ArgumentNullException.ThrowIfNull(obj);
```

---

## 3. 陣列與集合 (Arrays & Collections)

在 TS 中，萬物皆 `Array` (`T[]`)。在 C# 中，通常會區分長度固定的陣列與可變動長度的 `List<T>`。

- **TS**: `const users: string[] = ["Alice", "Bob"];`
- **C#**: `List<string> users = new List<string> { "Alice", "Bob" };`
  - *專案規範*：偏好使用強型別泛型集合 (如 `List<T>`)，不要使用非泛型的舊版集合 (如 `ArrayList`)。

---

## 4. 類別、介面與物件導向 (Classes & Interfaces)

TS 中的 `interface` 常被當作「資料形狀 (Shape)」的宣告（Type Alias 的替代品）。但在 C# 中，`interface` 是嚴格的合約，通常搭配 DI（依賴注入）使用。對於純資料傳遞，C# 現在偏好使用 `record`。

### 繼承與實作
- **TS**: `class User implements IUser, IEntity { }`
- **C#**: `public class User : IUser, IEntity { }` (使用 `:` 代替 `implements` 或 `extends`)

### 純資料物件 (DTO)
- **TS**: 
  ```typescript
  type UserDto = { id: number; name: string; }
  ```
- **C#** (建議使用 Record 或定義 Class):
  ```csharp
  public record UserDto(int Id, string Name);
  ```

---

## 5. 控制流與模式比對 (Pattern Matching)

TypeScript 開發者常使用 `typeof` 或 `instanceof` 以及 discriminated unions 來做控制流分析。C# 的 Pattern Matching 非常強大，本專案**強烈建議**使用 `is` 和 `switch` 表達式。

### 型別檢查與轉型
- **TS**:
  ```typescript
  if (obj instanceof User) {
      console.log(obj.name); // 自動轉型
  }
  ```
- **C#** (使用 `is` 宣告變數)：
  ```csharp
  if (obj is User userObj) {
      Console.WriteLine(userObj.Name); // userObj 是強型別的 User
  }
  // ⚠️ 避免使用舊式的 (User)obj 或 obj as User
  ```

### Switch 表達式
- **C#** 取代複雜的 `if-else` 或傳統 `switch` 語句：
  ```csharp
  var statusMessage = statusCode switch
  {
      200 => "Success",
      404 => "Not Found",
      _ => "Unknown Error"
  };
  ```

---

## 6. 非同步程式設計 (Async / Await)

兩者的非同步語法非常相似，但底層型別不同。
- **TS**: 回傳 `Promise<T>`。
- **C#**: 回傳 `Task<T>` (或 ValueTask)。

### ⚠️ EF Core 與記憶體深度檢查規範 (CRITICAL)
這是本專案與一般 TS 框架 (如 TypeORM/Prisma) 最大的不同處。在撰寫與資料庫 (EF Core) 互動的非同步程式碼時，必須嚴格遵守以下規則：

1. **強制使用 Async 方法**：
   - ❌ **絕對禁止**同步呼叫如 `.ToList()` 或 `.FirstOrDefault()`。
   - ✅ 必須使用 `await .ToListAsync()` 或 `await .FirstOrDefaultAsync()`。
2. **Read-Only 查詢的效能最佳化**：
   - ✅ 如果查詢出的實體只是用來顯示（不進行 Update），**必須**加上 `.AsNoTracking()` 以節省記憶體。
   - 範例：`await _context.Users.AsNoTracking().ToListAsync();`
3. **避免 N+1 查詢問題**：
   - ✅ 迴圈內禁止查詢資料庫。必須在迴圈外使用 `.Include()` 或 `.Select()` 預先抓取資料。
4. **確實處理 `IDisposable`**：
   - TS 有 Garbage Collection，C# 也有，但針對非受控資源 (Streams, HttpClients, DB 連線)，C# 要求明確釋放。
   - ✅ 必須使用 `using var stream = new MemoryStream();` 或 `using (...) { }` 區塊。

---

## 7. 模組系統 vs 命名空間 (Modules vs Namespaces)

- **TS** 依賴實體的檔案路徑來匯入模組：
  ```typescript
  import { UserService } from './services/UserService';
  ```
- **C#** 依賴邏輯上的命名空間 (Namespace)：
  ```csharp
  using DotNetMinimalAPI.Services; // 放在檔案頂部
  
  namespace DotNetMinimalAPI.Controllers;
  // 類別內容...
  ```
  *不用管檔案在哪個資料夾，只要命名空間對了就能使用。但專案規範要求資料夾結構必須與命名空間一致。*

---

## 總結：給 TS 開發者的 C# 心法

1. **擁抱強型別**：忘掉 `any`，定義好所有的 Models, ViewModels 和 DTOs。
2. **習慣顯式宣告**：少用 `var`，多把型別寫清楚。
3. **安全第一**：善用 `string.IsNullOrEmpty` 和 `is not null`。
4. **非同步必加 Await**：看到資料庫操作，永遠加上 `Async` 後綴並 `await` 它。
5. **關注效能**：EF Core 查詢記得 `.AsNoTracking()`，用完的資源記得 `using`。
