using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Auth.Requests
{
    // Anmeldung am Admin-Bereich. Nur ein Passwort, kein Benutzername: der
    // Workshop hat genau einen Betreuer, eine Benutzerverwaltung wäre Aufwand
    // ohne Gegenwert.
    public class AdminLoginDto
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
