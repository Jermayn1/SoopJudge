package soopjudge;

import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.Base64;
import java.util.function.Supplier;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.function.Executable;
import org.opentest4j.AssertionFailedError;

// Schreibt mit, was ein Test verglichen hat, auch wenn der Vergleich stimmt.
//
// JUnit nennt Erwartet und Erhalten nur, wenn ein Vergleich scheitert. Für eine
// bestandene Prüfung stünde im Ergebnis sonst bloß ein Haken. Der JUnitChecker
// leitet deshalb beim Übersetzen jeden Aufruf von assertEquals, assertTrue und
// Co. auf diese Klasse um. Sie gibt ihn unverändert an JUnit weiter und schreibt
// danach eine Zeile nach System.err, die der Launcher je Testfall in den Report
// legt.
//
// Die Überladungen sind aus org.junit.jupiter.api.Assertions (JUnit 6.1.3)
// abgelesen, nicht von Hand geschrieben. Nur mit genau denselben Signaturen
// entscheidet der Compiler bei assertEquals(5, einLong) so wie bei JUnit. Wer
// das JAR austauscht, gleicht die Liste ab; fehlt eine Überladung, übersetzt
// der Checker die Tests ohne Umleitung und es fehlen nur die Werte.
//
// Das Mitschreiben darf das Ergebnis eines Tests nie verändern. Deshalb läuft
// erst JUnit, und alles danach fängt seine eigenen Fehler.
public final class Werte {
    private static final String MARKE = "[soop-vergleich]";

    // Mehr braucht die Anzeige nicht, und eine Schleife über große Arrays soll
    // den Report nicht aufblähen.
    private static final int HOECHSTENS = 2000;

    private Werte() {
    }

    private static void pruefe(String methode, Runnable pruefung, Object erwartet, Object erhalten) {
        try {
            pruefung.run();
        } catch (AssertionError fehler) {
            melde(methode, false, erwartet, erhalten);
            throw fehler;
        }
        melde(methode, true, erwartet, erhalten);
    }

    private static <T extends Throwable> T wurf(String methode, Class<?> erwartet, Supplier<T> pruefung) {
        T geworfen;
        try {
            geworfen = pruefung.get();
        } catch (AssertionFailedError fehler) {
            Object ist = fehler.getActual() == null ? "keine Ausnahme" : kurzerName(fehler.getActual().getStringRepresentation());
            melde(methode, false, erwartet.getSimpleName(), ist);
            throw fehler;
        }
        melde(methode, true, erwartet.getSimpleName(), geworfen.getClass().getSimpleName());
        return geworfen;
    }

    private static String kurzerName(String typ) {
        int punkt = typ.lastIndexOf('.');
        return punkt < 0 ? typ : typ.substring(punkt + 1);
    }

    private static void melde(String methode, boolean bestanden, Object erwartet, Object erhalten) {
        try {
            StackWalker.StackFrame aufrufer = StackWalker.getInstance()
                .walk(frames -> frames.filter(f -> !f.getClassName().equals(Werte.class.getName())).findFirst())
                .orElse(null);
            String klasse = aufrufer == null ? "-" : aufrufer.getClassName();
            int zeile = aufrufer == null ? 0 : aufrufer.getLineNumber();
            System.err.println(MARKE + " " + methode + " " + (bestanden ? 1 : 0) + " " + klasse + " " + zeile
                + " " + kodiere(text(erwartet)) + " " + kodiere(text(erhalten)));
        } catch (RuntimeException | StackOverflowError ignoriert) {
            // Fehlt die Zeile, fehlen nur die Werte in der Anzeige. Eine
            // Ausnahme hier würde dagegen einen bestandenen Test durchfallen
            // lassen.
        }
    }

    private static String text(Object wert) {
        String text;
        try {
            if (wert == null) text = "null";
            else if (wert instanceof Object[] feld) text = Arrays.deepToString(feld);
            else if (wert instanceof int[] feld) text = Arrays.toString(feld);
            else if (wert instanceof long[] feld) text = Arrays.toString(feld);
            else if (wert instanceof double[] feld) text = Arrays.toString(feld);
            else if (wert instanceof float[] feld) text = Arrays.toString(feld);
            else if (wert instanceof char[] feld) text = Arrays.toString(feld);
            else if (wert instanceof byte[] feld) text = Arrays.toString(feld);
            else if (wert instanceof short[] feld) text = Arrays.toString(feld);
            else if (wert instanceof boolean[] feld) text = Arrays.toString(feld);
            else text = String.valueOf(wert);
        } catch (RuntimeException | StackOverflowError fehler) {
            // toString() der Abgabe darf werfen, ohne den Test zu beeinflussen.
            text = "(nicht darstellbar)";
        }
        return text.length() <= HOECHSTENS ? text : text.substring(0, HOECHSTENS) + " …";
    }

    private static String kodiere(String text) {
        return Base64.getEncoder().encodeToString(text.getBytes(StandardCharsets.UTF_8));
    }

    public static <T extends Throwable> T assertThrows(Class<T> p0, Executable p1) {
        return wurf("assertThrows", p0, () -> Assertions.assertThrows(p0, p1));
    }

    public static <T extends Throwable> T assertThrows(Class<T> p0, Executable p1, java.lang.String p2) {
        return wurf("assertThrows", p0, () -> Assertions.assertThrows(p0, p1, p2));
    }

    public static <T extends Throwable> T assertThrows(Class<T> p0, Executable p1, java.util.function.Supplier<java.lang.String> p2) {
        return wurf("assertThrows", p0, () -> Assertions.assertThrows(p0, p1, p2));
    }

    // Ab hier aus Assertions abgelesen.

    public static void assertArrayEquals(boolean[] p0, boolean[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(boolean[] p0, boolean[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(boolean[] p0, boolean[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(byte[] p0, byte[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(byte[] p0, byte[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(byte[] p0, byte[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(char[] p0, char[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(char[] p0, char[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(char[] p0, char[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(double[] p0, double[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(double[] p0, double[] p1, double p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(double[] p0, double[] p1, double p2, java.lang.String p3) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertArrayEquals(double[] p0, double[] p1, double p2, java.util.function.Supplier<java.lang.String> p3) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertArrayEquals(double[] p0, double[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(double[] p0, double[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(float[] p0, float[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(float[] p0, float[] p1, float p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(float[] p0, float[] p1, float p2, java.lang.String p3) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertArrayEquals(float[] p0, float[] p1, float p2, java.util.function.Supplier<java.lang.String> p3) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertArrayEquals(float[] p0, float[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(float[] p0, float[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(int[] p0, int[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(int[] p0, int[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(int[] p0, int[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(java.lang.Object[] p0, java.lang.Object[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(java.lang.Object[] p0, java.lang.Object[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(java.lang.Object[] p0, java.lang.Object[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(long[] p0, long[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(long[] p0, long[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(long[] p0, long[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(short[] p0, short[] p1) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1), p0, p1);
    }

    public static void assertArrayEquals(short[] p0, short[] p1, java.lang.String p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertArrayEquals(short[] p0, short[] p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertArrayEquals", () -> Assertions.assertArrayEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(byte p0, byte p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(byte p0, byte p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(byte p0, byte p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(byte p0, java.lang.Byte p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(byte p0, java.lang.Byte p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(byte p0, java.lang.Byte p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(char p0, char p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(char p0, char p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(char p0, char p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(char p0, java.lang.Character p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(char p0, java.lang.Character p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(char p0, java.lang.Character p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(double p0, double p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(double p0, double p1, double p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(double p0, double p1, double p2, java.lang.String p3) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertEquals(double p0, double p1, double p2, java.util.function.Supplier<java.lang.String> p3) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertEquals(double p0, double p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(double p0, double p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(double p0, java.lang.Double p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(double p0, java.lang.Double p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(double p0, java.lang.Double p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(float p0, float p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(float p0, float p1, float p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(float p0, float p1, float p2, java.lang.String p3) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertEquals(float p0, float p1, float p2, java.util.function.Supplier<java.lang.String> p3) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2, p3), p0, p1);
    }

    public static void assertEquals(float p0, float p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(float p0, float p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(float p0, java.lang.Float p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(float p0, java.lang.Float p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(float p0, java.lang.Float p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(int p0, int p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(int p0, int p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(int p0, int p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(int p0, java.lang.Integer p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(int p0, java.lang.Integer p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(int p0, java.lang.Integer p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Byte p0, byte p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Byte p0, byte p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Byte p0, byte p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Byte p0, java.lang.Byte p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Byte p0, java.lang.Byte p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Byte p0, java.lang.Byte p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Character p0, char p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Character p0, char p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Character p0, char p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Character p0, java.lang.Character p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Character p0, java.lang.Character p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Character p0, java.lang.Character p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Double p0, double p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Double p0, double p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Double p0, double p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Double p0, java.lang.Double p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Double p0, java.lang.Double p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Double p0, java.lang.Double p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Float p0, float p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Float p0, float p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Float p0, float p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Float p0, java.lang.Float p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Float p0, java.lang.Float p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Float p0, java.lang.Float p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Integer p0, int p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Integer p0, int p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Integer p0, int p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Integer p0, java.lang.Integer p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Integer p0, java.lang.Integer p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Integer p0, java.lang.Integer p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Long p0, java.lang.Long p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Long p0, java.lang.Long p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Long p0, java.lang.Long p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Long p0, long p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Long p0, long p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Long p0, long p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Object p0, java.lang.Object p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Object p0, java.lang.Object p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Object p0, java.lang.Object p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Short p0, java.lang.Short p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Short p0, java.lang.Short p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Short p0, java.lang.Short p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Short p0, short p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(java.lang.Short p0, short p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(java.lang.Short p0, short p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(long p0, java.lang.Long p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(long p0, java.lang.Long p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(long p0, java.lang.Long p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(long p0, long p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(long p0, long p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(long p0, long p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(short p0, java.lang.Short p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(short p0, java.lang.Short p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(short p0, java.lang.Short p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(short p0, short p1) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1), p0, p1);
    }

    public static void assertEquals(short p0, short p1, java.lang.String p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertEquals(short p0, short p1, java.util.function.Supplier<java.lang.String> p2) {
        pruefe("assertEquals", () -> Assertions.assertEquals(p0, p1, p2), p0, p1);
    }

    public static void assertFalse(boolean p0) {
        pruefe("assertFalse", () -> Assertions.assertFalse(p0), false, p0);
    }

    public static void assertFalse(boolean p0, java.lang.String p1) {
        pruefe("assertFalse", () -> Assertions.assertFalse(p0, p1), false, p0);
    }

    public static void assertFalse(boolean p0, java.util.function.Supplier<java.lang.String> p1) {
        pruefe("assertFalse", () -> Assertions.assertFalse(p0, p1), false, p0);
    }

    public static void assertFalse(java.util.function.BooleanSupplier p0) {
        boolean wert = p0.getAsBoolean();
        pruefe("assertFalse", () -> Assertions.assertFalse(wert), false, wert);
    }

    public static void assertFalse(java.util.function.BooleanSupplier p0, java.lang.String p1) {
        boolean wert = p0.getAsBoolean();
        pruefe("assertFalse", () -> Assertions.assertFalse(wert, p1), false, wert);
    }

    public static void assertFalse(java.util.function.BooleanSupplier p0, java.util.function.Supplier<java.lang.String> p1) {
        boolean wert = p0.getAsBoolean();
        pruefe("assertFalse", () -> Assertions.assertFalse(wert, p1), false, wert);
    }

    public static void assertNull(java.lang.Object p0) {
        pruefe("assertNull", () -> Assertions.assertNull(p0), null, p0);
    }

    public static void assertNull(java.lang.Object p0, java.lang.String p1) {
        pruefe("assertNull", () -> Assertions.assertNull(p0, p1), null, p0);
    }

    public static void assertNull(java.lang.Object p0, java.util.function.Supplier<java.lang.String> p1) {
        pruefe("assertNull", () -> Assertions.assertNull(p0, p1), null, p0);
    }

    public static void assertTrue(boolean p0) {
        pruefe("assertTrue", () -> Assertions.assertTrue(p0), true, p0);
    }

    public static void assertTrue(boolean p0, java.lang.String p1) {
        pruefe("assertTrue", () -> Assertions.assertTrue(p0, p1), true, p0);
    }

    public static void assertTrue(boolean p0, java.util.function.Supplier<java.lang.String> p1) {
        pruefe("assertTrue", () -> Assertions.assertTrue(p0, p1), true, p0);
    }

    public static void assertTrue(java.util.function.BooleanSupplier p0) {
        boolean wert = p0.getAsBoolean();
        pruefe("assertTrue", () -> Assertions.assertTrue(wert), true, wert);
    }

    public static void assertTrue(java.util.function.BooleanSupplier p0, java.lang.String p1) {
        boolean wert = p0.getAsBoolean();
        pruefe("assertTrue", () -> Assertions.assertTrue(wert, p1), true, wert);
    }

    public static void assertTrue(java.util.function.BooleanSupplier p0, java.util.function.Supplier<java.lang.String> p1) {
        boolean wert = p0.getAsBoolean();
        pruefe("assertTrue", () -> Assertions.assertTrue(wert, p1), true, wert);
    }
}
