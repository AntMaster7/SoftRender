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
            zBufferPictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)renderPictureBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)zBufferPictureBox).BeginInit();
            SuspendLayout();
            // 
            // renderPictureBox
            // 
            renderPictureBox.Dock = DockStyle.Fill;
            renderPictureBox.Location = new Point(0, 0);
            renderPictureBox.Name = "renderPictureBox";
            renderPictureBox.Size = new Size(1280, 720);
            renderPictureBox.TabIndex = 0;
            renderPictureBox.TabStop = false;
            renderPictureBox.MouseClick += RenderPictureBox_MouseClick;
            // 
            // zBufferPictureBox
            // 
            zBufferPictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            zBufferPictureBox.Location = new Point(788, 12);
            zBufferPictureBox.Name = "zBufferPictureBox";
            zBufferPictureBox.Size = new Size(480, 270);
            zBufferPictureBox.TabIndex = 1;
            zBufferPictureBox.TabStop = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 720);
            Controls.Add(zBufferPictureBox);
            Controls.Add(renderPictureBox);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            Text = "SharpRender - A Software Rasterizer";
            Load += Main_Load;
            ((System.ComponentModel.ISupportInitialize)renderPictureBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)zBufferPictureBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox renderPictureBox;
        private PictureBox zBufferPictureBox;
    }
}