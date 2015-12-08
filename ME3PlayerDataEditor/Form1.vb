Option Strict On

Public Class Form1

    Dim strRecent() As String

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text &= " - build date: " & RetrieveAppLinkerTimestampString()
        Me.ContextMenuStrip = CMS1
        LoadRecentItems()
        ' initialize description table here, and only once during current app instance
        If DescriptionTable Is Nothing Then InitializeTable(DescriptionTable, DESC_INV)
        If RewardsTable Is Nothing Then InitializeTable(RewardsTable, DESC_REWARDS)
        Dim initdir As String = Application.StartupPath + "\player"
        If IO.Directory.Exists(initdir) Then Open1.InitialDirectory = initdir
    End Sub

    Private Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click
        If Open1.ShowDialog = Windows.Forms.DialogResult.Cancel Then Exit Sub
        OpenThisFile(Open1.FileName)
    End Sub

    Private Sub OpenThisFile(ByVal strFile As String)
        PlayerTextFile = strFile
        InsertAndSaveRecentItems()
        Form2.Show()
        Me.Close()
    End Sub

    Private Sub LoadRecentItems()
        Try
            strRecent = System.IO.File.ReadAllLines(PLAYER_FILES)
        Catch ex As Exception
            Exit Sub
        End Try
        If strRecent Is Nothing Then Exit Sub
        For I As Integer = 0 To strRecent.Count - 1
            If System.IO.File.Exists(strRecent(I)) Then CMS1.Items.Add(strRecent(I)) Else strRecent(I) = vbNullString
        Next I
    End Sub

    Private Sub InsertAndSaveRecentItems()
        Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder
        sb.AppendLine(PlayerTextFile)
        If strRecent IsNot Nothing Then
            For I As Integer = 0 To strRecent.Count - 1
                If strRecent(I) Is Nothing OrElse strRecent(I) = PlayerTextFile Then Continue For
                sb.AppendLine(strRecent(I))
            Next I
        End If
        System.IO.File.WriteAllText(PLAYER_FILES, sb.ToString)
    End Sub

    Private Sub CMS1_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles CMS1.ItemClicked
        CMS1.Close()
        OpenThisFile(e.ClickedItem.Text)
    End Sub
End Class
