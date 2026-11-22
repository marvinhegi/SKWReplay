# Projekt: SKWReplay – Windows Kamera-Aufnahme mit Zeitverzögerung

## 1. Projektziel

Entwickle eine moderne, einfach bedienbare Windows-Desktop-Anwendung namens SKWReplay.

Die Anwendung soll Videos über eine angeschlossene Kamera aufnehmen und die Live-Vorschau mit einer frei einstellbaren Verzögerung von 0 bis maximal 60 Sekunden anzeigen.

Die Aufnahme soll beliebig lange laufen können, beispielsweise 2 Stunden oder länger. Das vollständige Video muss nach Beenden der Aufnahme gespeichert werden können.

Die Anwendung soll möglichst einfach auf Windows 10 und Windows 11 ausführbar sein. Es soll keine komplizierte Installation oder Konfiguration erforderlich sein.

Das bereitgestellte Mockup dient als verbindliche visuelle Orientierung für das Design.

## 2. Verbindliche Technologien

Verwende folgende Technologien:

- **Programmiersprache:** C#
- **Framework:** .NET 10
- **Benutzeroberfläche:** WPF mit XAML
- **UI-Architektur:** MVVM
- **Design:** Modernes Fluent Design, dunkles Theme
- **Videoaufnahme:** Windows Media Foundation bevorzugt
- **Video-Encoding und -Speicherung:** FFmpeg
- **Bildverarbeitung:** OpenCvSharp, sofern für Zoom und Bildverarbeitung erforderlich
- **Videoformat:** MP4 mit H.264
- **Audioformat:** AAC
- **Architektur:** Modular, mit klarer Trennung zwischen UI, Kamera, Aufnahme und Videoverarbeitung.

Die Kameraerkennung soll alle vom Betriebssystem erkannten kompatiblen Kamerageräte erfassen.

FFmpeg soll nicht unnötig über die Benutzeroberfläche gesteuert werden. Die Videoverarbeitung muss in einer separaten Komponente gekapselt sein.

Wichtig:
Die Anwendung soll als Self-contained Windows-x64-Anwendung veröffentlicht werden. Das benötigte .NET-Runtime-Paket soll somit nicht separat installiert werden müssen.

Windows 10 und Windows 11 müssen explizit getestet werden. Insbesondere Windows 10 22H2 Home/Pro ist bei der Auswahl der APIs zu berücksichtigen.

## 3. Hauptfunktionen

### 3.1 Kamera auswählen

- Alle verfügbaren Kameras des Computers automatisch erkennen.
- Kameras in einem Dropdown-Menü anzeigen.
- Gerätenamen möglichst vollständig anzeigen.
- Standardmässig die erste verfügbare Kamera auswählen.
- Kamera wechseln können, solange keine Aufnahme läuft.
- Live-Vorschau der ausgewählten Kamera anzeigen.
- Bei nicht verfügbarer Kamera eine verständliche Fehlermeldung anzeigen.
- Nach erneutem Anschliessen einer Kamera soll diese nach einer Aktualisierung erkannt werden.

Beispiel:
- Logitech C920 HD Pro Webcam
- Integrierte Laptopkamera
- Weitere USB-Webcams

### 3.2 Verzögerung einstellen

Die Verzögerung muss zwischen 0 und 60 Sekunden eingestellt werden können.

Bedienung:
- Anzeige der aktuell eingestellten Verzögerung.
- Pfeil nach oben: Verzögerung erhöhen.
- Pfeil nach unten: Verzögerung reduzieren.
- Mindestwert: 0 Sekunden.
- Höchstwert: 60 Sekunden.
- Schrittweite: 1 Sekunde.

Die Verzögerung bestimmt, wie lange das Kamerabild zurückliegt, das in der Vorschau angezeigt wird.

Beispiel bei einer Verzögerung von 5 Sekunden:

Die Kamera nimmt um 12:00:00 ein Bild auf. Dieses Bild wird erst um 12:00:05 in der Vorschau angezeigt.

### 3.3 Aufnahme starten

Beim Klick auf «Aufnahme starten»:

1. Kameraaufnahme initialisieren.
2. Aufnahme sofort starten.
3. Videodaten fortlaufend auf der Festplatte zwischenspeichern.
4. Den Verzögerungspuffer aufbauen.
5. Einen Countdown anzeigen, bis die verzögerte Vorschau verfügbar ist.
6. Sobald der Puffer vollständig ist, die verzögerte Live-Vorschau anzeigen.

Während des Countdowns soll eine gut sichtbare Anzeige erscheinen.

Beispiel bei 5 Sekunden Verzögerung:

- 5 Sekunden
- 4 Sekunden
- 3 Sekunden
- 2 Sekunden
- 1 Sekunde
- Verzögerte Vorschau aktiv

Bei einer Verzögerung von 0 Sekunden entfällt der Countdown.

Wichtig: Die Aufnahme beginnt bereits beim Klick auf «Aufnahme starten». Der Countdown darf nicht dazu führen, dass die ersten Sekunden des Videos verloren gehen.

### 3.4 Aufnahme pausieren und fortsetzen

Während einer Aufnahme muss diese pausiert und wieder fortgesetzt werden können.

Beim Pausieren:
- Weitere Videodaten werden nicht aufgenommen.
- Die Aufnahmedauer wird angehalten.
- Die Vorschau kann eingefroren oder entsprechend gekennzeichnet werden.
- Bereits aufgenommene Daten bleiben erhalten.

Beim Fortsetzen:
- Aufnahme wird fortgesetzt.
- Verzögerungspuffer wird erneut aufgebaut.
- Countdown wird erneut angezeigt, sofern eine Verzögerung eingestellt ist.

Die Pausen sollen im endgültigen Video nicht als lange Standbilder oder leere Zeitabschnitte erscheinen. Die einzelnen Aufnahmeabschnitte sollen zu einem vollständigen Video zusammengeführt werden.

### 3.5 Aufnahme beenden

Beim Klick auf «Aufnahme beenden»:

- Aufnahme vollständig stoppen.
- Alle noch ausstehenden Videodaten verarbeiten.
- Video-Encoder und Dateizugriffe sauber abschliessen.
- Aufnahmezustand auf beendet setzen.
- Speicherfunktion freigeben.

Das Video darf dabei nicht beschädigt werden.

### 3.6 Aufnahme speichern

Nach Beenden der Aufnahme soll die Schaltfläche «Aufnahme speichern» verfügbar sein.

Beim Klick:
- Windows-Dateiauswahldialog öffnen.
- Speicherort auswählen lassen.
- Dateiname festlegen lassen.
- Video als MP4 speichern.
- Fortschritt beim Export anzeigen.
- Nach erfolgreichem Speichern eine Bestätigung anzeigen.

Der Benutzer muss das vollständige Video speichern können, unabhängig davon, ob die Aufnahme 5 Minuten, 2 Stunden oder länger gedauert hat.

Die Anwendung darf nicht nur die letzten 60 Sekunden speichern. Die Verzögerung ist ausschliesslich für die Vorschau zuständig.

Während einer Aufnahme müssen die Daten kontinuierlich auf der Festplatte gespeichert werden. Der gesamte Videoinhalt darf nicht im RAM gehalten werden.

Die Anwendung soll eine temporäre Aufnahmedatei verwenden und diese erst nach erfolgreicher Verarbeitung und Speicherung entfernen.

## 4. Videoverarbeitung und Speicherverwaltung

Dieser Bereich ist besonders wichtig.

Die Anwendung muss auch bei langen Aufnahmen stabil funktionieren.

Anforderungen:

- Keine vollständige Speicherung des Videos im Arbeitsspeicher.
- Kontinuierliches Schreiben der Videodaten auf die Festplatte.
- Ringpuffer für die verzögerte Vorschau.
- Ringpuffer darf maximal die eingestellte Verzögerung enthalten.
- Temporäre Aufnahme unabhängig vom Vorschaupuffer speichern.
- Aufnahmen mit einer Dauer von mindestens 2 Stunden müssen möglich sein.
- Keine künstliche maximale Aufnahmedauer.
- Sauberes Finalisieren der Videodatei.
- Speicherplatz vor Aufnahmebeginn prüfen.
- Bei unzureichendem Speicherplatz verständliche Warnung anzeigen.
- Fehler bei der Kamera oder beim Encoding abfangen.
- Möglichst geringe CPU- und RAM-Auslastung.

Hardwarebeschleunigtes Encoding soll verwendet werden, wenn verfügbar. Beispielsweise über Intel Quick Sync, NVIDIA NVENC oder AMD AMF. Wenn keine geeignete Hardware verfügbar ist, soll Software-Encoding verwendet werden.

Die Anwendung soll bei einem unerwarteten Programmabbruch möglichst wenig bereits aufgenommene Daten verlieren. Eine segmentierte temporäre Speicherung oder regelmässige Wiederherstellungspunkte sind dafür zu berücksichtigen.

## 5. Zoom und Touch-Bedienung

Die Kameravorschau muss Zoom unterstützen.

Anforderungen:

- Digitales Hineinzoomen in das Kamerabild.
- Herauszoomen.
- Touch-Gesten unterstützen.
- Pinch-to-Zoom mit zwei Fingern.
- Zoomen mit Mausrad.
- Optional Verschieben des vergrösserten Bildausschnitts.

Der Zoom soll möglichst direkt auf dem Kamerabild funktionieren.

Wichtig: Der Zoom soll auch auf das gespeicherte Video angewendet werden, damit der aufgezeichnete Bildausschnitt dem angezeigten Bild entspricht.

Es handelt sich um digitalen Zoom. Die tatsächliche Auflösung der Kamera wird dadurch nicht erhöht.

## 6. Benutzeroberfläche (UI)

Orientiere dich eng am bereitgestellten Mockup.

### 6.1 Allgemeines Design

Die Anwendung soll ein modernes, hochwertiges und minimalistisches Erscheinungsbild erhalten.

Designvorgaben:

- Dunkles Theme.
- Hintergrund in Anthrazit und Dunkelgrau.
- Halbtransparente Panels.
- Abgerundete Ecken.
- Dezente Schatten.
- Blaue Akzentfarbe für aktive Elemente und Hauptaktionen.
- Weisse beziehungsweise hellgraue Schrift.
- Moderne Icons.
- Keine unnötigen Dekorationen.
- Klare visuelle Hierarchie.

Die Anwendung soll wie eine moderne Windows-Anwendung aussehen und nicht wie ein klassisches Windows-Formular.

### 6.2 Fensteraufbau

Das Hauptfenster soll aus zwei Bereichen bestehen:

**Linker und mittlerer Bereich: Kameravorschau**

- Nimmt möglichst viel Platz ein.
- Zeigt das Kamerabild an.
- Bild proportional skalieren.
- Kein Verzerren des Kamerabildes.
- Zoom direkt auf der Vorschau.
- Während einer Aufnahme optional kleines rotes REC-Symbol einblenden.
- Aufnahmedauer dezent anzeigen.

**Rechter Bereich: Einstellungs- und Steuerungspanel**

Das Panel soll ähnlich wie im Mockup über dem Kamerabild liegen.

Es soll eine abgerundete, leicht transparente Oberfläche haben.

### 6.3 Verzögerungseinstellung

Ganz oben im rechten Panel:

- Grosses Uhr-Icon.
- Aktuell eingestellte Verzögerung gross darstellen.
- Einheit «s» anzeigen.
- Pfeil nach oben und unten rechts.

Beispiel:

5 s

Die Einstellung soll optisch einer grossen, einfach bedienbaren Steuerung entsprechen.

### 6.4 Kameraauswahl

Unterhalb der Verzögerung:

- Kamera-Icon.
- Beschriftung «Kamera auswählen».
- Dropdown mit allen erkannten Kameras.

Die ausgewählte Kamera soll deutlich erkennbar sein.

### 6.5 Aufnahmemodus

Wie im Mockup sollen zwei Auswahlkarten vorhanden sein:

**Nur Kamera**
- Zeigt und nimmt ausschliesslich das Kamerabild auf.

**Bildschirm teilen (Präsentation + Kamera)**
- Optionaler Erweiterungsmodus für eine spätere Version.
- Kann für die erste Version als nicht verfügbar gekennzeichnet oder weggelassen werden, wenn die Kernfunktionen dadurch verzögert würden.

Der Fokus der ersten Version liegt auf einer zuverlässigen Kameraaufnahme.

### 6.6 Aufnahme-Steuerung

Im unteren Bereich des rechten Panels drei grosse Schaltflächen:

**Aufnahme starten**
- Blaue Hauptschaltfläche.
- Play-Icon.
- Startet die Aufnahme.

**Aufnahme pausieren / fortsetzen**
- Dezente Umrandung.
- Pause- beziehungsweise Play-Icon.
- Wird während einer laufenden Aufnahme aktiviert.
- Beschriftung abhängig vom aktuellen Zustand.

**Aufnahme speichern**
- Download-Icon.
- Erst nach Beenden der Aufnahme aktivieren.
- Öffnet den Windows-Speicherdialog.

### 6.7 Statusanzeigen

Die Benutzeroberfläche muss den aktuellen Zustand eindeutig anzeigen.

Mögliche Zustände:

- Kamera wird initialisiert.
- Bereit.
- Aufnahme läuft.
- Aufnahme pausiert.
- Verzögerungspuffer wird aufgebaut.
- Verzögerte Vorschau aktiv.
- Aufnahme wird verarbeitet.
- Aufnahme beendet.
- Aufnahme gespeichert.
- Fehler.

Während der Aufnahme soll zusätzlich eine gut lesbare Aufnahmedauer angezeigt werden.

Beispiel:

REC 01:24:35

Während des Countdowns soll die verbleibende Zeit bis zur verzögerten Vorschau gut sichtbar sein.

## 7. Technische Anforderungen

### 7.1 Betriebssystem

Unterstützung:
- Windows 10 22H2
- Windows 11

Zielplattform:
- Windows x64

Die Anwendung soll ohne Administratorrechte funktionieren, sofern die Kameratreiber und Windows-Berechtigungen dies erlauben.

### 7.2 Kameraauflösung

Die Anwendung soll die unterstützten Auflösungen und Bildraten der ausgewählten Kamera ermitteln.

Mögliche Einstellungen:
- 1280 × 720
- 1920 × 1080
- Weitere von der Kamera unterstützte Auflösungen.

Wenn möglich, soll standardmässig die höchste sinnvolle Auflösung verwendet werden.

Die tatsächlichen Kamerafähigkeiten dürfen nicht vorausgesetzt werden.

### 7.3 Audio

Wenn ein Mikrofon verfügbar ist, soll die Anwendung optional auch Audio aufnehmen können.

- Mikrofon auswählen.
- Audioaufnahme aktivieren und deaktivieren.
- Audio synchron zum Video speichern.
- AAC als Audioformat verwenden.

Die Audioaufnahme soll unabhängig von der Videoverzögerung korrekt synchronisiert bleiben.

### 7.4 Fehlerbehandlung

Folgende Fälle müssen behandelt werden:

- Keine Kamera gefunden.
- Kamera wird von einer anderen Anwendung verwendet.
- Kamera wird während der Aufnahme getrennt.
- Keine Berechtigung für Kamerazugriff.
- Festplatte voll.
- FFmpeg nicht verfügbar.
- Fehler beim Encoding.
- Fehler beim Speichern.
- Aufnahme wird unerwartet unterbrochen.

Fehler sollen verständlich angezeigt werden. Die Anwendung darf bei solchen Fehlern nicht einfach abstürzen.

## 8. Performance und Stabilität

Die Anwendung ist primär für längere Aufnahmen gedacht.

Daher gelten folgende Anforderungen:

- Zwei Stunden Aufnahme ohne Speicherleck.
- Keine kontinuierlich ansteigende RAM-Auslastung.
- Aufnahme unabhängig von der geöffneten Benutzeroberfläche.
- UI darf während der Aufnahme nicht einfrieren.
- Video- und Audioverarbeitung müssen asynchron erfolgen.
- Ressourcen nach Beenden vollständig freigeben.
- Keine unnötigen Kopiervorgänge grosser Videodaten im Arbeitsspeicher.
- Stabile Zeitstempel und Audio-/Videosynchronisation.

## 9. Projektstruktur

Die Anwendung soll sauber strukturiert sein.

Vorgeschlagene Aufteilung:

- SKWReplay.App
  - WPF-Anwendung
  - Fenster und Views
  - Styles und Ressourcen

- SKWReplay.Core
  - Geschäftslogik
  - Aufnahmestatus
  - Konfiguration
  - Interfaces

- SKWReplay.Camera
  - Kameraerkennung
  - Kameraauswahl
  - Kamerazugriff
  - Bildverarbeitung

- SKWReplay.Recording
  - Aufnahmeverwaltung
  - Verzögerungspuffer
  - FFmpeg-Integration
  - Encoding
  - Speicherung
  - Audioverarbeitung

- SKWReplay.Tests
  - Unit-Tests
  - Tests der Zustandsverwaltung
  - Tests der Verzögerungslogik

Die Struktur darf angepasst werden, wenn dadurch eine einfachere und zuverlässigere Lösung entsteht.

## 10. Abnahmekriterien

Die Anwendung gilt als fertig, wenn folgende Punkte erfüllt sind:

1. Die Anwendung startet unter Windows 10 und Windows 11.
2. Alle verfügbaren Kameras können erkannt und ausgewählt werden.
3. Das Kamerabild wird live angezeigt.
4. Die Verzögerung kann zwischen 0 und 60 Sekunden eingestellt werden.
5. Die verzögerte Vorschau funktioniert zuverlässig.
6. Der Countdown zeigt die verbleibende Zeit korrekt an.
7. Die Aufnahme beginnt bereits vor Ablauf des Countdowns.
8. Die Aufnahme kann pausiert und fortgesetzt werden.
9. Pausen werden im gespeicherten Video herausgeschnitten.
10. Die Aufnahme kann beendet werden.
11. Das vollständige Video kann anschliessend als MP4 gespeichert werden.
12. Eine zweistündige Aufnahme ist ohne RAM-Probleme möglich.
13. Der digitale Zoom funktioniert mit Touch und Maus.
14. Der Zoom wird auch im gespeicherten Video berücksichtigt.
15. Audio kann optional aufgenommen werden.
16. Die Benutzeroberfläche entspricht dem beschriebenen Design.
17. Die Anwendung stürzt bei normalen Bedienvorgängen nicht ab.
18. Temporäre Dateien werden nach erfolgreicher Speicherung korrekt aufgeräumt.

## 11. Vorgehensweise bei der Entwicklung

Entwickle die Anwendung schrittweise.

**Phase 1: Grundgerüst**
- Projekt erstellen.
- WPF-Oberfläche aufbauen.
- Dark Theme und Layout umsetzen.
- Kameras erkennen und auswählen.
- Live-Vorschau implementieren.

**Phase 2: Aufnahme**
- FFmpeg integrieren.
- Aufnahme starten und beenden.
- Videodaten kontinuierlich auf der Festplatte speichern.
- MP4-Ausgabe implementieren.

**Phase 3: Verzögerung**
- Ringpuffer implementieren.
- Verzögerung von 0 bis 60 Sekunden ermöglichen.
- Countdown implementieren.
- Verzögerte Vorschau darstellen.

**Phase 4: Erweiterte Funktionen**
- Pause und Fortsetzen.
- Audioaufnahme.
- Digitaler Zoom.
- Touch-Gesten.
- Speicherverwaltung und Wiederherstellung.

**Phase 5: Tests und Veröffentlichung**
- Zweistündige Testaufnahmen durchführen.
- Speicherverbrauch überwachen.
- Windows 10 und Windows 11 testen.
- Fehlerbehandlung prüfen.
- Self-contained Release erstellen.

## 12. Wichtige Entwicklungsgrundsätze

- Priorität hat eine zuverlässige Aufnahme, nicht eine möglichst grosse Anzahl an Funktionen.
- Keine unnötig komplizierte Architektur.
- Keine unnötigen Abhängigkeiten.
- Moderne C#-Syntax verwenden.
- Saubere Trennung von UI und Videoverarbeitung.
- Keine blockierenden Operationen im UI-Thread.
- Keine grossen Videodaten im RAM speichern.
- Keine Funktionen nur optisch implementieren: Alle Schaltflächen müssen tatsächlich funktionieren.
- Keine Platzhalter oder simulierten Kameraaufnahmen im fertigen Produkt.
- Regelmässig kompilieren und testen.

**Ergebnis:** Eine vollständig funktionsfähige, moderne Windows-Anwendung, mit der eine Kameraaufnahme mit einer frei einstellbaren Verzögerung von bis zu 60 Sekunden angezeigt und beliebig lange aufgenommen werden kann. Nach dem Beenden kann das gesamte Video als MP4 gespeichert werden.





# ToDo ab Hier

Das Disign anpassen Icon taskleiste oen in bar und als app

Desktopverknüpfung

voreinstellungen zeit zoom

deployment --> Sauber install wirklich als app
