using SeoManagement.Core.Entities;

namespace SeoManagement.Web.Models.ViewModels
{
	public class KeywordRankViewModel
	{
		public string Keyword { get; set; }
		public string Domain { get; set; }
		public int ProjectId { get; set; }
		public Keyword Rank { get; set; }
		public string Message { get; set; }
	}
}
