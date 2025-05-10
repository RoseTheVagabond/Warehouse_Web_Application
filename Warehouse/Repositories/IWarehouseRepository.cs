using Warehouse.Models;

namespace Warehouse.Repositories;

public interface IWarehouseRepository
{
    Task<bool> ProductExistsAsync(int idProduct, CancellationToken cancellationToken);
    Task<bool> WarehouseExistsAsync(int idWarehouse, CancellationToken cancellationToken);
    Task<Order> FindMatchingOrderAsync(int idProduct, int amount, DateTime createdAt, CancellationToken cancellationToken);
    Task<bool> IsOrderFulfilledAsync(int idOrder, CancellationToken cancellationToken);
    Task UpdateOrderFulfillmentAsync(int idOrder, CancellationToken cancellationToken);
    Task<decimal> GetProductPriceAsync(int idProduct, CancellationToken cancellationToken);
    Task<int> InsertProductWarehouseAsync(int idWarehouse, int idProduct, int idOrder, int amount, decimal price, CancellationToken cancellationToken);
    Task<int> ExecuteAddProductToWarehouseProcedureAsync(WarehouseRequest request, CancellationToken cancellationToken);
}