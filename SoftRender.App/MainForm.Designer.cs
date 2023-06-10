namespace SoftRender.App
{
    partial class MainForm
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
            renderPictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)renderPictureBox).BeginInit();
            SuspendLayout();
            // 
            // renderPictureBox
            // 
            renderPictureBox.Dock = DockStyle.Fill;
            renderPictureBox.Location = new Point(0, 0);
            renderPictureBox.Name = "renderPictureBox";
            renderPictureBox.Size = new Size(1024, 768);
            renderPictureBox.TabIndex = 0;
            renderPictureBox.TabStop = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1024, 768);
            Controls.Add(renderPictureBox);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            Text = "SoftRender - A Software Rasterizer";
            Load += Main_Load;
            ((System.ComponentModel.ISupportInitialize)renderPictureBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox renderPictureBox;
    }
}