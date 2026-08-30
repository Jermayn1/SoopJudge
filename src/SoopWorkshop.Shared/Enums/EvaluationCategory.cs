using System.Text.Json.Serialization;

namespace SoopWorkshop.Shared.Enums
{
    // Die Werte liegen als int in der Datenbank. Neue Kategorien werden deshalb
    // ausschließlich angehängt - wird umsortiert, deutet das Altbestände um.
    //
    // Über die Leitung gehen sie dagegen als Zeichenkette. Der Konverter steht
    // bewusst am Typ und nicht nur global in AddJsonOptions: der OpenAPI-Erzeuger
    // liest den Typ, die globale Registrierung nur die Laufzeit. Stand beides nicht
    // im Einklang, verschickte die API "Easy", während der erzeugte Vertrag eine
    // Zahl versprach - und das Frontend hätte sich still darauf verlassen.
    [JsonConverter(typeof(JsonStringEnumConverter<EvaluationCategory>))]
    public enum EvaluationCategory
    {
        // Altlast: seit der Bewertungs-Engine v2 sind Zeichensatz und
        // Namenskonventionen Teilprüfungen unter CleanCode. Die beiden Werte
        // werden nicht mehr vergeben, bleiben aber stehen, damit frühere
        // Auswertungen weiterhin richtig gelesen werden. Nicht wiederverwenden.
        CharacterSet,
        NamingConventions,

        Compilability,
        CleanCode,

        // Altlast: Konsolen-Testfälle und Aufgaben-Unittests prüfen dasselbe -
        // ob das Programm tut, was die Aufgabe verlangt. Sie waren kurzzeitig
        // getrennte Kategorien; das war eine Doppelung in der Anzeige und in der
        // Gewichtung. Beide zahlen jetzt auf Functionality ein.
        TestCases,
        UnitTests,

        // Erfüllt das Programm die Aufgabe? Speist sich aus Konsolen-Testfällen
        // und JUnit-Testmethoden - welcher Weg genutzt wird, entscheidet die
        // Aufgabe über ihren EvaluationMode.
        Functionality,
    }
}
