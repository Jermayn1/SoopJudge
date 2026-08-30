using System.Text.Json.Serialization;

namespace SoopWorkshop.Shared.Enums
{
    // Womit eine Aufgabe geprüft wird. Steuert die Auswertung: bei ConsoleOnly
    // laufen hinterlegte JUnit-Dateien nicht, auch wenn welche da sind. So fällt
    // ein falsch gesetzter Modus beim Anlegen auf, statt die Aufgabe still milder
    // zu bewerten.
    //
    // Als int in der Datenbank - neue Werte nur anhängen. Über die Leitung als
    // Zeichenkette, siehe Hinweis in EvaluationCategory.
    [JsonConverter(typeof(JsonStringEnumConverter<EvaluationMode>))]
    public enum EvaluationMode
    {
        // Nur Konsolen-Testfälle: Eingabe rein, Ausgabe vergleichen.
        ConsoleOnly,

        // Nur Aufgaben-Unittests (JUnit).
        UnitTestOnly,

        // Beides.
        Both
    }
}
