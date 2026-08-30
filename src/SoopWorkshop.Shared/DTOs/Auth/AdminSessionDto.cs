namespace SoopWorkshop.Shared.DTOs.Auth
{
    // Antwort auf GET api/admin/auth/session.
    //
    // Der Wert ist immer true - wer nicht angemeldet ist, bekommt 401 und gar
    // kein Objekt. Trotzdem ein Objekt statt eines leeren 200ers, damit der
    // OpenAPI-Vertrag beschreibt, was zurückkommt.
    public class AdminSessionDto
    {
        public bool IsAuthenticated { get; set; }
    }
}
