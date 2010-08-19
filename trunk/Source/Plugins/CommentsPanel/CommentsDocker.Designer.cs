namespace CodeImp.DoomBuilder.CommentsPanel
{
	partial class CommentsDocker
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
			if(disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.optionsgroup = new System.Windows.Forms.GroupBox();
			this.filtermode = new System.Windows.Forms.CheckBox();
			this.grid = new System.Windows.Forms.DataGridView();
			this.iconcolumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.textcolumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.groupcomments = new System.Windows.Forms.CheckBox();
			this.optionsgroup.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
			this.SuspendLayout();
			// 
			// optionsgroup
			// 
			this.optionsgroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.optionsgroup.Controls.Add(this.groupcomments);
			this.optionsgroup.Controls.Add(this.filtermode);
			this.optionsgroup.Location = new System.Drawing.Point(3, 551);
			this.optionsgroup.Name = "optionsgroup";
			this.optionsgroup.Size = new System.Drawing.Size(244, 103);
			this.optionsgroup.TabIndex = 1;
			this.optionsgroup.TabStop = false;
			this.optionsgroup.Text = " Options ";
			// 
			// filtermode
			// 
			this.filtermode.AutoSize = true;
			this.filtermode.Location = new System.Drawing.Point(15, 29);
			this.filtermode.Name = "filtermode";
			this.filtermode.Size = new System.Drawing.Size(169, 17);
			this.filtermode.TabIndex = 0;
			this.filtermode.Text = "Only comments from this mode";
			this.filtermode.UseVisualStyleBackColor = true;
			// 
			// grid
			// 
			this.grid.AllowUserToAddRows = false;
			this.grid.AllowUserToDeleteRows = false;
			this.grid.AllowUserToResizeColumns = false;
			this.grid.AllowUserToResizeRows = false;
			this.grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.grid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.grid.BackgroundColor = System.Drawing.SystemColors.Window;
			this.grid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.grid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.grid.ColumnHeadersVisible = false;
			this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.iconcolumn,
            this.textcolumn});
			this.grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.grid.Location = new System.Drawing.Point(0, 0);
			this.grid.Name = "grid";
			this.grid.ReadOnly = true;
			this.grid.RowHeadersVisible = false;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(2, 4, 2, 5);
			this.grid.RowsDefaultCellStyle = dataGridViewCellStyle1;
			this.grid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.grid.ShowCellErrors = false;
			this.grid.ShowCellToolTips = false;
			this.grid.ShowEditingIcon = false;
			this.grid.ShowRowErrors = false;
			this.grid.Size = new System.Drawing.Size(250, 545);
			this.grid.StandardTab = true;
			this.grid.TabIndex = 6;
			// 
			// iconcolumn
			// 
			this.iconcolumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.iconcolumn.HeaderText = "Icon";
			this.iconcolumn.MinimumWidth = 20;
			this.iconcolumn.Name = "iconcolumn";
			this.iconcolumn.ReadOnly = true;
			this.iconcolumn.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.iconcolumn.Width = 24;
			// 
			// textcolumn
			// 
			this.textcolumn.HeaderText = "Text";
			this.textcolumn.Name = "textcolumn";
			this.textcolumn.ReadOnly = true;
			// 
			// groupcomments
			// 
			this.groupcomments.AutoSize = true;
			this.groupcomments.Location = new System.Drawing.Point(15, 61);
			this.groupcomments.Name = "groupcomments";
			this.groupcomments.Size = new System.Drawing.Size(176, 17);
			this.groupcomments.TabIndex = 1;
			this.groupcomments.Text = "Group same comments together";
			this.groupcomments.UseVisualStyleBackColor = true;
			// 
			// CommentsDocker
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.grid);
			this.Controls.Add(this.optionsgroup);
			this.Name = "CommentsDocker";
			this.Size = new System.Drawing.Size(250, 657);
			this.optionsgroup.ResumeLayout(false);
			this.optionsgroup.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox optionsgroup;
		private System.Windows.Forms.CheckBox filtermode;
		private System.Windows.Forms.DataGridView grid;
		private System.Windows.Forms.DataGridViewImageColumn iconcolumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn textcolumn;
		private System.Windows.Forms.CheckBox groupcomments;
	}
}
