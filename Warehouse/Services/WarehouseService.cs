using Microsoft.Data.SqlClient;
using Warehouse.Models;
using Warehouse.Repositories;

namespace Warehouse.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    public async Task<int> AddProductToWarehouse(WarehouseRequest request, CancellationToken cancellationToken)
    {
        // 1. Check if product exists
        var productExists = await _warehouseRepository.ProductExistsAsync(request.IdProduct, cancellationToken);
        if (!productExists)
        {
            throw new Exception("Product not found");
        }

        // Check if warehouse exists
        var warehouseExists = await _warehouseRepository.WarehouseExistsAsync(request.IdWarehouse, cancellationToken);
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
        var order = await _warehouseRepository.FindMatchingOrderAsync(
            request.IdProduct, 
            request.Amount, 
            request.CreatedAt, 
            cancellationToken);
            
        if (order == null)
        {
            throw new Exception("No matching order found");
        }

        // 3. Check if the order has been fulfilled
        var orderFulfilled = await _warehouseRepository.IsOrderFulfilledAsync(order.IdOrder, cancellationToken);
        if (orderFulfilled)
        {
            throw new Exception("Order already fulfilled");
        }

        // 4. Update order as fulfilled
        await _warehouseRepository.UpdateOrderFulfillmentAsync(order.IdOrder, cancellationToken);

        // Get product price
        var productPrice = await _warehouseRepository.GetProductPriceAsync(request.IdProduct, cancellationToken);

        // 5. Insert record into Product_Warehouse
        return await _warehouseRepository.InsertProductWarehouseAsync(
            request.IdWarehouse,
            request.IdProduct,
            order.IdOrder,
            request.Amount,
            productPrice * request.Amount,
            cancellationToken);
    }

    public async Task<int> AddProductToWarehouseUsingProcedure(WarehouseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _warehouseRepository.ExecuteAddProductToWarehouseProcedureAsync(request, cancellationToken);
        }
        catch (SqlException ex)
        {
            throw new Exception($"Database error: {ex.Message}");
        }
    }
}