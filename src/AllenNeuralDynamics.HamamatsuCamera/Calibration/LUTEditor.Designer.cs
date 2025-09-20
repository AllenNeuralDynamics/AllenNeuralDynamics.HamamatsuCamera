
namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    partial class LUTEditor
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
            this.components = new System.ComponentModel.Container();
            this.Top_TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.LUT_ZedGraph = new ZedGraph.ZedGraphControl();
            this.POI_Label = new System.Windows.Forms.Label();
            this.Remove_Button = new System.Windows.Forms.Button();
            this.POI_Panel = new System.Windows.Forms.Panel();
            this.POI_Table = new System.Windows.Forms.TableLayoutPanel();
            this.SaveLoadTable = new System.Windows.Forms.TableLayoutPanel();
            this.Save_Button = new System.Windows.Forms.Button();
            this.Load_Button = new System.Windows.Forms.Button();
            this.Top_TableLayoutPanel.SuspendLayout();
            this.POI_Panel.SuspendLayout();
            this.SaveLoadTable.SuspendLayout();
            this.SuspendLayout();
            // 
            // Top_TableLayoutPanel
            // 
            this.Top_TableLayoutPanel.BackColor = System.Drawing.Color.Silver;
            this.Top_TableLayoutPanel.ColumnCount = 2;
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 73.79455F));
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26.20545F));
            this.Top_TableLayoutPanel.Controls.Add(this.LUT_ZedGraph, 0, 0);
            this.Top_TableLayoutPanel.Controls.Add(this.POI_Label, 1, 0);
            this.Top_TableLayoutPanel.Controls.Add(this.Remove_Button, 1, 2);
            this.Top_TableLayoutPanel.Controls.Add(this.POI_Panel, 1, 1);
            this.Top_TableLayoutPanel.Controls.Add(this.SaveLoadTable, 1, 3);
            this.Top_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Top_TableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Top_TableLayoutPanel.Name = "Top_TableLayoutPanel";
            this.Top_TableLayoutPanel.RowCount = 4;
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.Top_TableLayoutPanel.Size = new System.Drawing.Size(477, 278);
            this.Top_TableLayoutPanel.TabIndex = 0;
            // 
            // LUT_ZedGraph
            // 
            this.LUT_ZedGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LUT_ZedGraph.Location = new System.Drawing.Point(3, 3);
            this.LUT_ZedGraph.Name = "LUT_ZedGraph";
            this.Top_TableLayoutPanel.SetRowSpan(this.LUT_ZedGraph, 4);
            this.LUT_ZedGraph.ScrollGrace = 0D;
            this.LUT_ZedGraph.ScrollMaxX = 0D;
            this.LUT_ZedGraph.ScrollMaxY = 0D;
            this.LUT_ZedGraph.ScrollMaxY2 = 0D;
            this.LUT_ZedGraph.ScrollMinX = 0D;
            this.LUT_ZedGraph.ScrollMinY = 0D;
            this.LUT_ZedGraph.ScrollMinY2 = 0D;
            this.LUT_ZedGraph.Size = new System.Drawing.Size(345, 272);
            this.LUT_ZedGraph.TabIndex = 0;
            this.LUT_ZedGraph.UseExtendedPrintDialog = true;
            // 
            // POI_Label
            // 
            this.POI_Label.AutoSize = true;
            this.POI_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.POI_Label.Location = new System.Drawing.Point(354, 0);
            this.POI_Label.Name = "POI_Label";
            this.POI_Label.Size = new System.Drawing.Size(120, 30);
            this.POI_Label.TabIndex = 1;
            this.POI_Label.Text = "Pixels of Interest";
            this.POI_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Remove_Button
            // 
            this.Remove_Button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Remove_Button.Location = new System.Drawing.Point(354, 221);
            this.Remove_Button.Name = "Remove_Button";
            this.Remove_Button.Size = new System.Drawing.Size(120, 24);
            this.Remove_Button.TabIndex = 3;
            this.Remove_Button.Text = "Remove Selected";
            this.Remove_Button.UseVisualStyleBackColor = true;
            this.Remove_Button.Click += new System.EventHandler(this.Remove_Button_Click);
            // 
            // POI_Panel
            // 
            this.POI_Panel.AutoScroll = true;
            this.POI_Panel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.POI_Panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.POI_Panel.Controls.Add(this.POI_Table);
            this.POI_Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.POI_Panel.Location = new System.Drawing.Point(354, 33);
            this.POI_Panel.Name = "POI_Panel";
            this.POI_Panel.Size = new System.Drawing.Size(120, 182);
            this.POI_Panel.TabIndex = 4;
            // 
            // POI_Table
            // 
            this.POI_Table.AutoSize = true;
            this.POI_Table.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.POI_Table.ColumnCount = 1;
            this.POI_Table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.POI_Table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.POI_Table.Dock = System.Windows.Forms.DockStyle.Top;
            this.POI_Table.Location = new System.Drawing.Point(0, 0);
            this.POI_Table.Name = "POI_Table";
            this.POI_Table.RowCount = 1;
            this.POI_Table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.POI_Table.Size = new System.Drawing.Size(118, 32);
            this.POI_Table.TabIndex = 0;
            // 
            // SaveLoadTable
            // 
            this.SaveLoadTable.ColumnCount = 2;
            this.SaveLoadTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.SaveLoadTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.SaveLoadTable.Controls.Add(this.Save_Button, 0, 0);
            this.SaveLoadTable.Controls.Add(this.Load_Button, 1, 0);
            this.SaveLoadTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SaveLoadTable.Location = new System.Drawing.Point(351, 248);
            this.SaveLoadTable.Margin = new System.Windows.Forms.Padding(0);
            this.SaveLoadTable.Name = "SaveLoadTable";
            this.SaveLoadTable.RowCount = 1;
            this.SaveLoadTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SaveLoadTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.SaveLoadTable.Size = new System.Drawing.Size(126, 30);
            this.SaveLoadTable.TabIndex = 5;
            // 
            // Save_Button
            // 
            this.Save_Button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Save_Button.Location = new System.Drawing.Point(3, 3);
            this.Save_Button.Name = "Save_Button";
            this.Save_Button.Size = new System.Drawing.Size(57, 24);
            this.Save_Button.TabIndex = 0;
            this.Save_Button.Text = "Save";
            this.Save_Button.UseVisualStyleBackColor = true;
            this.Save_Button.Click += new System.EventHandler(this.Save_Button_Click);
            // 
            // Load_Button
            // 
            this.Load_Button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Load_Button.Location = new System.Drawing.Point(66, 3);
            this.Load_Button.Name = "Load_Button";
            this.Load_Button.Size = new System.Drawing.Size(57, 24);
            this.Load_Button.TabIndex = 1;
            this.Load_Button.Text = "Load";
            this.Load_Button.UseVisualStyleBackColor = true;
            this.Load_Button.Click += new System.EventHandler(this.Load_Button_Click);
            // 
            // LUTEditor
            // 
            this.Controls.Add(this.Top_TableLayoutPanel);
            this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "LUTEditor";
            this.Size = new System.Drawing.Size(477, 278);
            this.Top_TableLayoutPanel.ResumeLayout(false);
            this.Top_TableLayoutPanel.PerformLayout();
            this.POI_Panel.ResumeLayout(false);
            this.POI_Panel.PerformLayout();
            this.SaveLoadTable.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel Top_TableLayoutPanel;
        private ZedGraph.ZedGraphControl LUT_ZedGraph;
        private System.Windows.Forms.Label POI_Label;
        private System.Windows.Forms.Button Remove_Button;
        private System.Windows.Forms.Panel POI_Panel;
        private System.Windows.Forms.TableLayoutPanel POI_Table;
        private System.Windows.Forms.TableLayoutPanel SaveLoadTable;
        private System.Windows.Forms.Button Save_Button;
        private System.Windows.Forms.Button Load_Button;
    }
}
