
using IFileService = Shatbly.Services.Portfolio.IFilePortfolioService;
using Shatbly.Services.Portfolio;
namespace Shatbly.Services.Portfolio
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFilePortfolioService _fileService;

        public PortfolioService(IUnitOfWork unitOfWork, IFilePortfolioService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<IReadOnlyList<PortfolioVM>> GetWorkerPortfolioAsync(string userId)
        {
            var worker = await GetWorkerProfileAsync(userId);

            if (worker is null)
            {
                return [];
            }

            var media = await _unitOfWork.PortfolioMedia.GetAsync(
                x => x.WorkerId == worker.Id,
                tracking: false);

            return media
                .OrderByDescending(x => x.Id)
                .Select(x => new PortfolioVM
                {
                    Id = x.Id,
                    FilePath = x.MediaUrl,
                    FileType = x.MediaType.ToString()
                })
                .ToList();
        }

        public async Task<PortfolioOperationResult> UploadMediaAsync(string userId, UploadPortfolioVM model)
        {
            var worker = await GetWorkerProfileAsync(userId);

            if (worker is null)
            {
                return PortfolioOperationResult.Failure("Worker profile was not found.");
            }

            if (model.File is null)
            {
                return PortfolioOperationResult.Failure("Please choose a file.");
            }

            var uploadResult = await _fileService.UploadPortfolioMediaAsync(model.File, model.FileType);

            if (!uploadResult.Succeeded)
            {
                return PortfolioOperationResult.Failure(uploadResult.ErrorMessage!);
            }

            var media = new PortfolioMedia
            {
                WorkerId = worker.Id,
                MediaUrl = uploadResult.FilePath!,
                MediaType = Enum.Parse<MediaType>(model.FileType),
                Caption = model.Caption ?? string.Empty
            };


            await _unitOfWork.PortfolioMedia.CreateAsync(media);
            await _unitOfWork.CommitAsync();

            return PortfolioOperationResult.Success();
        }

        public async Task<PortfolioOperationResult> DeleteMediaAsync(string userId, int mediaId)
        {
            var worker = await GetWorkerProfileAsync(userId);

            if (worker is null)
            {
                return PortfolioOperationResult.Failure("Worker profile was not found.");
            }

            var media = await _unitOfWork.PortfolioMedia.GetOneAsync(
                x => x.Id == mediaId && x.WorkerId == worker.Id);

            if (media is null)
            {
                return PortfolioOperationResult.Failure("Media item was not found.");
            }

            _fileService.DeleteFile(media.MediaUrl);

            _unitOfWork.PortfolioMedia.Delete(media);
            await _unitOfWork.CommitAsync();

            return PortfolioOperationResult.Success();
        }

        private async Task<WorkerProfile?> GetWorkerProfileAsync(string userId)
        {
            return await _unitOfWork.WorkerProfiles.GetOneAsync(
                x => x.UserId == userId,
                tracking: false);
        }
    }
}
