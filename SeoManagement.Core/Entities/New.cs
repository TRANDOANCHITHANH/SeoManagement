using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class New
	{
		[Key]
		public int NewsID { get; set; }
		[Required]
		public string Title { get; set; }
		[Required]
		public string Content { get; set; }
		public DateTime CreatedDate { get; set; }
		public bool IsPublished { get; set; }
	}
}
