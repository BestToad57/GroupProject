using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupProject.Code.Models
{
    public class Comment
    {
        [Key]
        public int CommentID { get; set; }
        
        [Required]
        public int EpisodeID { get; set; }
        
        [Required]
        public string UserID { get; set; } = "";
        
        [Required]
        [StringLength(1000)]
        public string CommentText { get; set; } = "";
        
        public DateTime CommentDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("EpisodeID")]
        public virtual Episode? Episode { get; set; }
    }
}