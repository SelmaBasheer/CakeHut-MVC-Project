using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CakeHut.Models
{
    public class ProductDto
    {
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public decimal Price { get; set; }

        [Required]
        public IEnumerable<IFormFile>? ImageFiles { get; set; }

        public List<ProductImageDto> Images { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Weight must be greater than zero")]
        public decimal Weight { get; set; }

        [Required]
        public string Availability { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a non-negative number")]
        public int Stock { get; set; }
    }
}
