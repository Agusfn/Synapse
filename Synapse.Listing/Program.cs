using Synapse.Listing.Services;
using System.Net;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 5001, listenOptions =>
    {
        listenOptions.UseHttps("Certs/cert.pfx", "20596");
    });
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<ListingService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

////app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
