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
        
        // Checks if a product exists with the given ID
        using var command = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @IdProduct", connection);
        command.Parameters.AddWithValue("@IdProduct", idProduct);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null; // If result is not null, product exists
    }

    public async Task<bool> WarehouseExistsAsync(int idWarehouse, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Checks whether the warehouse with the given id exists
        using var command = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse", connection);
        command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        // If result is not null, warehouse exists
        return result != null;
    }
    
    public async Task<Order> FindMatchingOrderAsync(int idProduct, int amount, DateTime createdAt, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Finds an unfulfilled order with matching product and amount created before the specified date
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
                IdOrder = reader.GetInt32(reader.GetOrdinal("IdOrder")),
                IdProduct = reader.GetInt32(reader.GetOrdinal("IdProduct")),
                Amount = reader.GetInt32(reader.GetOrdinal("Amount")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                FulfilledAt = reader.IsDBNull(reader.GetOrdinal("FullfilledAt")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("FulfilledAt")),
            };
        }
        return null;
    }
    
    public async Task<bool> IsOrderFulfilledAsync(int idOrder, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Checks if there's an entry in Product_Warehouse for this order, which means that the order has been fulfilled
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
        
        // Sets the FulfilledAt date to the current date and time
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
        
        // Returns product's price
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
        
        // Inserts a new record to Product_Warehouse and returns its id using SCOPE_IDENTITY()
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
        
        // Execute the stored procedure and capture the output in a separate query
        using var command = new SqlCommand(
            "DECLARE @NewId INT; " +
            "EXEC AddProductToWarehouse " +
            "@IdProduct = @IdProduct, " +
            "@IdWarehouse = @IdWarehouse, " +
            "@Amount = @Amount, " +
            "@CreatedAt = @CreatedAt, " +
            "@IdProductWarehouse = @NewId OUTPUT; " +
            "SELECT @NewId AS IdProductWarehouse;", 
            connection);
        
        // Add parameters
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
        
        // Execute and get the result directly as a scalar value
        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        // Convert the result to an integer
        return Convert.ToInt32(result);
    }
}