Imports MediaPortal.Configuration
Imports MediaPortal.Dialogs
Imports MediaPortal.GUI.Library
Imports MediaPortal.Profile
Imports MediaPortal.Util
Imports MPVideoRedo5.MPVideoRedo5
Imports TvdbLib
Imports TvdbLib.Cache
Imports TvdbLib.Data
Imports TvControl
Imports System.IO
Imports System.Windows.Forms

Public Module Helper
    Friend VRD As VideoReDo5
    'Friend Recs As clsRecordings

    Public RecordingToCut As clsRecordings.Recordings

    Public Function ParseSaveVideoFilename(ByVal Recording As clsRecordings.Recordings, Optional ByVal IsSerie As Boolean = False) As String
        'Dim newFileName As String = Mid(Recording.VideoFilename, InStrRev(Recording.VideoFilename, "\") + 1)
        Dim Filename As String
        Dim Folder As String = Nothing
        If IsSerie Then
            Filename = HelpConfig.GetConfigString(ConfigKey.SaveSeriesFilename)
            If HelpConfig.GetConfigString(ConfigKey.CreateSeriesfolder) Then
                Folder = HelpConfig.GetConfigString(ConfigKey.SeriesFolder)
                Folder = Parse(Recording, Folder)
            End If
        Else
            Filename = HelpConfig.GetConfigString(ConfigKey.SaveMovieFilename)
            If HelpConfig.GetConfigString(ConfigKey.CreateMoviefolder) Then
                Folder = HelpConfig.GetConfigString(ConfigKey.MovieFolder)
                Folder = Parse(Recording, Folder)
            End If
        End If
        Filename = Parse(Recording, Filename)
        Filename = CleanFilename(Filename, "_")
        Folder = Parse(Recording, Folder)
        Folder = CleanPathname(Folder, "_")
        Filename = Folder & "\" & Filename
        Filename += ".%ext%"
        Return Filename
    End Function

    Public Function GetBannerPath(ByVal img As Drawing.Image, SeriesID As Integer) As String
        Try
            Dim mFolder As String = Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5\SeriesBanners"
            If IO.Directory.Exists(mFolder) Then
            Else
                IO.Directory.CreateDirectory(mFolder)
            End If
            mFolder = mFolder & "\" & SeriesID.ToString & ".jpg"
            If IO.File.Exists(mFolder) Then
                Logger.DebugM("The deserved thumbnail already exists in '{0}'. Returning path (GetSaveImagePath): ", mFolder)
                Return mFolder
            End If
            'IO.File.Delete(NewPath)
            img.Save(mFolder)
            Logger.DebugM("The deserved thumbnail was saved at '{0}'. Returning path (GetSaveImagePath): ", mFolder)
            Return mFolder
        Catch ex As Exception
            Logger.Info("Could not get the deserved thumbnail.(GetSaveImagePath)")
            Return Nothing
        End Try
    End Function

    Public Function GetSaveThumbPath(ByVal Record As clsRecordings.Recordings) As String
        'ffmpeg -an -ss 0:10:0 -t 0:0:0.001 -i "\\HTPC\Daten\Aufnahmen\American Blackout\American Blackout.ts" -f image2 -s 320x200 thumb.jpg
        Dim PreviewThumb As String
        Dim thumbnailFilename = System.IO.Path.GetFileNameWithoutExtension(Record.VideoFilename) + ".jpg"
        PreviewThumb = Thumbs.TVRecorded & "\" & thumbnailFilename
        If Not Utils.FileExistsInCache(PreviewThumb) Then
            Logger.DebugM("Thumbnail {0} does not exist in local thumbs folder - get it from TV server", PreviewThumb)
            Try
                Dim thumbData As Byte() = RemoteControl.Instance.GetRecordingThumbnail(thumbnailFilename)
                If (thumbData.Length > 0) Then
                    Dim fs As New FileStream(PreviewThumb, FileMode.Create)
                    fs.Write(thumbData, 0, thumbData.Length)
                    fs.Close()
                    fs.Dispose()
                    Utils.DoInsertExistingFileIntoCache(PreviewThumb)
                Else
                    Logger.DebugM("Thumbnail {0} not found on TV server", PreviewThumb)
                End If
            Catch ex As Exception
                Logger.DebugM("Error fetching thumbnail {0} from TV server - {1}", PreviewThumb, ex.Message)
            End Try
        End If
        If Utils.FileExistsInCache(PreviewThumb) Then
            Return PreviewThumb
            Exit Function
        Else
            Try
                Logger.DebugM("GetSaveThumbnailPath for recording: {0}", Record.VideoFilename)
                Dim mFolder As String = ""
                mFolder = Config.GetFolder(Config.Dir.Cache) & "\MPVideoRedo5"
                Logger.DebugM("Thumbsfolder: " & mFolder)
                If IO.Directory.Exists(mFolder) Then
                Else
                    Logger.DebugM("Thumbnailfolder does not exist. Creating folder.")
                    IO.Directory.CreateDirectory(mFolder)
                    Logger.DebugM("Thumbnailfolder successfully created.")
                End If
                Dim NewPath As String = mFolder & "\" & CleanFilename(Record.Title) & "_" & Record.StartTime.Ticks & ".jpg"
                If IO.File.Exists(NewPath) Then
                    Logger.DebugM("The deserved thumbnail already exists in '{0}' and is loaded (GetSaveThumbPath) from: ", NewPath)
                    Return NewPath
                Else
                    Logger.DebugM("The deserved thumbnail does not exists . Trying to extract thumbnail from the videofile (GetShellVideoThumbnail)...")
                    Dim img As Drawing.Bitmap = GetShellVideoThumbnail(Record.VideoFilename)
                    If img Is Nothing Then
                        'img = New Drawing.Bitmap(100, 100, Drawing.Imaging.PixelFormat.Format32bppArgb)
                        img.Save(NewPath)
                    Else
                        img.Save(NewPath)
                        Logger.DebugM("The deserved thumbnail was saved in '{0}' (GetSaveThumbPath).", NewPath)
                    End If
                    Return NewPath
                End If

            Catch ex As Exception
                Logger.Info("The deserved thumbnail could not be loaded.(GetSaveThumbPath)")
                Return Nothing
            End Try
        End If
    End Function

    Public Function GetShellVideoThumbnail(ByVal Filename As String) As Drawing.Bitmap
        Try
            Dim ShIcon As New ShellThumbnail
            ShIcon.DesiredSize = New System.Drawing.Size(330, 220)
            Return ShIcon.GetThumbnail(Filename)
        Catch ex As Exception
            Logger.Info("The deserved thumbnail " & Filename & " could not be loaded.")
            Return Nothing
        End Try
    End Function

#Region "Dialoge"

    Friend Sub DialogNotify(ByVal WindowID As Integer, ByVal TimeOut As Integer, ByVal Title As String, ByVal Text As String)
        Logger.DebugM("Creating NotifyDialog with message: {0}", Text)
        Dim dlg As GUIDialogNotify = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_NOTIFY, Integer)), GUIDialogNotify)
        Try
            dlg.SetHeading(Title)
            dlg.SetText(Text)
            dlg.TimeOut = TimeOut
            dlg.DoModal(WindowID)
            GUIWindowManager.Process()
            dlg.Reset()
            dlg = Nothing
        Catch ex As Exception
            Try
                dlg.Reset()
                dlg = Nothing
                Logger.Error(ex.ToString)
            Catch
            End Try
        End Try
    End Sub

    Public Function DialogYesNo(ByVal WindowID As Integer, ByVal actYes As Boolean, ByVal Title As String, ByVal Text1 As String, Optional ByVal Text2 As String = "", Optional ByVal Text3 As String = "") As Boolean
        Logger.DebugM("Creating YesNo-Dialog... WindowID:'{0}', Title:'{1}', Text:'{2} {3} {4}'", WindowID, Title, Text1, Text2, Text3)
        Dim dlg As GUIDialogYesNo = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_YES_NO, Integer)), GUIDialogYesNo)
        If actYes Then
            dlg.SetDefaultToYes(True)
        End If
        dlg.SetHeading(Title)
        dlg.SetLine(1, Text1)
        dlg.SetLine(2, Text2)
        dlg.SetLine(3, Text3)
        dlg.DoModal(WindowID)
        Logger.DebugM("Dialog created '{0}'", Title)
        If dlg.IsConfirmed Then
            Logger.DebugM("Dialog was answered with YES.")
            Return True
        Else
            Logger.DebugM("Dialog was answered with NO.")
            Return False
        End If
    End Function

    Public Sub DialogProfile(ByVal GetID As Integer)
        Dim dlgProfiles As GUIDialogMenu = CType(GUIWindowManager.GetWindow(CType(GUIWindow.Window.WINDOW_DIALOG_MENU, Integer)), GUIDialogMenu)
        dlgProfiles.Reset()
        For Each item In VRD.GetProfileList
            dlgProfiles.Add(item)
        Next
        dlgProfiles.SetHeading(Translation.SavingProfile)
        dlgProfiles.DoModal(GetID)
        If HelpConfig.GetConfigString(ConfigKey.ProfileDetails) = False Then
            Dim newprofilename As String = "Nothing"
            Do
                If dlgProfiles.SelectedLabel > -1 Then
                    Dim dlgProfileDetail As GUIProfileDetail = CType(GUIWindowManager.GetWindow(enumWindows.GUIProfileDetails), GUIProfileDetail)
                    dlgProfileDetail.Reset()
                    dlgProfileDetail.SetHeading(dlgProfiles.SelectedLabelText)
                    dlgProfileDetail.DoModal(GetID)
                    newprofilename = dlgProfileDetail.SelectedLabelText
                    dlgProfileDetail = Nothing
                Else
                    Exit Sub
                End If
            Loop While newprofilename = "Nothing"
            VRD.SavingProfile = newprofilename
        Else
            VRD.SavingProfile = dlgProfiles.SelectedLabelText
            GetProfileDetail(dlgProfiles.SelectedLabelText)
        End If
    End Sub

#End Region

    Public Function HasPathSubDirectories(ByVal Path As String) As Boolean
        Logger.DebugM("Checking for existance of subfolders in the folder ...")
        Dim aktDir As New IO.DirectoryInfo(Path)
        If aktDir.Exists Then
            If aktDir.GetDirectories().Count > 0 Then
                Logger.DebugM("There are {0} subfolders", aktDir.GetDirectories.Count)
                Return True
            Else
                Logger.DebugM("There are no more subfolders.")
                Return False
            End If
        Else
            Logger.Warn("HasPathSubDirectories returns that the path is not correct.")
            Return False
        End If
    End Function

    Public Function CleanFilename(ByVal sFilename As String, Optional ByVal sChar As String = "") As String
        ' replace all ineligible characters
        Return System.Text.RegularExpressions.Regex.Replace(sFilename, "[\\/:?*^""<>|]", sChar)
    End Function

    Public Function CleanPathname(ByVal sFilename As String, Optional ByVal sChar As String = "") As String
        ' replace all ineligible characters
        Return System.Text.RegularExpressions.Regex.Replace(sFilename, "[/:?*^""<>|]", sChar)
    End Function

    Public Function GetGUIWindow(ByVal Windows As enumWindows) As Integer
        Return Convert.ToInt16(Windows)
    End Function

    Public Enum enumWindows
        GUIstart = 1208
        GUIMain = 1209
        GUIProfileDetails = 1210
        GUISave = 1211
        GUISaveProgress = 1212
    End Enum

    Public Declare Function FindWindow Lib "user32.dll" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As Long
    Public Declare Function SetFocusAPI Lib "user32.dll" Alias "SetFocus" (ByVal hWnd As Long) As Long
    Public Declare Function SetForegroundWindow Lib "user32" (ByVal hWnd As Long) As Long

    Public Sub SetMPtoForeground(ModuleScreen As String)
        Dim hWnd As Long
        hWnd = FindWindow(vbNullString, "MediaPortal - " + ModuleScreen)
        SetForegroundWindow(hWnd)
    End Sub

    Public Sub GetProfileDetail(profile As String)
        Dim SavingFilename As String
        SavingFilename = Replace(RecordingToCut.SavingFilename, "%ext%", VRD.GetProfileInfo(profile).Filetype.ToLower)
        Translator.SetProperty("#Saving.Name", SavingFilename)
        Translator.SetProperty("#Saving.Profile", profile)
        Translator.SetProperty("#Profile.Encodingtype", VRD.GetProfileInfo(profile).Encodingtype)
        Translator.SetProperty("#Profile.Filetype", VRD.GetProfileInfo(profile).Filetype)
        Translator.SetProperty("#Profile.Resolution", VRD.GetProfileInfo(profile).Resolution)
        Translator.SetProperty("#Profile.Ratio", VRD.GetProfileInfo(profile).Ratio)
        Translator.SetProperty("#Profile.Deinterlacemode", VRD.GetProfileInfo(profile).DeintarlaceModus)
        Translator.SetProperty("#Profile.Framerate", VRD.GetProfileInfo(profile).FrameRate)

    End Sub

    Public Function Parse(ByVal Recording As clsRecordings.Recordings, ByVal ParseConfig As String) As String
        ParseConfig = Replace(ParseConfig, "%OriginalFilename%", Recording.Filename)
        ParseConfig = Replace(ParseConfig, "%Title%", Recording.Title)
        ParseConfig = Replace(ParseConfig, "%ChannelName%", Recording.Channelname)
        ParseConfig = Replace(ParseConfig, "%StartTime%", Recording.StartTime)
        ParseConfig = Replace(ParseConfig, "%EndTime%", Recording.EndTime)
        ParseConfig = Replace(ParseConfig, "%EpisodeName%", Recording.Episodename)
        ParseConfig = Replace(ParseConfig, "%EpisodeNumber%", Recording.EpisodeNum)
        ParseConfig = Replace(ParseConfig, "%SeriesName%", Recording.Seriesname)
        ParseConfig = Replace(ParseConfig, "%Genre%", Recording.Genre)
        ParseConfig = Replace(ParseConfig, "%SeasonNumber%", Recording.SeriesNum)
        Return ParseConfig
    End Function

    Public Function ShowKeyboard(ByVal Text As String, ByVal WindowID As Integer) As String
        Dim keyboard As VirtualKeyboard = DirectCast(GUIWindowManager.GetWindow(CInt(GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD)), VirtualKeyboard)
        If keyboard Is Nothing Then
            Return Nothing
        End If
        keyboard.Reset()
        keyboard.Text = Text
        keyboard.DoModal(WindowID)
        ' Do something here. The typed value is stored in "keyboard.Text" 
        If keyboard.IsConfirmed Then
            If keyboard.Text.Length = 0 Then Return Text
            Return keyboard.Text
        Else
            Return Nothing
        End If
    End Function

End Module