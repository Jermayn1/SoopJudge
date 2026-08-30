using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Transfer.Interfaces;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.DTOs.Transfer.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    // Der gesamte Aufgabenbestand als eine Datei heraus und wieder herein.
    //
    // Gedacht als Deployment-Weg: zuhause pflegen, als Datei auf den Server
    // bringen. Abgaben und Auswertungen sind bewusst nicht dabei.
    [ApiController]
    [Authorize]
    [Route("api/admin/transfer")]
    public class AdminTransferController : ControllerBase
    {
        // 20 MB. Der Rumpf besteht fast nur aus Java-Quelltext, das ist reichlich.
        // Ausdrücklich gesetzt, weil Kestrels stiller Standard von 30 MB keine
        // bewusste Grenze ist.
        private const int MaxRequestBytes = 20 * 1024 * 1024;

        private readonly ITaskTransferService _transferService;

        public AdminTransferController(ITaskTransferService transferService)
        {
            _transferService = transferService;
        }

        [HttpGet("export")]
        [ProducesResponseType<TaskBundleDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskBundleDto>> Export(CancellationToken cancellationToken)
        {
            var result = await _transferService.ExportAsync(cancellationToken);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Rechnet durch, was passieren würde, und schreibt nichts.
        //
        // Der Rumpf kommt als JSON und nicht als Datei-Upload: das Frontend liest
        // die Datei selbst und parst sie. Ein kaputtes JSON fällt damit schon
        // dort mit einer Meldung auf, statt als 400 aus dem Modelbinder zu kommen.
        [HttpPost("import/preview")]
        [RequestSizeLimit(MaxRequestBytes)]
        [ProducesResponseType<ImportReportDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImportReportDto>> Preview(
            [FromBody] ImportRequestDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _transferService.PreviewAsync(dto.Bundle, dto.Mode, cancellationToken);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Fehler in der Datei kommen NICHT als 400 zurück, sondern als Bericht
        // mit Status 200: es sind keine kaputten Aufrufe, sondern ein Befund
        // über den Inhalt - und davon will der Aufrufer alle sehen, nicht den
        // ersten als Fehlermeldung.
        [HttpPost("import")]
        [RequestSizeLimit(MaxRequestBytes)]
        [ProducesResponseType<ImportReportDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImportReportDto>> Import(
            [FromBody] ImportRequestDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _transferService.ImportAsync(dto.Bundle, dto.Mode, cancellationToken);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }
    }
}
