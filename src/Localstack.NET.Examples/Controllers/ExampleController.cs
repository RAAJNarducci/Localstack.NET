using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;

namespace Localstack.NET.Examples.Controllers
{
    [Route("api/services")]
    [ApiController]
    public class ExampleController : ControllerBase
    {
        private readonly IAmazonS3 _amazonS3;
        private readonly IAmazonSecretsManager _amazonSecretsManager;
        private const string S3BucketName = "test-bucket";

        public ExampleController(IAmazonS3 amazonS3, IAmazonSecretsManager amazonSecretsManager)
        {
            _amazonS3 = amazonS3;
            _amazonSecretsManager = amazonSecretsManager;
        }

        #region S3

        [HttpGet]
        [Route("s3/create-bucket")]
        public async Task<IActionResult> CreateBucket()
        {
            var getBucketLocationResponse = await _amazonS3.ListBucketsAsync();
            if (
                getBucketLocationResponse.Buckets.Any(
                    bucket => bucket.BucketName == S3BucketName))
            {
                return BadRequest("Bucket already created");
            }
            else
            {
                var response = await _amazonS3.PutBucketAsync(S3BucketName);
                return Ok();
            }
        }

        [HttpPost]
        [Route("s3/upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file, string fileName)
        {
            using var fs = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = S3BucketName,
                Key = fileName,
                InputStream = fs
            };

            var response = await _amazonS3.PutObjectAsync(request);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                return Ok();
            else
                return BadRequest("Some error!");
        }

        [HttpGet]
        [Route("s3/download-file/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var file = await _amazonS3.GetObjectAsync(S3BucketName, fileName);
            if (file.HttpStatusCode == System.Net.HttpStatusCode.OK)
                return File(file.ResponseStream, "image/jpeg");
            else
                return BadRequest("Some error!");
        }

        #endregion

        #region SecretsManager

        [HttpPost]
        [Route("secret/create")]
        public async Task<IActionResult> CreateSecret(string secretName)
        {
            Dictionary<string, string> secrets = new()
            {
                { "SQLServer", "localhost,1433" }
            };
            var response = await _amazonSecretsManager.CreateSecretAsync(new CreateSecretRequest
            {
                Description = "Secrets Test",
                Name = secretName,
                SecretString = secrets.ToString()
            });
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                return Ok();
            else
                return BadRequest("Some error!");
        }

        [HttpGet]
        [Route("secret/list")]
        public async Task<IActionResult> ListSecret(string secretName)
        {
            var response = await _amazonSecretsManager.ListSecretsAsync(new ListSecretsRequest { });
            var youSecret = response.SecretList.FirstOrDefault(x => x.Name == secretName);
            var value = await _amazonSecretsManager.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = youSecret.KmsKeyId,
            });
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                return Ok(youSecret);
            else
                return BadRequest("Some error!");
        }

        #endregion
    }
}
