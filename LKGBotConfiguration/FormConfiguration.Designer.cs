namespace LKGBotConfiguration
{
    partial class FormConfiguration
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            chkboxJava = new CheckBox();
            chkboxYouTube = new CheckBox();
            label1 = new Label();
            lblVerify = new LinkLabel();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            edtBotToken = new TextBox();
            edtPrefix = new TextBox();
            edtStatus = new TextBox();
            btnSave = new Button();
            btnCancel = new Button();
            label7 = new Label();
            btnClose = new Button();
            checkBox1 = new CheckBox();
            checkBox2 = new CheckBox();
            btnStart = new Button();
            btnStop = new Button();
            lblJavaStatus = new Label();
            lblYouTubeStatus = new Label();
            lblTips = new Label();
            lblServiceStatus = new Label();
            SuspendLayout();
            // 
            // chkboxJava
            // 
            chkboxJava.AutoCheck = false;
            chkboxJava.AutoSize = true;
            chkboxJava.Location = new Point(21, 35);
            chkboxJava.Name = "chkboxJava";
            chkboxJava.Size = new Size(78, 19);
            chkboxJava.TabIndex = 2;
            chkboxJava.Text = "Java SE 17";
            chkboxJava.UseVisualStyleBackColor = true;
            // 
            // chkboxYouTube
            // 
            chkboxYouTube.AutoCheck = false;
            chkboxYouTube.AutoSize = true;
            chkboxYouTube.Location = new Point(21, 64);
            chkboxYouTube.Name = "chkboxYouTube";
            chkboxYouTube.Size = new Size(134, 19);
            chkboxYouTube.TabIndex = 3;
            chkboxYouTube.Text = "YouTube Verification";
            chkboxYouTube.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label1.Location = new Point(7, 10);
            label1.Name = "label1";
            label1.Size = new Size(133, 19);
            label1.TabIndex = 4;
            label1.Text = "Basic Requirement";
            // 
            // lblVerify
            // 
            lblVerify.AutoSize = true;
            lblVerify.Location = new Point(312, 86);
            lblVerify.Name = "lblVerify";
            lblVerify.Size = new Size(64, 15);
            lblVerify.TabIndex = 5;
            lblVerify.TabStop = true;
            lblVerify.Text = "Verify Now";
            lblVerify.Visible = false;
            lblVerify.LinkClicked += lblVerify_LinkClicked;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label2.Location = new Point(7, 108);
            label2.Name = "label2";
            label2.Size = new Size(62, 19);
            label2.TabIndex = 6;
            label2.Text = "Settings";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(21, 139);
            label3.Name = "label3";
            label3.Size = new Size(59, 15);
            label3.TabIndex = 7;
            label3.Text = "Bot Token";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(300, 171);
            label4.Name = "label4";
            label4.Size = new Size(37, 15);
            label4.TabIndex = 8;
            label4.Text = "Prefix";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(21, 170);
            label5.Name = "label5";
            label5.Size = new Size(39, 15);
            label5.TabIndex = 9;
            label5.Text = "Status";
            // 
            // edtBotToken
            // 
            edtBotToken.Location = new Point(86, 136);
            edtBotToken.Name = "edtBotToken";
            edtBotToken.Size = new Size(290, 23);
            edtBotToken.TabIndex = 10;
            // 
            // edtPrefix
            // 
            edtPrefix.Location = new Point(343, 168);
            edtPrefix.MaxLength = 1;
            edtPrefix.Name = "edtPrefix";
            edtPrefix.Size = new Size(33, 23);
            edtPrefix.TabIndex = 11;
            // 
            // edtStatus
            // 
            edtStatus.Location = new Point(86, 168);
            edtStatus.Name = "edtStatus";
            edtStatus.Size = new Size(197, 23);
            edtStatus.TabIndex = 12;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(21, 201);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 13;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(117, 201);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label7.Location = new Point(7, 255);
            label7.Name = "label7";
            label7.Size = new Size(59, 19);
            label7.TabIndex = 16;
            label7.Text = "Service";
            // 
            // btnClose
            // 
            btnClose.Location = new Point(301, 344);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(75, 23);
            btnClose.TabIndex = 17;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // checkBox1
            // 
            checkBox1.AutoCheck = false;
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(21, 282);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(104, 19);
            checkBox1.TabIndex = 18;
            checkBox1.Text = "Lavalink Server";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            checkBox2.AutoCheck = false;
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(21, 307);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(79, 19);
            checkBox2.TabIndex = 19;
            checkBox2.Text = "Music Bot";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(21, 335);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 23);
            btnStart.TabIndex = 20;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(117, 335);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 21;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // lblJavaStatus
            // 
            lblJavaStatus.AutoSize = true;
            lblJavaStatus.Location = new Point(99, 36);
            lblJavaStatus.Name = "lblJavaStatus";
            lblJavaStatus.Size = new Size(47, 15);
            lblJavaStatus.TabIndex = 22;
            lblJavaStatus.Text = "(Status)";
            // 
            // lblYouTubeStatus
            // 
            lblYouTubeStatus.AutoSize = true;
            lblYouTubeStatus.Location = new Point(39, 86);
            lblYouTubeStatus.Name = "lblYouTubeStatus";
            lblYouTubeStatus.Size = new Size(210, 15);
            lblYouTubeStatus.TabIndex = 23;
            lblYouTubeStatus.Text = "(Code: WWW-WWW-WWW) - Copied!";
            // 
            // lblTips
            // 
            lblTips.AutoSize = true;
            lblTips.Font = new Font("Segoe UI", 8F);
            lblTips.Location = new Point(152, 66);
            lblTips.Name = "lblTips";
            lblTips.Size = new Size(228, 13);
            lblTips.TabIndex = 24;
            lblTips.Text = "(Recd. to use smurf Gmail account to verify)";
            lblTips.Visible = false;
            // 
            // lblServiceStatus
            // 
            lblServiceStatus.AutoSize = true;
            lblServiceStatus.Location = new Point(71, 258);
            lblServiceStatus.Name = "lblServiceStatus";
            lblServiceStatus.Size = new Size(47, 15);
            lblServiceStatus.TabIndex = 25;
            lblServiceStatus.Text = "(Status)";
            // 
            // FormConfiguration
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnClose;
            ClientSize = new Size(388, 375);
            Controls.Add(lblServiceStatus);
            Controls.Add(lblTips);
            Controls.Add(lblYouTubeStatus);
            Controls.Add(lblJavaStatus);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(checkBox2);
            Controls.Add(checkBox1);
            Controls.Add(btnClose);
            Controls.Add(label7);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(edtStatus);
            Controls.Add(edtPrefix);
            Controls.Add(edtBotToken);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(lblVerify);
            Controls.Add(label1);
            Controls.Add(chkboxYouTube);
            Controls.Add(chkboxJava);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormConfiguration";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Music Bot Configuration";
            Load += FormConfiguration_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private CheckBox chkboxJava;
        private CheckBox chkboxYouTube;
        private Label label1;
        private LinkLabel lblVerify;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private TextBox edtBotToken;
        private TextBox edtPrefix;
        private TextBox edtStatus;
        private Button btnSave;
        private Button btnCancel;
        private Label label7;
        private Button btnClose;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private Button btnStart;
        private Button btnStop;
        private Label lblJavaStatus;
        private Label lblYouTubeStatus;
        private Label lblTips;
        private Label lblServiceStatus;
    }
}
