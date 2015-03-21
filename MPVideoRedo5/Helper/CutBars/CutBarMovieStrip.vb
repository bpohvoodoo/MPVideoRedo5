﻿Imports System.Collections.Generic
Imports System.Text
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.ComponentModel
Imports System.Drawing.Design

Public Class CutBarMovieStrip
    Inherits CutBarProgressBase

    ''' <summary>Linemerker im Vordergrund</summary>
    Private m_LineMarkerForeground As Boolean = False
    ''' <summary>Durchsichtigkeit der Values</summary>
    Private m_Opacity As Integer = 100
    ''' <summary>Liste der Filmstripbitmaps</summary>
    Private m_MovieStripThumbs As New List(Of Bitmap)
    ''' <summary>Nimmt den String des Auswahldialogs für die Bilder auf</summary>
    Private m_PropertyMovieImages As New ImageList

    ''' <summary>Fired, when change the opacity</summary>
    Public Event ValueOpacityChanged As EventHandler
    Public Event Painted(ByVal sender As Object, ByVal e As CutBarProgressEventArgs)
    ''' <summary>The Listof Bitmaps for the Filmstrip</summary>
    <Browsable(False)> _
    <Category("Werteeinstellung")> _
    <Description("Verlangt ein List(of Single) um die StartCuts oder Terminstarts zu Kennzeichnen oder gibt diese zurück. Die List.Count sollte gleich sein mit der Liste des EndCutValues.Count")> _
    Public Property MovieStripThumbs() As List(Of Bitmap)
        Get
            Return m_MovieStripThumbs
        End Get
        Set(ByVal value As List(Of Bitmap))
            m_MovieStripThumbs = value
            'Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' Specifyes the Opacity of the Valuesbars
    ''' </summary>
    <DefaultValue(100)> _
    <Description("Bestimmt den grad der Duchsichtigkeit der Valuesbalken oder gibt diesen zurück. Wert muss >=0 und <=100 sein.")> _
    <Category("Werteeinstellung")> _
    Public Property Opacity() As Integer
        Get
            Return m_Opacity
        End Get
        Set(ByVal value As Integer)
            If value >= 0 Or value <= 100 Then
                m_Opacity = value
                Invalidate()
            End If
        End Set
    End Property


    ''' <summary>
    ''' Gets or sets the LineMarker to foreground
    ''' </summary>
    <DefaultValue(False)> _
    <Category("Werteeinstellung")> _
    <Description("Gibt an ob sich der Linemarker vor oder hinter den Filmstreifen befinden soll")> _
    Public Property LineMarkerForeground() As Boolean
        Get
            Return m_LineMarkerForeground
        End Get
        Set(ByVal value As Boolean)
            m_LineMarkerForeground = value
            Invalidate()
            OnLineMarkerChanged(New CutBarProgressEventArgs)
        End Set
    End Property

    ''' <summary>
    ''' The Imagelist of the bar
    ''' </summary>
    <DefaultValue("")> _
    <Description("Gibt die Imagelist für die Bilder an welche in der Bar erscheinen sollen. Note: Beim hinzufügen einer Imagelist werden die Bilder Automatisch dem Property 'Filmbitmaps' hinzugefügt")> _
    <Category("Werteeinstellung")> _
    Public Property MovieThumbs() As ImageList
        Get
            Return m_PropertyMovieImages
        End Get
        Set(ByVal value As ImageList)
            MovieStripThumbs.Clear()
            For Each itm As Bitmap In value.Images
                MovieStripThumbs.Add(itm)
            Next
            m_PropertyMovieImages = value
        End Set
    End Property

    Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)

        Dim rect As New Rectangle(1, 1, Width - 2, Height - 2)
        Dim g As Graphics = e.Graphics
        Dim xPos As Single
        Dim yPos As Single
        g.SmoothingMode = SmoothingMode.HighQuality
        Dim path As GraphicsPath = GetPath(rect)

        ' Draw back gradient
        Using brush As New LinearGradientBrush(rect, LightBackColor, DarkBackColor, LinearGradientMode.Vertical)
            g.FillPath(brush, path)
        End Using
        'Draw Linemarker if Linemarkerforeground = False
        If LineMarkerForeground = False Then
            If LineMarkerPosition >= 0 And LineMarkerPosition <= 100 Then
                Using pen1 As New Pen(LineMarkerColor, LineMarkerThickness)
                    g.SetClip(path)
                    xPos = rect.Width * (LineMarkerPosition / 100)
                    g.DrawLine(pen1, xPos, rect.Y, xPos, rect.Y + rect.Height)
                    'Obere Markierung
                    Dim l As New List(Of Point)
                    l.Add(New Point((xPos - (LineMarkerThickness / 2)) - (LineMarkerThickness * 2), rect.Y))
                    l.Add(New Point((xPos + (LineMarkerThickness / 2)) + (LineMarkerThickness * 2), rect.Y))
                    l.Add(New Point((xPos - (LineMarkerThickness / 2)), rect.Y + (LineMarkerThickness * 2)))
                    l.Add(New Point((xPos + (LineMarkerThickness / 2)), rect.Y + (LineMarkerThickness * 2)))
                    Dim br As Brush = pen1.Brush
                    g.FillPolygon(br, l.ToArray)
                    g.ResetClip()

                    'Untere Markierung
                    g.SetClip(path)
                    Dim l1 As New List(Of Point)
                    l1.Add(New Point((xPos - (LineMarkerThickness / 2)) - (LineMarkerThickness * 2), rect.Y + rect.Height))
                    l1.Add(New Point((xPos + (LineMarkerThickness / 2)) + (LineMarkerThickness * 2), rect.Y + rect.Height))
                    l1.Add(New Point((xPos - (LineMarkerThickness / 2)), (rect.Y + rect.Height) - (LineMarkerThickness * 2)))
                    l1.Add(New Point((xPos + (LineMarkerThickness / 2)), (rect.Y + rect.Height) - (LineMarkerThickness * 2)))
                    g.FillPolygon(br, l1.ToArray)
                    g.ResetClip()
                End Using
            End If
        End If

        'Draw the Filmstrip Bitmap ------------
        g.SetClip(path)
        Dim Gesamtbreite As Integer = 0
        Dim Counter As Integer = 0
        Dim bTemp As Bitmap = My.Resources.negativ
        bTemp = PicResizeByHeight(bTemp, rect.Height)
        Do
            Using myimg As New Bitmap(bTemp)
                If MovieStripThumbs.Count > 0 Then
                    If MovieStripThumbs.Count - 1 >= Counter Then

                        Dim bFilm As New Bitmap(PicResize(MovieStripThumbs(Counter), rect.Height - (rect.Height / 100 * 25), bTemp.Width))
                        If bFilm IsNot Nothing Then
                            g.DrawImage(bFilm, New PointF(Gesamtbreite + rect.X, rect.Y + (rect.Height / 100 * 13)))
                        End If
                    End If
                End If
                g.DrawImage(myimg, New PointF(Gesamtbreite + rect.X, rect.Y))
                Gesamtbreite += bTemp.Width - 1 : Counter += 1

            End Using
        Loop Until (Gesamtbreite >= rect.Width)
        bTemp = Nothing
        g.ResetClip()
        'Check if Startcutvalue.count or Endcutvalue.count is invalid
        For i As Integer = 0 To StartCutValues.Count - 1
            If EndCutValues.Count < StartCutValues.Count Then
                For o As Integer = StartCutValues.Count - EndCutValues.Count To StartCutValues.Count
                    EndCutValues.Add(StartCutValues(o - 1) + 0.1)
                Next
            End If
            If StartCutValues.Count < EndCutValues.Count Then
                For o1 As Integer = EndCutValues.Count - StartCutValues.Count To EndCutValues.Count
                    StartCutValues.Add(EndCutValues(o1 - 1) - 0.1)
                Next
            End If

            'Draw Cutter
            xPos = rect.X + rect.Width * (StartCutValues(i) / 100)
            yPos = rect.Width * ((EndCutValues(i) - StartCutValues(i)) / 100)
            'Adding one pixel to width, because it is too small to draw
            If yPos < 1 Then
                If StartCutValues(i) <= 50 Then
                    yPos = yPos + 1
                Else
                    xPos = xPos - 1
                End If
            End If
            ' Draw fill gradient
            Dim c As Color = Color.FromArgb(50, LightFillColor)
            Using brush As New LinearGradientBrush(rect, Color.FromArgb((Opacity / 100) * 255, LightFillColor), Color.FromArgb((Opacity / 100) * 255, DarkFillColor), LinearGradientMode.Vertical)
                Dim clip As New RectangleF(xPos, rect.Y, yPos, rect.Height)
                g.SetClip(clip)
                g.FillPath(brush, path)
                g.ResetClip()
            End Using
        Next

        'Draw Linemarker if Linemarkerforeground = true
        If LineMarkerForeground = True Then
            If LineMarkerPosition >= 0 Then
                Using pen1 As New Pen(LineMarkerColor, LineMarkerThickness)
                    g.SetClip(path)
                    xPos = rect.Width * (LineMarkerPosition / 100)
                    g.DrawLine(pen1, xPos, rect.Y, xPos, rect.Y + rect.Height)
                    'Obere Markierung
                    Dim l As New List(Of Point)
                    l.Add(New Point((xPos - (LineMarkerThickness / 2)) - (LineMarkerThickness * 2), rect.Y))
                    l.Add(New Point((xPos + (LineMarkerThickness / 2)) + (LineMarkerThickness * 2), rect.Y))
                    l.Add(New Point((xPos - (LineMarkerThickness / 2)), rect.Y + (LineMarkerThickness * 2)))
                    l.Add(New Point((xPos + (LineMarkerThickness / 2)), rect.Y + (LineMarkerThickness * 2)))
                    Dim br As Brush = pen1.Brush
                    g.FillPolygon(br, l.ToArray)
                    g.ResetClip()
                    'Untere Markierung
                    g.SetClip(path)
                    Dim l1 As New List(Of Point)
                    l1.Add(New Point((xPos - (LineMarkerThickness / 2)) - (LineMarkerThickness * 2), rect.Y + rect.Height))
                    l1.Add(New Point((xPos + (LineMarkerThickness / 2)) + (LineMarkerThickness * 2), rect.Y + rect.Height))
                    l1.Add(New Point((xPos - (LineMarkerThickness / 2)), (rect.Y + rect.Height) - (LineMarkerThickness * 2)))
                    l1.Add(New Point((xPos + (LineMarkerThickness / 2)), (rect.Y + rect.Height) - (LineMarkerThickness * 2)))
                    g.FillPolygon(br, l1.ToArray)
                    g.ResetClip()
                End Using
            End If
        End If

        ' draw light
        Using brush As New SolidBrush(Color.FromArgb(48, Color.White))
            g.SetClip(path)
            g.FillRectangle(brush, New RectangleF(rect.X, rect.Y, rect.Width, rect.Height / 2.0F))
            g.ResetClip()
        End Using


        'Draw Text
        If Text.Length > 0 Then
            Dim fontSize As SizeF = g.MeasureString(Text, Font)
            Dim fontRect As New RectangleF(rect.X + rect.Width / 2.0F - fontSize.Width / 2.0F, rect.Y + rect.Height / 2.0F - fontSize.Height / 2.0F, fontSize.Width, fontSize.Height)
            g.DrawString(Text, Font, New SolidBrush(ForeColor), fontRect)
        End If


        RaiseEvent Painted(Me, New CutBarProgressEventArgs)
    End Sub

    Private mgraphic As Graphics
    Private Property graphic() As Graphics
        Get
            Return mgraphic
        End Get
        Set(ByVal value As Graphics)
            mgraphic = value
        End Set
    End Property

    Friend ReadOnly Property ControlBitmap() As Bitmap
        Get
            Dim BMP As New Bitmap(Width, Height)
            Dim gr As Graphics = Graphics.FromImage(BMP)
            gr.Dispose()
            Return BMP

        End Get
    End Property


#Region "private functions"
    ''' <summary>
    ''' Diese Funktion ändert die größe eines Bilds und gibt es als Bitmap zurück. Hier ist die neue Höhe veränderbar
    ''' </summary>
    ''' <param name="SourceImage">Das Bild dessen größe verändert werden soll</param>
    ''' <param name="NewHeight">Die neue Breite des Bilds, das Größenverhältnis des Bilds wird eingehalten</param>
    Private Function PicResizeByHeight(ByVal SourceImage As Image, ByVal NewHeight As Integer) As Bitmap
        Dim SizeFactor As Decimal = NewHeight / SourceImage.Height
        Dim NewWidth As Integer = SizeFactor * SourceImage.Width
        Dim NewImage As New Bitmap(NewWidth, NewHeight)
        Using G As Graphics = Graphics.FromImage(NewImage)
            G.InterpolationMode = InterpolationMode.HighQualityBicubic
            G.DrawImage(SourceImage, New Rectangle(0, 0, NewWidth, NewHeight))
        End Using
        Return NewImage
    End Function
    ''' <summary>
    ''' Diese Funktion ändert die größe eines Bilds und gibt es als Bitmap zurück. Hier ist die neue Breite und die neue Höhe veränderbar
    ''' </summary>
    Private Function PicResize(ByVal SourceImage As Image, ByVal NewHeight As Integer, ByVal NewWidth As Integer) As Bitmap
        Dim NewImage As New Bitmap(NewWidth, NewHeight)
        Try
            Using G As Graphics = Graphics.FromImage(NewImage)
                G.InterpolationMode = InterpolationMode.HighQualityBicubic
                G.DrawImage(SourceImage, New Rectangle(0, 0, NewWidth, NewHeight))
            End Using
            Return NewImage
        Catch ex As Exception
            Return NewImage
        End Try
    End Function



#End Region

End Class
