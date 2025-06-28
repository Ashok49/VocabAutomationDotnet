using VocabAutomation.Services;
using VocabAutomation.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appSettings.json", optional: true)
    .AddEnvironmentVariables();

// Register services and interfaces
builder.Services.AddScoped<IVocabService, VocabService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<IGptService, GptService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<ITwilioService, TwilioService>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IDocxParserService, DocxParserService>();
builder.Services.AddScoped<IVocabSyncService, VocabSyncService>();
builder.Services.AddScoped<ISendBatchService, SendBatchService>();


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

