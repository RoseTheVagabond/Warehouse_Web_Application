using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.Models;

public class Product_Warehouse
{
    public int IdProductWarehouse { get; set; }
    public int IdWarehouse { get; set; }
    public int IdProduct { get; set; }
    public int IdOrder { get; set; }
    [Range(1, Int32.MaxValue)]
    public int Amount { get; set; }
    [Column(TypeName = "numeric(15,2)")]
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}