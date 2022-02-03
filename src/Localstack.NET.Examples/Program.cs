using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.SecretsManager;
using static Localstack.NET.Examples.Extensions.JsonInputFormatterExtension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
});
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IAmazonS3>(provider =>
{
    return new AmazonS3Client(new AmazonS3Config
    {
        UseHttp = true,
        ServiceURL = builder.Configuration["AWS:S3"],
        ForcePathStyle = true
    });
});
builder.Services.AddSingleton<IAmazonSecretsManager>(provider =>
{
    return new AmazonSecretsManagerClient(new AmazonSecretsManagerConfig
    {
        UseHttp = true,
        ServiceURL = builder.Configuration["AWS:SECRET"],
    });
});
builder.Services.AddSingleton<IAmazonDynamoDB>(provider =>
{
    return new AmazonDynamoDBClient(new AmazonDynamoDBConfig
    {
        UseHttp = true,
        ServiceURL = builder.Configuration["AWS:DYNAMO"],
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
