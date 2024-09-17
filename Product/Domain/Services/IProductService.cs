using Domain.Entities;
using System.Threading.Tasks;

namespace Domain.Services
{
    public interface IProductService
    {
        Task CreateProductAsync(Product product);
        Task<Product> GetProductByIdAsync(string id);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(string id);
    }
}
