using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TownePark.Billing.Api.Data;
using TownePark.Billing.Api.Data.Impl;
using TownePark.Billing.Api.Services;
using TownePark.Billing.Api.Services.Impl;
using TownePark.Billing.Api.Config;
using Microsoft.Extensions.Logging; // Add this

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Register your repository and service here:
builder.Services.AddSingleton<IEDWRepository, EDWRepository>();
builder.Services.AddSingleton<IEDWService, EDWService>();

builder.Services.AddSingleton<IEDWRepository>(sp =>
new EDWRepository(
Configuration.GetSQLConnectionString(), // Replace with your config access
        sp.GetRequiredService<ILogger<EDWRepository>>()
    )
);

builder.UseMiddleware<AuthenticationMiddleware>();

builder.Build().Run();