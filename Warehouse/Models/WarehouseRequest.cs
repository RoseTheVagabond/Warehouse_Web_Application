using System.ComponentModel.DataAnnotations;

namespace Warehouse.Models;

public class WarehouseRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "IdProduct must be greater than 0")]
    public int IdProduct { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "IdWarehouse must be greater than 0")]
    public int IdWarehouse { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public int Amount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }
}