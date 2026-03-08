using System.ComponentModel.DataAnnotations;

namespace TheWalkco.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        public decimal Price { get; set; }

        [Required]
        public string Category { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater")]
        public int Stock { get; set; }

        public string Sizes { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public string Description { get; set; }
    }
}
