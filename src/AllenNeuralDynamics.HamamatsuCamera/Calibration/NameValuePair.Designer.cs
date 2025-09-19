
namespace HamamatsuCamera.Calibration
{
    partial class NameValuePair
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Top_TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.Name_Label = new System.Windows.Forms.Label();
            this.Top_TableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Top_TableLayoutPanel
            // 
            this.Top_TableLayoutPanel.ColumnCount = 2;
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Top_TableLayoutPanel.Controls.Add(this.Name_Label, 0, 0);
            this.Top_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Top_TableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Top_TableLayoutPanel.Name = "Top_TableLayoutPanel";
            this.Top_TableLayoutPanel.RowCount = 1;
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.Top_TableLayoutPanel.Size = new System.Drawing.Size(324, 30);
            this.Top_TableLayoutPanel.TabIndex = 0;
            this.Top_TableLayoutPanel.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // Name_Label
            // 
            this.Name_Label.AutoSize = true;
            this.Name_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Name_Label.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name_Label.Location = new System.Drawing.Point(3, 3);
            this.Name_Label.Margin = new System.Windows.Forms.Padding(3);
            this.Name_Label.Name = "Name_Label";
            this.Name_Label.Size = new System.Drawing.Size(156, 24);
            this.Name_Label.TabIndex = 1;
            this.Name_Label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Name_Label.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // NameValuePair
            // 
            this.Controls.Add(this.Top_TableLayoutPanel);
            this.Name = "NameValuePair";
            this.Size = new System.Drawing.Size(324, 30);
            this.Click += new System.EventHandler(this.UnClickLabel);
            this.Top_TableLayoutPanel.ResumeLayout(false);
            this.Top_TableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel Top_TableLayoutPanel;
        private System.Windows.Forms.Label Name_Label;
    }
}
