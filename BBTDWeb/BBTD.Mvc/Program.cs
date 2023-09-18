using BBTD.DB.Models;
using BBTD.DB.Repository;
using BBTD.Mvc.Hubs;
using BBTD.Mvc.NLogExtensions;
using BBTD.Mvc.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.LayoutRenderers;
using NLog.Web;
using System;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
});

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
builder.Services.AddScoped<ILogFileHandler, LogFileHandler>();
builder.Services.AddSingleton<ILogForwarder, LogForwarder>();
builder.Services.AddSingleton<ISetupRepo, SetupRepo>();
builder.Services.AddSingleton<IEndpointDetector, EndpointDetector>();
//builder.Services.AddSingleton<ISerilogInterfaceDetector, SerilogInterfaceDetector>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();
builder.Services.AddSingleton<IWiFiInterfaceDetector, WiFiInterfaceDetector>();

builder.Services.AddLogging(loggingBuilder =>
 {
     loggingBuilder.ClearProviders();
     loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
     loggingBuilder.AddNLog();
 });

LayoutRenderer.Register<ExactTimestampLayoutRenderer>("exacttimestamp");
LayoutRenderer.Register<ReferenceLayoutRenderer>("reference");
LayoutRenderer.Register<OperationLayoutRenderer>("operation");

//var logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .Filter.ByExcluding(Matching.FromSource("Microsoft"))
//    .CreateLogger();
//builder.Logging.ClearProviders();
//builder.Logging.AddSerilog(logger);

//logger.Debug("Application started");

var app = builder.Build();
//var logForwarder = app.Services.GetService<ILogForwarder>();
//logForwarder?.LogForWebServer("Application started", 5);

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
    pattern: "{controller=Setup}/{action=Index}/{id?}");

app.MapHub<MessageHub>("/messageHub");

app.Run();
