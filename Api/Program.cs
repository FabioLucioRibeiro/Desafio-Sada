using TaskManagement.Domain.Handlers;
using TaskManagement.Domain.Repository;
using TaskManagement.Infra.Context;
using TaskManagement.Infra.Repositories;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api;
using TaskManagement.Domain.Handlers.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false));
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);

    options.SwaggerDoc("v1", new()
    {
        Title = "SADA — Gestão de Tarefas",
        Version = "v1",
        Description = "API REST. Status: Pending (Pendente), " +
                      "InProgress (Em progresso), Completed (Concluída). " +
                      "Datas no formato yyyy-MM-dd."
    });
});

builder.Services.AddDbContext<DataContext>(options =>
    options.UseInMemoryDatabase("TaskManagementTasks"));

builder.Services.AddScoped<ITaskItemRepository, TaskItemRepository>();
builder.Services.AddScoped<ITaskHandler, TaskHandler>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }
