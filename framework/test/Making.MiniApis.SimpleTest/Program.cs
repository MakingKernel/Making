var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMiniApis();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapMiniApis();

app.Run();