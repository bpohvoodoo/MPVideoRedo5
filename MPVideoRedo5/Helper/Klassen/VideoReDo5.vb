Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices

Public Class VideoReDo5
    Implements IDisposable
    Private VRD As Object

    Private SeekStopWatch As Stopwatch
    'Public IsSeekTimeOverrun As Boolean = False


    Public Event AdScanStarted(ByVal sender As Object, ByVal e As AdDetectiveEventArgs)
    Public Event AdScanFinished(ByVal sender As Object, ByVal e As AdDetectiveEventArgs)
    Public Event AdScanAborted(ByVal sender As Object, ByVal e As AdDetectiveEventArgs)
    Public Event RecordingSaveStart(ByVal sender As Object, ByVal e As RecordingSaveEventArgs)
    Public Event RecordingSaveProgressCanged(ByVal sender As Object, ByVal e As RecordingSaveEventArgs)
    Public Event RecordingSaveFinished(ByVal sender As Object, ByVal e As RecordingSaveEventArgs)
    Public Event RecordingSaveAborted(ByVal sender As Object, ByVal e As RecordingSaveEventArgs)
    Public Event QuickStreamFixNeeded(sender As Object, e As EventArgs)

    Public ReadOnly Property VRDobj() As Object
        Get
            Return VRD
        End Get
    End Property

    Private mInSilentMode As Boolean
    Public ReadOnly Property InSilentMode() As Boolean
        Get

            Return mInSilentMode

        End Get
    End Property

    Private _MaximumSeekTime As New TimeSpan(0, 0, 2)
    Public Property MaximumSeekTime As TimeSpan
        Get
            Return _MaximumSeekTime
        End Get
        Set(value As TimeSpan)
            _MaximumSeekTime = value
        End Set
    End Property


    Public ReadOnly Property GetCursorTime() As Long
        Get
            Try
                If VRD IsNot Nothing Then
                    Dim ret As Long = VRD.NavigationGetCursorTime()
                    Logger.DebugM("Return GetCursorTime: {0} / {1}", ret, PlayerHelper.GetPlayerTimeString(ret, GetFramerate))
                    Return ret
                Else
                    Return 0
                End If
            Catch comex As COMException
                Logger.DebugM(comex.ToString)
            End Try
        End Get
    End Property

    Private mRedoVersion As String = ""
    Public ReadOnly Property ReDoVersion() As String
        Get
            Return mRedoVersion
        End Get
    End Property

    Private mVRDLoaded As Boolean = False
    Public Property VRDLoaded() As Boolean
        Get
            Return mVRDLoaded
        End Get
        Set(ByVal value As Boolean)
            mVRDLoaded = value

        End Set
    End Property

    Public ReadOnly Property OutputInProgress() As Boolean
        Get
            Dim OutputState As Integer = 0
            OutputState = VRD.OutputGetState()
            Logger.DebugM("VRD OutputState is:'{0}'", OutputState)
            If OutputState = 1 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Private mMediaToCut As String
    Public ReadOnly Property MediaToCut() As String
        Get
            Return mMediaToCut
        End Get
    End Property

    Public ReadOnly Property AdScanInProgress() As Boolean
        Get
            Return VRD.AdScanIsScanning
        End Get
    End Property

    Private mCutMarker As New List(Of Long)
    Public Property CutMarkerList() As List(Of Long)
        Get
            Return mCutMarker
        End Get
        Set(ByVal value As List(Of Long))
            mCutMarker = value
        End Set
    End Property

    Public Property LoadCutMarkerList() As List(Of Long)
        Get
            Dim CutMarker As New List(Of Long)
            Dim EditListCount As Integer = VRD.EditGetEditsListCount
            'Logger.DebugM("VRD Cutmarker Count: '{0}'", EditListCount)
            For i As Integer = 0 To VRD.EditGetEditsListCount - 1
                'Logger.DebugM("VRD Cutmarker({0}) Start: '{1}'", i, VRD.EditGetEditStartTime(i))
                CutMarker.Add(VRD.EditGetEditStartTime(i))
                'Logger.DebugM("VRD Cutmarker({0}) End: '{1}'", i, VRD.EditGetEditEndTime(i))
                CutMarker.Add(VRD.EditGetEditEndTime(i))
            Next
            If mCutMarker.Count Mod 2 = 1 Then
                CutMarker.Add(mCutMarker(mCutMarker.Count - 1))
            End If
            mCutMarker = CutMarker
            Return mCutMarker
        End Get
        Set(ByVal value As List(Of Long))
        End Set
    End Property

    Private mVideoDuration As Long = 0
    Public Property VideoDuration() As Long
        Get
            Return mVideoDuration
        End Get
        Set(ByVal value As Long)
            mVideoDuration = value
        End Set
    End Property

    Private mSavingProfile As String
    Public Property SavingProfile() As String
        Get
            Return mSavingProfile
        End Get
        Set(ByVal value As String)
            If value <> "Nothing" Then mSavingProfile = value
        End Set
    End Property

    Public Sub Pause()
        VRD.NavigationPause()
    End Sub

    Public Sub Close()
        Try
            VRD.ProgramExit()
        Catch
        End Try
    End Sub

    Public Sub CloseFile()
        Try
            VRD.FileClose()
        Catch
        End Try
        mMediaToCut = ""
    End Sub

    Public Function GetProfileList() As List(Of String)
        Dim AnzProfiles As Integer = VRD.ProfilesGetCount
        Dim ProfileList As New List(Of String)
        For i As Integer = 1 To AnzProfiles
            ProfileList.Add(VRD.ProfilesGetProfileName(i))
        Next
        Return ProfileList
    End Function

    Public Function GetProfileInfo(ByVal Profile As String) As VRDProfileInfo
        Dim AktProfile As New VRDProfileInfo
        Dim ProfileIndex As Integer = 1
        Dim pList As List(Of String) = GetProfileList()
        For i As Integer = 0 To pList.Count - 1
            If pList(i) = Profile Then
                ProfileIndex = i + 1
                Exit For
            End If
        Next
        Dim ProfileString As String = VRD.ProfilesGetProfileXML(ProfileIndex)
        Dim ProfileXML As New Xml.XmlDocument
        ProfileXML.LoadXml(ProfileString)
        Dim FileType As Xml.XmlNodeList = ProfileXML.GetElementsByTagName("FileType")
        AktProfile.Filetype = FileType.Item(0).InnerText
        Dim VideoAttributes As Xml.XmlNodeList = ProfileXML.GetElementsByTagName("VideoAttributes")
        For Each Attribute In VideoAttributes
            Dim InfoNodes As Xml.XmlNodeList
            InfoNodes = Attribute.childnodes
            For Each InfoNode As Xml.XmlNode In InfoNodes
                Select Case InfoNode.Name
                    Case "Encoder"
                        AktProfile.Encodingtype = InfoNode.InnerText
                    Case "AspectRatio"
                        AktProfile.Ratio = InfoNode.InnerText
                    Case "EncodeDimensions"
                        AktProfile.Resolution = InfoNode.InnerText
                    Case "DeinterlaceMethod"
                        AktProfile.DeintarlaceModus = InfoNode.InnerText
                    Case "FrameRate"
                        AktProfile.FrameRate = InfoNode.InnerText

                End Select
            Next
        Next
        'I-Pod, IPhone, Sony PSP
        If AktProfile.Filetype.Contains("MP4") Then
            AktProfile.Filetype = "MP4"
        End If
        'Mpeg2 Elementary
        If AktProfile.Filetype = "Elementary" And AktProfile.Encodingtype = "MPEG2" Then
            AktProfile.Filetype = "M2V"
        End If
        'H264 Elementary Stream
        If AktProfile.Filetype = "Elementary" And AktProfile.Encodingtype = "H264" Then
            AktProfile.Filetype = "H264"
        End If
        'Audio Only
        If AktProfile.Encodingtype = "None" And AktProfile.Filetype = "Elementary" Then
            AktProfile.Filetype = "MPA"
        End If
        Return AktProfile

    End Function


    Private _AudioSyncValue As Single = 0
    Public Property AudioSyncValue() As Single
        Get
            Return _AudioSyncValue
        End Get
        Set(ByVal value As Single)
            If VRD.NavigationAdjustAudioSync(value) = True Then
                _AudioSyncValue = value
            End If
        End Set
    End Property


    Public Sub AddScene(ByVal Pos As Long)
        'If Pos >= 0 And Pos <= LoadedVideoDuration Then
        mCutMarker.Add(Pos)
        If mCutMarker.Count Mod 2 = 0 Then
            If mCutMarker(mCutMarker.Count - 2) > mCutMarker(mCutMarker.Count - 1) Then
                Dim tempCutMarker As Long = mCutMarker(mCutMarker.Count - 1)
                mCutMarker(mCutMarker.Count - 1) = mCutMarker(mCutMarker.Count - 2)
                mCutMarker(mCutMarker.Count - 2) = tempCutMarker
                Logger.DebugM("Position of Startmarker greater than Endmarker: Switching Start and Endmarker")
            End If
            VRD.EditSetSelectionStart(mCutMarker(mCutMarker.Count - 2))
            VRD.EditSetSelectionEnd(mCutMarker(mCutMarker.Count - 1))
            VRD.EditAddSelection()
        End If
        ' Else
        ' Throw New Exception("Der neue Marker kann nicht hinzugefügt werden da ausserhalb der Videolänge. Wert = " & Pos.ToString)
        ' End If
    End Sub

    Public Sub New(Optional ByVal SilentMode As Boolean = True)
        If SilentMode Then
            Dim VideoReDoSilent = CreateObject("VideoReDo5.VideoReDoSilent")
            VRD = VideoReDoSilent.VRDInterface
            'VRD.SetQuietMode(True)
            VRD.AdScanSetParameter(2, True)
            mInSilentMode = True
        Else
            VRD = CreateObject("VideoReDo5.Application")
            VRD.AdScanSetParameter(2, False)
            mInSilentMode = False
        End If
        VRDLoaded = True
        Me.LoggingCOM = True
        Application.DoEvents()
        mRedoVersion = VRD.ProgramGetVersionNumber
        SeekStopWatch = New Stopwatch

    End Sub

    Private _IsInQucikStreamMode As Boolean
    Public Property IsInQuickStreamMode() As Boolean
        Get
            Return _IsInQucikStreamMode
        End Get
        Set(ByVal value As Boolean)
            _IsInQucikStreamMode = value
        End Set
    End Property


    Public Function LoadMediaToCut(ByVal VRDFile As String, Optional DoQuickStreamFix As Boolean = False) As Boolean
        If IO.File.Exists(VRDFile) Then
            IsInQuickStreamMode = DoQuickStreamFix
            Dim mFile As New IO.FileInfo(VRDFile)
            Dim ProjectFile = System.IO.Path.GetDirectoryName(VRDFile) & "\" & System.IO.Path.GetFileNameWithoutExtension(VRDFile) & ".mpvr5"
            Dim LoadStatus As Boolean
            If mFile.Extension.ToLower = ".ts" Or mFile.Extension.ToLower = ".mpeg" Or mFile.Extension.ToLower = ".mpg" Then
                If mFile.Length > 10000000 Then
                    mMediaToCut = VRDFile
                    If DoQuickStreamFix Then
                        VRD.FileQueueToBatch(VRDFile)
                    Else
                        Logger.DebugM("Checking for Project File existance: {0}", ProjectFile)
                        If IO.File.Exists(ProjectFile) Then
                            If HelpConfig.GetConfigString(ConfigKey.AlwaysLoadProject) = False Then
                                If (DialogYesNo(enumWindows.GUIMain, True, Translation.LoadProjectMarkers, " ", Translation.LoadProjectMarkers1) = False) Then
                                    Logger.DebugM("Not loading Project File.")
                                Else
                                    VRDFile = ProjectFile
                                    Logger.DebugM("Loading Project File.")
                                End If
                            Else
                                VRDFile = ProjectFile
                                Logger.DebugM("Loading Project File.")
                            End If
                        End If
                    End If
                    LoadStatus = VRD.FileOpen(VRDFile, False)
                    If LoadStatus = False Then
                        Logger.DebugM("VideoRedo File {0} could not be loaded.", VRDFile)
                        Return False
                    Else
                        Logger.DebugM("VideoRedo File {0} was successfully loaded.", VRDFile)
                    End If
                    VideoDuration = VRD.FileGetOpenedFileDuration
                    Logger.DebugM("VRD Duration is:'{0}'", VideoDuration)
                    If VRDFile <> ProjectFile Then
                        VRD.EditSetMode(0)
                    End If
                    If VRD.EditGetMode = 0 Then
                        Logger.DebugM("VideoRedo Cutmode was successfully set to 'CutMode'.")
                    Else
                        Logger.Warn("VideoRedo Cutmode could not be set!")
                    End If

                    Return True
                Else
                    Throw New Exception("Bitte die Datei prüfen, die größe der Datei scheint fragwürdig", New Exception("Die größe der Datei " & VRDFile & " war unter 10000000 bytes, was für ein Videofile eher fragwürdig ist. Bitte prüfen"))
                    mMediaToCut = Nothing
                    Return False
                End If
            Else
                Throw New Exception("This file extension is not supported: ", New Exception("The extension is: '" & mFile.Extension.ToString))
                mMediaToCut = Nothing
                Return False
            End If
        Else
            Throw New Exception("The selected file could not be found!")
            mMediaToCut = Nothing
            Return False
        End If
    End Function

    Public Property LoggingCOM As Boolean
        Get
            Return IIf(VRD.ProgramSetLoggingLevel, 1, 0)
        End Get
        Set(value As Boolean)
            If value = True Then
                VRD.ProgramSetLoggingLevel(1)
            Else
                VRD.ProgramSetLoggingLevel(0)
            End If
        End Set
    End Property


    Private TemporarySeekTime As Long = 0
    Dim SeekStopWatchThread As Threading.Thread

    Public Function SeekToTime(ByVal Millisekunden, Optional BarPosition = 0) As Boolean
        Dim sobj As New SeekingObject(Millisekunden, BarPosition)
        Logger.DebugM("Starting SeekingInBackground Thread...")
        Dim SeekThread As New Threading.Thread(AddressOf SeekInBackground)
        SeekThread.Name = "SeekThread"
        SeekThread.Priority = Threading.ThreadPriority.Lowest
        SeekThread.Start(sobj)
        Logger.DebugM("SeekingInBackground Thread was started...")
        SeekThread.Join()
        Return True
    End Function

    Public Function MakeScreenshotClipboard(ByVal MSek As Long, Optional ByVal Quality As ScreenshotQuality = ScreenshotQuality.poor, Optional BarPosition As Integer = 0) As Image

        If VRD.AdScanIsScanning Then
            If VRD.NavigationCaptureFrame("", Quality) = True Then
                Return Clipboard.GetImage()
            Else
                Dim bmp3 As New Bitmap(20, 20)
                Return bmp3
            End If
        Else
            If Me.SeekToTime(MSek) = True Then
                Try
                    If VRD.NavigationCaptureFrame("", Quality) = True Then
                        Logger.DebugM("Thumbnail of millisecond {0} successfully created. VRD-Position: {1}", MSek.ToString, Me.GetCursorTime)
                        Return Clipboard.GetImage()
                    End If
                Catch excom As Exception
                    Dim bmp As New Bitmap(20, 20)
                    Return bmp
                End Try
            Else
                Dim bmp1 As New Bitmap(20, 20)
                Return bmp1
            End If
        End If
        Dim bmp2 As New Bitmap(20, 20)
        Return bmp2
    End Function
    Public Function MakeScreenshotFile(ByVal MSek As Integer, ByVal FileName As String, Optional ByVal Quality As ScreenshotQuality = ScreenshotQuality.poor) As Boolean
        VRD.NavigationSeekToTime(MSek)
        If VRD.NavigationCaptureFrame(FileName, Quality) = True Then
            Return True
        Else
            Return False
        End If
    End Function
    Public Function MakeScreenshotImageData(ByVal MSek As Integer, ByVal FileName As String, Optional ByVal Quality As ScreenshotQuality = ScreenshotQuality.poor) As Image
        VRD.NavigationSeekToTime(MSek)
        If VRD.NavigationCaptureFrame(FileName, Quality) = True Then
            Return Image.FromFile(FileName)
        Else
            Return Nothing
        End If
    End Function

    Public Function GetFramerate() As Long
        Return VRD.FileGetOpenedFileFrameRate()
    End Function

    Private AbortAdScan As Boolean = False
    Public Sub AbortScan()
        AbortAdScan = True
        VRD.AdScanToggleScan()
        'VRD.NavigationPause()
        'VRD.OutputAbort()
    End Sub

    Public Sub StartAdScan(ByVal FastSearch As Boolean, ByVal AutoCut As Boolean, Optional ByVal DisableDisplay As Boolean = True)
        Dim e As New AdDetectiveEventArgs
        Me.SeekToTime(0)
        VRD.AdScanSetParameter(0, FastSearch)
        VRD.AdScanSetParameter(1, AutoCut)
        VRD.AdScanSetParameter(2, DisableDisplay)
        VRD.AdScanToggleScan()
        If VRD.AdScanIsScanning = True Then
            RaiseEvent AdScanStarted(Me, e)
            Dim LastCutterCount As Integer = 0
            Do While VRD.AdScanIsScanning
                If AbortAdScan Then
                    RaiseEvent AdScanAborted(Me, New AdDetectiveEventArgs)
                    AbortAdScan = False
                    Exit Sub
                End If
                Threading.Thread.Sleep(1000)
            Loop
            RaiseEvent AdScanFinished(Me, e)
        Else
            Throw New Exception("Error on running 'AdDetective'")
        End If
    End Sub

    Private AbortSaving As Boolean = False
    Public Sub AbortVideoSaving()
        AbortSaving = True
        VRD.NavigationPause()
        VRD.OutputAbort()
        VRD.OUTPUT_STATE = 0
        VRD.OutputAbort()
        VRD.OUTPUT_STATE = 0
    End Sub

    Public Sub StartVideoSave(ByVal Filename As String)
        If SaveFileAsExt(Filename, VideoSaveFormat.MPEGtivo) Then
            Dim e As New RecordingSaveEventArgs
            RaiseEvent RecordingSaveStart(Me, e)
            Threading.Thread.Sleep(2000)
            Do While OutputInProgress
                e.PercentageComplete = VRD.OutputGetPercentComplete
                RaiseEvent RecordingSaveProgressCanged(Me, e)
                Threading.Thread.Sleep(500)
                If AbortSaving = True Then
                    RaiseEvent RecordingSaveAborted(Me, New RecordingSaveEventArgs)
                    AbortSaving = False
                    e.PercentageComplete = 0
                    Exit Sub
                End If
            Loop
            e.PercentageComplete = 100
            RaiseEvent RecordingSaveFinished(Me, e)
        Else
            Throw New Exception("Video could not be saved.")
        End If
    End Sub

    Public Sub ClearAllSelections()
        VRD.EditClearEditsList()
    End Sub

    Public Function SaveFileAsExt(ByVal Filename As String, Optional ByVal OutputType As VideoSaveFormat = 1) As Boolean
        If Left(Me.ReDoVersion, 1) = 5 Then
            Dim temp As Object = VRD.FileSaveAs(Filename, SavingProfile)
            If temp IsNot Nothing Then Return True
        End If
    End Function

    Public Function ProjectSave(ByVal Filename As String) As Boolean
        Return VRD.FileSaveProjectAs(Filename)
    End Function

    Public Enum OutputState As Integer
        None = 0
        Running = 1
        Paused = 2
    End Enum

    Public Enum NavigationState As Integer
        NoVideo = 0
        Paused = 1
        Playing = 2
        Scanning = 3
    End Enum

    Public Enum VideoSaveFormat As Integer
        ProgramStream = 1
        ElementaryStream = 2
        VOB = 3
        MPEGtivo = 7
    End Enum

    Public Enum ScreenshotQuality As Integer
        original = 1
        verygood = 2
        good = 3
        middle = 4
        poor = 5
        verypoor = 6
        Thumbnail = 7
        MiniThumbnail = 8
    End Enum

    Private disposedValue As Boolean = False        ' So ermitteln Sie überflüssige Aufrufe

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: Anderen Zustand freigeben (verwaltete Objekte).

            End If
            System.Runtime.InteropServices.Marshal. _
            ReleaseComObject(VRD)
            VRD = Nothing
            ' TODO: Eigenen Zustand freigeben (nicht verwaltete Objekte).
            ' TODO: Große Felder auf NULL festlegen.
        End If
        Me.disposedValue = True
    End Sub

    Public Structure VRDProfileInfo
        Dim Profilename As String
        Dim Encodingtype As String
        Dim Filetype As String
        Dim Resolution As String
        Dim Ratio As String
        Dim DeintarlaceModus As String
        Dim FrameRate As String
    End Structure

#Region " IDisposable Support "
    ' Dieser Code wird von Visual Basic hinzugefügt, um das Dispose-Muster richtig zu implementieren.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Ändern Sie diesen Code nicht. Fügen Sie oben in Dispose(ByVal disposing As Boolean) Bereinigungscode ein.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

    Private Sub CheckSeekingInBackground(SeekObject As Object)
        Dim sObj As SeekingObject = DirectCast(SeekObject, SeekingObject)
        Do
            If VRD IsNot Nothing And Me.GetCursorTime = sObj.SeekTime Then Exit Sub
            If DateDiff(DateInterval.Second, sObj.StartTime, Now) > _MaximumSeekTime.TotalSeconds Then
                Logger.DebugM("SeekStopWatch.ElapsedMilliseconds > MaximumSeekTime.Milliseconds. QuickstreamFix is Needed!!!!")
                RaiseEvent QuickStreamFixNeeded(Me, New EventArgs())
                Exit Sub
            End If
            Threading.Thread.Sleep(200)
        Loop
    End Sub

    Private Sub SeekInBackground(SeekObject As Object)
        Try
            Dim sObj As SeekingObject = DirectCast(SeekObject, SeekingObject)
            Logger.DebugM("Seek to position: {0}", sObj.SeekTime.ToString)
            Dim thSt As New Threading.ParameterizedThreadStart(Sub() CheckSeekingInBackground(SeekObject))
            Dim seekTr As New Threading.Thread(thSt)
            seekTr.Name = "CheckSeekingThread"
            seekTr.Priority = Threading.ThreadPriority.Lowest
            seekTr.Start()
            VRD.NavigationSeekToTime(sObj.SeekTime)
            Try
                seekTr.Abort()
            Catch ex As Threading.ThreadAbortException
                Logger.DebugM("The CheckSeeking thread was aborted!")
            End Try
        Catch ex As COMException
            Logger.DebugM("COM Exception: {0}", ex.ToString)
        Catch ex As Threading.ThreadAbortException
            Logger.DebugM("The Seeking thread was aborted!")
        Catch ex As Exception
            Logger.Info(ex.ToString)
        End Try

    End Sub

    Public Event SeekingComplete(sender As Object, e As SeekingEventArgs)
End Class

Public Class SeekingObject


    Public Sub New(ToTime As Long, OnBarPosition As Integer)
        Me.SeekTime = ToTime : Me.BarPosition = OnBarPosition
        Me.StartTime = Now
    End Sub

    Private _StarTime As DateTime
    Public Property StartTime() As DateTime
        Get
            Return _StarTime
        End Get
        Set(ByVal value As DateTime)
            _StarTime = value
        End Set
    End Property

    Private _SeekTime As Long
    Public Property SeekTime() As Long
        Get
            Return _SeekTime
        End Get
        Set(ByVal value As Long)
            _SeekTime = value
        End Set
    End Property

    Private _BarPosition As Integer
    Public Property BarPosition() As Integer
        Get
            Return _BarPosition
        End Get
        Set(ByVal value As Integer)
            _BarPosition = value
        End Set
    End Property
End Class


Public Class SeekingEventArgs
    Inherits EventArgs
    Public Sub New(Stat As enumStatus, ToTime As Long, ToBarPos As Integer)
        Me.Status = Stat : Me.Msek = ToTime : Me.MovieStripBarPosition = ToBarPos
    End Sub

    Private _Status As enumStatus
    Public Property Status() As enumStatus
        Get
            Return _Status
        End Get
        Set(ByVal value As enumStatus)
            _Status = value
        End Set
    End Property

    Private _Msek As Long
    Public Property Msek() As Long
        Get
            Return _Msek
        End Get
        Set(ByVal value As Long)
            _Msek = value
        End Set
    End Property

    Private _MovieStripBarPosition As Integer
    Public Property MovieStripBarPosition() As Integer
        Get
            Return _MovieStripBarPosition
        End Get
        Set(ByVal value As Integer)
            _MovieStripBarPosition = value
        End Set
    End Property

    Public Enum enumStatus
        Erfolgreich
        Zeitüberlauf
    End Enum

End Class

Public Class AdDetectiveEventArgs
    Inherits EventArgs

    Friend Sub New()
        Me.DetectCutter.Clear()
    End Sub

    Private mDetectCutter As New List(Of Long)
    ''' <summary>
    ''' Gibt die Liste von bis jetzt erkannten Schnittmarken zurück
    ''' </summary>
    Public Property DetectCutter() As List(Of Long)
        Get
            Return mDetectCutter
        End Get
        Set(ByVal value As List(Of Long))
            mDetectCutter = value
        End Set
    End Property

    Public ReadOnly Property LastCutString() As String
        Get
            If mDetectCutter.Count Mod 2 = 0 Then
                Return FormatTime(mDetectCutter(mDetectCutter.Count - 2)) & " - " & FormatTime(mDetectCutter(mDetectCutter.Count - 1))
            Else
                Return "Error"
            End If
        End Get
    End Property

    Public ReadOnly Property LastStartCut() As Long
        Get
            Return mDetectCutter(mDetectCutter.Count - 2)
        End Get
    End Property
    Public ReadOnly Property LastEndCut() As Long
        Get
            Return mDetectCutter(mDetectCutter.Count - 1)
        End Get
    End Property

    Public WriteOnly Property CutterAdd() As Long
        Set(ByVal value As Long)
            mDetectCutter.Add(value)
        End Set
    End Property

    Private Function FormatTime(ByVal lMSec As Long) _
      As String
        Dim ts As TimeSpan = TimeSpan.FromMilliseconds(lMSec)
        Return ts.Hours & ":" & ts.Minutes & ":" & ts.Seconds & "'" & ts.Milliseconds
    End Function

End Class

Public Class RecordingSaveEventArgs
    Inherits EventArgs

    Dim SaveStartTime As New Stopwatch
    Dim CalcTimeLeft As String = Translation.CalculateTimeLeft


    Public Sub New()
        Me.PercentageComplete = 0
        SaveStartTime.Start()

    End Sub

    Public ReadOnly Property TimeLeft() As String
        Get
            Return CalcTimeLeft
        End Get
    End Property

    Private mPercentageComplete As Double = 0
    Public Property PercentageComplete() As Double
        Get
            Return mPercentageComplete
        End Get
        Set(ByVal value As Double)
            mPercentageComplete = value
            If value <= 10 Then
                CalcTimeLeft = Translation.CalculateTimeLeft
            ElseIf value = 100 Then
                CalcTimeLeft = Translation.Complete
            Else
                Dim TimeRunning As Integer = SaveStartTime.ElapsedMilliseconds
                Dim TotalTime As Integer = (TimeRunning / value) * 100
                CalcTimeLeft = FormatTime(TotalTime - TimeRunning)
            End If
        End Set
    End Property

    Private Function FormatTime(ByVal lMSec As Long) _
      As String
        Dim ts As TimeSpan = TimeSpan.FromMilliseconds(lMSec)
        Return Format(ts.Minutes, "00") & ":" & Format(ts.Seconds, "00")
    End Function
End Class
