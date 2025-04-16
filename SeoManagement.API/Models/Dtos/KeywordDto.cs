namespace SeoManagement.API.Models.Dtos
{
	public class KeywordDto
	{
		public int KeywordID { get; set; }
		public int ProjectID { get; set; }
		public string KeywordName { get; set; }
		public int? SearchVolume { get; set; }
		public float? Competition { get; set; }
		public int? CurrentRank { get; set; }
		public string SearchIntent { get; set; }
		public DateTime CreatedDate { get; set; }
	}
}
