using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Transfer.Interfaces
{
    // Bestand als Datei heraus und wieder herein.
    //
    // Die Umsetzung liegt in Infrastructure und benutzt den DbContext direkt -
    // nicht die Repositories. Deren Einzel-Commits tun genau das Gegenteil
    // dessen, was ein Import braucht: der muss ganz oder gar nicht passieren.
    public interface ITaskTransferService
    {
        Task<Result<TaskBundleDto>> ExportAsync(CancellationToken cancellationToken);

        // Rechnet durch, was passieren würde, ohne etwas zu schreiben.
        Task<Result<ImportReportDto>> PreviewAsync(
            TaskBundleDto bundle,
            ImportMode mode,
            CancellationToken cancellationToken);

        Task<Result<ImportReportDto>> ImportAsync(
            TaskBundleDto bundle,
            ImportMode mode,
            CancellationToken cancellationToken);
    }
}
