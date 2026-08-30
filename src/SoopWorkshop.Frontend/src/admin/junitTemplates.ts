// Vorlagen für JUnit-Testdateien.
//
// Bewusst KEINE Vorlage für simulierte Eingaben über System.setIn. Eine im
// Testcode versteckte Eingabe kann die Anzeige nicht kennen — der Teilnehmer
// sähe „Erwartet 7", ohne zu erfahren, womit gerechnet wurde. Eingaben gehören
// deshalb in Konsolen-Testfälle: JUnit prüft Methoden, Konsolen-Testfälle
// prüfen das Programm.
// Faustregel: JUnit prüft Methoden, Konsolen-Testfälle prüfen das Programm.

export type JUnitTemplate = {
  id: string
  titel: string
  /** Wann man diese Vorlage nimmt. */
  wofuer: string
  /** Vorschlag für den Dateinamen. */
  dateiname: string
  inhalt: string
}

export const JUNIT_TEMPLATES: JUnitTemplate[] = [
  {
    id: 'konsole',
    titel: 'Konsolenausgabe prüfen',
    wofuer:
      'Frühe Aufgaben, in denen der Teilnehmer nur eine main schreibt. Funktioniert nur ohne Eingabe.',
    dateiname: 'MainTest.java',
    inhalt: `import static org.junit.jupiter.api.Assertions.assertEquals;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * Prüft, was das Programm auf der Konsole ausgibt.
 *
 * Drei Dinge stehen hier absichtlich so:
 *
 *  1. Der PrintStream bekommt StandardCharsets.UTF_8 ausdrücklich mit. Ohne
 *     das schreibt er in der Codepage des Systems (unter Windows Cp1252) und
 *     Umlaute kommen zerlegt an.
 *  2. System.setOut wird in @AfterEach zurückgesetzt, sonst beeinflussen sich
 *     die Testmethoden gegenseitig.
 *  3. Statische Felder der Abgabe überleben zwischen den Testmethoden - alle
 *     laufen in derselben JVM.
 */
class MainTest {

    private final PrintStream originalOut = System.out;
    private ByteArrayOutputStream ausgabe;

    @BeforeEach
    void ausgabeUmleiten() {
        ausgabe = new ByteArrayOutputStream();
        System.setOut(new PrintStream(ausgabe, true, StandardCharsets.UTF_8));
    }

    @AfterEach
    void ausgabeZuruecksetzen() {
        System.setOut(originalOut);
    }

    @Test
    @DisplayName("Das Programm gibt den Gruß aus")
    void gibtGrussAus() {
        Main.main(new String[0]);

        assertEquals("Hallo Soop", ausgabe.toString(StandardCharsets.UTF_8).trim());
    }
}
`,
  },
  {
    id: 'methode',
    titel: 'Rückgabewert einer Methode prüfen',
    wofuer:
      'Sobald der Teilnehmer eigene Methoden schreibt. Der Wert kommt zurück, statt ausgegeben zu werden.',
    dateiname: 'RechnerTest.java',
    inhalt: `import static org.junit.jupiter.api.Assertions.assertEquals;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * Prüft eine Methode über ihren Rückgabewert.
 *
 * Der @DisplayName ist nicht Zierde: er landet im XML-Report und ist genau der
 * Text, den der Teilnehmer im Ergebnis liest. "addiereZweiPositiveZahlen" hilft
 * ihm nicht, "Die Methode addiere addiert zwei positive Zahlen" schon.
 *
 * Bewusst ohne Eingabesimulation: was das Programm mit einer Eingabe macht,
 * gehört in einen Konsolen-Testfall. Dort erscheint die Eingabe im Ergebnis.
 */
class RechnerTest {

    @Test
    @DisplayName("Die Methode addiere addiert zwei positive Zahlen")
    void addiereZweiPositiveZahlen() {
        assertEquals(5, Main.addiere(2, 3));
    }

    @Test
    @DisplayName("Die Methode addiere rechnet auch mit negativen Zahlen")
    void addiereMitNegativerZahl() {
        assertEquals(-1, Main.addiere(2, -3));
    }
}
`,
  },
  {
    id: 'objekte',
    titel: 'Mehrere Klassen prüfen',
    wofuer:
      'OOP-Aufgaben, in denen Klassen voneinander abhängen. Alle abgegebenen Dateien werden zusammen übersetzt.',
    dateiname: 'BankTest.java',
    inhalt: `import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * Prüft mehrere Klassen, die voneinander abhängen.
 *
 * Dass das geht, ist keine Zusatzeinstellung: alle abgegebenen Dateien landen
 * in einem Arbeitsverzeichnis und gehen zusammen durch javac, und die Testdatei
 * wird danach gegen genau diese Klassen übersetzt.
 *
 * Jeder Test baut sich seine Objekte selbst. Statische Felder überleben
 * zwischen Testmethoden - geteilter Zustand macht die Reihenfolge der Tests
 * plötzlich bedeutsam.
 */
class BankTest {

    @Test
    @DisplayName("Ein Konto kennt den Namen seines Inhabers")
    void kontoKenntInhaber() {
        Kunde kunde = new Kunde("Anna");
        Konto konto = new Konto(kunde);

        assertEquals("Anna", konto.getInhaber().getName());
    }

    @Test
    @DisplayName("Die Methode einzahlen erhöht den Kontostand")
    void einzahlenErhoehtStand() {
        Konto konto = new Konto(new Kunde("Ben"));

        konto.einzahlen(50.0);

        assertEquals(50.0, konto.getStand(), 0.001);
    }

    @Test
    @DisplayName("Die Methode abheben lehnt ab, wenn das Konto nicht gedeckt ist")
    void abhebenOhneDeckung() {
        Konto konto = new Konto(new Kunde("Cem"));
        konto.einzahlen(20.0);

        assertFalse(konto.abheben(50.0), "abheben soll false liefern, wenn zu wenig Geld da ist");
        assertEquals(20.0, konto.getStand(), 0.001, "Ein abgelehntes Abheben darf den Stand nicht ändern");
    }

    @Test
    @DisplayName("Die Methode abheben gibt das Geld heraus, wenn das Konto gedeckt ist")
    void abhebenBeiDeckung() {
        Konto konto = new Konto(new Kunde("Dana"));
        konto.einzahlen(100.0);

        assertTrue(konto.abheben(40.0));
        assertEquals(60.0, konto.getStand(), 0.001);
    }
}
`,
  },
  {
    id: 'ausnahme',
    titel: 'Ausnahme erwarten',
    wofuer:
      'Wenn eine Methode bei unsinnigen Werten abbrechen soll, statt still etwas Falsches zu tun.',
    dateiname: 'PruefungTest.java',
    inhalt: `import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/*
 * Prüft, dass eine Methode bei unzulässigen Werten abbricht.
 *
 * assertThrows gibt die gefangene Ausnahme zurück - so lässt sich zusätzlich
 * prüfen, ob die Meldung etwas taugt. Der Teilnehmer soll ja nicht nur
 * irgendetwas werfen.
 *
 * Wichtig: der Aufruf steht INNERHALB der Lambda. Wer ihn davor schreibt,
 * bricht den Test ab, statt die Ausnahme zu prüfen.
 */
class PruefungTest {

    @Test
    @DisplayName("Die Methode wurzel lehnt negative Zahlen ab")
    void wurzelAusNegativerZahl() {
        IllegalArgumentException fehler = assertThrows(
                IllegalArgumentException.class,
                () -> Main.wurzel(-1));

        assertEquals("Negative Zahlen haben keine Wurzel.", fehler.getMessage());
    }

    @Test
    @DisplayName("Die Methode wurzel rechnet bei gültigen Zahlen richtig")
    void wurzelAusPositiverZahl() {
        assertEquals(3.0, Main.wurzel(9), 0.001);
    }
}
`,
  },
]
