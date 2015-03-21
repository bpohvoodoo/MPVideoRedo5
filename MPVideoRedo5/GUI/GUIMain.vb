Imports System
Imports System.Windows.Forms
Imports MediaPortal.GUI.Library
Imports MediaPortal.Dialogs
Imports MediaPortal.Player
Imports MediaPortal.Profile
Imports MediaPortal.Configuration
Imports System.Collections
Imports System.Collections.Generic
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging


Namespace MPVideoRedo5
    Public Class GUIMain
        Inherits GUIWindow

#Region "SkinControls"
        <SkinControlAttribute(44)> Protected VideoWindow As GUIVideoControl = Nothing
        <SkinControlAttribute(45)> Protected lblAudioResync As GUILabelControl = Nothing
        <SkinControlAttribute(46)> Protected sliderAudioSync As GUISliderControl = Nothing
        <SkinControlAttribute(23)> Protected btnCutSave As GUIButtonControl = Nothing
        <SkinControlAttribute(24)> Protected btnHelp As GUIButtonControl = Nothing
        <SkinControlAttribute(4)> Protected btnCutMake As GUIButtonControl = Nothing
        <SkinControlAttribute(5)> Protected btnCutChange As GUIButtonControl = Nothing
        <SkinControlAttribute(51)> Protected CutList As GUIListControl = Nothing
        <SkinControlAttribute(71)> Protected CutBarImage As GUIImage = Nothing
        <SkinControlAttribute(13)> Protected AnimWaiting As GUIAnimation = Nothing
        <SkinControlAttribute(25)> Protected HelpBackgroundImage As GUIImage = Nothing
        <SkinControlAttribute(26)> Protected btnExitHelp As GUIButtonControl = Nothing
        <SkinControlAttribute(29)> Protected ctlStillImage As GUIImage = Nothing
#End Region

#Region "Variablen"
        Private TempStartValue As Single
        Private AlwaysRefreshMoviestripThumbs As Boolean
        Private TempEndValue As Single
        Private PlayerPosition As Long
        Private PlayerDuration As Long
        Private PlayerFramerate As Integer
        Private ThumbCount As Integer
        Friend tmrDelayRefreshOnSkip As New Timer
        Friend tmrCheckplayback As New Timer
        Friend tmrAdScan As New Timer
        Friend tmrRefresh As New Timer
        Private IsLoadingHDContent As Boolean = False
        Dim Props As PropertyCollection
        Dim NoImage As System.Drawing.Image
        Dim PauseOnStartOnce As Boolean
        Dim ChangeCutMode As Boolean
        Dim ChangeCutModeMarker As Integer
        Dim VRDList As New List(Of Long)
#End Region

#Region "MP EventSubs"

        Public Overloads Overrides Property GetID() As Integer
            Get
                Return enumWindows.GUIMain
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property

        Protected Overridable ReadOnly Property SerializeName() As String
            Get
                Return "MPVideoRedo5"
            End Get
        End Property

        Public Overloads Overrides Function Init() As Boolean
            Return Load(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml")
        End Function

        Public Overrides Function GetModuleName() As String
            Return HelpConfig.GetConfigString(ConfigKey.ModuleName) & " - " & Translation.ModuleMain
        End Function

        Protected Overrides Sub OnPageLoad()
            MyBase.OnPageLoad()
            btnCutChange.Visible = False
            If GUIWindowManager.ActiveWindow = GetID Then
                If VRD.OutputInProgress = True Then
                    'GUIWindowManager.ActivateWindow(enumWindows.GUIstart, True)
                    Logger.DebugM("GUIMain Output in Progress. Redirecting.")
                    GUIWindowManager.ActivateWindow(enumWindows.GUISave, True)
                    Exit Sub
                End If
                ctlStillImage.RemoveMemoryImageTexture()
                ctlStillImage.FileName = GUIGraphicsContext.Skin & "\Media\MPVideoRedo5\MPVideoRedo5Working.png"
                Logger.DebugM("Set Playerhandler PlayBackStarted, PlayBackChanged, PlayBackEnded and PlayBackStopped")
                AlwaysRefreshMoviestripThumbs = HelpConfig.GetConfigString(ConfigKey.AlwaysRefreshMoviestripThumbs)
                tmrDelayRefreshOnSkip.Interval = 1000
                tmrDelayRefreshOnSkip.Enabled = False
                tmrCheckplayback.Interval = 100
                tmrCheckplayback.Enabled = False
                tmrRefresh.Interval = HelpConfig.GetConfigString(ConfigKey.AlwaysRefreshMoviestripThumbsDelay) * 1000
                tmrRefresh.Enabled = False
                NoImage = System.Drawing.Image.FromFile(GUIGraphicsContext.Skin & "\Media\MPVideoRedo5\MPVideoRedo5EmptyThumb.png")
                Props = BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml")
                ThumbCount = Props("ThumbnailsCount")
                PauseOnStartOnce = HelpConfig.GetConfigString(ConfigKey.PauseOnStart)
                AddHandler g_Player.PlayBackStarted, AddressOf VideoStarted
                AddHandler g_Player.PlayBackChanged, AddressOf VideoChange
                AddHandler g_Player.PlayBackEnded, AddressOf VideoEnded
                AddHandler g_Player.PlayBackStopped, AddressOf VideoStopped
                AddHandler tmrDelayRefreshOnSkip.Tick, AddressOf DelayOnRefreshTimerTick
                AddHandler tmrCheckplayback.Tick, AddressOf CheckPlaybackTick
                AddHandler tmrRefresh.Tick, AddressOf DelayOnRefreshTimerTick
                Logger.DebugM("Handler set")
                RecordingLoad()
                Logger.DebugM("Ermittle aktuelle Videoformat...")
                Dim CurrFormat As MediaPortal.Player.VideoStreamFormat = g_Player.GetVideoFormat
                Logger.DebugM("Aktuelles Videoformat des g_players ist {0} mit einer Auflösung von {1} * {2} und einer Bitrate von {3} .", _
                             CurrFormat.streamType.ToString, CurrFormat.width, CurrFormat.height, CurrFormat.bitrate)
                If CurrFormat.streamType = VideoStreamType.H264 Then
                    If Left(VRD.ReDoVersion, 1) < 5 Then
                        CutBarUnload()
                        MoviestripBarUnload()
                        DialogNotify(GetID, 10, Translation.ErrorOccured, Translation.VideoRedoNotCompatible)
                        MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                        CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                        g_Player.Stop()
                        GUIWindowManager.ActivateWindow(GetGUIWindow(enumWindows.GUIstart), True)
                        Exit Sub
                    Else
                        IsLoadingHDContent = True
                        VRD.SavingProfile = HelpConfig.GetConfigString(ConfigKey.VRDProfileH264)
                    End If
                Else
                    VRD.SavingProfile = HelpConfig.GetConfigString(ConfigKey.VRDProfile)
                End If
                GetProfileDetail(HelpConfig.GetConfigString(ConfigKey.VRDProfile))
                tmrCheckplayback.Enabled = True
                tmrRefresh.Enabled = AlwaysRefreshMoviestripThumbs
            End If
        End Sub

        Protected Overrides Sub OnPageDestroy(ByVal new_windowId As Integer)
            If VRD IsNot Nothing Then
                If VRD.OutputInProgress Then Exit Sub
                If GUIWindowManager.ActiveWindow = enumWindows.GUIMain Then ' My Window
                    CutBarUnload()
                    MoviestripBarUnload()
                    If VRD.AdScanInProgress Then
                        Logger.DebugM("Showing YES/NO Dialog for Aborting the AdDetective Scan...")
                        If DialogYesNo(GetID, False, Translation.ContinueScan, " ", Translation.ContinueScan1, ) = True Then
                            Logger.DebugM("Dialog was confirmed. Continuing with AdScanning in background!")
                            'zum Ausgangschirm vor dem Aufruf von MPVideoRedo5 zürckkehren
                            'ToDo
                        Else
                            Logger.DebugM("Scan should be aborted.")
                            VRD.AbortScan()
                            Do While VRD.AdScanInProgress
                            Loop
                            Logger.DebugM("Scan was aborted!")
                        End If
                    End If
                    tmrCheckplayback.Enabled = False
                    tmrRefresh.Enabled = False
                    g_Player.Stop()
                    If new_windowId < GetID Then
                        If VRD.AdScanInProgress = False And VRD.CutMarkerList.Count > 1 Then
                            If HelpConfig.GetConfigString(ConfigKey.AlwaysSaveProject) = False Then
                                If DialogYesNo(GetID, False, Translation.ClearCutsAtClose, " ", Translation.ClearCutsAtClose1) = False Then
                                    ProjectSave(False)
                                Else
                                    SceneClear()
                                End If
                            Else
                                ProjectSave(False)
                            End If
                        End If
                        If VRD.AdScanInProgress = False Then
                            RecordingToCut = Nothing
                        End If
                    End If
                    RemoveHandler g_Player.PlayBackStarted, AddressOf VideoStarted
                    RemoveHandler g_Player.PlayBackChanged, AddressOf VideoChange
                    RemoveHandler g_Player.PlayBackEnded, AddressOf VideoEnded
                    RemoveHandler g_Player.PlayBackStopped, AddressOf VideoStopped
                    RemoveHandler tmrDelayRefreshOnSkip.Tick, AddressOf DelayOnRefreshTimerTick
                    RemoveHandler tmrCheckplayback.Tick, AddressOf CheckPlaybackTick
                    RemoveHandler tmrRefresh.Tick, AddressOf DelayOnRefreshTimerTick
                End If
            End If
            If VRD.AdScanInProgress = True Then
                Logger.DebugM("GUIMain: AdScan is running. Redirecting to {0}", new_windowId)
                MyBase.OnPageDestroy(new_windowId)
            Else
                Logger.DebugM("GUIMain: Redirecting to {0}", enumWindows.GUIstart)
                MyBase.OnPageDestroy(enumWindows.GUIstart)
            End If
        End Sub

        Public Overrides Sub OnAction(ByVal action As MediaPortal.GUI.Library.Action)
            Dim AktWinId As Integer = GUIWindowManager.ActiveWindow
            Dim skipOnPlay As Integer
            Dim skipOnPause As Long
            If AktWinId = GetID Then
                Try
                    Logger.DebugM("Keypress on VideoReDo Screen. KeyChar={0} ; KeyCode={1} ; Actiontype={2}", action.m_key.KeyChar, action.m_key.KeyCode, action.wID.ToString)
                Catch
                    Logger.DebugM("Action on VideoReDo Screen. Action={0}", action.wID.ToString)
                End Try
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED Then

                    If action.m_key IsNot Nothing Then
                        'Button 1
                        If action.m_key.KeyChar = 49 Then
                            skipOnPause = -1 * GetConfigString(ConfigKey.SeekStepOnPause1)
                            skipOnPlay = -1 * GetConfigString(ConfigKey.SeekStepOnPlay1)
                            SkipTo(skipOnPause, skipOnPlay)
                        End If
                        'Button 2
                        If action.m_key.KeyChar = 50 Then
                            If VRD.AdScanInProgress Then
                                VRD.AbortScan()
                                SceneLoad(False, True)
                                PlayerPosition = VRD.GetCursorTime
                                GetThumbs(PlayerPosition, ThumbCount)
                            End If
                        End If
                        'Button 3
                        If action.m_key.KeyChar = 51 Then
                            skipOnPause = GetConfigString(ConfigKey.SeekStepOnPause3)
                            skipOnPlay = GetConfigString(ConfigKey.SeekStepOnPlay3)
                            SkipTo(skipOnPause, skipOnPlay)
                        End If
                        'Button 4
                        If action.m_key.KeyChar = 52 Then
                            skipOnPause = -1 * GetConfigString(ConfigKey.SeekStepOnPause4)
                            skipOnPlay = -1 * GetConfigString(ConfigKey.SeekStepOnPlay4)
                            SkipTo(skipOnPause, skipOnPlay)
                        End If
                        'Button 5
                        If action.m_key.KeyChar = 53 Then
                            If ChangeCutMode Then
                                SceneChange()
                            Else
                                SceneSet()
                            End If
                        End If
                        'Button 6
                        If action.m_key.KeyChar = 54 Then
                            skipOnPause = GetConfigString(ConfigKey.SeekStepOnPause6)
                            skipOnPlay = GetConfigString(ConfigKey.SeekStepOnPlay6)
                            SkipTo(skipOnPause, skipOnPlay)
                        End If
                        'Button 7
                        If action.m_key.KeyChar = 55 Then
                            skipOnPause = -1 * GetConfigString(ConfigKey.SeekStepOnPause7)
                            skipOnPlay = -1 * GetConfigString(ConfigKey.SeekStepOnPlay7)
                            SkipTo(skipOnPause, skipOnPlay)
                        End If
                        'Button 8
                        If action.m_key.KeyChar = 56 Then
                            If CutList.Count > 0 Then
                                SceneDelete(SkipToMarker(-1, True))
                            End If
                        End If
                        'Button 9
                        If action.m_key.KeyChar = 57 Then
                            skipOnPause = GetConfigString(ConfigKey.SeekStepOnPause9)
                            skipOnPlay = GetConfigString(ConfigKey.SeekStepOnPlay9)
                            SkipTo(skipOnPause, skipOnPlay)
                        End If
                        'Button *
                        If action.m_key.KeyChar = 42 Then
                            SkipToMarker(-1, True)
                        End If
                        'Button #
                        If action.m_key.KeyChar = 35 Then
                            SkipToMarker(-1, False)
                        End If
                    End If 'aktion.m_key is not nothing

                End If 'Key pressed

                ''ESC
                'If action.m_key.KeyCode = 27 Then
                '    If btnExitHelp.Visible = True Then
                '        Exit Sub
                '    End If
                'End If
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_PREVIOUS_MENU Then

                    If btnExitHelp IsNot Nothing AndAlso btnExitHelp.Visible = True Then
                        HelpBackgroundImage.Visible = False
                        MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                        CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                        GUIControl.FocusControl(GetID, btnHelp.GetID)
                        Exit Sub
                    End If
                End If
                'Wenn in der Cutlist was selektiert wird
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM And CutList.IsFocused Then
                    MenuScene()
                End If

                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM And sliderAudioSync.IsFocused Then
                    VRD.AudioSyncValue = sliderAudioSync.IntValue
                    GUIControl.FocusControl(Me.GetID, btnCutMake.GetID)
                    sliderAudioSync.Visible = False
                    MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                    CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                End If
                ' ContexMenu
                If action.wID = MediaPortal.GUI.Library.Action.ActionType.ACTION_CONTEXT_MENU Then
                    MenuContext()
                End If
            End If
            MyBase.OnAction(action)
        End Sub

        Protected Overrides Sub OnClicked(ByVal controlId As Integer, ByVal control As MediaPortal.GUI.Library.GUIControl, ByVal actionType As MediaPortal.GUI.Library.Action.ActionType)
            If control Is btnCutMake Then
                SceneSet()
            End If
            If control Is btnCutChange Then
                SceneChange()
            End If
            If control Is btnCutSave Then
                CutBarUnload()
                MoviestripBarUnload()
                RecordingSave()
            End If
            If control Is btnHelp Then
                CutBarUnload()
                MoviestripBarUnload()
                DialogHelp()
            End If
            If control Is btnExitHelp Then
                HelpBackgroundImage.Visible = False
                MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                GUIControl.FocusControl(GetID, btnHelp.GetID)
            End If
            MyBase.OnClicked(controlId, control, actionType)
        End Sub

#End Region


#Region "g_Player Events"
        Friend Sub VideoChange(ByVal type As MediaPortal.Player.g_Player.MediaType, ByVal stoptime As Integer, ByVal filename As String)

        End Sub

        Friend Sub VideoStarted(ByVal type As g_Player.MediaType, ByVal filename As String)

        End Sub

        Friend Sub VideoStopped(ByVal type As MediaPortal.Player.g_Player.MediaType, ByVal stoptime As Integer, ByVal filename As String)
            MovieStripBar.MovieStripThumbs.Clear()
            MovieStripBar.Invalidate()
        End Sub

        Friend Sub VideoEnded(ByVal type As MediaPortal.Player.g_Player.MediaType, ByVal filename As String)
            Logger.DebugM("Video Ended. Restarting Playback!!!")
            g_Player.Play(RecordingToCut.VideoFilename, g_Player.MediaType.Video)
            'PlayerPosition = 0
        End Sub

        Friend Sub DelayOnRefreshTimerTick(ByVal sender As Object, ByVal e As System.EventArgs)
            GetThumbs(PlayerPosition, ThumbCount)
            CutBar.Invalidate()
            If tmrDelayRefreshOnSkip.Enabled Then
                tmrDelayRefreshOnSkip.Enabled = False
            End If
            If (tmrRefresh.Enabled = False) And (IsPlayerPaused = False) Then
                tmrRefresh.Enabled = AlwaysRefreshMoviestripThumbs
            End If
        End Sub

        Private IsPlayerPaused As Boolean = False
        Friend Sub CheckPlaybackTick(ByVal sender As Object, ByVal e As System.EventArgs)
            If VRD.AdScanInProgress Then Exit Sub
            If IsPlayerPaused = False Then
                If g_Player.Paused Then
                    IsPlayerPaused = True
                    tmrRefresh.Enabled = False
                    'PlayerPosition = PlayerPosition - (5 * (1000 / PlayerFramerate))
                    If PlayerPosition < 0 Then
                        PlayerPosition = 0
                    Else
                        Logger.DebugM("Player paused at Position: {0} / {1}", g_Player.CurrentPosition, GetPlayerTimeString(g_Player.CurrentPosition * 1000, PlayerFramerate))
                        Logger.DebugM("Seeking five frames back ({0} ms).", (5 * (1000 / PlayerFramerate)))
                    End If
                    If PauseOnStartOnce = False Then
                        ctlStillImage.Visible = True
                        GetThumbs(PlayerPosition, ThumbCount)
                    Else
                        PauseOnStartOnce = False
                    End If
                Else
                    PlayerPosition = Int(g_Player.CurrentPosition * 1000)
                    ctlStillImage.Visible = False
                End If
            Else
                If g_Player.Paused = False Then
                    g_Player.SeekAbsolute(PlayerPosition / 1000)
                    tmrRefresh.Enabled = AlwaysRefreshMoviestripThumbs
                    IsPlayerPaused = False
                End If
            End If
            SetPlayerLabels()
        End Sub
#End Region

#Region "AdDetectiveSubs und Handler"
        Private Sub AdScanStart()
            AddHandler VRD.AdScanStarted, AddressOf AdScanStarted
            VRD.StartAdScan(True, True, False)
        End Sub

        Private Sub AdScanStarted(ByVal sender As Object, ByVal e As AdDetectiveEventArgs)
            AddHandler VRD.AdScanAborted, AddressOf AdScanAborted
            AnimWaiting.Visible = True
            btnCutMake.Visible = False
            btnCutSave.Visible = False
            ctlStillImage.RemoveMemoryImageTexture()
            ctlStillImage.FileName = GUIGraphicsContext.Skin & "\Media\MPVideoRedo5\MPVideoRedo5Working.png"
            MoviestripBarUnload()
        End Sub

        Private Sub AdScanFinished(ByVal sender As Object, ByVal e As AdDetectiveEventArgs)
            RemoveHandler VRD.AdScanAborted, AddressOf AdScanAborted
            RemoveHandler VRD.AdScanFinished, AddressOf AdScanFinished
            tmrAdScan.Enabled = False
            RemoveHandler tmrAdScan.Tick, AddressOf AdScanTimer
            AnimWaiting.Visible = False
            If GUIWindowManager.ActiveWindow = GetID Then
                CutBarUnload()
                MoviestripBarUnload()
                DialogNotify(GetID, 5, Translation.Done, Translation.AdDetectiveDone)
                CutBar.Text = ""
                btnCutMake.Visible = True
                btnCutSave.Visible = True
                MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                ctlStillImage.RemoveMemoryImageTexture()
                ctlStillImage.FileName = Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\StillImage.jpg"
                Logger.DebugM("Loading detected Scenes ...")
                SceneLoad(False, True)
                GUIWindowManager.NeedRefresh()
                Logger.DebugM("All Scenes Loaded.")
                PlayerPosition = 0
                GetThumbs(PlayerPosition, ThumbCount)
            Else
                DialogNotify(GetID, 5, Translation.Done, Translation.AdDetectiveDone)
                PlayerPosition = 0
            End If
        End Sub

        Private Sub AdScanAborted(ByVal sender As Object, ByVal e As AdDetectiveEventArgs)
            AnimWaiting.Visible = False
            tmrAdScan.Enabled = False
            RemoveHandler VRD.AdScanAborted, AddressOf AdScanAborted
            RemoveHandler tmrAdScan.Tick, AddressOf AdScanTimer
            CutBar.Text = ""
            btnCutMake.Visible = True
            btnCutSave.Visible = True
            MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
        End Sub

        Private Sub AdScanTimer(ByVal sender As Object, ByVal e As EventArgs)
            PlayerPosition = VRD.GetCursorTime
            Dim proz As Integer = (PlayerPosition / PlayerDuration) * 100
            CutBar.LineMarkerPosition = proz
            CutBar.Text = Translation.AdDetectiveRunning & " - " & proz.ToString & "%"
            SetPlayerLabels()
            SceneLoad(True, False)
            CutBar.Invalidate()
        End Sub
#End Region

        Private Sub RecordingLoad()
            AnimWaiting.Visible = True
            If IO.File.Exists(RecordingToCut.VideoFilename) Then
                If VRD.AdScanInProgress = False Then
                    If VRD.MediaToCut <> RecordingToCut.VideoFilename Then
                        SceneClear()
                        Dim FileList As Array
                        FileList = Directory.GetFiles(Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\", "*.bmp")
                        For Each item In FileList
                            Logger.DebugM("Deleting Cache-File: '{0}' ...", item)
                            IO.File.Delete(item)
                        Next
                        IO.File.Delete(Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\StillImage.jpg")
                        VRD.LoadMediaToCut(RecordingToCut.VideoFilename)
                        PlayerPosition = 0
                        Application.DoEvents()
                    End If
                    PlayerFramerate = VRD.GetFramerate
                    PlayerDuration = VRD.VideoDuration
                    SceneLoad(False, True)
                    If (HelpConfig.GetConfigString(ConfigKey.CutOnPlay) = True) And (CutList.Count = 0) Then
                        Logger.Info("'CutOnPlay' set to 'True' in Preferences. Therefore a first Cutmarker has been generated.")
                        SceneSet(0)
                        Logger.DebugM("The Start-Cutmarker was generated automatically")
                    End If
                    AnimWaiting.Visible = False
                    btnCutMake.Visible = True
                    btnCutSave.Visible = True
                    GetThumbs(PlayerPosition, ThumbCount)
                    g_Player.Play(RecordingToCut.VideoFilename, g_Player.MediaType.Video)
                    If (HelpConfig.GetConfigString(ConfigKey.PauseOnStart) = True) Then
                        g_Player.Pause()
                    End If
                    MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                Else
                    SceneLoad(False, False)
                    AnimWaiting.Visible = True
                    btnCutMake.Visible = False
                    btnCutSave.Visible = False
                End If
                CutBar.Text = ""
                CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                SetPlayerLabels()
            Else
                Logger.Warn("Error to load the Video {0}", RecordingToCut.VideoFilename)
                GUIWindowManager.GetWindow(enumWindows.GUIstart)
            End If
        End Sub


        Private Sub RecordingSave()
            Logger.DebugM("There are {0} Markers in the VideoReDo-Cutmarkers-List", VRD.CutMarkerList.Count)

            If VRD.CutMarkerList.Count Mod 2 = 1 Then
                If HelpConfig.GetConfigString(ConfigKey.CutOnEnd) = False Then
                    Logger.DebugM("NoEndmarker Dialog wird erstellt")
                    CutBarUnload()
                    MoviestripBarUnload()
                    DialogNotify(GetID, 5, Translation.ErrorOccured, Translation.NoEndmarker)
                    MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                    CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                    Exit Sub
                Else
                    Logger.DebugM(String.Format("Automatically adding Cutmarker at the end of the movie at Position {0}.", VRD.VideoDuration))
                    If IsPlayerPaused = False Then
                        IsPlayerPaused = True
                        g_Player.Pause()
                    End If
                    SceneSet(PlayerDuration)
                End If
            End If
            If CutList.ListItems.Count = 2 Then
                If CutList.ListItems(0).Label2 = PlayerHelper.GetPlayerTimeString(0, PlayerFramerate) And CutList.ListItems(1).Label2 = GetPlayerTimeString(PlayerDuration, PlayerFramerate) Then
                    Logger.Warn("Due to setting of automated Start- and End-Cutmarkes, the whole Video was cutted out. This is not permitted.")
                    CutBarUnload()
                    MoviestripBarUnload()
                    DialogNotify(GetID, 5, Translation.ErrorOccured, Translation.ForbiddenCutCompleteVideo)
                    MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                    CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
                    Exit Sub
                End If
            End If
            g_Player.Stop()
            GUIWindowManager.ActivateWindow(GetGUIWindow(enumWindows.GUISave), True)
        End Sub

        Private Sub SceneSet(Optional ByVal CutTime As Long = -1)
            Dim lItem As New GUIListItem
            If CutTime = -1 Then
                If IsPlayerPaused Then
                    CutTime = PlayerPosition
                Else
                    CutTime = g_Player.CurrentPosition * 1000
                End If
            End If
            VRD.AddScene(CutTime)
            Logger.DebugM("Cutmarker was created - CutTime={0} VRD.CutCount={1}", CutTime, VRD.CutMarkerList.Count)
            If (Not IO.File.Exists(Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\" & CutTime & ".bmp")) Then
                VRD.MakeScreenshotFile(CutTime, Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\" & CutTime & ".bmp", VideoReDo5.ScreenshotQuality.MiniThumbnail)
                Logger.DebugM("Missing Thumb was created: {0}", Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\" & CutTime & ".bmp")
            End If
            SceneLoad(False, True)
            If IsPlayerPaused Then
                MovieStripBarSceneSet(PlayerPosition)
                MovieStripBar.Invalidate()
            End If
        End Sub

        Private Sub SceneLoad(ByVal Compare As Integer, ByVal Screenshot As Boolean)
            Dim Differ As Boolean = False
            Dim newVRDList As New List(Of Long)
            newVRDList = VRD.LoadCutMarkerList()
            If Compare Then
                If newVRDList.Count = VRDList.Count Then
                    For i = 0 To newVRDList.Count - 1
                        If newVRDList(i) <> VRDList(i) Then
                            Differ = True
                            Exit For
                        End If
                    Next
                Else
                    Differ = True
                End If
            Else
                Differ = True
            End If
            If Differ Then
                VRDList = newVRDList
                CutList.Clear()
                CutBar.CutValues = ""
                Dim CutType As String = ""
                For i = 0 To VRDList.Count - 1
                    Dim lItem As New GUIListItem
                    If Screenshot Then
                        If (Not IO.File.Exists(Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\" & VRDList(i) & ".bmp")) Then
                            VRD.MakeScreenshotFile(VRDList(i), Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\" & VRDList(i) & ".bmp", VideoReDo5.ScreenshotQuality.MiniThumbnail)
                            Logger.DebugM("Missing Thumb was created: {0}", Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\" & VRDList(i) & ".bmp")
                        End If
                    End If
                    If CutList.ListItems.Count Mod 2 = 0 Then
                        CutType = "Start"
                        TempStartValue = Convert.ToSingle(VRDList(i) * 100 / PlayerDuration)
                    Else
                        CutType = "Stop"
                        TempEndValue = Convert.ToSingle(VRDList(i) * 100 / PlayerDuration)
                        CutBar.AddValues(TempStartValue, TempEndValue)
                        Logger.DebugM("Cutbar Marker added: Start = {0}; End = {1}", TempStartValue, TempEndValue)
                    End If
                    lItem.Label = CutType & ": "
                    lItem.Label2 = GetPlayerTimeString(VRDList(i), PlayerFramerate)
                    lItem.IconImage = Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\" & VRDList(i) & ".bmp"
                    GUIControl.AddListItemControl(GetID, CutList.GetID, lItem)
                Next i
                CutBar.Invalidate()
                GUIPropertyManager.SetProperty("#itemcount", CutList.ListItems.Count)
            End If

        End Sub

        Private Sub SceneChange()
            If ChangeCutMode = True Then
                Dim newVRDList As New List(Of Long)
                ChangeCutMode = False
                btnCutChange.Visible = False
                btnCutMake.Visible = True
                newVRDList = VRD.LoadCutMarkerList()
                If IsPlayerPaused Then
                    newVRDList(ChangeCutModeMarker) = PlayerPosition
                Else
                    newVRDList(ChangeCutModeMarker) = g_Player.CurrentPosition * 1000
                End If
                VRD.ClearAllSelections()
                For i = 0 To VRD.CutMarkerList.Count - 1
                    VRD.AddScene(newVRDList(i))
                Next
                VRDList = newVRDList
                SceneLoad(False, True)
            End If
        End Sub

        Private Sub SceneDelete(ByVal SelectedMarker As Integer)
            Dim oldCutList As List(Of Long) = VRD.CutMarkerList
            If SelectedMarker Mod 2 = 0 Then
                oldCutList.RemoveAt(SelectedMarker)
                Try
                    oldCutList.RemoveAt(SelectedMarker)
                Catch
                End Try
            Else
                oldCutList.RemoveAt(SelectedMarker - 1)
                oldCutList.RemoveAt(SelectedMarker - 1)
            End If
            VRD.ClearAllSelections()
            CutList.Clear()
            CutBar.CutValues = ""
            Dim newCutlist As New List(Of Long)
            newCutlist.AddRange(oldCutList)
            For Each item In newCutlist
                SceneSet(item)
            Next
            VRD.CutMarkerList = newCutlist
            SceneLoad(False, True)
        End Sub

        Private Sub SceneClear()
            CutList.Clear()
            CutBar.CutValues = ""
            CutBar.Invalidate()
            VRD.CutMarkerList.Clear()
            VRD.ClearAllSelections()
        End Sub

        Private Function ComSkipLoad() As Boolean
            Dim Filename = System.IO.Path.GetDirectoryName(RecordingToCut.VideoFilename) & "\" & System.IO.Path.GetFileNameWithoutExtension(RecordingToCut.VideoFilename) & ".txt"
            Logger.DebugM("Checking for ComSkip File existance: {0}", Filename)
            If IO.File.Exists(Filename) Then
                If HelpConfig.GetConfigString(ConfigKey.AlwaysLoadComSkipMarkers) = False Then
                    If (DialogYesNo(GetID, True, Translation.LoadComSkipMarkers, " ", Translation.LoadComSkipMarkers1) = False) Then
                        Logger.DebugM("Not loading ComSkip Markers.")
                        Return False
                    Else
                        Logger.DebugM("Loading ComSkip Markers.")
                    End If
                End If
                SceneClear()
                Logger.DebugM("Loading ComSkip File: {0}", Filename)
                Dim fs As FileStream = New FileStream(Filename, FileMode.Open, FileAccess.Read)
                Dim r As StreamReader = New StreamReader(fs)
                r.BaseStream.Seek(0, SeekOrigin.Begin)
                Dim line As String
                Dim startcut As Long
                Dim endcut As Long
                Dim i As Integer = 0
                While r.Peek() > -1
                    line = r.ReadLine()
                    If ((line.Contains("FILE") = False) And (line.Contains("-") = False)) Then
                        i = i + 1
                        startcut = (line.Split(ControlChars.Tab)(0)) / PlayerFramerate * 1000
                        Logger.DebugM("ComSkipStart ({0}): {1}", i, startcut)
                        SceneSet(startcut)
                        endcut = (line.Split(ControlChars.Tab)(1)) / PlayerFramerate * 1000
                        Logger.DebugM("ComSkipEnd ({0}): {1}", i, endcut)
                        SceneSet(endcut)
                    End If
                End While
                r.Close()
                fs.Close()
                If i = 0 Then
                    DialogNotify(GetID, 5, Translation.LoadComSkipMarkers, Translation.NothingFound)
                    Logger.DebugM("ComSkip File is empty!")
                Else
                    DialogNotify(GetID, 5, Translation.LoadComSkipMarkers, Translation.LoadComSkipMarkers3)
                    Logger.DebugM("ComSkip File loaded succesfully.")
                End If
                Return True
            Else
                DialogNotify(GetID, 5, Translation.LoadComSkipMarkers, Translation.NothingFound)
                Return False
            End If
        End Function

        Private Function ComSkipSave() As Boolean
            If VRD.CutMarkerList.Count > 1 Then
                Dim FilePath = System.IO.Path.GetDirectoryName(RecordingToCut.VideoFilename) & "\"
                Dim Filename = System.IO.Path.GetFileNameWithoutExtension(RecordingToCut.VideoFilename) & ".txt"
                If HelpConfig.GetConfigString(ConfigKey.AlwaysSaveComSkipMarkers) = False Then
                    If (DialogYesNo(GetID, True, Translation.SaveComSkipMarkers, " ", Translation.SaveComSkipMarkers1) = False) Then
                        Logger.DebugM("Not saving ComSkip Markers.")
                        Return True
                    End If
                End If
                Logger.DebugM("Checking for ComSkip Backup File existance: {0}", FilePath & ".txt.bak")
                If (IO.File.Exists(FilePath & Filename & ".bak") And IO.File.Exists(FilePath & Filename)) Then
                    My.Computer.FileSystem.DeleteFile(FilePath & Filename & ".bak")
                    Logger.DebugM("Deleting old ComSkip Backup File: {0}", FilePath & Filename & ".bak")
                End If
                Logger.DebugM("Checking for ComSkip File existance: {0}", FilePath & Filename)
                If IO.File.Exists(FilePath & Filename) Then
                    My.Computer.FileSystem.RenameFile(FilePath & Filename, Filename & ".bak")
                    Logger.DebugM("Renaming ComSkip File to: {0}", FilePath & Filename & ".bak")
                End If
                Logger.DebugM("Generating New ComSkip File: {0}", FilePath & Filename)
                Dim fs As FileStream = New FileStream(FilePath & Filename, FileMode.CreateNew)
                Dim r As StreamWriter = New StreamWriter(fs)
                Dim VRDList As New List(Of Long)
                VRDList = VRD.LoadCutMarkerList()
                r.WriteLine("FILE PROCESSING COMPLETE  " & PlayerDuration / (1000 / PlayerFramerate) & " FRAMES AT  " & PlayerFramerate * 100)
                r.WriteLine("-------------------")
                For i = 0 To VRDList.Count - 1
                    If i Mod 2 = 1 Then
                        r.WriteLine((VRDList(i - 1) / (1000 / PlayerFramerate)) & vbTab & (VRDList(i) / (1000 / PlayerFramerate)))
                    End If
                Next
                r.Close()
                fs.Close()
                If IO.File.Exists(FilePath & Filename) Then
                    DialogNotify(GetID, 5, Translation.SaveComSkipMarkers, Translation.SaveComSkipMarkers3)
                    Logger.DebugM("ComSkip File was saved succesfully.")
                    Return True
                Else
                    DialogNotify(GetID, 5, Translation.SaveComSkipMarkers, Translation.SaveComSkipMarkers4)
                    Logger.DebugM("Error on saving ComSkip File.")
                    Return False
                End If
            Else
                DialogNotify(GetID, 5, Translation.SaveComSkipMarkers, Translation.NothingToSave)
                Logger.DebugM("Nothing to save in a ComSkip file.")
                Return True
            End If
        End Function


        Private Function ProjectLoad() As Boolean
            Dim Filename = System.IO.Path.GetDirectoryName(RecordingToCut.VideoFilename) & "\" & System.IO.Path.GetFileNameWithoutExtension(RecordingToCut.VideoFilename)
            Filename = Filename & ".mpvr5"
            Logger.DebugM("Checking Project File: {0}", Filename)
            If IO.File.Exists(Filename) Then
                SceneClear()
                VRD.LoadMediaToCut(RecordingToCut.VideoFilename)
                PlayerDuration = VRD.VideoDuration
                PlayerFramerate = VRD.GetFramerate
                SceneLoad(False, True)
                DialogNotify(GetID, 5, Translation.LoadProjectMarkers, Translation.LoadProjectMarkers3)
                Logger.DebugM("Project File was loaded succesfully.")
                Return True
            Else
                DialogNotify(GetID, 5, Translation.LoadProjectMarkers, Translation.NothingFound)
                Logger.DebugM("There is no project file to load for this recording.")
                Return False
            End If
        End Function


        Private Function ProjectSave(Optional ByVal Dialog As Boolean = True) As Boolean
            If VRD.CutMarkerList.Count > 1 Then
                Dim FilePath = System.IO.Path.GetDirectoryName(RecordingToCut.VideoFilename) & "\"
                Dim Filename = System.IO.Path.GetFileNameWithoutExtension(RecordingToCut.VideoFilename) & ".mpvr5"
                If Dialog Then
                    If HelpConfig.GetConfigString(ConfigKey.AlwaysSaveProject) = False Then
                        If DialogYesNo(GetID, True, Translation.SaveProjectMarkers, " ", Translation.SaveProjectMarkers1) = False Then
                            Logger.DebugM("Not saving project file.")
                            Return True
                        End If
                    End If
                End If
                Logger.DebugM("Checking for Project Backup File existance: {0}", FilePath & Filename & ".bak")
                If (IO.File.Exists(FilePath & Filename & ".bak") And IO.File.Exists(FilePath & Filename)) Then
                    My.Computer.FileSystem.DeleteFile(FilePath & Filename & ".bak")
                    Logger.DebugM("Deleting old Project Backup File: {0}", FilePath & Filename & ".bak")
                End If
                Logger.DebugM("Checking for Project File existance: {0}", FilePath & Filename)
                If IO.File.Exists(FilePath & Filename) Then
                    My.Computer.FileSystem.RenameFile(FilePath & Filename, Filename & ".bak")
                    Logger.DebugM("Renaming Project File to: {0}", FilePath & Filename & ".bak")
                End If
                VRD.ProjectSave(FilePath & Filename)
                Logger.DebugM("Saving project file: {0}: ", FilePath & Filename)
                If IO.File.Exists(FilePath & Filename) Then
                    DialogNotify(GetID, 5, Translation.SaveProjectMarkers, Translation.SaveProjectMarkers3)
                    Logger.DebugM("Project File was saved succesfully.")
                    Return True
                Else
                    DialogNotify(GetID, 5, Translation.SaveProjectMarkers, Translation.SaveProjectMarkers4)
                    Logger.DebugM("Error on saving Project File.")
                    Return False
                End If
            Else
                DialogNotify(GetID, 5, Translation.SaveProjectMarkers, Translation.NothingToSave)
                Logger.DebugM("Nothing to save in a project file.")
                Return True
            End If
        End Function

        Private Sub DialogHelp()
            HelpBackgroundImage.Visible = True
            Application.DoEvents()
            GUIControl.FocusControl(GetID, btnExitHelp.GetID)
        End Sub

        Private Sub MenuScene()
            CutBarUnload()
            MoviestripBarUnload()
            Dim dlgContext As GUIDialogMenu = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_MENU, Integer)), GUIDialogMenu)
            dlgContext.Reset()
            dlgContext.SetHeading(Translation.CutContextMenu)
            dlgContext.Add(Translation.CutContextChange)
            dlgContext.Add(Translation.CutContextDelete)
            dlgContext.Add(Translation.CutContextJumpTo)
            dlgContext.DoModal(GetID)
            If dlgContext.SelectedLabel = 0 Then
                If ChangeCutMode = False Then
                    ChangeCutMode = True
                    btnCutMake.Visible = False
                    btnCutChange.Visible = True

                    Logger.DebugM("Internal ChangeCutMode was set to '{0}' for Marker {1}", ChangeCutMode, ChangeCutModeMarker)
                Else

                End If
                ChangeCutModeMarker = CutList.SelectedListItemIndex
            End If
            If dlgContext.SelectedLabel = 1 Then
                SceneDelete(CutList.SelectedListItemIndex)
                If IsPlayerPaused Then
                    MovieStripBarSceneSet(PlayerPosition)
                    MovieStripBar.Invalidate()
                End If
            End If
            If dlgContext.SelectedLabel = 2 Then
                SkipToMarker(CutList.SelectedListItemIndex)
            End If
            MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
            CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
            GUIWindowManager.Process()
            dlgContext.Reset()
            dlgContext = Nothing
        End Sub

        Private Sub MenuContext()
            If VRD.AdScanInProgress Then Exit Sub
            If IsPlayerPaused = False Then
                g_Player.Pause()
            End If
            sliderAudioSync.Visible = False
            CutBarUnload()
            MoviestripBarUnload()
            Dim dlgContext As GUIDialogMenu = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_MENU, Integer)), GUIDialogMenu)
            dlgContext.Reset()
            dlgContext.SetHeading(GetConfigString(ConfigKey.ModuleName))
            dlgContext.Add(Translation.StartAdDetectiveScan)
            dlgContext.Add(Translation.LoadComSkipMarkers)
            dlgContext.Add(Translation.SaveComSkipMarkers)
            dlgContext.Add(Translation.LoadProjectMarkers)
            dlgContext.Add(Translation.SaveProjectMarkers)
            dlgContext.Add(Translation.ClearCutlist)
            dlgContext.Add(Translation.EditEndFilename)
            dlgContext.Add(Translation.SavingProfile & ": " & VRD.SavingProfile)
            dlgContext.Add(Translation.AudioSyncLabelContext)
            dlgContext.DoModal(GetID)
            If dlgContext.SelectedLabel = 0 Then
                Dim adScan As New Threading.Thread(AddressOf AdScanStart)
                adScan.IsBackground = True
                adScan.Priority = Threading.ThreadPriority.BelowNormal
                AddHandler tmrAdScan.Tick, AddressOf AdScanTimer
                AddHandler VRD.AdScanAborted, AddressOf AdScanAborted
                AddHandler VRD.AdScanFinished, AddressOf AdScanFinished
                tmrAdScan.Interval = 1000
                tmrAdScan.Enabled = True
                tmrAdScan.Start()
                adScan.Start()
            End If
            If dlgContext.SelectedLabel = 1 Then
                ComSkipLoad()
            End If
            If dlgContext.SelectedLabel = 2 Then
                ComSkipSave()
            End If
            If dlgContext.SelectedLabel = 3 Then
                ProjectLoad()
            End If
            If dlgContext.SelectedLabel = 4 Then
                ProjectSave(True)
            End If
            If dlgContext.SelectedLabel = 5 Then
                SceneClear()
            End If
            If dlgContext.SelectedLabel = 6 Then
                Dim SavingFilename As String
                SavingFilename = Replace(RecordingToCut.SavingFilename, "%ext%", VRD.GetProfileInfo(VRD.SavingProfile).Filetype.ToLower)
                Dim NewFilename As String = ShowKeyboard(SavingFilename, GetID)
                If NewFilename IsNot Nothing Then
                    RecordingToCut.SavingFilename = NewFilename
                    Translator.SetProperty("#Saving.Name", RecordingToCut.SavingFilename)
                End If
            End If
            If dlgContext.SelectedLabel = 7 Then
                DialogProfile(GetID)
            End If
            If dlgContext.SelectedLabel = 8 Then
                sliderAudioSync.Visible = True
                GUIControl.ShowControl(Me.GetID, sliderAudioSync.GetID)
                GUIControl.FocusControl(Me.GetID, sliderAudioSync.GetID)
                sliderAudioSync.SetRange(-1000, 1000)
                sliderAudioSync.IntValue = VRD.AudioSyncValue
            End If
            If dlgContext.SelectedLabel < 8 Then
                MoviestripBarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml"), VideoWindow)
                CutbarLoad(BarsGetProperties(GUIGraphicsContext.Skin & "\MPVideoRedo5.Main.xml", True), VideoWindow)
            End If
            GUIWindowManager.Process()
            dlgContext.Reset()
            dlgContext = Nothing
        End Sub

        Private Sub SkipTo(ByVal skipOnPause As Long, ByVal skipOnPlay As Long)
            tmrRefresh.Enabled = False
            tmrDelayRefreshOnSkip.Enabled = False
            If IsPlayerPaused Then
                skipOnPause = PlayerPosition + ((1000 / PlayerFramerate) * skipOnPause)
                If skipOnPause < 0 Then
                    skipOnPause = PlayerDuration + skipOnPause
                Else
                    If skipOnPause > PlayerDuration Then
                        skipOnPause = skipOnPause - PlayerDuration
                    End If
                End If
                tmrDelayRefreshOnSkip.Enabled = True
                PlayerPosition = skipOnPause
            Else
                skipOnPlay = PlayerPosition + (skipOnPlay * 1000)
                If skipOnPlay < 0 Then
                    skipOnPlay = PlayerDuration + skipOnPlay
                Else
                    If skipOnPlay > PlayerDuration Then
                        skipOnPlay = skipOnPlay - PlayerDuration
                    End If
                End If
                If (HelpConfig.GetConfigString(ConfigKey.AlwaysRefreshMoviestripThumbs) = True) Then
                    tmrDelayRefreshOnSkip.Enabled = True
                End If
                g_Player.SeekAbsolute(skipOnPlay / 1000)
            End If
        End Sub

        Private Function SkipToMarker(Optional ByVal JumpMarker As Integer = -1, Optional ByVal Backwards As Boolean = False) As Integer
            tmrRefresh.Enabled = False
            tmrDelayRefreshOnSkip.Enabled = False
            Dim marker As Integer = 0
            If (JumpMarker = -1) And (VRDList.Count > 0) Then
                If Backwards = True Then
                    For i = 0 To VRDList.Count - 1
                        If VRDList(i) + 1000 >= PlayerPosition Then
                            marker = i - 1
                            Exit For
                        End If
                    Next i
                    If marker < 0 Then
                        marker = VRDList.Count - 1
                    End If
                Else
                    For i = 0 To VRDList.Count - 1
                        If (VRDList(i) - 1) > PlayerPosition Then
                            marker = i
                            Exit For
                        End If
                    Next i
                End If
                JumpMarker = marker
            End If
            If JumpMarker < VRDList.Count Then
                Logger.DebugM("Jumping to Marker {0} on Position {1} ms ...", JumpMarker, VRDList(JumpMarker))
                If IsPlayerPaused Or HelpConfig.GetConfigString(ConfigKey.AlwaysRefreshMoviestripThumbs) = True Then
                    tmrDelayRefreshOnSkip.Enabled = True
                End If
                If IsPlayerPaused Then
                    PlayerPosition = VRDList(JumpMarker)
                Else
                    g_Player.SeekAbsolute(VRDList(JumpMarker) / 1000)
                End If
            End If
            Return marker
        End Function

        Public Sub GetThumbs(ByVal _Position As Long, ByVal _ThumbCount As Integer)
            Dim factor As Integer = Int(_ThumbCount / 2)
            Dim LineMarker As Single = (factor / _ThumbCount) * 100
            Dim ThumbList As New ImageList
            ThumbList.ColorDepth = ColorDepth.Depth24Bit
            ThumbList.ImageSize = New Drawing.Size(128, 128)
            Logger.DebugM("Generating {2} thumbnails for position {0} with a stepping of {1} ms ...", _Position, (1000 / PlayerFramerate), _ThumbCount)
            Try
                For i As Integer = 0 To _ThumbCount - 1
                    If VRD Is Nothing Then Exit Sub
                    Dim temptime As Long = _Position + ((1000 / PlayerFramerate) * (i - factor))
                    If (temptime < 0) Or (temptime >= PlayerDuration) Then
                        ThumbList.Images.Add(NoImage)
                    Else
                        ThumbList.Images.Add(VRD.MakeScreenshotClipboard(temptime, VideoReDo5.ScreenshotQuality.Thumbnail, i))
                    End If
                Next
                If VRD Is Nothing Then Exit Sub
                MovieStripBar.MovieThumbs = ThumbList
                MovieStripBar.LineMarkerPosition = LineMarker
                'Für ein Vorschaubild welches in  einem Image im Skin verwendet werden kann
                If g_Player.Paused Or PauseOnStartOnce Then
                    Dim ThumbFileName As String = Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\StillImage.jpg"
                    VRD.MakeScreenshotFile(PlayerPosition, ThumbFileName, VideoReDo5.ScreenshotQuality.good)
                    Logger.DebugM("Generating StimllImage for position {0}", _Position)
                    ctlStillImage.RemoveMemoryImageTexture()
                    ctlStillImage.FileName = ThumbFileName
                End If
                MovieStripBarSceneSet(_Position)
                MovieStripBar.Invalidate()
            Catch ex As Exception
                Logger.Info(ex.Message)
            End Try
            Logger.DebugM("Done.")
        End Sub

        Friend Sub SetPlayerLabels()
            Try
                If VRD Is Nothing Then
                    Logger.Warn("The VideoRedo object was 'Nothing' in 'SetPlayerLabels()'. Canceling.")
                    Translator.SetProperty("#MPVideoRedo5.Translation.Duration", Translation.ErrorOccured)
                    Translator.SetProperty("#MPVideoRedo5.Translation.Position", Translation.ErrorOccured)
                    Translator.SetProperty("#MPVideoRedo5.Translation.TimeLeft", Translation.ErrorOccured)

                End If
                Dim proz As Single = (PlayerPosition / PlayerDuration) * 100
                CutBar.LineMarkerPosition = proz
                CutBar.Invalidate()
                Translator.SetProperty("#MPVideoRedo5.Translation.Duration", String.Format(Translation.Duration, GetPlayerTimeString(PlayerDuration.ToString, PlayerFramerate)))
                Translator.SetProperty("#MPVideoRedo5.Translation.Position", String.Format(Translation.Position, GetPlayerTimeString(PlayerPosition.ToString, PlayerFramerate)))
                Translator.SetProperty("#MPVideoRedo5.Translation.TimeLeft", String.Format(Translation.TimeLeft, GetPlayerTimeString(PlayerDuration - PlayerPosition, PlayerFramerate)))


            Catch ex As Exception
                Logger.Error("Error in SetPlayerLabels: {0}", ex.ToString)
            End Try
        End Sub

        Public Sub MovieStripBarSceneSet(ByVal _Position As Long)
            Dim factor As Integer = Int(ThumbCount / 2)
            Dim MovieStripBarStart As Long = _Position - ((1000 / PlayerFramerate) * factor)
            Dim MovieStripBarEnd As Long = _Position + ((1000 / PlayerFramerate) * (ThumbCount - factor))
            Dim MovieStripBarLength As Long = ThumbCount * (1000 / PlayerFramerate)
            Dim StartMarker As Single = 0
            Dim EndMarker As Single = 0
            MovieStripBar.StartCutValues.Clear()
            MovieStripBar.EndCutValues.Clear()
            For i = 0 To VRDList.Count - 1
                If i Mod 2 = 1 Then
                    StartMarker = ((VRDList(i - 1) - MovieStripBarStart) / MovieStripBarLength) * 100
                    EndMarker = ((VRDList(i) - MovieStripBarStart) / MovieStripBarLength) * 100
                    If EndMarker >= 100 Then
                        EndMarker = 100
                        If StartMarker < 0 Then
                            StartMarker = 0
                        End If
                    End If
                    If StartMarker <= 0 Then
                        StartMarker = 0
                        If EndMarker > 100 Then
                            EndMarker = 100
                        End If
                    End If
                    If StartMarker >= 0 And StartMarker <= 100 And EndMarker <= 100 And EndMarker >= 0 Then
                        MovieStripBar.AddValues(StartMarker, EndMarker)
                        Logger.DebugM("Setting MovieStripBar Cut Markers for Position {0}: Start: {1}; End: {2}", PlayerPosition, StartMarker, EndMarker)
                    End If
                End If
            Next
        End Sub
    End Class
End Namespace