using mediconyx_api.Data.Seeding;
using mediconyx_api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediconyxServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseMediconyxPipeline();

await app.InitializeDatabaseAsync();

app.Run();
