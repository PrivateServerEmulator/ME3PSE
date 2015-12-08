Option Strict On

Imports ME3PlayerDataEditor.PlayerProfile

Module Module1

    Public Const PLAYER_FILES As String = "PLAYER_FILES.TXT"
    Public Const DESC_INV As String = "DESC_INV.TXT"
    Public Const DESC_REWARDS As String = "DESC_REWARDS.TXT"
    Public PlayerTextFile As String
    Public DescriptionTable As XHashtable
    Public RewardsTable As XHashtable
    Public CurrentProfile As ME3MP_Profile

    Public Class XHashtable
        Inherits Hashtable
        Public KeysList As List(Of Object)

        Public Sub New()
            KeysList = New List(Of Object)
        End Sub

        Public Overrides Sub Add(key As Object, value As Object)
            MyBase.Add(key, value)
            KeysList.Add(key)
        End Sub
    End Class

    'http://blog.codinghorror.com/determining-build-date-the-hard-way/
    Function RetrieveAppLinkerTimestampString() As String
        Const PeHeaderOffset As Integer = 60
        Const LinkerTimestampOffset As Integer = 8
        Dim b(2047) As Byte
        Dim s As IO.Stream
        Try
            s = New IO.FileStream(Application.ExecutablePath, IO.FileMode.Open, IO.FileAccess.Read)
            s.Read(b, 0, 2048)
        Finally
            If Not s Is Nothing Then s.Close()
        End Try
        Dim i As Integer = BitConverter.ToInt32(b, PeHeaderOffset)
        Dim SecondsSince1970 As Integer = BitConverter.ToInt32(b, i + LinkerTimestampOffset)
        Dim dt As New DateTime(1970, 1, 1, 0, 0, 0)
        dt = dt.AddSeconds(SecondsSince1970)
        'dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours)
        Return dt.ToString("MMM d yyyy HH:mm:ss UTC")
    End Function

    'Public Sub InitializeDescriptionTable()
    '       DescriptionTable = New Hashtable
    '  Dim strDesc() As String
    '     Try
    '        strDesc = System.IO.File.ReadAllLines(DESC_INV)
    '   Catch ex As Exception
    '      Exit Sub
    ' End Try

    'Dim key As Integer
    'Dim value As String
    '   For I As Integer = 0 To strDesc.Count - 1
    'Dim n As Integer = strDesc(I).IndexOf("=")
    '       If n < 1 Then Continue For
    '      If Not Integer.TryParse(strDesc(I).Substring(0, n), key) Then Continue For
    '     value = strDesc(I).Substring(n + 1)
    '    DescriptionTable(key) = value
    'Next I
    'End Sub

    Public Sub InitializeTable(ByRef TargetTable As XHashtable, ByVal strTextFile As String)
        TargetTable = New XHashtable
        Dim strLines() As String
        Try
            strLines = System.IO.File.ReadAllLines(strTextFile)
        Catch ex As Exception
            Exit Sub
        End Try

        Dim key As Integer
        Dim value As String
        For I As Integer = 0 To strLines.Count - 1
            If strLines(I).IndexOf("#") = 0 Then Continue For
            Dim n As Integer = strLines(I).IndexOf("=")
            If n < 1 Then Continue For
            If Not Integer.TryParse(strLines(I).Substring(0, n), key) Then Continue For
            If TargetTable.ContainsKey(key) Then Continue For
            value = strLines(I).Substring(n + 1)
            'TargetTable(key) = value
            TargetTable.Add(key, value)
        Next I
    End Sub

End Module
