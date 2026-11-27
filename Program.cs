using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Data;

bool seedDemo = args.Contains("--seed-demo");

var builder = WebApplication.CreateBuilder(
    args.Where(arg => arg != "--seed-demo").ToArray());

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<SiteActionTraceContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (seedDemo)
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<SiteActionTraceContext>();

    await context.Database.MigrateAsync();

    bool initialized = await DemoData.InitializeAsync(context);

    Console.WriteLine(initialized
        ? "Demonstration data initialized successfully."
        : "Database already contains records. No demonstration data was added.");

    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();