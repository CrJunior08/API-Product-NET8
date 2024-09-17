using Domain.Entities;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IProductRepository
    {
        Task CreateAsync(Product product);
        Task<Product> GetByIdAsync(string id);
        Task UpdateAsync(Product product);
        Task DeleteAsync(string id);
    }
}
