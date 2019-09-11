using DataVisualization.DTOs;

namespace DataVisualization
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.StepId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.jobNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.jobIdDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.moduleIdDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.moduleAssemblyDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.configFileDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.moduleNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.moduleObjectDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.configContentDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.execMethodDtoBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.execMethodDtoBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.jobNameDataGridViewTextBoxColumn,
            this.jobIdDataGridViewTextBoxColumn,
            this.StepId,
            this.moduleIdDataGridViewTextBoxColumn,
            this.moduleAssemblyDataGridViewTextBoxColumn,
            this.configFileDataGridViewTextBoxColumn,
            this.moduleNameDataGridViewTextBoxColumn,
            this.moduleObjectDataGridViewTextBoxColumn,
            this.configContentDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.execMethodDtoBindingSource;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(800, 450);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_ColumnHeaderMouseClick);
            // 
            // StepId
            // 
            this.StepId.DataPropertyName = "StepId";
            this.StepId.HeaderText = "StepId";
            this.StepId.Name = "StepId";
            this.StepId.ReadOnly = true;
            this.StepId.Width = 63;
            // 
            // jobNameDataGridViewTextBoxColumn
            // 
            this.jobNameDataGridViewTextBoxColumn.DataPropertyName = "JobName";
            this.jobNameDataGridViewTextBoxColumn.HeaderText = "JobName";
            this.jobNameDataGridViewTextBoxColumn.Name = "jobNameDataGridViewTextBoxColumn";
            this.jobNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.jobNameDataGridViewTextBoxColumn.Width = 77;
            // 
            // jobIdDataGridViewTextBoxColumn
            // 
            this.jobIdDataGridViewTextBoxColumn.DataPropertyName = "JobId";
            this.jobIdDataGridViewTextBoxColumn.HeaderText = "JobId";
            this.jobIdDataGridViewTextBoxColumn.Name = "jobIdDataGridViewTextBoxColumn";
            this.jobIdDataGridViewTextBoxColumn.ReadOnly = true;
            this.jobIdDataGridViewTextBoxColumn.Width = 58;
            // 
            // moduleIdDataGridViewTextBoxColumn
            // 
            this.moduleIdDataGridViewTextBoxColumn.DataPropertyName = "ModuleId";
            this.moduleIdDataGridViewTextBoxColumn.HeaderText = "ModuleId";
            this.moduleIdDataGridViewTextBoxColumn.Name = "moduleIdDataGridViewTextBoxColumn";
            this.moduleIdDataGridViewTextBoxColumn.ReadOnly = true;
            this.moduleIdDataGridViewTextBoxColumn.Width = 76;
            // 
            // moduleAssemblyDataGridViewTextBoxColumn
            // 
            this.moduleAssemblyDataGridViewTextBoxColumn.DataPropertyName = "ModuleAssembly";
            this.moduleAssemblyDataGridViewTextBoxColumn.HeaderText = "ModuleAssembly";
            this.moduleAssemblyDataGridViewTextBoxColumn.Name = "moduleAssemblyDataGridViewTextBoxColumn";
            this.moduleAssemblyDataGridViewTextBoxColumn.ReadOnly = true;
            this.moduleAssemblyDataGridViewTextBoxColumn.Width = 111;
            // 
            // configFileDataGridViewTextBoxColumn
            // 
            this.configFileDataGridViewTextBoxColumn.DataPropertyName = "ConfigFile";
            this.configFileDataGridViewTextBoxColumn.HeaderText = "ConfigFile";
            this.configFileDataGridViewTextBoxColumn.Name = "configFileDataGridViewTextBoxColumn";
            this.configFileDataGridViewTextBoxColumn.ReadOnly = true;
            this.configFileDataGridViewTextBoxColumn.Width = 78;
            // 
            // moduleNameDataGridViewTextBoxColumn
            // 
            this.moduleNameDataGridViewTextBoxColumn.DataPropertyName = "ModuleName";
            this.moduleNameDataGridViewTextBoxColumn.HeaderText = "ModuleName";
            this.moduleNameDataGridViewTextBoxColumn.Name = "moduleNameDataGridViewTextBoxColumn";
            this.moduleNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.moduleNameDataGridViewTextBoxColumn.Width = 95;
            // 
            // moduleObjectDataGridViewTextBoxColumn
            // 
            this.moduleObjectDataGridViewTextBoxColumn.DataPropertyName = "ModuleObject";
            this.moduleObjectDataGridViewTextBoxColumn.HeaderText = "ModuleObject";
            this.moduleObjectDataGridViewTextBoxColumn.Name = "moduleObjectDataGridViewTextBoxColumn";
            this.moduleObjectDataGridViewTextBoxColumn.ReadOnly = true;
            this.moduleObjectDataGridViewTextBoxColumn.Width = 98;
            // 
            // configContentDataGridViewTextBoxColumn
            // 
            this.configContentDataGridViewTextBoxColumn.DataPropertyName = "ConfigContent";
            this.configContentDataGridViewTextBoxColumn.HeaderText = "ConfigContent";
            this.configContentDataGridViewTextBoxColumn.Name = "configContentDataGridViewTextBoxColumn";
            this.configContentDataGridViewTextBoxColumn.ReadOnly = true;
            this.configContentDataGridViewTextBoxColumn.Width = 99;
            // 
            // execMethodDtoBindingSource
            // 
            this.execMethodDtoBindingSource.DataSource = typeof(DataVisualization.DTOs.ExecMethodDto);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dataGridView1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.execMethodDtoBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.BindingSource execMethodDtoBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn jobNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn jobIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepId;
        private System.Windows.Forms.DataGridViewTextBoxColumn moduleIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn moduleAssemblyDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn configFileDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn moduleNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn moduleObjectDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn configContentDataGridViewTextBoxColumn;
    }
}

