namespace SeoManagement.Web.Models.ViewModels
{
	public class BacklinkCheckViewModel
	{
		public int ProjectId { get; set; }
		public string ProjectName { get; set; }
		public string ProjectDescription { get; set; }
		public List<BacklinkResultViewModel> BacklinkResults { get; set; } = new List<BacklinkResultViewModel>();
	}
}
