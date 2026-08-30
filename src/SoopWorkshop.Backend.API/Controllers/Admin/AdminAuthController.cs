using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.API.Configuration;
using SoopWorkshop.Shared.DTOs.Auth;
using SoopWorkshop.Shared.DTOs.Auth.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly AdminOptions _options;
        private readonly ILogger<AdminAuthController> _logger;

        public AdminAuthController(IOptions<AdminOptions> options, ILogger<AdminAuthController> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        // Prüft das Passwort und setzt bei Erfolg das Anmelde-Cookie.
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<string>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Login([FromBody] AdminLoginDto dto)
        {
            if (!IsCorrectPassword(dto.Password))
            {
                // Ohne Protokollierung bliebe ein Durchprobieren unsichtbar.
                // Das Passwort selbst gehört dabei nicht ins Log.
                _logger.LogWarning("Fehlgeschlagene Anmeldung im Admin-Bereich.");
                return Unauthorized("Das Passwort stimmt nicht.");
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "Admin")],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            _logger.LogInformation("Anmeldung im Admin-Bereich erfolgreich.");

            return NoContent();
        }

        // Bewusst ohne [Authorize]: Abmelden soll auch dann 204 liefern, wenn
        // das Cookie bereits abgelaufen ist. Ein 401 an dieser Stelle würde
        // dem Frontend einen Fehler melden für etwas, das schon erledigt ist.
        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }

        // Das Frontend fragt hier beim Start, ob es die Anmeldung zeigen muss.
        // Ohne diesen Endpunkt bliebe ihm nur, irgendeinen Admin-Aufruf zu
        // versuchen und aus dessen Fehlschlag zu raten.
        [HttpGet("session")]
        [Authorize]
        [ProducesResponseType<AdminSessionDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<AdminSessionDto> Session() =>
            Ok(new AdminSessionDto { IsAuthenticated = true });

        // Vergleich in konstanter Zeit statt mit ==. Ein Zeichenkettenvergleich
        // bricht beim ersten Unterschied ab; aus den Laufzeitunterschieden lässt
        // sich ein Passwort zeichenweise erraten.
        //
        // Die Länge verrät FixedTimeEquals weiterhin (ungleich lange Eingaben
        // sind sofort false). Für ein workshop-internes Passwort ist das
        // vertretbar - es wäre sonst ein Hashvergleich nötig.
        private bool IsCorrectPassword(string candidate) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(candidate),
                Encoding.UTF8.GetBytes(_options.Password));
    }
}
