namespace Localstack.NET.Examples.Models
{
    public abstract class Customer
    {
        public Guid CustomerId { get; set; }
        public string? Name { get; set; }
    }
}
