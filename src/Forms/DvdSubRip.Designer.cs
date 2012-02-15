﻿namespace Nikse.SubtitleEdit.Forms
{
    sealed partial class DvdSubRip
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
            this.buttonOpenIfo = new System.Windows.Forms.Button();
            this.textBoxIfoFileName = new System.Windows.Forms.TextBox();
            this.labelIfoFile = new System.Windows.Forms.Label();
            this.groupBoxDvd = new System.Windows.Forms.GroupBox();
            this.buttonClear = new System.Windows.Forms.Button();
            this.groupBoxLanguages = new System.Windows.Forms.GroupBox();
            this.comboBoxLanguages = new System.Windows.Forms.ComboBox();
            this.groupBoxPalNtsc = new System.Windows.Forms.GroupBox();
            this.radioButtonNtsc = new System.Windows.Forms.RadioButton();
            this.radioButtonPal = new System.Windows.Forms.RadioButton();
            this.ButtonRemoveVob = new System.Windows.Forms.Button();
            this.ButtonMoveVobDown = new System.Windows.Forms.Button();
            this.ButtonMoveVobUp = new System.Windows.Forms.Button();
            this.buttonAddVobFile = new System.Windows.Forms.Button();
            this.labelVobFiles = new System.Windows.Forms.Label();
            this.listBoxVobFiles = new System.Windows.Forms.ListBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.progressBarRip = new System.Windows.Forms.ProgressBar();
            this.labelStatus = new System.Windows.Forms.Label();
            this.buttonStartRipping = new System.Windows.Forms.Button();
            this.groupBoxDvd.SuspendLayout();
            this.groupBoxLanguages.SuspendLayout();
            this.groupBoxPalNtsc.SuspendLayout();
            this.SuspendLayout();
            //
            // buttonOpenIfo
            //
            this.buttonOpenIfo.Location = new System.Drawing.Point(447, 32);
            this.buttonOpenIfo.Name = "buttonOpenIfo";
            this.buttonOpenIfo.Size = new System.Drawing.Size(25, 23);
            this.buttonOpenIfo.TabIndex = 1;
            this.buttonOpenIfo.Text = "...";
            this.buttonOpenIfo.UseVisualStyleBackColor = true;
            this.buttonOpenIfo.Click += new System.EventHandler(this.ButtonOpenIfoClick);
            //
            // textBoxIfoFileName
            //
            this.textBoxIfoFileName.AllowDrop = true;
            this.textBoxIfoFileName.Location = new System.Drawing.Point(6, 34);
            this.textBoxIfoFileName.Name = "textBoxIfoFileName";
            this.textBoxIfoFileName.ReadOnly = true;
            this.textBoxIfoFileName.Size = new System.Drawing.Size(435, 24);
            this.textBoxIfoFileName.TabIndex = 0;
            this.textBoxIfoFileName.DragDrop += new System.Windows.Forms.DragEventHandler(this.TextBoxIfoFileNameDragDrop);
            this.textBoxIfoFileName.DragEnter += new System.Windows.Forms.DragEventHandler(this.TextBoxIfoFileNameDragEnter);
            //
            // labelIfoFile
            //
            this.labelIfoFile.AutoSize = true;
            this.labelIfoFile.Location = new System.Drawing.Point(6, 17);
            this.labelIfoFile.Name = "labelIfoFile";
            this.labelIfoFile.Size = new System.Drawing.Size(48, 17);
            this.labelIfoFile.TabIndex = 2;
            this.labelIfoFile.Text = "IFO file";
            //
            // groupBoxDvd
            //
            this.groupBoxDvd.Controls.Add(this.buttonClear);
            this.groupBoxDvd.Controls.Add(this.groupBoxLanguages);
            this.groupBoxDvd.Controls.Add(this.groupBoxPalNtsc);
            this.groupBoxDvd.Controls.Add(this.ButtonRemoveVob);
            this.groupBoxDvd.Controls.Add(this.ButtonMoveVobDown);
            this.groupBoxDvd.Controls.Add(this.ButtonMoveVobUp);
            this.groupBoxDvd.Controls.Add(this.buttonAddVobFile);
            this.groupBoxDvd.Controls.Add(this.labelVobFiles);
            this.groupBoxDvd.Controls.Add(this.listBoxVobFiles);
            this.groupBoxDvd.Controls.Add(this.labelIfoFile);
            this.groupBoxDvd.Controls.Add(this.buttonOpenIfo);
            this.groupBoxDvd.Controls.Add(this.textBoxIfoFileName);
            this.groupBoxDvd.Location = new System.Drawing.Point(4, 4);
            this.groupBoxDvd.Name = "groupBoxDvd";
            this.groupBoxDvd.Size = new System.Drawing.Size(527, 264);
            this.groupBoxDvd.TabIndex = 3;
            this.groupBoxDvd.TabStop = false;
            this.groupBoxDvd.Text = "DVD files/info";
            //
            // buttonClear
            //
            this.buttonClear.Location = new System.Drawing.Point(446, 131);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(74, 21);
            this.buttonClear.TabIndex = 9;
            this.buttonClear.Text = "Clear";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.ButtonClearClick);
            //
            // groupBoxLanguages
            //
            this.groupBoxLanguages.Controls.Add(this.comboBoxLanguages);
            this.groupBoxLanguages.Location = new System.Drawing.Point(6, 206);
            this.groupBoxLanguages.Name = "groupBoxLanguages";
            this.groupBoxLanguages.Size = new System.Drawing.Size(242, 52);
            this.groupBoxLanguages.TabIndex = 14;
            this.groupBoxLanguages.TabStop = false;
            this.groupBoxLanguages.Text = "Languages";
            //
            // comboBoxLanguages
            //
            this.comboBoxLanguages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguages.FormattingEnabled = true;
            this.comboBoxLanguages.Location = new System.Drawing.Point(6, 18);
            this.comboBoxLanguages.Name = "comboBoxLanguages";
            this.comboBoxLanguages.Size = new System.Drawing.Size(230, 25);
            this.comboBoxLanguages.TabIndex = 14;
            //
            // groupBoxPalNtsc
            //
            this.groupBoxPalNtsc.Controls.Add(this.radioButtonNtsc);
            this.groupBoxPalNtsc.Controls.Add(this.radioButtonPal);
            this.groupBoxPalNtsc.Location = new System.Drawing.Point(254, 212);
            this.groupBoxPalNtsc.Name = "groupBoxPalNtsc";
            this.groupBoxPalNtsc.Size = new System.Drawing.Size(266, 46);
            this.groupBoxPalNtsc.TabIndex = 20;
            this.groupBoxPalNtsc.TabStop = false;
            this.groupBoxPalNtsc.Text = "PAL/NTSC";
            //
            // radioButtonNtsc
            //
            this.radioButtonNtsc.AutoSize = true;
            this.radioButtonNtsc.Location = new System.Drawing.Point(104, 18);
            this.radioButtonNtsc.Name = "radioButtonNtsc";
            this.radioButtonNtsc.Size = new System.Drawing.Size(111, 21);
            this.radioButtonNtsc.TabIndex = 1;
            this.radioButtonNtsc.TabStop = true;
            this.radioButtonNtsc.Text = "NTSC (30fps)";
            this.radioButtonNtsc.UseVisualStyleBackColor = true;
            //
            // radioButtonPal
            //
            this.radioButtonPal.AutoSize = true;
            this.radioButtonPal.Checked = true;
            this.radioButtonPal.Location = new System.Drawing.Point(6, 19);
            this.radioButtonPal.Name = "radioButtonPal";
            this.radioButtonPal.Size = new System.Drawing.Size(100, 21);
            this.radioButtonPal.TabIndex = 0;
            this.radioButtonPal.TabStop = true;
            this.radioButtonPal.Text = "PAL (25fps)";
            this.radioButtonPal.UseVisualStyleBackColor = true;
            //
            // ButtonRemoveVob
            //
            this.ButtonRemoveVob.Location = new System.Drawing.Point(447, 104);
            this.ButtonRemoveVob.Name = "ButtonRemoveVob";
            this.ButtonRemoveVob.Size = new System.Drawing.Size(74, 21);
            this.ButtonRemoveVob.TabIndex = 8;
            this.ButtonRemoveVob.Text = "Remove";
            this.ButtonRemoveVob.UseVisualStyleBackColor = true;
            this.ButtonRemoveVob.Click += new System.EventHandler(this.ButtonRemoveVob_Click);
            //
            // ButtonMoveVobDown
            //
            this.ButtonMoveVobDown.Location = new System.Drawing.Point(446, 185);
            this.ButtonMoveVobDown.Name = "ButtonMoveVobDown";
            this.ButtonMoveVobDown.Size = new System.Drawing.Size(74, 21);
            this.ButtonMoveVobDown.TabIndex = 12;
            this.ButtonMoveVobDown.Text = "Move down";
            this.ButtonMoveVobDown.UseVisualStyleBackColor = true;
            this.ButtonMoveVobDown.Click += new System.EventHandler(this.ButtonMoveVobDown_Click);
            //
            // ButtonMoveVobUp
            //
            this.ButtonMoveVobUp.Location = new System.Drawing.Point(447, 158);
            this.ButtonMoveVobUp.Name = "ButtonMoveVobUp";
            this.ButtonMoveVobUp.Size = new System.Drawing.Size(74, 21);
            this.ButtonMoveVobUp.TabIndex = 10;
            this.ButtonMoveVobUp.Text = "Move up";
            this.ButtonMoveVobUp.UseVisualStyleBackColor = true;
            this.ButtonMoveVobUp.Click += new System.EventHandler(this.ButtonMoveVobUp_Click);
            //
            // buttonAddVobFile
            //
            this.buttonAddVobFile.Location = new System.Drawing.Point(447, 77);
            this.buttonAddVobFile.Name = "buttonAddVobFile";
            this.buttonAddVobFile.Size = new System.Drawing.Size(73, 21);
            this.buttonAddVobFile.TabIndex = 5;
            this.buttonAddVobFile.Text = "Add...";
            this.buttonAddVobFile.UseVisualStyleBackColor = true;
            this.buttonAddVobFile.Click += new System.EventHandler(this.ButtonAddVobFileClick);
            //
            // labelVobFiles
            //
            this.labelVobFiles.AutoSize = true;
            this.labelVobFiles.Location = new System.Drawing.Point(6, 60);
            this.labelVobFiles.Name = "labelVobFiles";
            this.labelVobFiles.Size = new System.Drawing.Size(59, 17);
            this.labelVobFiles.TabIndex = 4;
            this.labelVobFiles.Text = "VOB files";
            //
            // listBoxVobFiles
            //
            this.listBoxVobFiles.AllowDrop = true;
            this.listBoxVobFiles.FormattingEnabled = true;
            this.listBoxVobFiles.ItemHeight = 17;
            this.listBoxVobFiles.Location = new System.Drawing.Point(6, 77);
            this.listBoxVobFiles.Name = "listBoxVobFiles";
            this.listBoxVobFiles.Size = new System.Drawing.Size(435, 123);
            this.listBoxVobFiles.TabIndex = 3;
            this.listBoxVobFiles.DragDrop += new System.Windows.Forms.DragEventHandler(this.ListBoxVobFilesDragDrop);
            this.listBoxVobFiles.DragEnter += new System.Windows.Forms.DragEventHandler(this.ListBoxVobFilesDragEnter);
            //
            // openFileDialog1
            //
            this.openFileDialog1.FileName = "openFileDialog1";
            //
            // progressBarRip
            //
            this.progressBarRip.Location = new System.Drawing.Point(117, 290);
            this.progressBarRip.Name = "progressBarRip";
            this.progressBarRip.Size = new System.Drawing.Size(414, 14);
            this.progressBarRip.TabIndex = 4;
            this.progressBarRip.Visible = false;
            //
            // labelStatus
            //
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(117, 273);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(47, 17);
            this.labelStatus.TabIndex = 5;
            this.labelStatus.Text = "Status";
            //
            // buttonStartRipping
            //
            this.buttonStartRipping.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStartRipping.Location = new System.Drawing.Point(4, 273);
            this.buttonStartRipping.Name = "buttonStartRipping";
            this.buttonStartRipping.Size = new System.Drawing.Size(107, 31);
            this.buttonStartRipping.TabIndex = 25;
            this.buttonStartRipping.Text = "Start ripping";
            this.buttonStartRipping.UseVisualStyleBackColor = true;
            this.buttonStartRipping.Click += new System.EventHandler(this.ButtonStartRippingClick);
            //
            // DvdSubRip
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(536, 310);
            this.Controls.Add(this.buttonStartRipping);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.progressBarRip);
            this.Controls.Add(this.groupBoxDvd);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DvdSubRip";
            this.ShowIcon = false;
            this.Text = "Rip subtitles from ifo/vobs (dvd)";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DvdSubRip_KeyDown);
            this.groupBoxDvd.ResumeLayout(false);
            this.groupBoxDvd.PerformLayout();
            this.groupBoxLanguages.ResumeLayout(false);
            this.groupBoxPalNtsc.ResumeLayout(false);
            this.groupBoxPalNtsc.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonOpenIfo;
        private System.Windows.Forms.TextBox textBoxIfoFileName;
        private System.Windows.Forms.Label labelIfoFile;
        private System.Windows.Forms.GroupBox groupBoxDvd;
        private System.Windows.Forms.ListBox listBoxVobFiles;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button ButtonRemoveVob;
        private System.Windows.Forms.Button ButtonMoveVobDown;
        private System.Windows.Forms.Button ButtonMoveVobUp;
        private System.Windows.Forms.Button buttonAddVobFile;
        private System.Windows.Forms.Label labelVobFiles;
        private System.Windows.Forms.ProgressBar progressBarRip;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Button buttonStartRipping;
        private System.Windows.Forms.ComboBox comboBoxLanguages;
        private System.Windows.Forms.GroupBox groupBoxLanguages;
        private System.Windows.Forms.GroupBox groupBoxPalNtsc;
        private System.Windows.Forms.RadioButton radioButtonNtsc;
        private System.Windows.Forms.RadioButton radioButtonPal;
        private System.Windows.Forms.Button buttonClear;
    }
}