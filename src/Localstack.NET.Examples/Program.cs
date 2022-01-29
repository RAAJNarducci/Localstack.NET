using Amazon.S3;
using Amazon.SecretsManager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
