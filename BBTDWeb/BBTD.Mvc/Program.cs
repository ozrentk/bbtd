using BBTD.DB.Models;
using BBTD.DB.Repository;
using BBTD.Mvc.Hubs;
using BBTD.Mvc.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddSignalR();

builder.Services.AddDbContext<BbtddbContext>(options =>
{
    options.UseSqlServer("name=ConnectionStrings:BbtddbConnStr");
});

builder.Services.AddAutoMapper(typeof(Program).Assembly);

builder.Services.AddScoped<IPersonRepo, PersonRepo>();
builder.Services.AddScoped<IBarcodeGenerator, BarcodeGenerator>();
builder.Services.AddScoped<INetworkInterfaceDetector, NetworkInterfaceDetector>();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<MessageHub>("/messageHub");

app.Run();
