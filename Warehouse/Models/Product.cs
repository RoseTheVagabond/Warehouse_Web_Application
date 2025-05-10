using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.Models;

public class Product
{
    public int IdProduct { get; set; }
    [MaxLength(200)]
    public string Name { get; set; }
    [MaxLength(200)]
    public string Description { get; set; }
    [Column(TypeName = "numeric(25,2)")]
    public decimal Price { get; set; }
}