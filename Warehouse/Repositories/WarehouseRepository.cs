using System.Data;
using Microsoft.Data.SqlClient;
using Warehouse.Models;

namespace Warehouse.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly string _connectionString;

    public WarehouseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<bool> ProductExistsAsync(int idProduct, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @IdProduct", connection);
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }

    public async Task<bool> WarehouseExistsAsync(int idWarehouse, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse", connection);
        command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }

    public async Task<Order> FindMatchingOrderAsync(int idProduct, int amount, DateTime createdAt, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(
            "SELECT TOP 1 o.IdOrder, o.IdProduct, o.Amount, o.CreatedAt, o.FulfilledAt " +
            "FROM \"Order\" o " + 
            "LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder " +
            "WHERE o.IdProduct = @IdProduct AND o.Amount = @Amount " +
            "AND pw.IdProductWarehouse IS NULL AND o.CreatedAt < @CreatedAt",
            connection);
        
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@CreatedAt", createdAt);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
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

    public async Task<bool> IsOrderFulfilledAsync(int idOrder, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(
            "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder",
            connection);
        
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }

    public async Task UpdateOrderFulfillmentAsync(int idOrder, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(
            "UPDATE \"Order\" SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder",
            connection);
        
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<decimal> GetProductPriceAsync(int idProduct, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(
            "SELECT Price FROM Product WHERE IdProduct = @IdProduct",
            connection);
        
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (decimal)result;
    }

    public async Task<int> InsertProductWarehouseAsync(
        int idWarehouse,
        int idProduct,
        int idOrder,
        int amount,
        decimal price,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(
            "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) " +
            "VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE()); " +
            "SELECT SCOPE_IDENTITY();",
            connection);
        
        command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@Price", price);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> ExecuteAddProductToWarehouseProcedureAsync(WarehouseRequest request, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand("AddProductToWarehouse", connection);
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
        
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (int)returnParameter.Value;
    }
}