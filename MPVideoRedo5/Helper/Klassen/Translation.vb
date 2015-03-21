
''' <summary>
''' These will be loaded with the language files content
''' if the selected lang file is not found, it will first try to load en(us).xml as a backup
''' if that also fails it will use the hardcoded strings as a last resort.
''' </summary>
Public NotInheritable Class Translation

    Private Sub New()
    End Sub
    ' A
    Public Shared Abort As String = "Abbruch"
    Public Shared AdDetectiveDone As String = "Der AdDetective Scan wurde abgeschlossen."
    Public Shared AdDetectiveRunning As String = "AdDetective Scan läuft"
    Public Shared AddReplaceString As String = "Replace-String hinzufügen"
    Public Shared AlwaysSaveProject As String = "Schnitte als Projekt speichern"
    Public Shared AlwaysKeepOriginalFile As String = "immer die Originaldatei behalten"
    Public Shared AlwaysLoadComSkipMarkers As String = "Schnitte aus ComSkip Datei laden"
    Public Shared AlwaysLoadProject As String = "Schnitte aus Projekt Datei laden"
    Public Shared AlwaysRefreshMoviestripThumbs As String = "Filmstreifen Vorschaubilder aktualisieren"
    Public Shared AlwaysRefreshMoviestripThumbsDelay As String = "Wiederholungsintervall"
    Public Shared AlwaysSaveComSkipMarkers As String = "Speichere Schnitte immer als ComSkip Datei"
    Public Shared AudioSyncLabel As String = "Nachsyncronisierung der Audiospur:"
    Public Shared AudioSyncLabelContext As String = "Audiospur syncronisieren umschalten"
    Public Shared AutoEndCutLabel As String = "Endmarker automatisch setzen wenn nötig"

    ' B
    Public Shared ButtonCheckVRD As String = "Auf VRD 5 prüfen..."
    Public Shared BackgroundSave As String = "Es wird im Hintergrund weiter gespeichert!"

    ' C
    Public Shared CalculateTimeLeft As String = "Berechne die Restzeit"
    Public Shared CalculatedTimeLeft As String = "Voraussichtliche Restzeit: "
    Public Shared ChangeCut As String = "Neue Position"
    Public Shared ChooseSeries As String = "Wähle die Serie aus..."
    Public Shared ClearCutlist As String = "Lösche alle Cut`s"
    Public Shared ClearCutsAtClose As String = "Schnitte verwerfen"
    Public Shared ClearCutsAtClose1 As String = "Wollen Sie die Schnitte verwerfen?"
    Public Shared CloseVRD As String = "VideoRedo schließen"
    Public Shared CloseVRD1 As String = "Möchten Sie auch VideoRedo"
    Public Shared CloseVRD2 As String = "schliessen? Alle ungespeicherten"
    Public Shared CloseVRD3 As String = "Schnitte gehen verloren?"
    Public Shared Complete As String = "Abgeschlossen"
    Public Shared ConfigureSeekSteps As String = "Spulsprünge Konfigurieren"
    Public Shared ContinueScan As String = "Continue scan?"
    Public Shared ContinueScan1 As String = "Continue the scan in the background?"
    Public Shared CreateMovieSubfolder As String = "Erstelle Unterordner für Filme"
    Public Shared CreateSeriesSubfolder As String = "Erstelle Unterordner für Serien"
    Public Shared CutContextMenu As String = "Schnittmenü"
    Public Shared CutContextChange As String = "Gewählten Schnitt ändern"
    Public Shared CutContextDelete As String = "Gewählte Szene löschen"
    Public Shared CutContextJumpTo As String = "Springe zum gewählten Marker"

    ' D
    Public Shared DebugMode As String = "Debug Modus"
    Public Shared DeleteOriginalFile As String = "Möchten Sie das Originalfile löschen?"
    Public Shared DeleteOriginalFileTitle As String = "Original löschen?"
    Public Shared DelReplaceString As String = "Ausgewählten String entfernen"
    Public Shared DescriptionRecPathDialog As String = "Wähle den Ordner der gespeicherten Aufnahmen"
    Public Shared DescriptionSavePathDialog As String = "Wähle den Ordner in dem die geschnittenen Videos gespeichert werden sollen."
    Public Shared DescriptionVRDProfiles As String = "Wenn VRD 5 auf Ihrem System aktiv ist, können Sie hier das Standardprofil für die Konvertierung auswählen. Wenn dies der Fall ist wird eine Profile Liste erzeugt. Klicken Sie auf den Button um zu prüfen, ob die VRD 5 Anwendung aktiv ist und warten Sie einen Moment bis Sie die Profile auswählen können. Die Details des Profile werden dann angezeigt."
    Public Shared Deinterlacemode As String = "De-Interlace Modus:"
    Public Shared DisableProfileDetails As String = "Profil Bestätigung deaktivieren"
    Public Shared Done As String = "Fertig"
    Public Shared Duration As String = "Gesamtlaufzeit:{0}"

    ' E
    Public Shared EditEndFilename As String = "End-Dateinamen bearbeiten"
    Public Shared EditReplacementString As String = "Replacement-Strings bearbeiten"
    Public Shared EditVideo As String = "Schneiden"
    Public Shared Encodingtype As String = "Encodierungstyp:"
    Public Shared EpisodeFound As String = "Episode gefunden"
    Public Shared EpisodeTitle As String = "Episodentitel"
    Public Shared ErrorOccured As String = "Fehler"

    ' F
    Public Shared FollowEpisodeWasFound As String = "Folgende Episode wurde gefunden. Verwenden?"
    Public Shared Forward As String = "Vorspulen"
    Public Shared ForwardStep As String = "Vor"
    Public Shared ForbiddenCutCompleteVideo As String = "Es ist nicht möglich das ganze Video zu beschneiden!!"
    Public Shared Filetype As String = "Dateityp:"
    Public Shared Framerate As String = "Bildrate:"
    Public Shared Frames As String = "Bilder"

    ' G
    Public Shared GeneralOptions1 As String = "Grundeinstellungen 1"
    Public Shared GeneralOptions2 As String = "Grundeinstellungen 2"
    Public Shared Genre As String = "Genre"
    Public Shared GetNewFilename As String = "Bestimme neuen Dateinamen"
    Public Shared GroupAlwaysRefreshMoviestripThumbs = "Vorschaubilder"
    Public Shared GroupCutSettingCaption As String = "Schnitt-Einstellungen"
    Public Shared GroupDialogs As String = "Dialog Einstellungen"
    Public Shared GroupModuleName As String = "Modul Name"
    Public Shared GroupOnPauseCaption As String = "Während der Pause"
    Public Shared GroupOnPlayCaption As String = "Während der Wiedergabe"
    Public Shared GroupRecordingSettingCaption As String = "Pfadeinstellungen"
    Public Shared GroupStringSettingCaption As String = "Datei/Ordner Benennungseinstellungen"
    Public Shared GroupVRDProfile As String = "Standard Profil"
    Public Shared GroupVRDProfileH264 As String = "Standard Profil H.264"

    ' H
    Public Shared HelpAlwaysSaveProject As String = "Wenn diese Option aktiviert ist wird die Frage, ob die Schnitte behalten werden soll, unterdrückt und die Schnitte immer behalten und als Projekt gespeichert."
    Public Shared HelpAlwaysKeepOriginalFile As String = "Wenn diese Option aktiviert ist wird die Frage, ob die Orginaldatei behalten werden soll, unterdrückt und die Orginaldatei immer behalten."
    Public Shared HelpAlwaysLoadComSkipMarkers As String = "Wenn diese Option aktiviert wird die Frage, ob die Schnitte aus der ComSkip Datei geladen werden soll, falls diese vorhanden ist, unterdrückt."
    Public Shared HelpAlwaysLoadProject As String = "Wenn diese Option aktiviert ist werden beim Laden des Videos die Schnitte aus der Projekt Datei ohne Nachfrage erstellt, falls diese vorhanden ist."
    Public Shared HelpAlwaysRefreshMoviestripThumbs As String = "Wenn diese Option aktiviert ist wird der Filstreifen auch wärend der Wiedergabe aktualisiert."
    Public Shared HelpAlwaysSaveComSkipMarkers As String = "Wenn diese Option aktiviert wird die Frage, ob die Schnitte in eine ComSkip Datei gespeichert werden soll unterdrückt."
    Public Shared HelpSetEndmarker As String = "Wenn diese Option aktiviert ist wird beim Speichern des Videos ein EndCut automatisch erstellt sofern nicht vorhanden. Sollte aktiviert sein wenn eine Nachlaufzeit für Aufnahmen eingestellt ist."
    Public Shared HelpSetStartmarker As String = "Wenn diese Option aktiviert ist wird beim Laden des Videos ein StartCut erstellt. Sollte aktiviert sein wenn eine Vorlaufzeit für Aufnahmen eingestellt ist."
    Public Shared HelpParseMovieFile As String = "Konfigurieren Sie den Dateinamen nach Ihren Wünschen. Mit klick auf den Button rechts können sie die möglichen Optionen sehen"
    Public Shared HelpParseSeriesFile As String = "Konfigurieren Sie Dateinamen nach Ihren Wünschen. Mit klick auf den Button rechts können sie die möglichen Optionen sehen"
    Public Shared HelpPauseOnStart As String = "Wenn diese Option aktiviert ist wird die Wiedergabe sofort nach dem Laden pausiert."
    Public Shared HelpDisableProfileDetails As String = "Wenn diese Option aktiviert ist wird die Sicherheitsfrage, ob sie das Profil auswählen wollen, unterdrückt und das Profil direkt ausgewäht."
    Public Shared HowHandleVideo As String = "Was ist das für ein Video?"

    ' I
    Public Shared IdentifiedEpisode As String = "Gefundene Episode:"

    ' J

    ' K

    ' L
    Public Shared LoadComSkipMarkers As String = "ComSkip Marker Laden"
    Public Shared LoadComSkipMarkers1 As String = "Willen Sie die ComSkip Marker laden?"
    Public Shared LoadComSkipMarkers3 As String = "Die ComSkip Marker wurden geladen."
    Public Shared LoadProjectMarkers As String = "Projektdatei Laden"
    Public Shared LoadProjectMarkers1 As String = "Willen Sie die Projekt-Datei laden?"
    Public Shared LoadProjectMarkers3 As String = "Projekt-Datei wurden geladen."
    Public Shared LabelRecordingsPath As String = "Aufnahmeordner:"
    Public Shared LabelSavePath As String = "Video-Speicherpfad:"
    Public Shared LoadOtherVideo As String = "Lade Video"

    ' M
    Public Shared MakeCut As String = "Schneide hier"
    Public Shared ModuleFunction As String = "Plugin zum Schneiden von Videos mit Hilfe von VideoRedo"
    Public Shared ModuleName As String = "Modul Name"
    Public Shared ModuleMain = "Film schneiden"
    Public Shared ModuleStart = "Film Auswahl"
    Public Shared ModuleSaveVideo = "Film speichern"
    Public Shared Movie As String = "Film"

    ' N
    Public Shared NewFilename As String = "Neuer Dateiname"
    Public Shared NoEndmarker As String = "Kein Endmarker"
    Public Shared NoEpisodesFoundDialog As String = "Es wurde keine Episode gefunden. Es wird normal gespeichert..."
    Public Shared NoRecordingsAviable As String = "Keine Aufnahmen vorhanden. Abbruch"
    Public Shared NoSeriesFoundDialog As String = "Für die Serie wurde nichts gefunden. Es wird normal gespeichert..."
    Public Shared NothingFound As String = "Nichts gefunden"
    Public Shared NothingToSave As String = "Es gibt nichts zu speichern"

    ' O
    Public Shared optionsCutBars As String = "Progressbaroptionen"
    Public Shared OriginalString As String = "Orig. Zeichenkette"

    ' P
    Public Shared ParseMovieFileLabel As String = "Filmdateimuster:"
    Public Shared ParseSeriesFileLabel As String = "Episodendateimuster:"
    Public Shared PauseOnStart As String = "Pause nach dem Start"
    Public Shared Position As String = "Aktuelle Position:{0}"
	
    ' Q
    Public Shared QuickStreamFix As String = "QuickStreamfix erforderlich"""
    Public Shared QuickStreamFix1 As String = "Die Datei benötigt einen Quickstreamfix, ohne diesen kann das File nicht"
    Public Shared QuickStreamFix2 As String = "bearbeitet werden. Möchten Sie einen Quickstreamfix durchführen oder abbrechen?"

    ' R
    Public Shared Ratio As String = "Verhältnis:"
    Public Shared RecordingDialogDescription As String = "Ordner der gespeicherten Aufnahmen"
    Public Shared RecordingPathIncorrect As String = "Ungültiger Pfad zu den Aufnahmen, bitte öffne die Configuration!!"
    Public Shared ReplacementString As String = "Ersatz Zeichenkette"
    Public Shared Resolution As String = "Auflösung:"
    Public Shared Rewind As String = "Zurückspulen"
    Public Shared RewindStep As String = "Zurück"

    ' S
    Public Shared SaveCuttedVideo As String = "Speichere Video mit Schnitten..."
    Public Shared SaveComSkipMarkers As String = "ComSkip Marker speichern"
    Public Shared SaveComSkipMarkers1 As String = "Willen Sie die ComSkip Marker speichern?"
    Public Shared SaveComSkipMarkers3 As String = "Die ComSkip Marker wurden gespeichert."
    Public Shared SaveComSkipMarkers4 As String = "Fehler beim Speichern der ComSkip Marker."
    Public Shared SaveDialogDescription As String = "Ordner wo die Filme gespeichert werden sollen"
    Public Shared SaveHere As String = "Hier speichern"
    Public Shared SaveProgressLabel As String = "Speichern zu {0}% abgeschlossen"
    Public Shared SaveProjectMarkers As String = "Projekt speichern"
    Public Shared SaveProjectMarkers1 As String = "Wollen Sie die Schnitte als Projekt speichern?"
    Public Shared SaveProjectMarkers3 As String = "Die Schnitte wurden als Projekt gespeichert."
    Public Shared SaveProjectMarkers4 As String = "Fehler beim Speichern als Projekt."
    Public Shared SavingProfile As String = "Speicherprofil"
    Public Shared SavingPathError As String = "Fehler im Speicherpfad"
	Public Shared SaveVideo As String = "Video speichern"
    Public Shared SearchFolder As String = "Ordner suchen"
    Public Shared SearchWithAnotherString As String = "Suche mit anderer Zeichenfolge"
    Public Shared Seconds As String = "Sek."
    Public Shared SelRecording As String = "Wähle Sie eine Aufnahme"
    Public Shared ShowFileParserStrings As String = "Zeige Strings"
    Public Shared StartAdDetectiveScan As String = "Starte AdDetective-Scan"
    Public Shared StartCutAtStart As String = "Startmarker beim Abspielen setzten"
    Public Shared [Step] As String = "Sprung {0}"

    ' T
    Public Shared TimeLeft As String = "Restdauer:{0}"
    Public Shared Title As String = "Titel"
    Public Shared VRD As String = "VRD"

    ' U
    Public Shared Unknown As String = "Unbekannt"
    Public Shared UserAbortDialog As String = "Benutzerabbruch - Es wird normal gespeichert..."
    Public Shared UseVideoAsSeries As String = "Ist eine Serienfolge"

    ' V
    Public Shared VideoRedoNotCompatible As String = "Deine Version von VideoRedo ist nicht kompatibel, es wird abgebrochen."

    ' W

    ' X

    ' Y

    ' Z

End Class

