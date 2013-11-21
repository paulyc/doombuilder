namespace CodeImp.DoomBuilder.Windows
{
	partial class TextureBrowserForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextureBrowserForm));
			this.cancel = new System.Windows.Forms.Button();
			this.apply = new System.Windows.Forms.Button();
			this.smallimages = new System.Windows.Forms.ImageList(this.components);
			this.tvTextureSets = new System.Windows.Forms.TreeView();
			this.browser = new CodeImp.DoomBuilder.Controls.ImageBrowserControl();
			this.SuspendLayout();
			// 
			// cancel
			// 
			this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancel.Location = new System.Drawing.Point(783, 597);
			this.cancel.Name = "cancel";
			this.cancel.Size = new System.Drawing.Size(98, 25);
			this.cancel.TabIndex = 3;
			this.cancel.TabStop = false;
			this.cancel.Text = "Cancel";
			this.cancel.UseVisualStyleBackColor = true;
			this.cancel.Click += new System.EventHandler(this.cancel_Click);
			// 
			// apply
			// 
			this.apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.apply.Location = new System.Drawing.Point(681, 597);
			this.apply.Name = "apply";
			this.apply.Size = new System.Drawing.Size(98, 25);
			this.apply.TabIndex = 2;
			this.apply.TabStop = false;
			this.apply.Text = "OK";
			this.apply.UseVisualStyleBackColor = true;
			this.apply.Click += new System.EventHandler(this.apply_Click);
			// 
			// smallimages
			// 
			this.smallimages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("smallimages.ImageStream")));
			this.smallimages.TransparentColor = System.Drawing.Color.Transparent;
			this.smallimages.Images.SetKeyName(0, "KnownTextureSet2.ico");
			this.smallimages.Images.SetKeyName(1, "AllTextureSet2.ico");
			this.smallimages.Images.SetKeyName(2, "FileTextureSet.ico");
			this.smallimages.Images.SetKeyName(3, "FolderTextureSet.ico");
			this.smallimages.Images.SetKeyName(4, "PK3TextureSet.ico");
			this.smallimages.Images.SetKeyName(5, "FolderImage.png");
			this.smallimages.Images.SetKeyName(6, "ArchiveImage.png");
			// 
			// tvTextureSets
			// 
			this.tvTextureSets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tvTextureSets.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tvTextureSets.ImageIndex = 0;
			this.tvTextureSets.ImageList = this.smallimages;
			this.tvTextureSets.Location = new System.Drawing.Point(681, 12);
			this.tvTextureSets.Name = "tvTextureSets";
			this.tvTextureSets.SelectedImageIndex = 0;
			this.tvTextureSets.Size = new System.Drawing.Size(200, 576);
			this.tvTextureSets.TabIndex = 4;
			this.tvTextureSets.TabStop = false;
			this.tvTextureSets.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tvTextureSets_KeyUp);
			this.tvTextureSets.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvTextureSets_NodeMouseClick);
			// 
			// browser
			// 
			this.browser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.browser.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.browser.HideInputBox = false;
			this.browser.Location = new System.Drawing.Point(12, 12);
			this.browser.Name = "browser";
			this.browser.PreventSelection = false;
			this.browser.Size = new System.Drawing.Size(663, 610);
			this.browser.TabIndex = 1;
			this.browser.TabStop = false;
			this.browser.SelectedItemDoubleClicked += new CodeImp.DoomBuilder.Controls.ImageBrowserControl.SelectedItemDoubleClickDelegate(this.browser_SelectedItemDoubleClicked);
			this.browser.SelectedItemChanged += new CodeImp.DoomBuilder.Controls.ImageBrowserControl.SelectedItemChangedDelegate(this.browser_SelectedItemChanged);
			// 
			// TextureBrowserForm
			// 
			this.AcceptButton = this.apply;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancel;
			this.ClientSize = new System.Drawing.Size(893, 628);
			this.Controls.Add(this.tvTextureSets);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.apply);
			this.Controls.Add(this.browser);
			this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KeyPreview = true;
			this.MinimizeBox = false;
			this.Name = "TextureBrowserForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Browse Textures";
			this.Load += new System.EventHandler(this.TextureBrowserForm_Load);
			this.Shown += new System.EventHandler(this.TextureBrowserForm_Shown);
			this.Activated += new System.EventHandler(this.TextureBrowserForm_Activated);
			this.Move += new System.EventHandler(this.TextureBrowserForm_Move);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TextureBrowserForm_FormClosing);
			this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.TextureBrowserForm_HelpRequested);
			this.ResizeEnd += new System.EventHandler(this.TextureBrowserForm_ResizeEnd);
			this.ResumeLayout(false);

		}

		#endregion

		private CodeImp.DoomBuilder.Controls.ImageBrowserControl browser;
		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.Button apply;
		private System.Windows.Forms.ImageList smallimages;
		private System.Windows.Forms.TreeView tvTextureSets;
	}
}