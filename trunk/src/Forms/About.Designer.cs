﻿namespace Nikse.SubtitleEdit.Forms
{
    partial class About
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
            this.okButton = new System.Windows.Forms.Button();
            this.labelProduct = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.richTextBoxAbout1 = new System.Windows.Forms.RichTextBox();
            this.labelFindHeight = new System.Windows.Forms.Label();
            this.buttonDonate = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            //
            // okButton
            //
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.okButton.Location = new System.Drawing.Point(355, 342);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(83, 21);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "&OK";
            this.okButton.Click += new System.EventHandler(this.OkButtonClick);
            //
            // labelProduct
            //
            this.labelProduct.AutoSize = true;
            this.labelProduct.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProduct.Location = new System.Drawing.Point(11, 16);
            this.labelProduct.Name = "labelProduct";
            this.labelProduct.Size = new System.Drawing.Size(140, 19);
            this.labelProduct.TabIndex = 26;
            this.labelProduct.Text = "Subtitle Edit 3.2";
            //
            // pictureBox1
            //
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(368, 14);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(70, 64);
            this.pictureBox1.TabIndex = 27;
            this.pictureBox1.TabStop = false;
            //
            // richTextBoxAbout1
            //
            this.richTextBoxAbout1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxAbout1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxAbout1.Location = new System.Drawing.Point(16, 43);
            this.richTextBoxAbout1.Name = "richTextBoxAbout1";
            this.richTextBoxAbout1.ReadOnly = true;
            this.richTextBoxAbout1.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.richTextBoxAbout1.Size = new System.Drawing.Size(429, 266);
            this.richTextBoxAbout1.TabIndex = 40;
            this.richTextBoxAbout1.TabStop = false;
            this.richTextBoxAbout1.Text = "About...";
            this.richTextBoxAbout1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.RichTextBoxAbout1LinkClicked);
            //
            // labelFindHeight
            //
            this.labelFindHeight.AutoSize = true;
            this.labelFindHeight.Location = new System.Drawing.Point(187, 24);
            this.labelFindHeight.Name = "labelFindHeight";
            this.labelFindHeight.Size = new System.Drawing.Size(80, 13);
            this.labelFindHeight.TabIndex = 41;
            this.labelFindHeight.Text = "labelFindHeight";
            this.labelFindHeight.Visible = false;
            //
            // buttonDonate
            //
            this.buttonDonate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDonate.AutoSize = true;
            this.buttonDonate.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonDonate.FlatAppearance.BorderSize = 0;
            this.buttonDonate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonDonate.ForeColor = System.Drawing.Color.Transparent;
            this.buttonDonate.Image = global::Nikse.SubtitleEdit.Properties.Resources.Donate;
            this.buttonDonate.Location = new System.Drawing.Point(16, 328);
            this.buttonDonate.Name = "buttonDonate";
            this.buttonDonate.Size = new System.Drawing.Size(98, 32);
            this.buttonDonate.TabIndex = 42;
            this.buttonDonate.UseVisualStyleBackColor = false;
            this.buttonDonate.Click += new System.EventHandler(this.buttonDonate_Click);
            //
            // About
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(457, 372);
            this.Controls.Add(this.buttonDonate);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.labelFindHeight);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.richTextBoxAbout1);
            this.Controls.Add(this.labelProduct);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "About";
            this.Padding = new System.Windows.Forms.Padding(9);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About Subtitle Edit";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.About_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label labelProduct;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.RichTextBox richTextBoxAbout1;
        private System.Windows.Forms.Label labelFindHeight;
        private System.Windows.Forms.Button buttonDonate;

    }
}