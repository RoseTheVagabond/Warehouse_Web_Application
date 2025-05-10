using System.ComponentModel.DataAnnotations;

namespace Warehouse.Models;

public class Order
{
    public int IdOrder { get; set; }
    [Range(0, Int32.MaxValue)]
    public int IdProduct { get; set; }
    [Range(1, Int32.MaxValue)]
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
}