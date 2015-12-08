<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form2
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.lblCredits = New System.Windows.Forms.Label()
        Me.txtCredits = New System.Windows.Forms.TextBox()
        Me.lblGames = New System.Windows.Forms.Label()
        Me.txtGames = New System.Windows.Forms.TextBox()
        Me.gbTimePlayed = New System.Windows.Forms.GroupBox()
        Me.txtSeconds = New System.Windows.Forms.TextBox()
        Me.lblSeconds = New System.Windows.Forms.Label()
        Me.txtMinutes = New System.Windows.Forms.TextBox()
        Me.lblMinutes = New System.Windows.Forms.Label()
        Me.txtHours = New System.Windows.Forms.TextBox()
        Me.lblHours = New System.Windows.Forms.Label()
        Me.lblInventory = New System.Windows.Forms.Label()
        Me.lv = New System.Windows.Forms.ListView()
        Me.btnBack = New System.Windows.Forms.Button()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.lblReward = New System.Windows.Forms.Label()
        Me.txtReward = New System.Windows.Forms.TextBox()
        Me.cmsReward = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.btnFilterRemove = New System.Windows.Forms.Button()
        Me.lblFilter = New System.Windows.Forms.Label()
        Me.gbTimePlayed.SuspendLayout()
        Me.SuspendLayout()
        '
        'lblCredits
        '
        Me.lblCredits.AutoSize = True
        Me.lblCredits.Location = New System.Drawing.Point(17, 19)
        Me.lblCredits.Name = "lblCredits"
        Me.lblCredits.Size = New System.Drawing.Size(42, 13)
        Me.lblCredits.TabIndex = 0
        Me.lblCredits.Text = "Credits:"
        '
        'txtCredits
        '
        Me.txtCredits.Location = New System.Drawing.Point(20, 35)
        Me.txtCredits.Name = "txtCredits"
        Me.txtCredits.Size = New System.Drawing.Size(141, 20)
        Me.txtCredits.TabIndex = 1
        '
        'lblGames
        '
        Me.lblGames.AutoSize = True
        Me.lblGames.Location = New System.Drawing.Point(17, 77)
        Me.lblGames.Name = "lblGames"
        Me.lblGames.Size = New System.Drawing.Size(77, 13)
        Me.lblGames.TabIndex = 2
        Me.lblGames.Text = "Games played:"
        '
        'txtGames
        '
        Me.txtGames.Location = New System.Drawing.Point(20, 93)
        Me.txtGames.Name = "txtGames"
        Me.txtGames.Size = New System.Drawing.Size(141, 20)
        Me.txtGames.TabIndex = 3
        '
        'gbTimePlayed
        '
        Me.gbTimePlayed.Controls.Add(Me.txtSeconds)
        Me.gbTimePlayed.Controls.Add(Me.lblSeconds)
        Me.gbTimePlayed.Controls.Add(Me.txtMinutes)
        Me.gbTimePlayed.Controls.Add(Me.lblMinutes)
        Me.gbTimePlayed.Controls.Add(Me.txtHours)
        Me.gbTimePlayed.Controls.Add(Me.lblHours)
        Me.gbTimePlayed.Location = New System.Drawing.Point(182, 19)
        Me.gbTimePlayed.Name = "gbTimePlayed"
        Me.gbTimePlayed.Size = New System.Drawing.Size(249, 94)
        Me.gbTimePlayed.TabIndex = 4
        Me.gbTimePlayed.TabStop = False
        Me.gbTimePlayed.Text = "Time played"
        '
        'txtSeconds
        '
        Me.txtSeconds.Location = New System.Drawing.Point(171, 44)
        Me.txtSeconds.Name = "txtSeconds"
        Me.txtSeconds.Size = New System.Drawing.Size(46, 20)
        Me.txtSeconds.TabIndex = 9
        '
        'lblSeconds
        '
        Me.lblSeconds.AutoSize = True
        Me.lblSeconds.Location = New System.Drawing.Point(168, 28)
        Me.lblSeconds.Name = "lblSeconds"
        Me.lblSeconds.Size = New System.Drawing.Size(52, 13)
        Me.lblSeconds.TabIndex = 8
        Me.lblSeconds.Text = "Seconds:"
        '
        'txtMinutes
        '
        Me.txtMinutes.Location = New System.Drawing.Point(100, 44)
        Me.txtMinutes.Name = "txtMinutes"
        Me.txtMinutes.Size = New System.Drawing.Size(46, 20)
        Me.txtMinutes.TabIndex = 7
        '
        'lblMinutes
        '
        Me.lblMinutes.AutoSize = True
        Me.lblMinutes.Location = New System.Drawing.Point(97, 28)
        Me.lblMinutes.Name = "lblMinutes"
        Me.lblMinutes.Size = New System.Drawing.Size(47, 13)
        Me.lblMinutes.TabIndex = 6
        Me.lblMinutes.Text = "Minutes:"
        '
        'txtHours
        '
        Me.txtHours.Location = New System.Drawing.Point(31, 44)
        Me.txtHours.Name = "txtHours"
        Me.txtHours.Size = New System.Drawing.Size(46, 20)
        Me.txtHours.TabIndex = 5
        '
        'lblHours
        '
        Me.lblHours.AutoSize = True
        Me.lblHours.Location = New System.Drawing.Point(28, 28)
        Me.lblHours.Name = "lblHours"
        Me.lblHours.Size = New System.Drawing.Size(38, 13)
        Me.lblHours.TabIndex = 0
        Me.lblHours.Text = "Hours:"
        '
        'lblInventory
        '
        Me.lblInventory.AutoSize = True
        Me.lblInventory.Location = New System.Drawing.Point(17, 178)
        Me.lblInventory.Name = "lblInventory"
        Me.lblInventory.Size = New System.Drawing.Size(54, 13)
        Me.lblInventory.TabIndex = 5
        Me.lblInventory.Text = "Inventory:"
        '
        'lv
        '
        Me.lv.FullRowSelect = True
        Me.lv.HideSelection = False
        Me.lv.Location = New System.Drawing.Point(20, 194)
        Me.lv.MultiSelect = False
        Me.lv.Name = "lv"
        Me.lv.Size = New System.Drawing.Size(411, 251)
        Me.lv.TabIndex = 6
        Me.lv.UseCompatibleStateImageBehavior = False
        Me.lv.View = System.Windows.Forms.View.Details
        '
        'btnBack
        '
        Me.btnBack.Location = New System.Drawing.Point(20, 451)
        Me.btnBack.Name = "btnBack"
        Me.btnBack.Size = New System.Drawing.Size(141, 41)
        Me.btnBack.TabIndex = 7
        Me.btnBack.Text = "<< Go back"
        Me.btnBack.UseVisualStyleBackColor = True
        '
        'btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(290, 451)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(141, 41)
        Me.btnSave.TabIndex = 8
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        'lblReward
        '
        Me.lblReward.AutoSize = True
        Me.lblReward.Location = New System.Drawing.Point(249, 134)
        Me.lblReward.Name = "lblReward"
        Me.lblReward.Size = New System.Drawing.Size(130, 13)
        Me.lblReward.TabIndex = 9
        Me.lblReward.Text = "Challenge reward/banner:"
        '
        'txtReward
        '
        Me.txtReward.ContextMenuStrip = Me.cmsReward
        Me.txtReward.Location = New System.Drawing.Point(385, 131)
        Me.txtReward.Name = "txtReward"
        Me.txtReward.Size = New System.Drawing.Size(46, 20)
        Me.txtReward.TabIndex = 10
        '
        'cmsReward
        '
        Me.cmsReward.Name = "cmsReward"
        Me.cmsReward.Size = New System.Drawing.Size(61, 4)
        '
        'btnFilterRemove
        '
        Me.btnFilterRemove.Location = New System.Drawing.Point(136, 166)
        Me.btnFilterRemove.Name = "btnFilterRemove"
        Me.btnFilterRemove.Size = New System.Drawing.Size(85, 22)
        Me.btnFilterRemove.TabIndex = 11
        Me.btnFilterRemove.Text = "Button1"
        Me.btnFilterRemove.UseVisualStyleBackColor = True
        '
        'lblFilter
        '
        Me.lblFilter.AutoSize = True
        Me.lblFilter.Location = New System.Drawing.Point(227, 171)
        Me.lblFilter.Name = "lblFilter"
        Me.lblFilter.Size = New System.Drawing.Size(32, 13)
        Me.lblFilter.TabIndex = 12
        Me.lblFilter.Text = "Filter:"
        '
        'Form2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(452, 504)
        Me.Controls.Add(Me.lblFilter)
        Me.Controls.Add(Me.btnFilterRemove)
        Me.Controls.Add(Me.txtReward)
        Me.Controls.Add(Me.lblReward)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.btnBack)
        Me.Controls.Add(Me.lv)
        Me.Controls.Add(Me.lblInventory)
        Me.Controls.Add(Me.gbTimePlayed)
        Me.Controls.Add(Me.txtGames)
        Me.Controls.Add(Me.lblGames)
        Me.Controls.Add(Me.txtCredits)
        Me.Controls.Add(Me.lblCredits)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "Form2"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Form2"
        Me.gbTimePlayed.ResumeLayout(False)
        Me.gbTimePlayed.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents lblCredits As System.Windows.Forms.Label
    Friend WithEvents txtCredits As System.Windows.Forms.TextBox
    Friend WithEvents lblGames As System.Windows.Forms.Label
    Friend WithEvents txtGames As System.Windows.Forms.TextBox
    Friend WithEvents gbTimePlayed As System.Windows.Forms.GroupBox
    Friend WithEvents txtSeconds As System.Windows.Forms.TextBox
    Friend WithEvents lblSeconds As System.Windows.Forms.Label
    Friend WithEvents txtMinutes As System.Windows.Forms.TextBox
    Friend WithEvents lblMinutes As System.Windows.Forms.Label
    Friend WithEvents txtHours As System.Windows.Forms.TextBox
    Friend WithEvents lblHours As System.Windows.Forms.Label
    Friend WithEvents lblInventory As System.Windows.Forms.Label
    Friend WithEvents lv As System.Windows.Forms.ListView
    Friend WithEvents btnBack As System.Windows.Forms.Button
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents lblReward As System.Windows.Forms.Label
    Friend WithEvents txtReward As System.Windows.Forms.TextBox
    Friend WithEvents cmsReward As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents btnFilterRemove As System.Windows.Forms.Button
    Friend WithEvents lblFilter As System.Windows.Forms.Label
End Class
