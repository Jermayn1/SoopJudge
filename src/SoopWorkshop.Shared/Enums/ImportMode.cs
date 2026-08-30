using System.Text.Json.Serialization;

namespace SoopWorkshop.Shared.Enums
{
    // Wie ein Import mit dem vorhandenen Bestand umgeht.
    //
    // Über die Leitung als Zeichenkette, und der Konverter steht hier am Typ und
    // nicht in AddJsonOptions: eine globale Registrierung wirkt nur zur Laufzeit,
    // der OpenAPI-Erzeuger dagegen liest den Typ. Stünde er nur global, schickte
    // die API "Merge", während das erzeugte Schema - und damit die daraus
    // erzeugten TypeScript-Typen - eine Zahl behauptet.
    [JsonConverter(typeof(JsonStringEnumConverter<ImportMode>))]
    public enum ImportMode
    {
        // Was dieselbe Id hat, wird aktualisiert; alles Neue kommt dazu. Nichts
        // wird gelöscht - eine zuhause gelöschte Aufgabe bleibt auf dem Server
        // also bestehen.
        Merge,

        // Der Bestand wird geleert, danach ist die Datei die Wahrheit.
        //
        // Achtung: das Löschen einer Kategorie nimmt per Cascade alles mit, was
        // darunter hängt - einschließlich der Abgaben der Teilnehmer und ihrer
        // Auswertungen. Der Bericht nennt die Zahl vorher.
        Replace
    }
}
