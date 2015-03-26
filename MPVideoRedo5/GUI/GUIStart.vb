Imports System
Imports System.IO
Imports System.Windows.Forms
Imports MediaPortal.GUI.Library
Imports MediaPortal.Dialogs
Imports MediaPortal.Player
Imports MediaPortal.Profile
Imports MediaPortal.Configuration
Imports TvdbLib
Imports TvdbLib.Cache
Imports TvdbLib.Data

Namespace MPVideoRedo5
    <PluginIcons("MPVideoRedo5.MPVideoRedo5.png", "MPVideoRedo5.MPVideoRedo5_disabled.png")> _
    Public Class GUIStart
        Inherits GUIWindow
        Implements ISetupForm

#Region "Skin Controls"
        <SkinControlAttribute(10)> Protected lstRecList As GUIListControl = Nothing
        <SkinControlAttribute(40)> Protected lstEpisodesList As GUIListControl = Nothing
        <SkinControlAttribute(4)> Protected btnCheckUseAsSeries As GUICheckButton = Nothing
        <SkinControlAttribute(52)> Protected imgWaiting As GUIAnimation = Nothing
        <SkinControlAttribute(51)> Protected imgWaitingEpisodes As GUIAnimation = Nothing
        <SkinControlAttribute(21)> Protected btnCutVideo As GUIButtonControl = Nothing
#End Region

#Region "iSetupFormImplementation"

        Public Function Author() As String Implements MediaPortal.GUI.Library.ISetupForm.Author
            Return "BPoHVoodoo"
        End Function

        Public Function CanEnable() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.CanEnable
            Return True
        End Function

        Public Function DefaultEnabled() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.DefaultEnabled
            Return True
        End Function

        Public Function Description() As String Implements MediaPortal.GUI.Library.ISetupForm.Description
            Return Translation.ModuleFunction
        End Function

        Public Function GetHome(ByRef strButtonText As String, ByRef strButtonImage As String, ByRef strButtonImageFocus As String, ByRef strPictureImage As String) As Boolean Implements MediaPortal.GUI.Library.ISetupForm.GetHome
            strButtonText = HelpConfig.GetConfigString(ConfigKey.ModuleName) : strButtonImage = String.Empty : strButtonImageFocus = String.Empty : strPictureImage = "MPVideoRedo5Hover.png" : Return True
        End Function

        Public Function GetWindowId() As Integer Implements MediaPortal.GUI.Library.ISetupForm.GetWindowId
            Return enumWindows.GUIstart
        End Function

        Public Function HasSetup() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.HasSetup
            Return True
        End Function

        Public Overrides Function GetModuleName() As String
            Return HelpConfig.GetConfigString(ConfigKey.ModuleName) & " - " & Translation.ModuleStart
        End Function

        Public Function PluginName() As String Implements MediaPortal.GUI.Library.ISetupForm.PluginName
            Return "MPVideoRedo5"
        End Function

        Protected Overridable ReadOnly Property SerializeName() As String
            Get
                Return PluginName()
            End Get
        End Property

        Public Sub ShowPlugin() Implements MediaPortal.GUI.Library.ISetupForm.ShowPlugin
            'Damit die Config auch übersetzt wird
            Translator.TranslateSkin()
            Dim setup As New SetupForm : setup.Show()
        End Sub

        Public Overloads Overrides Property GetID() As Integer
            Get
                Return enumWindows.GUIstart
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property


        Public Overloads Overrides Function Init() As Boolean
            'Beim initialisieren des Plugin den Screen laden
            Translator.TranslateSkin()
            Return Load(GUIGraphicsContext.Skin + "\MPVideoRedo5.Start.xml")
        End Function

#End Region

#Region "Variablen"
        Private RecRootPath As String
        Friend RecList As clsRecordings
        Private lastSelRecording As clsRecordings.Recordings
        Friend AktEpisode As TvdbEpisode
        Friend AktSerie As TvdbSeries
        'Der Thread für das abrufen der Serieninfos
        Dim trSeries As New Threading.Thread(AddressOf GetSeriesInfosBackground)
        Dim NothingInProgress As Boolean = True
#End Region

        Protected Overrides Sub OnPageLoad()
            MyBase.OnPageLoad()
            If Directory.Exists(Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5") = False Then
                Directory.CreateDirectory(Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5")
            End If
            GUIWindowManager.NeedRefresh()
            If GUIWindowManager.ActiveWindow = GetID Then
                lstEpisodesList.Visible = False
                Translator.SetProperty("#RecordingTitle", " ")
                Translator.SetProperty("#RecordingGenre", " ")
                Translator.SetProperty("#RecordingEpisodename", " ")
                'Wird gerade was geschnitten oder läuft der AdScan 
                If VRD IsNot Nothing Then
                    GUIButtonControl.DisableControl(GetID, btnCutVideo.GetID)
                    GUIButtonControl.DisableControl(GetID, btnCheckUseAsSeries.GetID)
                    GUIListControl.DisableControl(GetID, lstRecList.GetID)
                    btnCheckUseAsSeries.Selected = False
                    NothingInProgress = False
                    If VRD.OutputInProgress = True Then
                        If GUIWindowManager.GetPreviousActiveWindow <> enumWindows.GUISave Then 'Wenn geschnitten wird, dann direkt in das ProgressWindow gehen
                            Logger.DebugM("GUIStart: Output in progress. Redirecting...")
                            GUIWindowManager.ActivateWindow(enumWindows.GUISave, True)
                            Exit Sub
                        End If
                    ElseIf VRD.AdScanInProgress = True Then
                        If GUIWindowManager.GetPreviousActiveWindow <> enumWindows.GUIMain Then 'Wenn AdScan läuft, dann direkt in das MainWindow gehen
                            Logger.DebugM("GUIStart: AdScan is running. Redirecting...")
                            GUIWindowManager.ActivateWindow(enumWindows.GUIMain, True)
                            Exit Sub
                        End If
                    ElseIf Not RecordingToCut.VideoFilename Is Nothing Then
                        If GUIWindowManager.GetPreviousActiveWindow <> enumWindows.GUIMain Then
                            Logger.DebugM("GUIStart: File in Memory {0}. Redirecting...", VRD.MediaToCut)
                            'RecordingToCut.VideoFilename = VRD.MediaToCut
                            GUIWindowManager.ActivateWindow(enumWindows.GUIMain, True)
                            Exit Sub
                        End If
                    Else
                        GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                        GUIButtonControl.EnableControl(GetID, btnCheckUseAsSeries.GetID)
                        GUIListControl.EnableControl(GetID, lstRecList.GetID)
                        NothingInProgress = True
                    End If
                End If
                If g_Player.Playing Then g_Player.StopAndKeepTimeShifting() 'Falls ein Stream läuft dann stoppen
                RecRootPath = HelpConfig.GetConfigString(ConfigKey.RecordingsPath)
                If RecRootPath = "" Or IO.Directory.Exists(RecRootPath) = False Then
                    DialogNotify(GetID, 10, Translation.ErrorOccured, Translation.RecordingPathIncorrect)
                    GUIWindowManager.ActivateWindow(enumWindows.GUISave, True)
                    Exit Sub
                End If
                FillRecListControl(RecRootPath)
                If lstRecList.ListItems.Count > 0 Then
                    For i As Integer = 0 To lstRecList.ListItems.Count - 1
                        If lstRecList.ListItems(i).IsFolder = False Then
                            lstRecList.SelectedListItemIndex = i
                            SetRecordingsDetails(lstRecList.SelectedListItem)
                            Exit For
                        End If
                    Next
                Else
                    'Wenn es keine Aufnahmen gibt
                    DialogNotify(GetID, 5, Translation.NothingFound, Translation.NoRecordingsAviable)
                    GUIWindowManager.CloseCurrentWindow()
                    Exit Sub
                End If
                If VRD Is Nothing Then
                    'Debugmodus aus oder ein. Je nach dem wird das Fenster von VRD angezeigt oder nicht
                    KillVRD()
                    If HelpConfig.GetConfigString(ConfigKey.DebugVideoRedo) Then
                        VRD = New VideoReDo5(False)
                    Else
                        VRD = New VideoReDo5(True)
                    End If
                    Logger.DebugM("Trying to bring MediaPortal to the foreground...")
                    Helper.SetMPtoForeground(GetModuleName())
                    Logger.DebugM("Mediaportal is now in foreground again.")
                    AddHandler VRD.QuickStreamFixNeeded, AddressOf QuickStreamFixIsNeeded
                End If
                Logger.Info("VideoReDo Version:{0}", VRD.ReDoVersion)
            End If
        End Sub


        Public Overrides Sub OnAction(ByVal action As MediaPortal.GUI.Library.Action)
            MyBase.OnAction(action)
            Dim AktWinId As Integer = GUIWindowManager.ActiveWindow
            If AktWinId = GetID And NothingInProgress Then
                Try
                    Logger.DebugM("Keypress on VideoReDo Screen. KeyChar={0} ; KeyCode={1} ; Actiontype={2}", action.m_key.KeyChar, action.m_key.KeyCode, action.wID.ToString)
                Catch ex As Exception
                    Logger.DebugM("Action on VideoReDo Screen. Action={0}", action.wID.ToString)
                End Try
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then
                    If action.m_key IsNot Nothing Then

                    End If
                End If
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM Or action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_DOWN Or action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_UP Then
                    If lstRecList.IsFocused Then
                        RecList_ItemSelected(False)
                    End If
                End If
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM And lstEpisodesList.IsFocused Then
                    EpisodeList_ItemSelected()
                End If

                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_CONTEXT_MENU Then
                    ' MsgBox("Context")
                End If
            End If
        End Sub


        Protected Overrides Sub OnClicked(ByVal controlId As Integer, ByVal control As MediaPortal.GUI.Library.GUIControl, ByVal actionType As MediaPortal.GUI.Library.Action.ActionType)
            MyBase.OnClicked(controlId, control, actionType)
            '---Recordingliste---
            If NothingInProgress Then
                If control Is lstRecList Then
                    RecList_ItemSelected(True)
                End If
                '---UseAsSeries Checkbutton---
                If control Is btnCheckUseAsSeries Then
                    btnUseAsSeries_Clicked()
                End If
                '--- Die Episodenliste---
                If control Is lstEpisodesList Then
                    EpisodeList_ItemSelected()
                    lstEpisodesList.Visible = False
                    GUIControl.FocusControl(GetID, btnCutVideo.GetID)
                End If
                '--- DerCut-Button ---
                If control Is btnCutVideo Then
                    RecordingCut()
                End If
            End If
        End Sub

        Protected Overrides Sub OnPageDestroy(ByVal new_windowId As Integer)
            MyBase.OnPageDestroy(new_windowId)
            If new_windowId < 1208 Or new_windowId > 1212 Then 'Wenn kein Fenster vom Plugin
                Logger.DebugM("Start page is closing...")
                'System.Diagnostics.Process.GetProcessesByName("VRDQuickstreamfix")(0).Kill()
                If VRD IsNot Nothing Then
                    Logger.DebugM("VRD.CutMarkerList.count = {0} ; VRD is Nothing = {1}", VRD.CutMarkerList.Count, IIf(VRD Is Nothing, "True", "False").ToString)
                    'Dialog ob man wirklich beenden will
                    'If (HelpConfig.GetConfigString(ConfigKey.AlwaysSaveProject) = False) And (VRD.AdScanInProgress = False) And (VRD.OutputInProgress = False) Then
                    If (VRD.AdScanInProgress = False) And (VRD.OutputInProgress = False) Then
                        'Logger.DebugM("Zeige Dialog ob VRD gelöscht werden sollen...")
                        'If DialogYesNo(GetID, False, Translation.CloseVRD, Translation.CloseVRD1, Translation.CloseVRD2, Translation.CloseVRD3) = True Then
                        Logger.DebugM("VRD-Objekte werden zerstört.")
                        VRD.Close()
                        VRD.Dispose()
                        VRD = Nothing
                        'Else
                        ' Logger.DebugM("User hat den Dialog nicht bestätigt, VRD wird nicht beendet.")
                        ' End If
                    End If
                End If
                Logger.DebugM("Start Page was closed.")
            Else
                'GUIWindowManager.Clear()
            End If
        End Sub


        ''' <summary>
        ''' Füllt das ListControl für Aufnahmen mit den Aufnahmen und den Ordnern im RecordingPath
        ''' </summary>
        ''' <param name="RecordingPath">Der Pfad der aktuell geladen werden soll</param>
        Private Sub FillRecListControl(ByVal RecordingPath As String)
            lstRecList.ListItems.Clear()
            RecList = New clsRecordings(RecordingPath)
            Logger.DebugM("Filling RecordingListcontrol with path {0}", RecordingPath)
            Logger.DebugM("There are {0} recordings to load in this path.", RecList.lRecordings.Count)
            'Wenn es nicht der RecordingRoot ist dann .. einfügen
            Dim itemcount As Integer
            itemcount = 0
            If RecordingPath <> RecRootPath Then
                Dim lItem As New GUIListItem
                lItem.ItemId = lstRecList.ListItems.Count - 1
                lItem.Label = ".."
                lItem.IconImage = "defaultFolderBack.png"
                lItem.IsFolder = True
                lItem.Path = Directory.GetParent(RecordingPath).FullName
                Logger.DebugM("Filling RecordingListcontrol with pathlabel {0} and path: {1}", lItem.Label, lItem.Path)
                GUIControl.AddListItemControl(GetID, lstRecList.GetID, lItem)
            End If
            'Erst Liste mit Ordnern Füllen
            Try
                For Each dire In Directory.GetDirectories(RecordingPath)
                    Dim lItem As New GUIListItem
                    lItem.ItemId = lstRecList.ListItems.Count - 1
                    lItem.Label = Path.GetFileName(dire)
                    lItem.IconImage = "defaultFolder.png"
                    lItem.ThumbnailImage = lItem.IconImage
                    lItem.IsFolder = True
                    lItem.Path = dire
                    Logger.DebugM("Filling RecordingListcontrol with pathlabel {0} and path: {1}", lItem.Label, lItem.Path)
                    GUIControl.AddListItemControl(GetID, lstRecList.GetID, lItem)
                    itemcount = itemcount + 1
                Next

                ' ToDo Machbarkeitsprüfung für Odner alternativ zu Aufnahmeordnern
                ' Jetzt mit den Aufnamen im Pfad füllen
                For Each item As clsRecordings.Recordings In RecList.lRecordings
                    Dim lItem As New GUIListItem
                    If item.Title <> "" Then
                        lItem.Label = item.Title & " - " & item.Channelname
                        lItem.ItemId = lstRecList.ListItems.Count - 1
                        If item.Episodename <> "" Then
                            lItem.Label2 = item.Episodename & " - "
                        End If
                        lItem.Label2 = lItem.Label2 & item.StartTime.Date
                        lItem.Path = item.VideoFilename
                        lItem.IconImage = GetSaveThumbPath(item)
                        lItem.ThumbnailImage = GetSaveThumbPath(item)
                        lItem.IsFolder = False
                        Logger.DebugM("Filling RecordingListcontrol with file {0} and path: {1}", lItem.Label, lItem.Path)
                        GUIControl.AddListItemControl(GetID, lstRecList.GetID, lItem)
                        itemcount = itemcount + 1
                    End If
                Next
                lstRecList.SelectedListItemIndex = 0
                RecList_ItemSelected(False)
                GUIPropertyManager.SetProperty("#itemcount", itemcount)
                Logger.Info("{0} recordings were loaded to the list.", RecList.lRecordings.Count)
            Catch ex As IO.IOException
                Logger.Warn("Error in FillRecListControl():{0}", ex.ToString)
            End Try
        End Sub

        ''' <summary>
        ''' Recordingliste wurde ein Item selectiert bzw. wurde geklickt
        ''' </summary>
        ''' <param name="Sel"></param>
        ''' <remarks></remarks>
        Private Sub RecList_ItemSelected(ByVal Sel As Boolean)
            lstEpisodesList.Visible = False
            GUIButtonControl.DisableControl(GetID, btnCutVideo.GetID)
            GUIButtonControl.DisableControl(GetID, btnCheckUseAsSeries.GetID)
            btnCheckUseAsSeries.Selected = False
            Logger.DebugM("RecordingListControl was selected")
            'Ist es eine Datei oder ein Ordner

            If lstRecList.SelectedListItem.IsFolder Then
                Logger.DebugM("Selection is a folder: {0} ; Index:{1}", lstRecList.SelectedListItem.Label, lstRecList.SelectedItem)
                If Sel Then FillRecListControl(lstRecList.SelectedListItem.Path)
                SetRecordingsDetails(lstRecList.SelectedListItem)
            Else
                GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                GUIButtonControl.EnableControl(GetID, btnCheckUseAsSeries.GetID)
                Logger.DebugM("Selection is a file: {0} ; Index:{1}", lstRecList.SelectedListItem.Label, lstRecList.SelectedItem)
                SetRecordingsDetails(lstRecList.SelectedListItem)
                If Sel Then GUIControl.FocusControl(GetID, btnCutVideo.GetID)
            End If

        End Sub

        ''' <summary>
        ''' Episodeliste wurde ein Item selectiert bzw. wurde geklickt
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub EpisodeList_ItemSelected()
            For Each item In lstEpisodesList.ListItems
                If item.IconImage <> "" Then
                    item.IconImage = ""
                End If
            Next
            lstEpisodesList.SelectedListItem.IconImage = "MPVideoRedo5\MPVideoRedo5SelectedEpisode.png"
            AktEpisode = AktSerie.Episodes(lstEpisodesList.SelectedListItemIndex)

            If AktSerie.Genre.Count > 0 Then lastSelRecording.Genre = AktSerie.Genre(0)
            lastSelRecording.SeriesNum = AktEpisode.SeasonNumber
            lastSelRecording.EpisodeNum = AktEpisode.EpisodeNumber
            lastSelRecording.Episodename = AktEpisode.EpisodeName
            lastSelRecording.Seriesname = AktSerie.SeriesName
        End Sub

        ''' <summary>
        ''' Die Sub für den Backgroundthread welche die Serieninfos abruft
        ''' </summary>
        Private Sub GetSeriesInfosBackground()
            Try
                Dim SetSerie As TvdbSeries = GetActSeries(GetActRecording(lstRecList.SelectedListItem.Path))
                AktSerie = SetSerie
                If SetSerie IsNot Nothing Then
                    imgWaitingEpisodes.Visible = True
                    lstEpisodesList.Visible = True
                    FillSeriesSkinProperties(SetSerie)
                Else
                    lstEpisodesList.Visible = False
                    btnCheckUseAsSeries.Selected = False
                End If
            Catch ex As System.Threading.ThreadAbortException
                imgWaitingEpisodes.Visible = False
                lstEpisodesList.ListItems.Clear()
                Logger.DebugM("Get Series Background thread ended.")
            End Try
        End Sub

        ''' <summary>
        ''' Setzt die Labels im Skin mit den Daten der aktuellen Aufnahme und die Variable 'lastSelRecording'
        ''' </summary>
        ''' <param name="SelItem">Das aktuell selektierte Objekt in der GUIListControl</param>
        Private Sub SetRecordingsDetails(ByVal SelItem As GUIListItem)
            Try
                Logger.DebugM("Filling recording skin properties. Actual selection: {0}", SelItem.Path)
                If SelItem.IsFolder Then
                    Translator.SetProperty("#RecordingTitle", Translation.Title & ": " & SelItem.Label)
                    Translator.SetProperty("#RecordingGenre", " ")
                    Translator.SetProperty("#RecordingEpisodename", " ")
                    Translator.SetProperty("#RecordingImage", "")
                    GUIButtonControl.DisableControl(GetID, btnCutVideo.GetID)
                    GUIButtonControl.DisableControl(GetID, btnCheckUseAsSeries.GetID)
                    btnCheckUseAsSeries.Selected = False
                Else
                    Dim item As clsRecordings.Recordings = GetActRecording(SelItem.Path)
                    lastSelRecording = item

                    Translator.SetProperty("#RecordingTitle", Translation.Title & ": " & item.Title)
                    Translator.SetProperty("#RecordingGenre", Translation.Genre & ": " & item.Genre)

                    If item.SeriesNum = 0 OrElse item.EpisodeNum = 0 Then
                        btnCheckUseAsSeries.Selected = False
                        Translator.SetProperty("#RecordingEpisodename", Translation.EpisodeTitle & ": " & item.Episodename)
                    Else
                        Dim SeriesNumb As String = "S" & item.SeriesNum.ToString("00") & "E" & item.EpisodeNum.ToString("00")
                        btnCheckUseAsSeries.Selected = True
                        Translator.SetProperty("#RecordingEpisodename", Translation.EpisodeTitle & ": " & item.Episodename & " (" & SeriesNumb & ")")
                    End If
                    Translator.SetProperty("#RecordingImage", SelItem.IconImage)
                    GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                    GUIButtonControl.EnableControl(GetID, btnCheckUseAsSeries.GetID)
                End If
            Catch ex As Exception
                Logger.Error("Error in SetRecordingsDetails. Error: {0}", ex.ToString)
            End Try
        End Sub

        ''' <summary>
        ''' Setzt die SkinPropertie mit den Infos einer Serie und füllt die Episodenliste
        ''' </summary>
        ''' <param name="Serie">Die Serie als TVdbSeries</param>
        Private Sub FillSeriesSkinProperties(ByVal Serie As TvdbSeries)
            Logger.Info("Filling series skin properties with infos from series {0}", Serie.SeriesName)
            Dim selIndex As Integer = 0
            AktSerie = Serie
            Try
                Translator.SetProperty("#NewSeriesName", Serie.SeriesName)
                If Serie.PosterBanners.Count > 0 Then
                    Serie.PosterBanners(0).LoadBanner()
                    Dim SeriesBanner As String = GetBannerPath(Serie.PosterBanners(0).BannerImage, Serie.Id)
                    Translator.SetProperty("#Seriescover", SeriesBanner)
                    Logger.DebugM("Series banner was loaded from {0}.", SeriesBanner)
                Else
                    'DEFAULT BANNER Laden
                    Translator.SetProperty("#Seriescover", " ")
                    Logger.DebugM("Series banner does exist - using empty string")
                End If
                Translator.SetProperty("#NewSeriesOverview", Serie.Overview)
                lstEpisodesList.ListItems.Clear()
                imgWaitingEpisodes.Visible = True
                Logger.Info("Beginne mit dem Füllen der Episodenliste der Serie {0}. Es sind {1} Episoden vorhanden...", Serie.SeriesName, Serie.Episodes.Count)
                For Each eps As TvdbEpisode In Serie.Episodes
                    Dim epsLitem As New GUIListItem
                    epsLitem.Label = eps.EpisodeName
                    epsLitem.Label2 = String.Format("S{0}E{1}", eps.SeasonNumber, eps.EpisodeNumber)
                    Logger.DebugM("Füge Episode {0} - {1} der GUI-List hinzu", epsLitem.Label, epsLitem.Label2)

                    Dim vergleich As New MyStringComparer

                    Logger.Info("Vergleiche Epsisodentitel der Aufnahme ('{0}') mit dem Titel der OnlineEpisode ('{1}')", GetActRecording(lstRecList.SelectedListItem.Path).Episodename, eps.EpisodeName)

                    Dim VergleichProz As Single = 0
                    Try
                        If GetActRecording(lstRecList.SelectedListItem.Path).Episodename.Length < 3 Or eps.EpisodeName.Length < 3 Then
                            VergleichProz = 0
                        Else
                            VergleichProz = vergleich.IsEqual(GetActRecording(lstRecList.SelectedListItem.Path).Episodename, eps.EpisodeName)
                        End If
                    Catch ex As IndexOutOfRangeException
                        VergleichProz = 0
                        Logger.Info("Überlauf beim vergleichen der beiden Strings festgestellt, folge wird übersprungen...")
                    Catch ex As Exception
                        VergleichProz = 0
                        Logger.Warn("Fehler beim vergleichen der beiden Strings festgestellt. Fehler: " & ex.ToString)
                    End Try

                    Logger.DebugM("Vergleich ergab eine Übereinstimmung von {0}%", VergleichProz * 100)
                    If VergleichProz > 0.9 Then
                        Logger.Info("Der Vergleich ergab {0}% und wird somit markiert und die Veriablen werden aktualisiert. Aktuelle Episode: {1}", VergleichProz * 100, eps.EpisodeName)
                        epsLitem.IconImage = "MPVideoRedo5\MPVideoRedo5SelectedEpisode.png"
                        Translator.SetProperty("#NewSeriesName", Serie.SeriesName & " - " & String.Format("S{0}E{1}", eps.SeasonNumber, eps.EpisodeNumber))
                        selIndex = lstEpisodesList.ListItems.Count
                        AktEpisode = eps
                    End If
                    GUIControl.AddListItemControl(GetID, lstEpisodesList.GetID, epsLitem)
                Next
                lstEpisodesList.SelectedListItemIndex = selIndex
                lstEpisodesList.Item(selIndex).Selected = True
                GUIControl.RefreshControl(GetID, lstEpisodesList.GetID)
                imgWaitingEpisodes.Visible = False
                GUIControl.FocusControl(GetID, lstEpisodesList.GetID)
                'ctlEpisodesList.Visible = True
            Catch ex As Exception
                Logger.Error("Fehler in FillSeriesSkinProperties. Fehler: {0}", ex.ToString)
            End Try

        End Sub

        ''' <summary>
        ''' Durchsucht die clsRecordings nach übereinstimmung durch Pfad und gibt die Aufnahme als Objekt zurück
        ''' </summary>
        ''' <param name="Path">Den Pfad der Aufnahme</param>
        Private Function GetActRecording(ByVal Path As String) As clsRecordings.Recordings
            For Each item As clsRecordings.Recordings In RecList.lRecordings
                If item.VideoFilename = Path Then
                    Logger.DebugM("Gebe aktuelle Aufnahme zurück. Aktuell: {0}", item.Filename)
                    Return item
                    Exit For
                End If
            Next
            Logger.Warn("Achtung, aktuell zurückgegebene Aufnahme ist Nothing")
            Return Nothing
        End Function

        ''' <summary>
        ''' Läd Online eine Serie nach den Angaben in einem RecordingsObjekt und gibt die Serie zurück
        ''' </summary>
        Private Function GetActSeries(ByVal AktRec As clsRecordings.Recordings) As TvdbSeries
            Try
                GUIButtonControl.DisableControl(GetID, btnCutVideo.GetID)
                Logger.DebugM("GetActSeries() started")
                lstEpisodesList.Visible = False
                imgWaitingEpisodes.Visible = True
                Logger.DebugM("Instanziere the replacer...")
                Dim Replacer As New clsSeriesReplacer

                Dim LanguageString As String = HelpConfig.GetConfigString(ConfigKey.FavSeriesLanguage)
                Try
                    LanguageString = Translator.Lang
                    Logger.Info("Determining language:(from CultureInfo) {0}", LanguageString)
                Catch ex As Exception
                    LanguageString = HelpConfig.GetConfigString(ConfigKey.FavSeriesLanguage)
                    Logger.Warn("Determining language:(from Config) {0}", LanguageString)
                End Try

                Dim prov As New XmlCacheProvider(HelpConfig.GetConfigString(ConfigKey.TVdbAPICachePath))
                Dim TheTVdbHandler As New TvdbHandler(prov, HelpConfig.GetConfigString(ConfigKey.TVdbAPI))
                Dim m_languages As List(Of TvdbLanguage) = TheTVdbHandler.Languages
                Dim SerieDirekt As TvdbLib.Data.TvdbSeries = Nothing
                Dim listFoundedSeries As New List(Of TvdbLib.Data.TvdbSearchResult)

                Logger.DebugM("Initialising the Cache for TheTVdbLib")
                TheTVdbHandler.InitCache()

                Logger.DebugM("Setting tvDB language...")
                'Seriensprache festlegen
                Dim myLanguages As TvdbLanguage = Nothing
                For l = 0 To m_languages.Count - 1
                    Debug.WriteLine(m_languages(l).Name)
                    If m_languages(l).Abbriviation = LanguageString Then myLanguages = m_languages(l)
                Next
                'Wenn nicht in der Sprache vorhanden dann Default Language nehmen (English)
                If myLanguages Is Nothing Then myLanguages = TvdbLanguage.DefaultLanguage
                Logger.Info("tvDB language is {0}", myLanguages.Name)

                Logger.DebugM("Starting renaming of the series names for better search results.")
                'Die ReplaceStrings laden damit wir die Serie unbenennen können
                Replacer = Replacer.Load()
                Dim ReplaceSeriesID As Integer = 0
                Dim NewSeriesTitle As String = AktRec.Title
                Logger.Info("Original series name from recordings: {0}", NewSeriesTitle)
                If Replacer.ReplacerList.Count > 0 Then
                    Logger.Info("{0} Replacer has been found, starting with processing.", Replacer.ReplacerList.Count)
                    For Each item In Replacer.ReplacerList

                        Logger.Info("Vergleiche RecordingTitel '{0}' mit Originalstring aus Replacerklasse '{1}'", AktRec.Title.ToLower, item.OriginalString.ToLower)
                        If AktRec.Title.ToLower = item.OriginalString.ToLower Then
                            Logger.DebugM("Werte waren gleich, ersetzte Suchstring für Serie")
                            Logger.Info("Versuche ob der Replace {0} von {1} in eine Zahl gewandelt werden kann...", item.ReplaceString, item.OriginalString)
                            Int32.TryParse(item.ReplaceString, ReplaceSeriesID)
                            If ReplaceSeriesID > 0 Then
                                Logger.Info("Erfolgreich umgewandelt, schleife wird verlassen da die Serie DIREKT geladen werden kann.")
                                Exit For
                            Else
                                Logger.Info("{0} konnte nicht in eine Zahl umgewandelt werden", item.ReplaceString)
                            End If
                            NewSeriesTitle = Replace(NewSeriesTitle, item.OriginalString, item.ReplaceString)
                            Logger.Info("Neuer Serientitel mit dem gesucht wird: {0}", NewSeriesTitle)
                            Exit For
                        Else
                            Logger.Info("Werte waren ungleich, versuche nächsten Replacer")
                        End If
                    Next
                Else
                    Logger.Info("Da keine Replacer gefunden wurden wird versucht die Serie durch kürzen des Titels zu finden.")
                    Dim cutIndex As Integer = InStr(NewSeriesTitle, " ")
                    If cutIndex > 0 Then NewSeriesTitle = Mid(NewSeriesTitle, 1, cutIndex - 1)
                    Logger.Info("Searching for string '{0}'", NewSeriesTitle)
                End If
                If ReplaceSeriesID > 0 Then
                    'direkt die Serie mit der ID holen
                    Logger.Info("Serie wird direkt abgerufen mit der SeriesID {0}", ReplaceSeriesID)
                    SerieDirekt = TheTVdbHandler.GetSeries(ReplaceSeriesID, myLanguages, True, False, True)
                    Logger.Info("Serie erfolgreich geladen. Serientitel: {0}", SerieDirekt.SeriesName)
                    lstEpisodesList.Visible = True
                    Return SerieDirekt
                Else
                    'Suche nach Serien
                    Logger.Info("Serie wird über die Engine mit Suchstring '{0}' gesucht.", NewSeriesTitle)
                    listFoundedSeries = TheTVdbHandler.SearchSeries(NewSeriesTitle)
                    Logger.Info("Suche ergab {0} treffer.", listFoundedSeries.Count.ToString)
                End If
                'Wenn es nur ein Suchergebniss gibt dann direkt laden.
                If listFoundedSeries.Count = 1 Then
                    Logger.Info("Es wurde nur {0} Serien gefunden, verwende diese", listFoundedSeries.Count)
                    Logger.Info("Verwende Serie {0}", listFoundedSeries(0).SeriesName)
                    lstEpisodesList.Visible = True
                    Return TheTVdbHandler.GetSeries(listFoundedSeries(0).Id, myLanguages, True, False, True)
                End If
                'Es gibt mehrere Ergebnisse
                If listFoundedSeries.Count > 0 Then
                    Logger.Info("Es wurden {0} Serien gefunden, beginne mit erstellung des Dialogs zum auswählen", listFoundedSeries.Count)
                    Dim SeriesDlg As GUIDialogMenu = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_MENU, Integer)), GUIDialogMenu)
                    SeriesDlg.Reset()
                    SeriesDlg.SetHeading(Translation.ChooseSeries)
                    SeriesDlg.ShowQuickNumbers = False
                    For Each item In listFoundedSeries
                        Logger.DebugM("Erstelle Listitem für Serie {0}...", item.SeriesName)
                        Logger.DebugM("Rufe Serieninfos ab...")
                        Dim TempSerie As TvdbLib.Data.TvdbSeries = TheTVdbHandler.GetSeries(item.Id, myLanguages, True, False, True)
                        Logger.DebugM("Infos abgerufen")
                        Logger.DebugM("Lade Banner für Serie {0}", item.SeriesName)
                        Do Until TempSerie.BannersLoaded

                        Loop
                        Logger.DebugM("Banner geladen")
                        Dim mItem As New GUIListItem()
                        mItem.Label = item.SeriesName
                        If TempSerie.PosterBanners.Count > 0 Then
                            TempSerie.PosterBanners(0).LoadBanner()
                            '    mItem.IconImageBig = GetSaveImagePath(TempSerie.PosterBanners(0).BannerImage, TempSerie.Id)
                            mItem.IconImage = GetBannerPath(TempSerie.PosterBanners(0).BannerImage, TempSerie.Id)

                            Logger.DebugM("Poster geladen: {0}", GetBannerPath(TempSerie.PosterBanners(0).BannerImage, TempSerie.Id))
                        End If
                        SeriesDlg.Add(mItem)
                        Logger.DebugM("Menüitem hinzugefügt")
                    Next
                    Logger.DebugM("Erstelle Listitem für das suchen über die OnScreen Tastatur...")
                    Dim mItemKeyboard As New GUIListItem()
                    mItemKeyboard.Label = Translation.SearchWithAnotherString
                    'mItemKeyboard.IconImageBig = GetSaveImagePath(TempSerie.PosterBanners(0).BannerImage)
                    mItemKeyboard.IconImage = "MPVideoRedo5Keyboard.png"
                    SeriesDlg.Add(mItemKeyboard)
                    Logger.DebugM("Menüitem hinzugefügt")

                    imgWaitingEpisodes.Visible = False
                    Logger.DebugM("Zeige Dialog für Serienauswahl...")
                    SeriesDlg.DoModal(GetID)
                    imgWaitingEpisodes.Visible = True
                    Dim selindex As Integer = SeriesDlg.SelectedLabel
                    'Bei abbruch Dialog zeigen
                    If selindex = -1 Then
                        imgWaitingEpisodes.Visible = False
                        DialogNotify(GetID, 5, Translation.Abort, Translation.UserAbortDialog)
                        GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                        Return Nothing
                    End If
                    If selindex = listFoundedSeries.Count Then
                        RecordingToCut.Title = ShowKeyboard(NewSeriesTitle, GetID)
                        GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                        Return GetActSeries(RecordingToCut)
                    End If
                    lstEpisodesList.Visible = True
                    GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                    Return TheTVdbHandler.GetSeries(listFoundedSeries(selindex).Id, myLanguages, True, False, True)
                Else
                    RecordingToCut.Title = ShowKeyboard(NewSeriesTitle, GetID)
                    Dim KeySerie As TvdbSeries = GetActSeries(RecordingToCut)
                    If KeySerie Is Nothing Then
                        'KEINE SERIE GEFUNDEN,ABBRUCHDIALOG ZEIGEN
                        DialogNotify(GetID, 5, Translation.NothingFound, Translation.NoSeriesFoundDialog)
                        lstEpisodesList.Visible = False
                        imgWaitingEpisodes.Visible = False
                        GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                        Return Nothing
                    Else
                        GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                        Return KeySerie
                    End If
                End If
            Catch ex As Exception
                Logger.Error("Error in GetAktSerie() - Error: {0}", ex.ToString)
                GUIButtonControl.EnableControl(GetID, btnCutVideo.GetID)
                Return Nothing
            End Try
        End Function

#Region "btnCutVideo & btnUseAsSeries"

        Private Sub btnUseAsSeries_Clicked()
            If btnCheckUseAsSeries.Selected Then
                imgWaitingEpisodes.Visible = True
                lstEpisodesList.ListItems.Clear()
                'Falls noch ein Thread läuft dann zuerst beenden
                If trSeries.IsAlive Then
                    trSeries.Abort()
                    imgWaitingEpisodes.Visible = False
                    Logger.DebugM("Warte auf beendigung des Backgroudthreads...")
                    Do Until trSeries.IsAlive = False
                        Threading.Thread.Sleep(100)
                    Loop
                    Logger.DebugM("Backgroudthreads wurde beendet")
                End If
                trSeries = New Threading.Thread(AddressOf GetSeriesInfosBackground)
                trSeries.IsBackground = True
                trSeries.Priority = Threading.ThreadPriority.Lowest
                trSeries.Start()
            Else
                lstEpisodesList.Clear()
                lstEpisodesList.Visible = False
                imgWaitingEpisodes.Visible = False
            End If
        End Sub

        Private Sub RecordingCut()
            If btnCheckUseAsSeries.Selected Then
                'es ist eine Serie
                'lastSelRecording.Genre = AktSerie.Genre(0)
                'lastSelRecording.SeriesNum = AktEpisode.SeasonNumber
                'lastSelRecording.EpisodeNum = AktEpisode.EpisodeNumber
                'lastSelRecording.Episodename = AktEpisode.EpisodeName
                'lastSelRecording.Seriesname = AktSerie.SeriesName
                lastSelRecording.SavingFilename = ParseSaveVideoFilename(lastSelRecording, True)
                RecordingToCut = lastSelRecording
            Else
                'es ist ein film
                imgWaiting.Visible = True
                lastSelRecording.SavingFilename = ParseSaveVideoFilename(lastSelRecording)
                RecordingToCut = lastSelRecording
            End If
            Logger.Info("LastSelRecording.Savingfilename: {0}", lastSelRecording.SavingFilename)

            GUIWindowManager.ActivateWindow(enumWindows.GUIMain)
        End Sub

#End Region


        Private Sub QuickStreamFixIsNeeded(sender As Object, e As EventArgs)
            Dim Title As String = "QuickStreamfix needed..."
            Dim Text As String = "Die Datei benötigt einen Quickstreamfix, ohne diesen kann das File nicht bearbeitet werden. Möchtest du einen Quickstreamfix durchführen oder abbrechen?"
            Dim oldMedia As String = Nothing
            If VRD IsNot Nothing Then
                oldMedia = VRD.MediaToCut
                g_Player.Stop()
                VRD = Nothing
                KillVRD()
            End If
            MoviestripBarUnload()
            CutBarUnload()
            If DialogYesNo(GUIWindowManager.ActiveWindow, True, Translation.QuickStreamFix, Translation.QuickStreamFix1, Translation.QuickStreamFix2) = True Then
                If oldMedia IsNot Nothing Then
                    VRD = New VideoReDo5(False)
                    VRD.LoadMediaToCut(oldMedia, True)
                    imgWaiting.Visible = True
                    lastSelRecording.SavingFilename = ParseSaveVideoFilename(lastSelRecording)
                    RecordingToCut = lastSelRecording
                End If
                Logger.Info("LastSelRecording.Savingfilename: {0}", lastSelRecording.SavingFilename)
                GUIWindowManager.ActivateWindow(enumWindows.GUISave)
            End If

        End Sub

        Private Sub KillVRD()
            Dim p() As Process = System.Diagnostics.Process.GetProcesses
            For Each item As Process In p
                If item.ProcessName.Contains("VideoReDo") Then
                    item.Kill()
                    Logger.DebugM("VideoRedo Process has been killed...")
                    'Exit For
                End If
            Next
        End Sub
    End Class
End Namespace
