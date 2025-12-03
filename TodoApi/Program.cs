// ========== שלב 1: הוספת using ==========
// ודא ששני אלו קיימים בראש הקובץ
using Microsoft.EntityFrameworkCore;
// (ה-namespace של הפרויקט שלך, מכיל את Item ו-ToDoDbContext)
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// הוספת שירותים ל-container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ========== שלב 2: רישום ה-DbContext (ההזרקה) ==========
// 
// הוסף את הקוד הזה כאן. 
// זה קורא את מחרוזת החיבור מ-appsettings.json
// ורושם את ToDoDbContext כשירות
// ==========================================================

// 1. קבל את מחרוזת החיבור - נסה כמה משתנים אפשריים
var connectionString = 
    Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
    Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
    builder.Configuration.GetConnectionString("ToDoDB");

// טיפול בשגיאה אם Connection String חסר
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string not found. Checked: DATABASE_URL, CONNECTION_STRING, DB_CONNECTION_STRING, and appsettings.");
}

Console.WriteLine($"✓ Connection string found. Starting with: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

// 2. הזרק את ה-DbContext לשירותים
builder.Services.AddDbContext<ToDoDbContext>(options =>
    // 3. הגדר אותו להשתמש ב-MySQL עם מחרוזת החיבור
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// ==========================================================

// הוספת שירותים ל-container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ========== הוספת שירות CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
       policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// =====================================

var app = builder.Build();

// Exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
        
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { 
            error = ex.Message,
            details = ex.InnerException?.Message ?? "No additional details"
        });
    }
});

// Enable CORS FIRST
app.UseCors("AllowAll");

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ========== שלב 3: מיפוי ה-Routes (נקודות הקצה) ==========
// 
// הוסף את כל הקוד הבא כאן, לפני השורה app.Run()
// זה יוצר את 4 הפעולות שביקשת
// ============================================================

// מגדיר קבוצת routes עם קידומת אחידה /api/items
var apiRoutes = app.MapGroup("/api/items");

// 1. שליפת כל המשימות (GET)
//    HTTP Method: GET
//    Route: /api/items/
apiRoutes.MapGet("/", async (ToDoDbContext db) =>
{
    // שולף את כל הרשומות מטבלת Items ומחזיר אותן
    return Results.Ok(await db.Items.ToListAsync());
});

// 2. הוספת משימה חדשה (POST)
//    HTTP Method: POST
//    Route: /api/items/
apiRoutes.MapPost("/", async (Item item, ToDoDbContext db) =>
{
    // מקבל אובייקט 'Item' מגוף הבקשה, מוסיף אותו למסד הנתונים
    db.Items.Add(item);
    await db.SaveChangesAsync();

    // מחזיר תשובת "נוצר" (201) עם האובייקט החדש
    return Results.Created($"/api/items/{item.Id}", item);
});

// 3. עדכון משימה קיימת (PUT)
//    HTTP Method: PUT
//    Route: /api/items/{id} (לדוגמה: /api/items/5)
apiRoutes.MapPut("/{id}", async (int id, Item inputItem, ToDoDbContext db) =>
{
    // מוצא את המשימה לפי ה-ID שקיבלנו ב-URL
    var itemToUpdate = await db.Items.FindAsync(id);

    // אם המשימה לא קיימת, מחזיר 404 Not Found
    if (itemToUpdate == null)
    {
        return Results.NotFound();
    }

    // מעדכן את השדות של המשימה הקיימת
    itemToUpdate.Name = inputItem.Name;
    itemToUpdate.IsComplete = inputItem.IsComplete;

    // שומר את השינויים
    await db.SaveChangesAsync();

    // מחזיר תשובת "אין תוכן" (204) שמסמנת הצלחה
    return Results.NoContent();
});

// 4. מחיקת משימה (DELETE)
//    HTTP Method: DELETE
//    Route: /api/items/{id} (לדוגמה: /api/items/5)
apiRoutes.MapDelete("/{id}", async (int id, ToDoDbContext db) =>
{
    // מוצא את המשימה לפי ה-ID
    var itemToDelete = await db.Items.FindAsync(id);

    // אם היא קיימת, מוחק אותה
    if (itemToDelete != null)
    {
        db.Items.Remove(itemToDelete);
        await db.SaveChangesAsync();
        return Results.NoContent(); // הצלחה
    }

    // אם המשימה לא קיימת, מחזיר 404
    return Results.NotFound();
});

// ============================================================

app.MapGet("/", () => "Welcome to the ToDo API! Use /api/items to manage your tasks.");
app.Run();