namespace ME3Server_WV
{
    partial class Frontend
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frontend));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.serverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showPlayerListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showGameListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mITMModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recordPlayerSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importPlayerSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patchGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hostsFileToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.aktivateRedirectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deactivateRedirectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showContentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packetEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.localProfileCreatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteLogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playerDataEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logLevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level0MostCriticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.level5EverythingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serverToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.logLevelToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(781, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "MenuStrip";
            // 
            // serverToolStripMenuItem
            // 
            this.serverToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showLogToolStripMenuItem,
            this.showPlayerListToolStripMenuItem,
            this.showGameListToolStripMenuItem,
            this.mITMModeToolStripMenuItem,
            this.recordPlayerSettingsToolStripMenuItem,
            this.importPlayerSettingsToolStripMenuItem});
            this.serverToolStripMenuItem.Name = "serverToolStripMenuItem";
            this.serverToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
            this.serverToolStripMenuItem.Text = "Server";
            // 
            // showLogToolStripMenuItem
            // 
            this.showLogToolStripMenuItem.Name = "showLogToolStripMenuItem";
            this.showLogToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.showLogToolStripMenuItem.Text = "Show Log";
            this.showLogToolStripMenuItem.Click += new System.EventHandler(this.showLogToolStripMenuItem_Click);
            // 
            // showPlayerListToolStripMenuItem
            // 
            this.showPlayerListToolStripMenuItem.Name = "showPlayerListToolStripMenuItem";
            this.showPlayerListToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.showPlayerListToolStripMenuItem.Text = "Show Player List";
            this.showPlayerListToolStripMenuItem.Click += new System.EventHandler(this.showPlayerListToolStripMenuItem_Click);
            // 
            // showGameListToolStripMenuItem
            // 
            this.showGameListToolStripMenuItem.Name = "showGameListToolStripMenuItem";
            this.showGameListToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.showGameListToolStripMenuItem.Text = "Show Game List";
            this.showGameListToolStripMenuItem.Click += new System.EventHandler(this.showGameListToolStripMenuItem_Click);
            // 
            // mITMModeToolStripMenuItem
            // 
            this.mITMModeToolStripMenuItem.CheckOnClick = true;
            this.mITMModeToolStripMenuItem.Name = "mITMModeToolStripMenuItem";
            this.mITMModeToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.mITMModeToolStripMenuItem.Text = "MITM mode";
            this.mITMModeToolStripMenuItem.Click += new System.EventHandler(this.mITMModeToolStripMenuItem_Click);
            // 
            // recordPlayerSettingsToolStripMenuItem
            // 
            this.recordPlayerSettingsToolStripMenuItem.CheckOnClick = true;
            this.recordPlayerSettingsToolStripMenuItem.Name = "recordPlayerSettingsToolStripMenuItem";
            this.recordPlayerSettingsToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.recordPlayerSettingsToolStripMenuItem.Text = "Record player settings";
            this.recordPlayerSettingsToolStripMenuItem.Visible = false;
            this.recordPlayerSettingsToolStripMenuItem.Click += new System.EventHandler(this.recordPlayerSettingsToolStripMenuItem_Click);
            // 
            // importPlayerSettingsToolStripMenuItem
            // 
            this.importPlayerSettingsToolStripMenuItem.Name = "importPlayerSettingsToolStripMenuItem";
            this.importPlayerSettingsToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.importPlayerSettingsToolStripMenuItem.Text = "Import player settings";
            this.importPlayerSettingsToolStripMenuItem.Visible = false;
            this.importPlayerSettingsToolStripMenuItem.Click += new System.EventHandler(this.importPlayerSettingsToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.patchGameToolStripMenuItem,
            this.hostsFileToolStripMenuItem1,
            this.packetEditorToolStripMenuItem,
            this.localProfileCreatorToolStripMenuItem,
            this.deleteLogsToolStripMenuItem,
            this.playerDataEditorToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // patchGameToolStripMenuItem
            // 
            this.patchGameToolStripMenuItem.Name = "patchGameToolStripMenuItem";
            this.patchGameToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.patchGameToolStripMenuItem.Text = "Patch Game";
            this.patchGameToolStripMenuItem.Click += new System.EventHandler(this.patchGameToolStripMenuItem_Click);
            // 
            // hostsFileToolStripMenuItem1
            // 
            this.hostsFileToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aktivateRedirectionToolStripMenuItem,
            this.deactivateRedirectionToolStripMenuItem,
            this.showContentToolStripMenuItem});
            this.hostsFileToolStripMenuItem1.Name = "hostsFileToolStripMenuItem1";
            this.hostsFileToolStripMenuItem1.Size = new System.Drawing.Size(188, 22);
            this.hostsFileToolStripMenuItem1.Text = "Hosts File";
            // 
            // aktivateRedirectionToolStripMenuItem
            // 
            this.aktivateRedirectionToolStripMenuItem.Name = "aktivateRedirectionToolStripMenuItem";
            this.aktivateRedirectionToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.aktivateRedirectionToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.aktivateRedirectionToolStripMenuItem.Text = "Activate Redirection";
            this.aktivateRedirectionToolStripMenuItem.Click += new System.EventHandler(this.aktivateRedirectionToolStripMenuItem_Click);
            // 
            // deactivateRedirectionToolStripMenuItem
            // 
            this.deactivateRedirectionToolStripMenuItem.Name = "deactivateRedirectionToolStripMenuItem";
            this.deactivateRedirectionToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F6;
            this.deactivateRedirectionToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.deactivateRedirectionToolStripMenuItem.Text = "Deactivate Redirection";
            this.deactivateRedirectionToolStripMenuItem.Click += new System.EventHandler(this.deactivateRedirectionToolStripMenuItem_Click);
            // 
            // showContentToolStripMenuItem
            // 
            this.showContentToolStripMenuItem.Name = "showContentToolStripMenuItem";
            this.showContentToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F7;
            this.showContentToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.showContentToolStripMenuItem.Text = "Show Content";
            this.showContentToolStripMenuItem.Click += new System.EventHandler(this.showContentToolStripMenuItem_Click);
            // 
            // packetEditorToolStripMenuItem
            // 
            this.packetEditorToolStripMenuItem.Name = "packetEditorToolStripMenuItem";
            this.packetEditorToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
            this.packetEditorToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.packetEditorToolStripMenuItem.Text = "Packet Viewer";
            this.packetEditorToolStripMenuItem.Click += new System.EventHandler(this.packetEditorToolStripMenuItem_Click);
            // 
            // localProfileCreatorToolStripMenuItem
            // 
            this.localProfileCreatorToolStripMenuItem.Name = "localProfileCreatorToolStripMenuItem";
            this.localProfileCreatorToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.localProfileCreatorToolStripMenuItem.Text = "Create Player Profile";
            this.localProfileCreatorToolStripMenuItem.Click += new System.EventHandler(this.localProfileCreatorToolStripMenuItem_Click);
            // 
            // deleteLogsToolStripMenuItem
            // 
            this.deleteLogsToolStripMenuItem.Name = "deleteLogsToolStripMenuItem";
            this.deleteLogsToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.deleteLogsToolStripMenuItem.Text = "Delete Logs";
            this.deleteLogsToolStripMenuItem.Click += new System.EventHandler(this.deleteLogsToolStripMenuItem_Click);
            // 
            // playerDataEditorToolStripMenuItem
            // 
            this.playerDataEditorToolStripMenuItem.Name = "playerDataEditorToolStripMenuItem";
            this.playerDataEditorToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.playerDataEditorToolStripMenuItem.Text = "Player Data Editor";
            this.playerDataEditorToolStripMenuItem.Click += new System.EventHandler(this.playerDataEditorToolStripMenuItem_Click);
            // 
            // logLevelToolStripMenuItem
            // 
            this.logLevelToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.level0MostCriticalToolStripMenuItem,
            this.level3ToolStripMenuItem,
            this.level5EverythingToolStripMenuItem});
            this.logLevelToolStripMenuItem.Name = "logLevelToolStripMenuItem";
            this.logLevelToolStripMenuItem.Size = new System.Drawing.Size(69, 20);
            this.logLevelToolStripMenuItem.Text = "Log Level";
            // 
            // level0MostCriticalToolStripMenuItem
            // 
            this.level0MostCriticalToolStripMenuItem.Name = "level0MostCriticalToolStripMenuItem";
            this.level0MostCriticalToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.level0MostCriticalToolStripMenuItem.Text = "Level 0 Most Critical";
            this.level0MostCriticalToolStripMenuItem.Click += new System.EventHandler(this.level0MostCriticalToolStripMenuItem_Click);
            // 
            // level3ToolStripMenuItem
            // 
            this.level3ToolStripMenuItem.Name = "level3ToolStripMenuItem";
            this.level3ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.level3ToolStripMenuItem.Text = "Level 3 Moderate";
            this.level3ToolStripMenuItem.Click += new System.EventHandler(this.level3ToolStripMenuItem_Click);
            // 
            // level5EverythingToolStripMenuItem
            // 
            this.level5EverythingToolStripMenuItem.Name = "level5EverythingToolStripMenuItem";
            this.level5EverythingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.level5EverythingToolStripMenuItem.Text = "Level 5 Everything";
            this.level5EverythingToolStripMenuItem.Click += new System.EventHandler(this.level5EverythingToolStripMenuItem_Click);
            // 
            // Frontend
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(781, 463);
            this.Controls.Add(this.menuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip;
            this.Name = "Frontend";
            this.Text = "Frontend";
            this.Load += new System.EventHandler(this.Frontend_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion


        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem serverToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem patchGameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hostsFileToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem aktivateRedirectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deactivateRedirectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showContentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem packetEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showPlayerListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mITMModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showGameListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem localProfileCreatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteLogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logLevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level0MostCriticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem level5EverythingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recordPlayerSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importPlayerSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem playerDataEditorToolStripMenuItem;
    }
}



