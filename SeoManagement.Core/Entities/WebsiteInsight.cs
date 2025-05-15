using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class WebsiteInsight
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(500)]
		public string Domain { get; set; }

		[StringLength(200)]
		public string Title { get; set; }

		[StringLength(500)]
		public string Description { get; set; }

		[StringLength(100)]
		public string Category { get; set; }

		public DateTime SnapshotDate { get; set; }

		// Traffic Data
		public long? GlobalVisits { get; set; }
		public double? BounceRate { get; set; }
		public double? PagesPerVisit { get; set; }
		public double? TimeOnSite { get; set; }
		public double? SearchTrafficPercentage { get; set; }
		public double? DirectTrafficPercentage { get; set; }
		public double? ReferralTrafficPercentage { get; set; }
		public double? SocialTrafficPercentage { get; set; }
		public double? PaidReferralTrafficPercentage { get; set; }
		public double? MailTrafficPercentage { get; set; }

		// SEO Insights
		public string TopKeywordsJson { get; set; }
		public string TopCountrySharesJson { get; set; }
		public bool? IsDataFromGa { get; set; }
		// Rank Data
		public long? GlobalRank { get; set; }
		public string? CountryRankCountry { get; set; }
		public long? CountryRankValue { get; set; }
		public string? CategoryRankCategory { get; set; }
		public long? CategoryRankValue { get; set; }

		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }
	}
}