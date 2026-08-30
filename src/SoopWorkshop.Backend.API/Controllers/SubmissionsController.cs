using System.Text;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.API.Validation;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;

namespace SoopWorkshop.Backend.API.Controllers
{
    [ApiController]
    [Route("api/submissions")]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        // Nimmt alle (ein oder mehrere .java Dateien für die Aufgabe entgegen und startet die Auswertung
        //
        // Die Fehlerantwort ist bewusst ein fertiger deutscher Satz als Klartext, kein
        // Fehlerobjekt: sie wird dem Teilnehmer im Wortlaut angezeigt.
        [HttpPost]
        [RequestSizeLimit(SubmissionUploadLimits.MaxTotalSizeBytes)]
        [ProducesResponseType<SubmissionDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SubmissionDto>> Create([FromForm] Guid taskItemId, [FromForm] List<IFormFile> files)
        {
            var errors = SubmissionUploadValidator.Validate(files);
            if (errors.Count > 0)
                return BadRequest(string.Join(" ", errors));

            var fileContents = new List<(string FileName, string Content)>();

            foreach (var file in files)
            {
                // Zeichensatz fest auf UTF-8, sonst hängt das Ergebnis von der
                // Systemeinstellung des Servers ab — und Umlaute zerlegen sich.
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var content = await reader.ReadToEndAsync(HttpContext.RequestAborted);
                fileContents.Add((file.FileName, content));
            }

            var result = await _submissionService.CreateAsync(taskItemId, fileContents, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Gibt den Auswertungsstand zurück. Anders als /result unterscheidet dieser
        // Endpunkt zwischen "läuft noch", "fehlgeschlagen" und "nicht gefunden" —
        // sonst kann das Frontend einen Fehlschlag nicht erkennen und pollt endlos.
        [HttpGet("{id:guid}/status")]
        [ProducesResponseType<SubmissionStatusDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SubmissionStatusDto>> GetStatus(Guid id)
        {
            var result = await _submissionService.GetStatusAsync(id, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        // Gibt das Ausgabeergebnis einer Aubgabe zurück
        // Während der Auswertung, wird ein Fehler zurück gegeben
        [HttpGet("{id:guid}/result")]
        [ProducesResponseType<EvaluationResultDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EvaluationResultDto>> GetResult(Guid id)
        {
            var result = await _submissionService.GetResultAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }
    }
}
