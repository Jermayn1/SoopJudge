namespace SoopWorkshop.Backend.Application.Common
{
    // Warum ein Fehlschlag nicht gleich ein Fehlschlag ist.
    //
    // Ohne diese Unterscheidung bildet die API jeden Fehlschlag auf denselben
    // Statuscode ab. Genau das ist bei ToggleVisibility passiert: "die Aufgabe
    // hat keine JUnit-Datei" kam als 404 heraus, obwohl es die Aufgabe gibt.
    // Im Frontend landete die Meldung damit als notFound - dieselbe
    // Zusammenlegung, gegen die ApiResult gebaut wurde.
    public enum ResultFailure
    {
        // Die Anfrage passt nicht zum Zustand der Daten. Wird zu 400.
        Invalid = 0,

        // Das Angefragte gibt es nicht. Wird zu 404.
        NotFound = 1
    }

    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        // Nur bei IsSuccess == false aussagekräftig.
        public ResultFailure Failure { get; private set; }

        private Result() { }

        public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };

        // Bleibt der Standard, damit bestehende Aufrufer unverändert gelten:
        // ein Fehlschlag ohne nähere Angabe ist eine ungültige Anfrage.
        public static Result<T> Fail(string error) =>
            new() { IsSuccess = false, ErrorMessage = error, Failure = ResultFailure.Invalid };

        public static Result<T> NotFound(string error) =>
            new() { IsSuccess = false, ErrorMessage = error, Failure = ResultFailure.NotFound };
    }
}
