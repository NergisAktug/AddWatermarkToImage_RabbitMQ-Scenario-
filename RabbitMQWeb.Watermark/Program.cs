using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQWeb.Watermark.BackgroundServices;
using RabbitMQWeb.Watermark.Models;
using RabbitMQWeb.Watermark.Services;

var builder = WebApplication.CreateBuilder(args);

//Burası DI container oluyor; Buraya erismek istediğimiz zaman istediğimiz Servis'in(RabbitMQClientService) constructorunda bunu cagirabiliyoruz.

builder.Services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")),DispatchConsumersAsync=true});//RabbitMq'da consumer Recieved'ı asenkron bir şekilde aldıgı icin DispatchConsumersAsync  ozelligini true yaparak buna izin vermeliyiz.

builder.Services.AddSingleton<RabbitMQClientService>();
builder.Services.AddSingleton<RabbitMQPublisher>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase(databaseName:"ProductDb");
});
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<ImageWatermarkProcessBackgroundService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
