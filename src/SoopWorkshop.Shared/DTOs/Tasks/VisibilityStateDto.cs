namespace SoopWorkshop.Shared.DTOs.Tasks
{
    // Antwort der Sichtbarkeits-Endpunkte (PATCH .../visibility).
    //
    // Vorher stand hier ein anonymer Typ (new { isVisible = ... }). Für ein Frontend
    // in .NET war das gleichgültig, ein erzeugter API-Vertrag kann daraus aber kein
    // Schema ableiten — die Antwort wäre im Frontend als "unbekannt" angekommen.
    public class VisibilityStateDto
    {
        public bool IsVisible { get; set; }
    }
}
