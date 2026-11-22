# SKWReplay

SKWReplay ist eine Windows-Desktop-App für Kameraaufnahmen mit verzögerter Live-Vorschau. Das Projekt verwendet C#, .NET 10, WPF/XAML, OpenCvSharp und FFmpeg.

> **Projektstatus:** SKWReplay befindet sich noch in Entwicklung. Die Oberfläche und grundlegende Kamera-, Aufnahme-, Pause-, Zoom- und Exportabläufe sind implementiert. Die Anwendung wurde noch nicht systematisch unter Windows 10 22H2 und Windows 11 oder mit zweistündigen Aufnahmen validiert. Audioaufnahme, Touch-Pinch-Zoom und Wiederherstellung nach einem Absturz sind derzeit nicht implementiert. Bitte wichtige Aufnahmen zusätzlich mit einem anderen Gerät sichern.

## Aktuelle Funktionen

- Kamera-Vorschau und Kameraauswahl
- Verzögerung der Vorschau von 0 bis 60 Sekunden
- Die zuletzt eingestellte Verzögerung bleibt nach einem Neustart erhalten
- Verzögerung während Aufnahme und Pause verstellen; beim Erhöhen wird der Countdown für noch fehlenden Puffer angezeigt
- Aufnahme starten, pausieren, fortsetzen und beenden
- Beendete Aufnahme speichern oder verwerfen; danach kann eine neue Aufnahme gestartet werden
- Kameras über DirectShow mit den von Windows gelieferten Gerätenamen erkennen
- Digitale Vergrösserung über das Mausrad
- Einstellungsbereich ein- und ausklappen
- Vollbildmodus: Hauptfenster auf dem primären Monitor, reine Vorschau auf weiteren Monitoren
- MP4-Export mit H.264 über FFmpeg
- Pausen werden als separate Videosegmente aufgezeichnet und beim Export zusammengeführt
- Self-contained Windows-x64-Publish: Die .NET-Runtime muss auf dem Zielcomputer nicht separat installiert werden

**Noch nicht enthalten:** Mikrofon-/Audioaufnahme, Touch-Gesten, Bildschirmaufnahme und eine garantierte Wiederherstellung nach Programm- oder Systemabbruch.

## App von GitHub herunterladen und starten

### Empfohlener Weg: fertiger Release

1. Öffne die GitHub-Seite des SKWReplay-Repositories.
2. Wähle **Releases** und öffne die neueste Version.
3. Lade das Windows-x64-Archiv (üblicherweise `.zip`) herunter. Wenn dort kein fertiges Archiv angeboten wird, verwende stattdessen den Abschnitt [Aus dem Quellcode bauen](#aus-dem-quellcode-bauen).
4. Entpacke **das ganze Archiv** in einen eigenen Ordner. Starte die App nicht direkt aus dem ZIP.
5. Starte `SKWReplay.App.exe` im entpackten Ordner. Behalte auch die übrigen Dateien und Unterordner, insbesondere `ffmpeg\ffmpeg.exe`, im selben Ordner.
6. Schliesse eine Kamera an und erlaube den Kamerazugriff, falls Windows danach fragt. Die Windows-Einstellungen dafür befinden sich unter **Einstellungen → Datenschutz und Sicherheit → Kamera** (in Windows 10: **Einstellungen → Datenschutz → Kamera**).

Das veröffentlichte Archiv muss `SKWReplay.App.exe` und `ffmpeg\ffmpeg.exe` enthalten. Ohne FFmpeg kann die App kein Video encodieren oder exportieren. Falls Windows eine Sicherheitswarnung für eine heruntergeladene App anzeigt, führe die Datei nur aus, wenn du dem Herausgeber und der Quelle vertraust.

> Es ist derzeit keine konkrete GitHub-Repository-Adresse in diesem Projekt hinterlegt. Verwende die Repository-Seite, von der du SKWReplay erhalten hast. Falls unter **Releases** noch kein App-Archiv bereitsteht, muss die Anwendung aus dem Quellcode gebaut werden.

## Aus dem Quellcode bauen

### Voraussetzungen

- Windows x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- FFmpeg für Windows x64 mit dem Encoder `libx264`
- Eine von Windows erkannte Kamera für den praktischen Kameratest

Alternativ kann **Visual Studio 2022** mit der Workload **.NET-Desktopentwicklung** verwendet werden. WPF-Builds müssen auf Windows erstellt werden.

### 1. Quellcode beziehen

Auf der GitHub-Seite des Repositories kannst du über **Code → Download ZIP** den Quellcode herunterladen und entpacken. Für Git verwende im Repository unter **Code** die angezeigte Klon-URL:

```powershell
git clone <Klon-URL-des-SKWReplay-Repositories>
cd SKWReplay
```

`<Klon-URL-des-SKWReplay-Repositories>` ist durch die HTTPS- oder SSH-URL zu ersetzen, die GitHub für dieses Repository anzeigt.

### 2. FFmpeg bereitstellen

Lade eine passende Windows-x64-Version von [ffmpeg.org](https://ffmpeg.org/download.html) herunter und entpacke sie. Lege die ausführbare Datei `ffmpeg.exe` im Projekt unter folgendem Pfad ab:

```text
SKWReplay.App\ffmpeg\ffmpeg.exe
```

Der Ordner `ffmpeg` beziehungsweise die ausführbare Datei ist nicht Bestandteil des Git-Quellcodes. Bei einem normalen Klon musst du FFmpeg daher selbst hinzufügen. Die Anwendung sucht FFmpeg ausserdem in einigen lokalen Installationspfaden und im `PATH`; für reproduzierbare Builds sollte es aber wie oben im App-Projekt liegen.

Beachte die Lizenzbedingungen der verwendeten FFmpeg-Distribution und ihrer Codecs. Wenn du ein App-Archiv weitergibst, prüfe, ob die enthaltene FFmpeg-Version mit deiner Art der Weitergabe kompatibel ist und stelle gegebenenfalls die erforderlichen Lizenzinformationen bereit.

### 3. Wiederherstellen, bauen und starten

Führe die folgenden Befehle im Verzeichnis mit `SKWReplay.sln` aus:

```powershell
dotnet restore .\SKWReplay.sln
dotnet build .\SKWReplay.sln
dotnet run --project .\SKWReplay.App\SKWReplay.App.csproj
```

NuGet-Pakete wie OpenCvSharp werden beim Wiederherstellen automatisch geladen. Für die Kameraerkennung und die Live-Vorschau muss eine Kamera angeschlossen und der Windows-Kamerazugriff freigegeben sein.

### Tests ausführen

```powershell
dotnet test .\SKWReplay.sln
```

Die vorhandenen Tests prüfen grundlegende Fälle des Verzögerungspuffers. Sie ersetzen keine Tests mit realen Kameras, langen Aufnahmen oder verschiedenen Windows-Versionen.

### Self-contained Windows-x64-Version veröffentlichen

```powershell
dotnet publish .\SKWReplay.App\SKWReplay.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\publish-win-x64
```

Die veröffentlichte App liegt danach in `publish-win-x64`. Prüfe vor dem Weitergeben, dass diese Datei existiert:

```text
publish-win-x64\ffmpeg\ffmpeg.exe
```

Wenn sie fehlt, wurde FFmpeg nicht in das App-Projektverzeichnis gelegt. Ergänze es unter `SKWReplay.App\ffmpeg\ffmpeg.exe` und veröffentliche erneut. Gib den **gesamten** Inhalt des Publish-Verzeichnisses weiter, nicht nur `SKWReplay.App.exe`. Das Zielsystem braucht das .NET 10 Runtime-Paket nicht separat; FFmpeg und die nativen App-Abhängigkeiten müssen jedoch mitgeliefert werden.

## Bedienung

1. Wähle eine Kamera im Dropdown aus. **Kameras aktualisieren** startet eine erneute Erkennung.
2. Stelle die Verzögerung mit den Pfeiltasten ein. Der Bereich ist 0–60 Sekunden. Du kannst sie auch während der Aufnahme verändern; wenn für eine längere Verzögerung noch Videopuffer fehlt, erscheint erneut ein Countdown.
3. Klicke **Aufnahme starten**. Die Aufnahme beginnt, während der Countdown für die verzögerte Vorschau läuft.
4. Mit **Aufnahme pausieren** und **Aufnahme fortsetzen** kannst du Abschnitte unterbrechen. Mit **Aufnahme beenden** wird die Aufnahme abgeschlossen.
5. Klicke **Aufnahme speichern**, um die MP4-Datei zu exportieren, oder **Aufnahme verwerfen**, wenn du sie nicht behalten möchtest. Nach erfolgreichem Speichern oder Verwerfen ist eine neue Aufnahme möglich.
6. Zum Zoomen bewege das Mausrad über der Vorschau.

Die Verzögerungseinstellung wird lokal unter `%LOCALAPPDATA%\SKWReplay\delay-seconds.txt` gespeichert. Dafür wird keine Datenbank oder zusätzliche Bibliothek benötigt.

Die Aufnahme wird vorübergehend unter `%TEMP%\SKWReplay` abgelegt. Nach erfolgreichem Export räumt die App ihre Segmente auf. Bei einem Absturz kann die temporäre Aufnahme unvollständig oder nicht wiederherstellbar sein.

## Fehlerbehebung

- **Keine Kamera sichtbar:** Prüfe den Anschluss, die Windows-Datenschutzeinstellungen und ob eine andere Anwendung die Kamera exklusiv verwendet. Danach **Kameras aktualisieren** wählen.
- **Kamera kann nicht geöffnet werden:** Beende andere Kamera-Apps und starte SKWReplay erneut. Bei einer neu angeschlossenen Kamera nochmals aktualisieren.
- **FFmpeg nicht gefunden / Aufnahme startet nicht:** Prüfe, ob `ffmpeg\ffmpeg.exe` neben der App liegt beziehungsweise beim Entwickeln unter `SKWReplay.App\ffmpeg\ffmpeg.exe`. Eine passende `ffmpeg.exe` muss `libx264` enthalten.
- **Export schlägt fehl:** Prüfe den freien Speicherplatz auf dem Ziellaufwerk und ob der Zielordner beschreibbar ist. Temporäre Segmente benötigen zusätzlichen Speicherplatz.
- **Windows blockiert den Kamerazugriff:** Aktiviere den Kamerazugriff für Desktop-Apps in den Windows-Datenschutzeinstellungen.

## Projektstruktur

| Projekt | Zweck |
| --- | --- |
| `SKWReplay.App` | WPF-Oberfläche, MVVM-ViewModel, App-Einstiegspunkt und Icon-Dateien unter `Assets` |
| `SKWReplay.Core` | Gemeinsame Modelle, Status und UI-Hilfstypen |
| `SKWReplay.Camera` | Kameraerkennung, Kamerazugriff und digitale Bildverarbeitung |
| `SKWReplay.Recording` | Verzögerungspuffer, FFmpeg-Encoding und Export |
| `SKWReplay.Tests` | Unit-Tests |

## Zielplattform und Teststatus

Das Projekt zielt auf `net10.0-windows` und `win-x64`. Windows 10 22H2 und Windows 11 sind Projektziele, aber derzeit nicht als umfassend getestet zu verstehen. Vor wichtigen Einsätzen sollte die App auf dem konkreten Computer mit dessen Kamera und den vorgesehenen Aufnahmeeinstellungen ausprobiert werden.

