<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.btnLoad = New System.Windows.Forms.Button()
        Me.Open1 = New System.Windows.Forms.OpenFileDialog()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.CMS1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.Label4 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(45, 18)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(468, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "For use with player text files generated from ME3 Private Server by WarrantyVoide" & _
    "r. ('player' folder)"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(45, 44)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(535, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "DESC_INV.TXT in this app's path  is used to give descriptions to inventory items." & _
    " Special contributor: JunkoXan."
        '
        'btnLoad
        '
        Me.btnLoad.Location = New System.Drawing.Point(48, 138)
        Me.btnLoad.Name = "btnLoad"
        Me.btnLoad.Size = New System.Drawing.Size(514, 46)
        Me.btnLoad.TabIndex = 2
        Me.btnLoad.Text = "Open player file"
        Me.btnLoad.UseVisualStyleBackColor = True
        '
        'Open1
        '
        Me.Open1.Filter = "*.txt|*.txt"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(45, 106)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(377, 13)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "PLAYER_FILES.TXT is a list of files opened before. (right click on this window)"
        '
        'CMS1
        '
        Me.CMS1.Name = "CMS1"
        Me.CMS1.Size = New System.Drawing.Size(61, 4)
        '
        'Label4
        '
        Me.Label4.Location = New System.Drawing.Point(45, 70)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(535, 36)
        Me.Label4.TabIndex = 4
        Me.Label4.Text = "DESC_REWARDS.TXT is a list of challenge rewards/banners, available through a popu" & _
    "p menu. (right click on its text box)"
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(610, 208)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.btnLoad)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "Form1"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "ME3 Player Data Editor"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents btnLoad As System.Windows.Forms.Button
    Friend WithEvents Open1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents CMS1 As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents Label4 As System.Windows.Forms.Label

End Class
