Module PlayerHelper

    Friend Function GetPlayerTimeString(ByVal milliseconds As Long, ByVal Framerate As Integer) As String
        Dim ts As TimeSpan = TimeSpan.FromMilliseconds(milliseconds)
        Dim Frame As Integer
        Frame = Int(ts.Milliseconds / (1000 / Framerate))
        Return Format(ts.Hours, "00") & ":" & Format(ts.Minutes, "00") & ":" & Format(ts.Seconds, "00") & " F:" & Format(Frame, "00") & "/" & Format(Framerate, "00")
    End Function



End Module
