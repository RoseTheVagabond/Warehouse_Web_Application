using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Warehouse.Models;

namespace Warehouse.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString;

    public WarehouseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<int> AddProductToWarehouse(WarehouseRequest request, CancellationToken cancellationToken)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // 1. Check if product exists
                    var productExists = await CheckProductExists(connection, transaction, request.IdProduct, cancellationToken);
                    if (!productExists)
                    {
                        throw new Exception("Product not found");
                    }

                    // Check if warehouse exists
                    var warehouseExists = await CheckWarehouseExists(connection, transaction, request.IdWarehouse, cancellationToken);
                    if (!warehouseExists)
                    {
                        throw new Exception("Warehouse not found");
                    }

                    // Check if amount is valid
                    if (request.Amount <= 0)
                    {
                        throw new Exception("Amount must be greater than 0");
                    }

                    // 2. Check if there is a matching order
                    var order = await FindMatchingOrder(connection, transaction, request.IdProduct, request.Amount, request.CreatedAt, cancellationToken);
                    if (order == null)
                    {
                        throw new Exception("No matching order found");
                    }

                    // 3. Check if the order has been fulfilled
                    var orderFulfilled = await IsOrderFulfilled(connection, transaction, order.IdOrder, cancellationToken);
                    if (orderFulfilled)
                    {
                        throw new Exception("Order already fulfilled");
                    }

                    // 4. Update order as fulfilled
                    await UpdateOrderFulfillment(connection, transaction, order.IdOrder, cancellationToken);

                    // Get product price
                    var productPrice = await GetProductPrice(connection, transaction, request.IdProduct, cancellationToken);

                    // 5. Insert record into Product_Warehouse
                    var idProductWarehouse = await InsertProductWarehouse(
                        connection, 
                        transaction, 
                        request.IdWarehouse,
                        request.IdProduct,
                        order.IdOrder,
                        request.Amount,
                        productPrice * request.Amount,
                        cancellationToken);

                    transaction.Commit();
                    return idProductWarehouse;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public async Task<int> AddProductToWarehouseUsingProcedure(WarehouseRequest request, CancellationToken cancellationToken)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            
            using (var command = new SqlCommand("AddProductToWarehouse", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                
                // Add parameters
                command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
                command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
                command.Parameters.AddWithValue("@Amount", request.Amount);
                command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
                
                // Add output parameter for the newly created Product_Warehouse ID
                var returnParameter = new SqlParameter("@IdProductWarehouse", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(returnParameter);
                
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    return (int)returnParameter.Value;
                }
                catch (SqlException ex)
                {
                    throw new Exception($"Database error: {ex.Message}");
                }
            }
        }
    }

    private async Task<bool> CheckProductExists(SqlConnection connection, SqlTransaction transaction, int idProduct, CancellationToken cancellationToken)
    {
        using (var command = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @IdProduct", connection, transaction))
        {
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
    }

    private async Task<bool> CheckWarehouseExists(SqlConnection connection, SqlTransaction transaction, int idWarehouse, CancellationToken cancellationToken)
    {
        using (var command = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse", connection, transaction))
        {
            command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
    }

    private async Task<Order> FindMatchingOrder(SqlConnection connection, SqlTransaction transaction, int idProduct, int amount, DateTime createdAt, CancellationToken cancellationToken)
    {
        using (var command = new SqlCommand(
            "SELECT TOP 1 o.IdOrder, o.IdProduct, o.Amount, o.CreatedAt, o.FulfilledAt " +
            "FROM \"Order\" o " + 
            "LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder " +
            "WHERE o.IdProduct = @IdProduct AND o.Amount = @Amount " +
            "AND pw.IdProductWarehouse IS NULL AND o.CreatedAt < @CreatedAt", 
            connection, 
            transaction))
        {
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    return new Order
                    {
                        IdOrder = reader.GetInt32(0),
                        IdProduct = reader.GetInt32(1),
                        Amount = reader.GetInt32(2),
                        CreatedAt = reader.GetDateTime(3),
                        FulfilledAt = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4)
                    };
                }
                return null;
            }
        }
    }

    private async Task<bool> IsOrderFulfilled(SqlConnection connection, SqlTransaction transaction, int idOrder, CancellationToken cancellationToken)
    {
        using (var command = new SqlCommand(
            "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder", 
            connection, 
            transaction))
        {
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
    }

    private async Task UpdateOrderFulfillment(SqlConnection connection, SqlTransaction transaction, int idOrder, CancellationToken cancellationToken)
    {
        using (var command = new SqlCommand(
            "UPDATE \"Order\" SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder", 
            connection, 
            transaction))
        {
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<decimal> GetProductPrice(SqlConnection connection, SqlTransaction transaction, int idProduct, CancellationToken cancellationToken)
    {
        using (var command = new SqlCommand(
            "SELECT Price FROM Product WHERE IdProduct = @IdProduct", 
            connection, 
            transaction))
        {
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return (decimal)result;
        }
    }

    private async Task<int> InsertProductWarehouse(
        SqlConnection connection, 
        SqlTransaction transaction, 
        int idWarehouse, 
        int idProduct, 
        int idOrder, 
        int amount, 
        decimal price, 
        CancellationToken cancellationToken)
    {
        using (var command = new SqlCommand(
            "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) " +
            "VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE()); " +
            "SELECT SCOPE_IDENTITY();", 
            connection, 
            transaction))
        {
            command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@Price", price);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
    }
}