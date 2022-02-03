using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Localstack.NET.Examples.Models;
using Localstack.NET.Examples.Models.Dynamo;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Localstack.NET.Examples.Controllers
{
    [Route("api/services")]
    [ApiController]
    public class ExampleController : ControllerBase
    {
        private readonly IAmazonS3 _amazonS3;
        private readonly IAmazonSecretsManager _amazonSecretsManager;
        private readonly IAmazonDynamoDB _amazonDynamoDb;
        private readonly DynamoDBContext _dynamoDBContext;

        private const string S3BucketName = "test-bucket";

        public ExampleController(IAmazonS3 amazonS3, IAmazonSecretsManager amazonSecretsManager, IAmazonDynamoDB amazonDynamoDb)
        {
            _amazonS3 = amazonS3;
            _amazonSecretsManager = amazonSecretsManager;
            _amazonDynamoDb = amazonDynamoDb;
            _dynamoDBContext = new(_amazonDynamoDb);
        }

        #region S3

        [HttpGet]
        [Route("s3/create-bucket")]
        public async Task<IActionResult> CreateBucket()
        {
            try
            {
                var getBucketLocationResponse = await _amazonS3.ListBucketsAsync();
                if (getBucketLocationResponse.Buckets.Any(bucket => bucket.BucketName == S3BucketName))
                {
                    return BadRequest("Bucket already created");
                }
                else
                {
                    var response = await _amazonS3.PutBucketAsync(S3BucketName);
                    return Ok();
                }
            }
            catch (AmazonS3Exception e) { return BadRequest($"S3 Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        [HttpPost]
        [Route("s3/upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file, string fileName)
        {
            try
            {
                using var fs = file.OpenReadStream();
                var request = new PutObjectRequest
                {
                    BucketName = S3BucketName,
                    Key = fileName,
                    InputStream = fs
                };

                var response = await _amazonS3.PutObjectAsync(request);
                return Ok();
            }
            catch (AmazonS3Exception e) { return BadRequest($"S3 Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        [HttpGet]
        [Route("s3/download-file/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var file = await _amazonS3.GetObjectAsync(S3BucketName, fileName);
                return File(file.ResponseStream, "image/jpeg");
            }
            catch (AmazonS3Exception e) { return BadRequest($"S3 Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        #endregion

        #region SecretsManager

        [HttpPost]
        [Route("secret/create")]
        public async Task<IActionResult> CreateSecret(string secretName)
        {
            try
            {
                var secretExample = "Server=127.0.0.1;Port=5432;Database=myDataBase;User Id=myUsername;Password=myPassword;";
                var response = await _amazonSecretsManager.CreateSecretAsync(new CreateSecretRequest
                {
                    Description = "Secrets Test",
                    Name = secretName,
                    SecretString = secretExample
                });

                return Ok();
            }
            catch (AmazonSecretsManagerException e) { return BadRequest($"Secrets Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        [HttpGet]
        [Route("secret/list")]
        public async Task<IActionResult> ListSecret(string secretName)
        {
            try
            {
                var response = await _amazonSecretsManager.ListSecretsAsync(new ListSecretsRequest { });
                if (!response.SecretList.Any())
                    return BadRequest("List Secrets Empty");

                var youSecret = response.SecretList.FirstOrDefault(x => x.Name == secretName);
                if (youSecret is not null)
                {
                    var value = await _amazonSecretsManager.GetSecretValueAsync(new GetSecretValueRequest
                    {
                        SecretId = youSecret.ARN
                    });

                    return Ok(value.SecretString);
                }
                else
                    return BadRequest("Secret name not found");
            }
            catch (AmazonSecretsManagerException e) { return BadRequest($"Secrets Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        #endregion

        #region DynamoDB

        [HttpPost]
        [Route("dynamo/insert")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerDynamo customer)
        {
            try
            {
                CreateTable("customer");
                CustomerDynamo customerDynamo = new()
                {
                    Name = customer.Name,
                    Id = Guid.NewGuid().ToString()
                };

                await _dynamoDBContext.SaveAsync(customerDynamo);

                return Created("", customerDynamo);
            }
            catch (AmazonDynamoDBException e) { return BadRequest($"Dynamo Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        [HttpGet]
        [Route("dynamo/list")]
        public async Task<IActionResult> GetAllCustomers()
        {
            try
            {
                var conditions = new List<ScanCondition>();
                var response = await _dynamoDBContext.ScanAsync<CustomerDynamo>(conditions).GetRemainingAsync();

                return Ok(response);
            }
            catch (AmazonDynamoDBException e) { return BadRequest($"Dynamo Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        [HttpGet]
        [Route("dynamo/get/{id}")]
        public async Task<IActionResult> GetCustomerById(string id)
        {
            try
            {
                var customer = await _dynamoDBContext.LoadAsync<CustomerDynamo>(id);
                if (customer is null)
                    return NoContent();

                return Ok(customer);
            }
            catch (AmazonDynamoDBException e) { return BadRequest($"Dynamo Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        [HttpPatch]
        [Route("dynamo/update")]
        public async Task<IActionResult> Update(string id, [FromBody]JsonPatchDocument<CustomerDynamo> customerPatch)
        {
            try
            {
                var customer = await _dynamoDBContext.LoadAsync<CustomerDynamo>(id);
                if (customer is null)
                    return NoContent();

                customerPatch.ApplyTo(customer, ModelState);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _dynamoDBContext.SaveAsync(customer);

                return Ok(customer);
            }
            catch (AmazonDynamoDBException e) { return BadRequest($"Dynamo Exception: {e.Message} "); }
            catch (AmazonServiceException e) { return BadRequest($"Service Exception: {e.Message} "); }
            catch (Exception e) { return BadRequest($"Exception: {e.Message} "); }
        }

        private void CreateTable(string tableName)
        {
            var tables = _amazonDynamoDb.ListTablesAsync().Result;
            if (!tables.TableNames.Contains(tableName))
            {
                var request = new CreateTableRequest
                {
                    AttributeDefinitions = new List<AttributeDefinition>()
                    {
                        new AttributeDefinition
                        {
                            AttributeName = "Id",
                            AttributeType = "S"
                        }

                    },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "Id",
                            KeyType = "HASH" //Partition key
                        }
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 6
                    },
                    TableName = tableName
                };

                _amazonDynamoDb.CreateTableAsync(request).Wait();
            }
        }

        #endregion
    }
}