using Domain.Repositories;
using Domain.Services;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StackExchange.Redis;
using Amazon.SQS;
using Microsoft.OpenApi.Models;
using Service.Services;
using Infrastructure.Messaging;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Configura��o do MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoClient>(s =>
{
    var settings = s.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddScoped<MongoDbContext>();

// Configura��o do Redis para cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetSection("RedisSettings:ConnectionString").Value;
    options.InstanceName = "ProductCache_";
});

builder.Services.AddSingleton<IAmazonSQS>(sp =>
{
    var credentials = new BasicAWSCredentials(
        builder.Configuration["AWS:AccessKey"],
        builder.Configuration["AWS:SecretKey"]);

    var sqsConfig = new AmazonSQSConfig
    {
        ServiceURL = "http://localhost:4566", 
        UseHttp = true  
    };

    return new AmazonSQSClient(credentials, sqsConfig);
});

// Registro dos reposit�rios e servi�os
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// Registro AWS/SQS (respons�vel por enviar mensagens para a fila)
builder.Services.AddSingleton<SqsProducer>();

// Adicionar os controladores da API
builder.Services.AddControllers();

// Configura��o do Swagger para documenta��o da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product API",
        Version = "v1",
        Description = "API para gerenciar produtos com integra��o MongoDB, Redis e AWS SQS."
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
