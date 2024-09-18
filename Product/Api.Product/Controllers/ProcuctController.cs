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
using Microsoft.AspNetCore.Http.HttpResults;
using Api.Product.Response;
using Amazon.Runtime.Internal;

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

        /// <summary>
        /// Cria um novo produto e o insere no banco de dados. 
        /// Se o produto foi adicionado, o metodo tenta enviar uma mensagem para a fila SQS.
        /// </summary>
        /// <param name="productRequest">Dados do produto.</param>
        /// <returns>Retorna o produto criado ou uma mensagem de erro em caso de falha.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProduct([FromBody] ProductRequest productRequest)
        {
            try
            {
                if (productRequest == null)
                {
                    _logger.LogWarning("Requisição com produto inválido.");
                    return BadRequest("Produto inválido."); 
                }

                var product = new Domain.Entities.Product
                {
                    Name = productRequest.Name,
                    Description = productRequest.Description,
                    Price = productRequest.Price,
                    CreatedDate = DateTime.Now   
                };

                await _productService.CreateProductAsync(product);
                _logger.LogInformation($"Produto {product.Name} salvo com sucesso no MongoDB.");

                var messageSent = await _sqsProducer.SendMessageAsync($"Novo produto adicionado: {product.ToString()}");

                if (messageSent)
                {
                    _logger.LogInformation($"Mensagem sobre o produto {product.Name} enviada com sucesso para a fila SQS.");
                    return Ok(product); 
                }
                else
                {
                    _logger.LogWarning($"Produto {product.Name} foi salvo no MongoDB, mas teve um erro ao enviar a mensagem para a fila SQS.");
                    return StatusCode(202, new { product, Message = "Produto salvo, mas falha ao enviar mensagem para a fila SQS." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar o produto.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        /// <summary>
        /// Busca o produto pelo ID.
        /// Primeiro tenta buscar o produto no cache Redis.
        /// Se não encontrar no cache, busca no banco de dados MongoDB.
        /// </summary>
        /// <param name="id">ID do produto a ser buscado.</param>
        /// <returns>Retorna o produto encontrado.</returns>
        /// <response code="200">Produto encontrado com sucesso.</response>
        /// <response code="404">Produto não encontrado.</response>
        /// <response code="500">Erro interno no servidor ao tentar obter o produto.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundObjectResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Atualiza um produto pelo ID.
        /// O produto é atualizado no banco de dados MongoDB e o cache é resetado.
        /// </summary>
        /// <param name="id">ID do produto a ser atualizado.</param>
        /// <param name="productRequest">Dados atualizados do produto.</param>
        /// <returns>Produto atualizado com sucesso.</returns>
        /// <response code="204">Produto atualizado com sucesso.</response>
        /// <response code="404">Produto não encontrado.</response>
        /// <response code="400">Dados do produto inválidos.</response>
        /// <response code="500">Erro interno no servidor ao tentar atualizar o produto.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(NotFoundObjectResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                existingProduct.UpdatedDate = DateTime.Now;

                await _productService.UpdateProductAsync(existingProduct);
                _logger.LogInformation($"Produto {id} atualizado com sucesso.");

                await _cache.RemoveAsync(id);

                return Ok(new
                {
                    Message = $"Produto {id} atualizado com sucesso.",
                    ProdutoAtualizado = existingProduct
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar o produto com ID {id}.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        /// <summary>
        /// Deleta um produto pelo ID.
        /// O produto é deletado do MongoDB e o cache reseta.
        /// </summary>
        /// <param name="id">ID do produto a ser deletado.</param>
        /// <returns>Produto deletado com sucesso.</returns>
        /// <response code="204">Produto deletado com sucesso.</response>
        /// <response code="404">Produto não encontrado.</response>
        /// <response code="500">Erro interno no servidor ao tentar deletar o produto.</response>
        [HttpDelete("{id}/logical")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundObjectResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

                return Ok($"Produto {id} foi deletado com sucesso.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir o produto de id: {id}.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }
    }
}
