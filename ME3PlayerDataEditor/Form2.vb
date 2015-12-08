Option Strict On

Imports ME3PlayerDataEditor.PlayerProfile

Public Class Form2

    Private IsFilterActive As Boolean

    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim tmpProfile As ME3MP_Profile
        tmpProfile = ME3MP_Profile.InitializeFromFile(PlayerTextFile)
        If tmpProfile Is Nothing Then
            MsgBox("Profile error. Cannot open this file.", MsgBoxStyle.Critical)
            Me.Close()
            Exit Sub
        End If
        CurrentProfile = tmpProfile
        Me.Text = CurrentProfile.GetPlayerName() & " - ME3 Player Data Editor"
        txtCredits.Text = CurrentProfile.Base.GetCredits().ToString()
        txtGames.Text = CurrentProfile.Base.GetGamesPlayed().ToString()
        Dim Seconds As Integer = CurrentProfile.Base.GetTimePlayedSeconds()
        txtHours.Text = CStr(Seconds \ 3600)
        Seconds -= (Seconds \ 3600) * 3600
        txtMinutes.Text = CStr(Seconds \ 60)
        txtSeconds.Text = CStr(Seconds Mod 60)
        'InitializeDescriptionTable()
        lv.Columns.Clear()
        lv.Columns.Add("Index", 60)
        lv.Columns.Add("Value", 60)
        lv.Columns.Add("Description", 240)
        ListAllInventoryItems()
        txtReward.Text = CurrentProfile.Banner.GetBannerID().ToString()
        LoadRewardsMenu()
        btnFilterRemove.Text = "Filter"
        lblFilter.Visible = False
        IsFilterActive = False
    End Sub

    Private Sub ListAllInventoryItems()
        lv.Items.Clear()
        Dim tmpItem As ListViewItem
        For I = 0 To ME3PlayerBase.INVENTORYITEMLASTINDEX
            Dim StringArray(2) As String
            StringArray(0) = I.ToString
            StringArray(1) = CurrentProfile.Base.GetItem(I).ToString
            If DescriptionTable.ContainsKey(I) Then StringArray(2) = DescriptionTable(I).ToString
            tmpItem = New ListViewItem(StringArray)
            lv.Items.Add(tmpItem)
        Next I
    End Sub

    Private Sub LoadRewardsMenu()
        If RewardsTable.Count = 0 Then Exit Sub
        Dim tmpItem As ToolStripItem
        'For Each key As Integer In RewardsTable.Keys
        '  tmpItem = cmsReward.Items.Add(CStr(RewardsTable(key)))
        '   tmpItem.Tag = key
        'Next
        For I = 0 To RewardsTable.KeysList.Count - 1
            If Integer.Parse(CStr(RewardsTable.KeysList(I))) < 0 Then
                cmsReward.Items.Add("-")
            Else
                tmpItem = cmsReward.Items.Add(CStr(RewardsTable(RewardsTable.KeysList(I))))
                tmpItem.Tag = RewardsTable.KeysList(I)
                tmpItem.ToolTipText = CStr(tmpItem.Tag)
            End If
        Next
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Form1.Show()
        Me.Close()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If Not CheckTextBoxes() Then Exit Sub
        CurrentProfile.Base.SetCredits(Integer.Parse(txtCredits.Text))
        CurrentProfile.Base.SetGamesPlayed(Integer.Parse(txtGames.Text))
        CurrentProfile.Base.SetTimePlayed(Integer.Parse(txtHours.Text), Integer.Parse(txtMinutes.Text), Integer.Parse(txtSeconds.Text))
        CurrentProfile.Banner.SetBannerID(Integer.Parse(txtReward.Text))

        If CurrentProfile.SaveToFile(PlayerTextFile) Then
            MsgBox("Save OK.", MsgBoxStyle.Information)
        Else
            MsgBox("Save Error.", MsgBoxStyle.Critical)
        End If
    End Sub

    Private Function CheckTextBoxes() As Boolean
        Dim vTest As Integer
        If Not Integer.TryParse(txtCredits.Text, vTest) Then
            txtCredits.Focus()
            Return False
        End If
        If Not Integer.TryParse(txtGames.Text, vTest) Then
            txtGames.Focus()
            Return False
        End If
        If Not Integer.TryParse(txtHours.Text, vTest) Then
            txtHours.Focus()
            Return False
        End If
        If Not Integer.TryParse(txtMinutes.Text, vTest) Then
            txtMinutes.Focus()
            Return False
        End If
        If Not Integer.TryParse(txtSeconds.Text, vTest) Then
            txtSeconds.Focus()
            Return False
        End If
        If Not Integer.TryParse(txtReward.Text, vTest) Then
            txtReward.Focus()
            Return False
        End If
        Return True
    End Function

    Private Sub lv_DoubleClick(sender As Object, e As EventArgs) Handles lv.DoubleClick
        If lv.SelectedItems.Count = 0 Then Exit Sub
        Dim ind As Integer = Integer.Parse(lv.SelectedItems(0).SubItems(0).Text)
        Dim val As Byte
        Dim msg As String = "Change value for item #" & ind & vbCrLf & CStr(DescriptionTable(ind))
        Dim resp As String = InputBox(msg, "#" & ind, CurrentProfile.Base.GetItem(ind).ToString)
        If Not Byte.TryParse(resp, val) Then Exit Sub
        CurrentProfile.Base.SetItem(ind, val)
        lv.SelectedItems(0).SubItems(1).Text = val.ToString
    End Sub

    Private Sub lv_KeyDown(sender As Object, e As KeyEventArgs) Handles lv.KeyDown
        If e.KeyCode = Keys.Return Then lv_DoubleClick(sender, e)
    End Sub

    Private Sub cmsReward_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsReward.ItemClicked
        txtReward.Text = e.ClickedItem.Tag.ToString
    End Sub

    Private Sub btnFilterRemove_Click(sender As Object, e As EventArgs) Handles btnFilterRemove.Click
        If IsFilterActive Then
            IsFilterActive = False
            btnFilterRemove.Text = "Filter"
            lblFilter.Visible = False
            ListAllInventoryItems()
        Else
            ApplyFilterForInventoryItemm()
        End If
    End Sub

    Private Sub ApplyFilterForInventoryItemm()
        Dim strFilter As String = InputBox("Please enter expression:", "Filter for inventory items list")
        strFilter = Trim(strFilter)
        If strFilter = "" Then Exit Sub
        For Each lvi As ListViewItem In lv.Items
            Dim strDesc As String = lvi.SubItems(2).Text
            If strDesc.IndexOf(strFilter, StringComparison.CurrentCultureIgnoreCase) < 0 Then
                lv.Items.Remove(lvi)
            End If
        Next
        btnFilterRemove.Text = "Remove"
        lblFilter.Text = "Filter: " & strFilter
        lblFilter.Visible = True
        IsFilterActive = True
    End Sub

End Class