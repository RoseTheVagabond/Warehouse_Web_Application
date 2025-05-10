using Warehouse.Models;

namespace Warehouse.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouse(WarehouseRequest request, CancellationToken cancellationToken);
    Task<int> AddProductToWarehouseUsingProcedure(WarehouseRequest request, CancellationToken cancellationToken);
}