namespace CodeImp.DoomBuilder.BuilderModes
{
	partial class EditSelectionPanel
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.orgposy = new System.Windows.Forms.Button();
			this.orgposx = new System.Windows.Forms.Button();
			this.label16 = new System.Windows.Forms.Label();
			this.label19 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.relposy = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.relposx = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.absposy = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.absposx = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.orgsizey = new System.Windows.Forms.Button();
			this.orgsizex = new System.Windows.Forms.Button();
			this.label12 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.relsizey = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.relsizex = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.label4 = new System.Windows.Forms.Label();
			this.abssizey = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.abssizex = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label14 = new System.Windows.Forms.Label();
			this.flipv = new System.Windows.Forms.Button();
			this.fliph = new System.Windows.Forms.Button();
			this.label13 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.absrot = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.orgCenterY = new System.Windows.Forms.Button();
			this.orgCenterX = new System.Windows.Forms.Button();
			this.label10 = new System.Windows.Forms.Label();
			this.label18 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.label21 = new System.Windows.Forms.Label();
			this.relCenterY = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.relCenterX = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.absCenterY = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.absCenterX = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
			this.label22 = new System.Windows.Forms.Label();
			this.label23 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.orgposy);
			this.groupBox1.Controls.Add(this.orgposx);
			this.groupBox1.Controls.Add(this.label16);
			this.groupBox1.Controls.Add(this.label19);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.label5);
			this.groupBox1.Controls.Add(this.relposy);
			this.groupBox1.Controls.Add(this.relposx);
			this.groupBox1.Controls.Add(this.absposy);
			this.groupBox1.Controls.Add(this.absposx);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(3, 130);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(243, 117);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Position:";
			// 
			// orgposy
			// 
			this.orgposy.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.orgposy.Location = new System.Drawing.Point(136, 23);
			this.orgposy.Name = "orgposy";
			this.orgposy.Size = new System.Drawing.Size(72, 24);
			this.orgposy.TabIndex = 29;
			this.orgposy.Text = "-2000";
			this.orgposy.UseVisualStyleBackColor = true;
			this.orgposy.Click += new System.EventHandler(this.orgposy_Click);
			// 
			// orgposx
			// 
			this.orgposx.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.orgposx.Location = new System.Drawing.Point(58, 23);
			this.orgposx.Name = "orgposx";
			this.orgposx.Size = new System.Drawing.Size(72, 24);
			this.orgposx.TabIndex = 28;
			this.orgposx.Text = "-2000";
			this.orgposx.UseVisualStyleBackColor = true;
			this.orgposx.Click += new System.EventHandler(this.orgposx_Click);
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(214, 28);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(21, 14);
			this.label16.TabIndex = 26;
			this.label16.Text = "mp";
			// 
			// label19
			// 
			this.label19.AutoSize = true;
			this.label19.Location = new System.Drawing.Point(9, 28);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(46, 14);
			this.label19.TabIndex = 23;
			this.label19.Text = "Original:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(214, 88);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(21, 14);
			this.label6.TabIndex = 13;
			this.label6.Text = "mp";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(214, 58);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(21, 14);
			this.label5.TabIndex = 12;
			this.label5.Text = "mp";
			// 
			// relposy
			// 
			this.relposy.AllowDecimal = true;
			this.relposy.AllowNegative = true;
			this.relposy.AllowRelative = true;
			this.relposy.ButtonStep = 1;
			this.relposy.Location = new System.Drawing.Point(136, 83);
			this.relposy.Name = "relposy";
			this.relposy.Size = new System.Drawing.Size(72, 24);
			this.relposy.StepValues = null;
			this.relposy.TabIndex = 11;
			this.relposy.WhenEnterPressed += new System.EventHandler(this.relposy_Validated);
			this.relposy.Validated += new System.EventHandler(this.relposy_Validated);
			this.relposy.WhenButtonsClicked += new System.EventHandler(this.relposy_Validated);
			this.relposy.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// relposx
			// 
			this.relposx.AllowDecimal = true;
			this.relposx.AllowNegative = true;
			this.relposx.AllowRelative = true;
			this.relposx.ButtonStep = 1;
			this.relposx.Location = new System.Drawing.Point(58, 83);
			this.relposx.Name = "relposx";
			this.relposx.Size = new System.Drawing.Size(72, 24);
			this.relposx.StepValues = null;
			this.relposx.TabIndex = 10;
			this.relposx.WhenEnterPressed += new System.EventHandler(this.relposx_Validated);
			this.relposx.Validated += new System.EventHandler(this.relposx_Validated);
			this.relposx.WhenButtonsClicked += new System.EventHandler(this.relposx_Validated);
			this.relposx.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// absposy
			// 
			this.absposy.AllowDecimal = true;
			this.absposy.AllowNegative = true;
			this.absposy.AllowRelative = true;
			this.absposy.ButtonStep = 1;
			this.absposy.Location = new System.Drawing.Point(136, 53);
			this.absposy.Name = "absposy";
			this.absposy.Size = new System.Drawing.Size(72, 24);
			this.absposy.StepValues = null;
			this.absposy.TabIndex = 9;
			this.absposy.WhenEnterPressed += new System.EventHandler(this.absposy_Validated);
			this.absposy.Validated += new System.EventHandler(this.absposy_Validated);
			this.absposy.WhenButtonsClicked += new System.EventHandler(this.absposy_Validated);
			this.absposy.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// absposx
			// 
			this.absposx.AllowDecimal = true;
			this.absposx.AllowNegative = true;
			this.absposx.AllowRelative = true;
			this.absposx.ButtonStep = 1;
			this.absposx.Location = new System.Drawing.Point(58, 53);
			this.absposx.Name = "absposx";
			this.absposx.Size = new System.Drawing.Size(72, 24);
			this.absposx.StepValues = null;
			this.absposx.TabIndex = 8;
			this.absposx.WhenEnterPressed += new System.EventHandler(this.absposx_Validated);
			this.absposx.Validated += new System.EventHandler(this.absposx_Validated);
			this.absposx.WhenButtonsClicked += new System.EventHandler(this.absposx_Validated);
			this.absposx.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(7, 88);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(48, 14);
			this.label2.TabIndex = 1;
			this.label2.Text = "Relative:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 58);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 14);
			this.label1.TabIndex = 0;
			this.label1.Text = "Absolute:";
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.orgsizey);
			this.groupBox2.Controls.Add(this.orgsizex);
			this.groupBox2.Controls.Add(this.label12);
			this.groupBox2.Controls.Add(this.label9);
			this.groupBox2.Controls.Add(this.label8);
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Controls.Add(this.relsizey);
			this.groupBox2.Controls.Add(this.relsizex);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.abssizey);
			this.groupBox2.Controls.Add(this.abssizex);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Location = new System.Drawing.Point(3, 253);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(243, 117);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Size:";
			// 
			// orgsizey
			// 
			this.orgsizey.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.orgsizey.Location = new System.Drawing.Point(136, 23);
			this.orgsizey.Name = "orgsizey";
			this.orgsizey.Size = new System.Drawing.Size(72, 24);
			this.orgsizey.TabIndex = 31;
			this.orgsizey.Text = "-2000";
			this.orgsizey.UseVisualStyleBackColor = true;
			this.orgsizey.Click += new System.EventHandler(this.orgsizey_Click);
			// 
			// orgsizex
			// 
			this.orgsizex.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.orgsizex.Location = new System.Drawing.Point(58, 23);
			this.orgsizex.Name = "orgsizex";
			this.orgsizex.Size = new System.Drawing.Size(72, 24);
			this.orgsizex.TabIndex = 30;
			this.orgsizex.Text = "-2000";
			this.orgsizex.UseVisualStyleBackColor = true;
			this.orgsizex.Click += new System.EventHandler(this.orgsizex_Click);
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(214, 28);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(21, 14);
			this.label12.TabIndex = 21;
			this.label12.Text = "mp";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(9, 28);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(46, 14);
			this.label9.TabIndex = 18;
			this.label9.Text = "Original:";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(214, 88);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(17, 14);
			this.label8.TabIndex = 17;
			this.label8.Text = "%";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(214, 58);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(21, 14);
			this.label7.TabIndex = 16;
			this.label7.Text = "mp";
			// 
			// relsizey
			// 
			this.relsizey.AllowDecimal = true;
			this.relsizey.AllowNegative = true;
			this.relsizey.AllowRelative = true;
			this.relsizey.ButtonStep = 1;
			this.relsizey.Location = new System.Drawing.Point(136, 83);
			this.relsizey.Name = "relsizey";
			this.relsizey.Size = new System.Drawing.Size(72, 24);
			this.relsizey.StepValues = null;
			this.relsizey.TabIndex = 15;
			this.relsizey.WhenEnterPressed += new System.EventHandler(this.relsizey_Validated);
			this.relsizey.Validated += new System.EventHandler(this.relsizey_Validated);
			this.relsizey.WhenButtonsClicked += new System.EventHandler(this.relsizey_Validated);
			this.relsizey.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// relsizex
			// 
			this.relsizex.AllowDecimal = true;
			this.relsizex.AllowNegative = true;
			this.relsizex.AllowRelative = true;
			this.relsizex.ButtonStep = 1;
			this.relsizex.Location = new System.Drawing.Point(58, 83);
			this.relsizex.Name = "relsizex";
			this.relsizex.Size = new System.Drawing.Size(72, 24);
			this.relsizex.StepValues = null;
			this.relsizex.TabIndex = 14;
			this.relsizex.WhenEnterPressed += new System.EventHandler(this.relsizex_Validated);
			this.relsizex.Validated += new System.EventHandler(this.relsizex_Validated);
			this.relsizex.WhenButtonsClicked += new System.EventHandler(this.relsizex_Validated);
			this.relsizex.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(15, 88);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(37, 14);
			this.label4.TabIndex = 13;
			this.label4.Text = "Scale:";
			// 
			// abssizey
			// 
			this.abssizey.AllowDecimal = true;
			this.abssizey.AllowNegative = true;
			this.abssizey.AllowRelative = true;
			this.abssizey.ButtonStep = 1;
			this.abssizey.Location = new System.Drawing.Point(136, 53);
			this.abssizey.Name = "abssizey";
			this.abssizey.Size = new System.Drawing.Size(72, 24);
			this.abssizey.StepValues = null;
			this.abssizey.TabIndex = 12;
			this.abssizey.WhenEnterPressed += new System.EventHandler(this.abssizey_Validated);
			this.abssizey.Validated += new System.EventHandler(this.abssizey_Validated);
			this.abssizey.WhenButtonsClicked += new System.EventHandler(this.abssizey_Validated);
			this.abssizey.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// abssizex
			// 
			this.abssizex.AllowDecimal = true;
			this.abssizex.AllowNegative = true;
			this.abssizex.AllowRelative = true;
			this.abssizex.ButtonStep = 1;
			this.abssizex.Location = new System.Drawing.Point(58, 53);
			this.abssizex.Name = "abssizex";
			this.abssizex.Size = new System.Drawing.Size(72, 24);
			this.abssizex.StepValues = null;
			this.abssizex.TabIndex = 11;
			this.abssizex.WhenEnterPressed += new System.EventHandler(this.abssizex_Validated);
			this.abssizex.Validated += new System.EventHandler(this.abssizex_Validated);
			this.abssizex.WhenButtonsClicked += new System.EventHandler(this.abssizex_Validated);
			this.abssizex.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(2, 58);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 14);
			this.label3.TabIndex = 10;
			this.label3.Text = "Absolute:";
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this.label14);
			this.groupBox3.Controls.Add(this.flipv);
			this.groupBox3.Controls.Add(this.fliph);
			this.groupBox3.Controls.Add(this.label13);
			this.groupBox3.Controls.Add(this.label11);
			this.groupBox3.Controls.Add(this.absrot);
			this.groupBox3.Location = new System.Drawing.Point(3, 376);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(243, 91);
			this.groupBox3.TabIndex = 2;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Transform:";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(9, 61);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(46, 14);
			this.label14.TabIndex = 27;
			this.label14.Text = "Flipping:";
			// 
			// flipv
			// 
			this.flipv.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.flipv.Image = global::CodeImp.DoomBuilder.BuilderModes.Properties.Resources.FlipSelectionV;
			this.flipv.Location = new System.Drawing.Point(94, 53);
			this.flipv.Name = "flipv";
			this.flipv.Size = new System.Drawing.Size(30, 30);
			this.flipv.TabIndex = 26;
			this.flipv.UseVisualStyleBackColor = true;
			this.flipv.Click += new System.EventHandler(this.flipv_Click);
			// 
			// fliph
			// 
			this.fliph.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.fliph.Image = global::CodeImp.DoomBuilder.BuilderModes.Properties.Resources.FlipSelectionH;
			this.fliph.Location = new System.Drawing.Point(58, 53);
			this.fliph.Name = "fliph";
			this.fliph.Size = new System.Drawing.Size(30, 30);
			this.fliph.TabIndex = 25;
			this.fliph.UseVisualStyleBackColor = true;
			this.fliph.Click += new System.EventHandler(this.fliph_Click);
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(6, 28);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(49, 14);
			this.label13.TabIndex = 23;
			this.label13.Text = "Rotation:";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(146, 28);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(13, 14);
			this.label11.TabIndex = 22;
			this.label11.Text = "�";
			// 
			// absrot
			// 
			this.absrot.AllowDecimal = true;
			this.absrot.AllowNegative = true;
			this.absrot.AllowRelative = true;
			this.absrot.ButtonStep = 1;
			this.absrot.Location = new System.Drawing.Point(58, 23);
			this.absrot.Name = "absrot";
			this.absrot.Size = new System.Drawing.Size(82, 24);
			this.absrot.StepValues = null;
			this.absrot.TabIndex = 24;
			this.absrot.WhenEnterPressed += new System.EventHandler(this.absrot_Validated);
			this.absrot.Validated += new System.EventHandler(this.absrot_Validated);
			this.absrot.WhenButtonsClicked += new System.EventHandler(this.absrot_Validated);
			this.absrot.WhenTextChanged += new System.EventHandler(this.WhenTextChanged);
			// 
			// groupBox5
			// 
			this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox5.Controls.Add(this.orgCenterY);
			this.groupBox5.Controls.Add(this.orgCenterX);
			this.groupBox5.Controls.Add(this.label10);
			this.groupBox5.Controls.Add(this.label18);
			this.groupBox5.Controls.Add(this.label20);
			this.groupBox5.Controls.Add(this.label21);
			this.groupBox5.Controls.Add(this.relCenterY);
			this.groupBox5.Controls.Add(this.relCenterX);
			this.groupBox5.Controls.Add(this.absCenterY);
			this.groupBox5.Controls.Add(this.absCenterX);
			this.groupBox5.Controls.Add(this.label22);
			this.groupBox5.Controls.Add(this.label23);
			this.groupBox5.Location = new System.Drawing.Point(3, 7);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(243, 117);
			this.groupBox5.TabIndex = 30;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Center:";
			// 
			// orgCenterY
			// 
			this.orgCenterY.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.orgCenterY.Location = new System.Drawing.Point(136, 23);
			this.orgCenterY.Name = "orgCenterY";
			this.orgCenterY.Size = new System.Drawing.Size(72, 24);
			this.orgCenterY.TabIndex = 29;
			this.orgCenterY.Text = "-2000";
			this.orgCenterY.UseVisualStyleBackColor = true;
			// 
			// orgCenterX
			// 
			this.orgCenterX.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.orgCenterX.Location = new System.Drawing.Point(58, 23);
			this.orgCenterX.Name = "orgCenterX";
			this.orgCenterX.Size = new System.Drawing.Size(72, 24);
			this.orgCenterX.TabIndex = 28;
			this.orgCenterX.Text = "-2000";
			this.orgCenterX.UseVisualStyleBackColor = true;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(214, 28);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(21, 14);
			this.label10.TabIndex = 26;
			this.label10.Text = "mp";
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(9, 28);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(46, 14);
			this.label18.TabIndex = 23;
			this.label18.Text = "Original:";
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Location = new System.Drawing.Point(214, 88);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(21, 14);
			this.label20.TabIndex = 13;
			this.label20.Text = "mp";
			// 
			// label21
			// 
			this.label21.AutoSize = true;
			this.label21.Location = new System.Drawing.Point(214, 58);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(21, 14);
			this.label21.TabIndex = 12;
			this.label21.Text = "mp";
			// 
			// relCenterY
			// 
			this.relCenterY.AllowDecimal = true;
			this.relCenterY.AllowNegative = true;
			this.relCenterY.AllowRelative = true;
			this.relCenterY.ButtonStep = 1;
			this.relCenterY.Location = new System.Drawing.Point(136, 83);
			this.relCenterY.Name = "relCenterY";
			this.relCenterY.Size = new System.Drawing.Size(72, 24);
			this.relCenterY.StepValues = null;
			this.relCenterY.TabIndex = 11;
			// 
			// relCenterX
			// 
			this.relCenterX.AllowDecimal = true;
			this.relCenterX.AllowNegative = true;
			this.relCenterX.AllowRelative = true;
			this.relCenterX.ButtonStep = 1;
			this.relCenterX.Location = new System.Drawing.Point(58, 83);
			this.relCenterX.Name = "relCenterX";
			this.relCenterX.Size = new System.Drawing.Size(72, 24);
			this.relCenterX.StepValues = null;
			this.relCenterX.TabIndex = 10;
			// 
			// absCenterY
			// 
			this.absCenterY.AllowDecimal = true;
			this.absCenterY.AllowNegative = true;
			this.absCenterY.AllowRelative = true;
			this.absCenterY.ButtonStep = 1;
			this.absCenterY.Location = new System.Drawing.Point(136, 53);
			this.absCenterY.Name = "absCenterY";
			this.absCenterY.Size = new System.Drawing.Size(72, 24);
			this.absCenterY.StepValues = null;
			this.absCenterY.TabIndex = 9;
			// 
			// absCenterX
			// 
			this.absCenterX.AllowDecimal = true;
			this.absCenterX.AllowNegative = true;
			this.absCenterX.AllowRelative = true;
			this.absCenterX.ButtonStep = 1;
			this.absCenterX.Location = new System.Drawing.Point(58, 53);
			this.absCenterX.Name = "absCenterX";
			this.absCenterX.Size = new System.Drawing.Size(72, 24);
			this.absCenterX.StepValues = null;
			this.absCenterX.TabIndex = 8;
			// 
			// label22
			// 
			this.label22.AutoSize = true;
			this.label22.Location = new System.Drawing.Point(7, 88);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(48, 14);
			this.label22.TabIndex = 1;
			this.label22.Text = "Relative:";
			// 
			// label23
			// 
			this.label23.AutoSize = true;
			this.label23.Location = new System.Drawing.Point(6, 58);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(53, 14);
			this.label23.TabIndex = 0;
			this.label23.Text = "Absolute:";
			// 
			// EditSelectionPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "EditSelectionPanel";
			this.Size = new System.Drawing.Size(249, 652);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox relposy;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox relposx;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox absposy;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox absposx;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox relsizey;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox relsizex;
		private System.Windows.Forms.Label label4;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox abssizey;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox abssizex;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label12;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox absrot;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Button flipv;
		private System.Windows.Forms.Button fliph;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Button orgposx;
		private System.Windows.Forms.Button orgposy;
		private System.Windows.Forms.Button orgsizey;
		private System.Windows.Forms.Button orgsizex;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.Button orgCenterY;
		private System.Windows.Forms.Button orgCenterX;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.Label label21;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox relCenterY;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox relCenterX;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox absCenterY;
		private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox absCenterX;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.Label label23;
	}
}
