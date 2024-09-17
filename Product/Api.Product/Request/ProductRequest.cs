using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Api.Product.Request
{
    public class ProductRequest
    {
        public required string Name { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }
    }
}
