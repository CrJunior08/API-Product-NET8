using Amazon.SQS.Model;
using Amazon.SQS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Domain.Repositories;
using Domain.Services;
using Api.Product.Request;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Infrastructure.Messaging;

namespace Api.Products.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IDistributedCache _cache;
        private readonly SqsProducer _sqsProducer;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, IDistributedCache cache, SqsProducer sqsProducer, ILogger<ProductController> logger)
        {
            _productService = productService;
            _cache = cache;
            _sqsProducer = sqsProducer;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductRequest productRequest)
        {
            try
            {
                if (productRequest == null)
                {
                    return BadRequest("Produto inválido.");
                }

                var product = new Domain.Entities.Product
                {
                    Name = productRequest.Name,
                    Description = productRequest.Description,
                    Price = productRequest.Price
                };

                await _productService.CreateProductAsync(product);

                await _sqsProducer.SendMessageAsync($"Novo produto adicionado: {product.Name}");

                _logger.LogInformation($"Produto {product.Name} adicionado com sucesso e mensagem enviada para a fila SQS.");
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar o produto.");
                return StatusCode(500, "Erro interno do servidor.");
            }
         }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            try
            {
                var cachedProduct = await _cache.GetStringAsync(id);
                if (cachedProduct != null)
                {
                    _logger.LogInformation($"Produto {id} encontrado no cache.");
                    return Ok(cachedProduct);
                }

                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    _logger.LogWarning($"Produto {id} não encontrado.");
                    return NotFound();
                }

                await _cache.SetStringAsync(id, product.ToString());

                _logger.LogInformation($"Produto {id} encontrado do banco de dados e armazenado no cache.");
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter o produto com ID {id}.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] ProductRequest productRequest)
        {
            try
            {
                if (productRequest == null)
                {
                    return BadRequest("Produto inválido.");
                }

                var existingProduct = await _productService.GetProductByIdAsync(id);
                if (existingProduct == null)
                {
                    _logger.LogWarning($"Produto {id} não encontrado.");
                    return NotFound();
                }

                existingProduct.Name = productRequest.Name;
                existingProduct.Description = productRequest.Description;
                existingProduct.Price = productRequest.Price;

                await _productService.UpdateProductAsync(existingProduct);
                _logger.LogInformation($"Produto {id} atualizado com sucesso.");

                await _cache.RemoveAsync(id); 

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar o produto com ID {id}.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                var existingProduct = await _productService.GetProductByIdAsync(id);
                if (existingProduct == null)
                {
                    _logger.LogWarning($"Produto {id} não encontrado.");
                    return NotFound();
                }

                await _productService.DeleteProductAsync(id);
                _logger.LogInformation($"Produto {id} deletado com sucesso.");

                await _cache.RemoveAsync(id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir o produto de id: {id}.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }
    }
}
