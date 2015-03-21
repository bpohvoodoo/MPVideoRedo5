Imports MediaPortal.GUI.Library
Imports System.IO
Imports System.Drawing.Drawing2D
Imports System.Drawing

Module CutBarhelper

    Friend MovieStripBar As New CutBarMovieStrip
    Friend CutBar As New CutBarStandard

    Friend Sub MoviestripBarLoad(ByVal BarProperties As PropertyCollection, ByVal VideoWindow As GUIVideoControl)
        Logger.DebugM("Loading the MovieStripBar with properties of: ")
        For i As Integer = 0 To BarProperties.Count - 1
            Logger.DebugM("{0}-Property: {1}", BarProperties.Keys(i), BarProperties.Values(i))
        Next
        MovieStripBar.Top = VideoWindow.Location.Y + VideoWindow.Size.Height + BarProperties("Top")
        MovieStripBar.Left = VideoWindow.Location.X + BarProperties("Left")
        MovieStripBar.Width = BarProperties("Width")
        MovieStripBar.Height = BarProperties("Height")
        MovieStripBar.LineMarkerForeground = BarProperties("LineMarkerForeground")
        MovieStripBar.LineMarkerColor = Drawing.Color.FromName(BarProperties("LinemarkerColor"))
        MovieStripBar.LineMarkerThickness = BarProperties("LineMarkerThickness")
        MovieStripBar.DarkBackColor = Drawing.Color.FromName(BarProperties("DarkBackColor"))
        MovieStripBar.LightFillColor = Drawing.Color.FromName(BarProperties("LightFillColor"))
        MovieStripBar.LightBackColor = Drawing.Color.FromName(BarProperties("LightBackColor"))
        MovieStripBar.DarkFillColor = Drawing.Color.FromName(BarProperties("DarkFillColor"))
        MovieStripBar.Opacity = BarProperties("Opacity")
        MovieStripBar.Enabled = False
        GUIGraphicsContext.form.Controls.Add(MovieStripBar)
        GUIGraphicsContext.form.Focus()
        Logger.DebugM("MovieStripBar successfully loaded.")
    End Sub
    Friend Sub MoviestripBarUnload()
        Logger.DebugM("Removing the Moviestripbar...")
        GUIGraphicsContext.form.Controls.Remove(MovieStripBar)
        Logger.DebugM("Moviestripbar was removed.")
    End Sub

    Friend Sub CutbarLoad(ByVal BarProperties As PropertyCollection, ByVal VideoWindow As GUIVideoControl)
        Logger.DebugM("Loading the Cutbar with properties of: ")
        For i As Integer = 0 To BarProperties.Count - 1
            Logger.DebugM("{0}-Property: {1}", BarProperties.Keys(i), BarProperties.Values(i))
        Next
        CutBar.Top = VideoWindow.Location.Y + VideoWindow.Size.Height + BarProperties("Top")
        CutBar.Left = VideoWindow.Location.X + BarProperties("Left")
        CutBar.Width = BarProperties("Width")
        CutBar.Height = BarProperties("Height")
        CutBar.LineMarkerColor = Drawing.Color.FromName(BarProperties("LinemarkerColor"))
        CutBar.LineMarkerThickness = BarProperties("LineMarkerThickness")
        CutBar.DarkBackColor = Drawing.Color.FromName(BarProperties("DarkBackColor"))
        CutBar.LightFillColor = Drawing.Color.FromName(BarProperties("LightFillColor"))
        CutBar.LightBackColor = Drawing.Color.FromName(BarProperties("LightBackColor"))
        CutBar.DarkFillColor = Drawing.Color.FromName(BarProperties("DarkFillColor"))
        CutBar.Enabled = False
        GUIGraphicsContext.form.Controls.Add(CutBar)
        GUIGraphicsContext.form.Focus()
        Logger.DebugM("Cutbar successfully loaded.")
    End Sub

    Friend Sub CutBarUnload()
        Logger.DebugM("Removing the Cutbar...")
        GUIGraphicsContext.form.Controls.Remove(CutBar)
        Logger.DebugM("Cutbar was removed.")
    End Sub

    Friend Function BarsGetProperties(ByVal XMLfile As String, Optional ByVal Cutbar As Boolean = False) As PropertyCollection
        BarsGetProperties = New PropertyCollection
        Dim objDateiLeser As New StreamReader(XMLfile)
        Dim XmlText As String = objDateiLeser.ReadToEnd()
        objDateiLeser.Close()
        objDateiLeser = Nothing

        If XmlText IsNot Nothing Then
            Dim propStart As Integer, propCount As Integer
            If Cutbar Then
                propStart = InStr(XmlText, "Start Cutbar-Properties") + 23
                propCount = InStrRev(XmlText, "End Cutbar-Properties") - propStart
            Else
                propStart = InStr(XmlText, "Start MoviestripBar-Properties") + 30
                propCount = InStrRev(XmlText, "End MoviestripBar-Properties") - propStart
            End If
            Dim newText As String = Mid(XmlText, propStart, propCount)

            Dim arrPropStrings As String() = Split(newText, ";")

            For i As Integer = 0 To arrPropStrings.Length - 1
                For o = 0 To 50
                    arrPropStrings(i) = Replace(arrPropStrings(i), " ", "")
                    arrPropStrings(i) = Replace(arrPropStrings(i), vbNewLine, "")
                Next
                BarsGetProperties.Add(Left(arrPropStrings(i), InStr(arrPropStrings(i), "=") - 1), Mid(arrPropStrings(i), InStr(arrPropStrings(i), "=") + 1))
            Next
            Return BarsGetProperties
        Else
            Return Nothing
        End If
    End Function
End Module
