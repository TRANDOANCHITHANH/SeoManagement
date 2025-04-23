using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Models.ViewModels
{
	public class KeywordViewModel
	{
		public int KeywordID { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn dự án.")]
		public int ProjectID { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập từ khóa.")]
		[StringLength(100, ErrorMessage = "Từ khóa không được dài quá 100 ký tự.")]
		public string KeywordName { get; set; }

		[Range(0, int.MaxValue, ErrorMessage = "Lưu lượng tìm kiếm phải là số không âm.")]
		public int? SearchVolume { get; set; }

		[Range(0, 1, ErrorMessage = "Độ cạnh tranh phải từ 0 đến 1.")]
		public float? Competition { get; set; }

		[Range(1, 100, ErrorMessage = "Thứ hạng phải từ 1 đến 100.")]
		public int? CurrentRank { get; set; }

		[StringLength(50, ErrorMessage = "Ý định tìm kiếm không được dài quá 50 ký tự.")]
		public string SearchIntent { get; set; }

		public DateTime CreatedDate { get; set; }

		public List<KeywordHIstoryViewModel> KeywordHistories { get; set; } = new List<KeywordHIstoryViewModel>();
	}
}
