namespace CodeImp.DoomBuilder.Windows
{
	partial class TextureSetForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.name = new System.Windows.Forms.TextBox();
			this.filters = new System.Windows.Forms.ListView();
			this.filtercolumn = new System.Windows.Forms.ColumnHeader();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.apply = new System.Windows.Forms.Button();
			this.cancel = new System.Windows.Forms.Button();
			this.addfilter = new System.Windows.Forms.Button();
			this.removefilter = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(30, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(37, 14);
			this.label1.TabIndex = 0;
			this.label1.Text = "Name:";
			// 
			// name
			// 
			this.name.Location = new System.Drawing.Point(73, 21);
			this.name.Name = "name";
			this.name.Size = new System.Drawing.Size(179, 20);
			this.name.TabIndex = 1;
			// 
			// filters
			// 
			this.filters.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.filtercolumn});
			this.filters.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.filters.HideSelection = false;
			this.filters.LabelEdit = true;
			this.filters.Location = new System.Drawing.Point(21, 110);
			this.filters.Name = "filters";
			this.filters.ShowGroups = false;
			this.filters.Size = new System.Drawing.Size(219, 173);
			this.filters.TabIndex = 2;
			this.filters.UseCompatibleStateImageBehavior = false;
			this.filters.View = System.Windows.Forms.View.Details;
			// 
			// filtercolumn
			// 
			this.filtercolumn.Text = "Filter";
			this.filtercolumn.Width = 192;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(18, 28);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(248, 42);
			this.label2.TabIndex = 3;
			this.label2.Text = "Add the names of the textures in this set below. You can use the following wildca" +
				"rds:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(28, 65);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(175, 14);
			this.label3.TabIndex = 4;
			this.label3.Text = "? = matches exactly one character";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(28, 83);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(181, 14);
			this.label4.TabIndex = 5;
			this.label4.Text = "* = matches one or more characters";
			// 
			// apply
			// 
			this.apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.apply.Location = new System.Drawing.Point(58, 417);
			this.apply.Name = "apply";
			this.apply.Size = new System.Drawing.Size(105, 25);
			this.apply.TabIndex = 6;
			this.apply.Text = "OK";
			this.apply.UseVisualStyleBackColor = true;
			this.apply.Click += new System.EventHandler(this.apply_Click);
			// 
			// cancel
			// 
			this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancel.Location = new System.Drawing.Point(177, 417);
			this.cancel.Name = "cancel";
			this.cancel.Size = new System.Drawing.Size(105, 25);
			this.cancel.TabIndex = 7;
			this.cancel.Text = "Cancel";
			this.cancel.UseVisualStyleBackColor = true;
			this.cancel.Click += new System.EventHandler(this.cancel_Click);
			// 
			// addfilter
			// 
			this.addfilter.Location = new System.Drawing.Point(21, 289);
			this.addfilter.Name = "addfilter";
			this.addfilter.Size = new System.Drawing.Size(97, 24);
			this.addfilter.TabIndex = 8;
			this.addfilter.Text = "Add Texture";
			this.addfilter.UseVisualStyleBackColor = true;
			// 
			// removefilter
			// 
			this.removefilter.Location = new System.Drawing.Point(135, 289);
			this.removefilter.Name = "removefilter";
			this.removefilter.Size = new System.Drawing.Size(105, 24);
			this.removefilter.TabIndex = 9;
			this.removefilter.Text = "Remove Selection";
			this.removefilter.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.removefilter);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.addfilter);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.filters);
			this.groupBox1.Location = new System.Drawing.Point(12, 60);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(269, 333);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = " Filters ";
			// 
			// TextureSetForm
			// 
			this.AcceptButton = this.apply;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.CancelButton = this.cancel;
			this.ClientSize = new System.Drawing.Size(294, 455);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.apply);
			this.Controls.Add(this.name);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TextureSetForm";
			this.Opacity = 0;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit Texture Set";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox name;
		private System.Windows.Forms.ListView filters;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button apply;
		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.ColumnHeader filtercolumn;
		private System.Windows.Forms.Button addfilter;
		private System.Windows.Forms.Button removefilter;
		private System.Windows.Forms.GroupBox groupBox1;
	}
}