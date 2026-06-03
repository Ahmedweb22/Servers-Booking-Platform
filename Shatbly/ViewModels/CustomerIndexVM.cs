namespace Shatbly.ViewModels
{
    public class CustomerIndexVM
    {

        public IEnumerable<WorkerProfile> Workers { get; set; }
        public IEnumerable<int> FavoriteWorkerIds { get; set; }
        public IEnumerable<ServiceCategory> Categories { get; set; }
        public IEnumerable<Banner>? Banners { get; set; }
    }
}
