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
    Environment.GetEnvironmentVariable("ConnectionStrings__ToDoDB") ??
    Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
    Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
    builder.Configuration.GetConnectionString("ToDoDB");

// טיפול בשגיאה אם Connection String חסר
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string not found. ChecxxxxxxxxxxctionStrings__ToDoDB, DATABASxxxxxxxxxxxxCTION_STRING, and appsettings.");
}

Console.WriteLine($"✓ Connection string found!");

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

// Store items in memory for now
var items = new List<Item>();
var nextId = 1;

// מגדיר קבוצת routes עם קידומת אחידה /api/items
var apiRoutes = app.MapGroup("/api/items");

// 1. שליפת כל המשימות (GET)
apiRoutes.MapGet("/", async () =>
{
    return Results.Ok(items);
});

// 2. הוספת משימה חדשה (POST)
apiRoutes.MapPost("/", async (Item item) =>
{
    item.Id = nextId++;
    items.Add(item);
    return Results.Created($"/api/items/{item.Id}", item);
});

// 3. עדכון משימה קיימת (PUT)
apiRoutes.MapPut("/{id}", async (int id, Item inputItem) =>
{
    var itemToUpdate = items.FirstOrDefault(x => x.Id == id);
    if (itemToUpdate == null)
        return Results.NotFound();

    itemToUpdate.Name = inputItem.Name;
    itemToUpdate.IsComplete = inputItem.IsComplete;
    return Results.NoContent();
});

// 4. מחיקת משימה (DELETE)
apiRoutes.MapDelete("/{id}", async (int id) =>
{
    var itemToDelete = items.FirstOrDefault(x => x.Id == id);
    if (itemToDelete != null)
    {
        items.Remove(itemToDelete);
        return Results.NoContent();
    }
    return Results.NotFound();
});

app.MapGet("/", () => "Welcome to the ToDo API! Use /api/items to manage your tasks.");
app.MapGet("/health", () => Results.Ok(new { status = "API is running", timestamp = DateTime.UtcNow }));

// Test database connection
app.MapGet("/db-test", async (ToDoDbContext db) =>
{
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();
        return Results.Ok(new { status = "Database connection successful" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();