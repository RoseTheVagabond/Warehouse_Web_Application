using Microsoft.AspNetCore.Mvc;
using Warehouse.Models;
using Warehouse.Services;

namespace Warehouse.Controllers;

[ApiController]
[Route("[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }
    
    [HttpPost]
    public async Task<IActionResult> InsertRecordIntoWarehouse([FromBody] WarehouseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var idProductWarehouse = await _warehouseService.AddProductToWarehouse(request, cancellationToken);
            return Ok(idProductWarehouse);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("procedure")]
    public async Task<IActionResult> InsertRecordIntoWarehouseUsingProcedure([FromBody] WarehouseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var idProductWarehouse = await _warehouseService.AddProductToWarehouseUsingProcedure(request, cancellationToken);
            return Ok(idProductWarehouse);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}