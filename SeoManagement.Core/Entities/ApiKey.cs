using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class ApiKey
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(50)]
		public string ServiceName { get; set; }

		[Required]
		[StringLength(200)]
		public string EncryptedKeyValue { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		public DateTime? LastUsedDate { get; set; }

		public DateTime? ExpiryDate { get; set; }
	}
}
