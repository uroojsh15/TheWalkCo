using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheWalkco.Interfaces;
using TheWalkco.Models;

namespace TheWalkco.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(IConfiguration config, ILogger<ProductRepository> logger)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Helper method: create and open connection asynchronously
        private async Task<SqlConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all products");
                await using var db = await CreateOpenConnectionAsync();
                return await db.QueryAsync<Product>("SELECT * FROM Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all products");
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
        {
            await using var db = await CreateOpenConnectionAsync();

            if (string.IsNullOrEmpty(category) || category.ToLower() == "all")
            {
                // No filter, return all products
                return await db.QueryAsync<Product>("SELECT * FROM Products");
            }

            if (category.ToLower() == "men")
            {
                return await db.QueryAsync<Product>(
                    "SELECT * FROM Products WHERE Category LIKE 'men-%'");
            }

            if (category.ToLower() == "women")
            {
                return await db.QueryAsync<Product>(
                    "SELECT * FROM Products WHERE Category LIKE 'women-%'");
            }

            // Default: exact match
            return await db.QueryAsync<Product>(
                "SELECT * FROM Products WHERE Category = @Category",
                new { Category = category });
        }


        public async Task<Product> GetProductByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching product with ID {ProductId}", id);
                await using var db = await CreateOpenConnectionAsync();
                return await db.QueryFirstOrDefaultAsync<Product>(
                    "SELECT * FROM Products WHERE Id = @Id",
                    new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product by ID {ProductId}", id);
                throw;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            try
            {
                _logger.LogInformation("Adding new product {Name}", product.Name);
                await using var db = await CreateOpenConnectionAsync();

                var sql = @"INSERT INTO Products (Name, Description, Sizes, Price, Category, ImageUrl, Stock)
                            OUTPUT INSERTED.Id
                            VALUES (@Name, @Description, @Sizes, @Price, @Category, @ImageUrl, @Stock);";

                var newId = await db.ExecuteScalarAsync<int>(sql, product);
                product.Id = newId; _logger.LogInformation("Product added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                throw;
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            try
            {
                _logger.LogInformation("Updating product {ProductId}", product.Id);
                await using var db = await CreateOpenConnectionAsync();

                var sql = @"UPDATE Products
                            SET Name = @Name,
                                Description = @Description,
                                Price = @Price,
                                Sizes = @Sizes,
                                Category = @Category,
                                ImageUrl = @ImageUrl,
                                Stock = @Stock
                            WHERE Id = @Id;";

                await db.ExecuteAsync(sql, product);
                _logger.LogInformation("Product updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
                throw;
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            try
            {
                _logger.LogWarning("Deleting product {ProductId}", id);
                await using var db = await CreateOpenConnectionAsync();

                await db.ExecuteAsync("DELETE FROM Products WHERE Id = @Id", new { Id = id });
                _logger.LogInformation("Product deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string term)
        {
            try
            {
                await using var db = await CreateOpenConnectionAsync();

                if (string.IsNullOrWhiteSpace(term))
                    return Enumerable.Empty<Product>();

                term = "%" + term.Trim().Replace(" ", "%") + "%";

                var result = await db.QueryAsync<Product>(
                    "SELECT * FROM Products WHERE Name LIKE @Term",
                    new { Term = term }
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                throw;
            }
        }
    }


    }

