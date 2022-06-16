using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using Catalog.Api.Repositories;
using Catalog.Api.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));
var mongodbSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();

// Add services to the container.
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
  return new MongoClient(mongodbSettings.ConnectionString);
});
builder.Services.AddSingleton<IItemsRepository, MongoDbItemsRepository>();
builder.Services.AddControllers(options =>
{
  options.SuppressAsyncSuffixInActionNames = false;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add health check
builder.Services.AddHealthChecks()
    .AddMongoDb(
        mongodbSettings.ConnectionString,
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3),
        tags: new[] { "ready" }
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}
if (app.Environment.IsDevelopment()){
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Add the healthcheck page
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
  Predicate = (check) => check.Tags.Contains("ready"),
  ResponseWriter = async (context, report) =>
  {
    var result = JsonSerializer.Serialize(
        new {
            status  = report.Status.ToString(),
            checks = report.Entries.Select(entry => new {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                exception = entry.Value.Exception != null ? entry.Value.Exception.Message : "none",
                duration = entry.Value.Duration.ToString()
            })
        }
    );

    context.Response.ContentType = MediaTypeNames.Application.Json;
    await context.Response.WriteAsync(result);
  }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
  Predicate = (_) => false
});

app.Run();
