# Hinweise zu Drittanbieter-Software

Der Quelltext dieses Projekts steht unter der [MIT-Lizenz](LICENSE). Daneben
wird eine fremde Komponente unverändert mitgeliefert, für die eigene
Lizenzbedingungen gelten.

## junit-platform-console-standalone 6.1.3

| | |
|---|---|
| Datei im Repository | `lib/junit-platform-console-standalone-6.1.3.jar` |
| Herausgeber | JUnit Team, <https://junit.org> |
| Maven-Koordinaten | `org.junit.platform:junit-platform-console-standalone:6.1.3` |
| Lizenz | [Eclipse Public License 2.0](https://www.eclipse.org/legal/epl-2.0/) |
| Zustand | unverändert übernommen, nicht modifiziert |

Die Datei liegt im Repository, damit der Build ohne Netzzugang auskommt und im
Workshop nichts nachgeladen werden muss. Sie wird **nicht als Bibliothek
eingebunden**, sondern zur Laufzeit als eigener Prozess gestartet
(`java -jar …`), um die JUnit-Tests einer Abgabe auszuführen.

### Enthaltene Unterkomponenten

Das Standalone-JAR bündelt weitere Bibliotheken mit je eigener Lizenz:

| Komponente | Lizenz |
|---|---|
| JUnit 4 | Eclipse Public License 1.0 |
| Hamcrest | BSD License |
| picocli | Apache License 2.0 |
| FastCSV | MIT License |
| Open Test Reporting | Apache License 2.0 |

### Vollständige Lizenztexte

Sie liegen unverändert im JAR unter `META-INF/` und werden mit jeder Kopie
dieses Repositorys und jedem daraus gebauten Container weitergegeben:

| Datei im JAR | Inhalt |
|---|---|
| `META-INF/LICENSE.md` | Eclipse Public License 2.0 |
| `META-INF/LICENSE-notice.md` | Hinweis auf die Unterkomponenten |
| `META-INF/LICENSE-junit4` | JUnit 4 |
| `META-INF/LICENSE-hamcrest` | Hamcrest |
| `META-INF/LICENSE-picocli.md` | picocli |
| `META-INF/LICENSE-fastcsv` | FastCSV |
| `META-INF/LICENSE-open-test-reporting.md` | Open Test Reporting |

Ansehen lassen sie sich ohne Entpacken, zum Beispiel:

```bash
unzip -p lib/junit-platform-console-standalone-6.1.3.jar META-INF/LICENSE.md
```
