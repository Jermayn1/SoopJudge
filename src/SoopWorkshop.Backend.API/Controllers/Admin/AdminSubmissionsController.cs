using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    // Übersicht über die abgegebenen Lösungen. Sie beantwortet eine andere Frage
    // als der Probelauf: nicht "bewertet die Aufgabe richtig", sondern "wo stehen
    // die Teilnehmer gerade" — und ist damit der einzige Blick auf den laufenden
    // Workshop.
    [ApiController]
    [Authorize]
    [Route("api/admin/submissions")]
    public class AdminSubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public AdminSubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        // Seitenweise, neueste zuerst. Filter sind optional.
        //
        // Die Zeilen tragen keine Auswertungsdetails - dafür verlinkt das
        // Panel auf /abgaben/{id}, also auf dieselbe Ergebnisanzeige, die der
        // Teilnehmer sieht. Eine zweite, nachgebaute Anzeige liefe beim ersten
        // Umbau auseinander (dieselbe Entscheidung wie bei der Vorschau in 5.5).
        [HttpGet]
        [ProducesResponseType<SubmissionPageDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SubmissionPageDto>> GetPage(
            [FromQuery] Guid? taskItemId,
            [FromQuery] SubmissionStatus? status,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 25)
        {
            var result = await _submissionService.GetPageAsync(
                taskItemId, status, skip, take, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Eine einzelne Abgabe samt ihrer .java-Dateien.
        //
        // Anders als die Liste trägt diese Antwort die vollen Dateiinhalte: sie
        // beantwortet eine Abgabe auf einmal, nicht hunderte Zeilen. Damit lässt
        // sich im Workshop über eine konkrete Lösung sprechen, ohne dass jemand
        // seinen Bildschirm teilen muss.
        //
        // Die Bewertung steht bewusst NICHT hier drin — die holt die Seite über
        // /status und /result, also über dieselben Endpunkte wie der Teilnehmer.
        [HttpGet("{id:guid}")]
        [ProducesResponseType<SubmissionDetailDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SubmissionDetailDto>> GetDetail(Guid id)
        {
            var result = await _submissionService.GetDetailAsync(id, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }
    }
}
