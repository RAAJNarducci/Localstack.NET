using Amazon.DynamoDBv2.DataModel;

namespace Localstack.NET.Examples.Models.Dynamo
{
    [DynamoDBTable("customer")]
    public class CustomerDynamo
    {
        [DynamoDBHashKey]
        public string? Id { get; set; }

        public string? Name { get; set; }
    }
}
