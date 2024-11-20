using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CakeHut.Models
{
    public class Review
    {

        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        
        [Range(1, 5)]
        public int Rating { get; set; }
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
