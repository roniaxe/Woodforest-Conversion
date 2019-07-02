using System;
using DataVisualization.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WoodForestConversion.API.Conversion.Jobs;
using WoodForestConversion.Data;

namespace DataVisualization
{
    public partial class Form1 : Form
    {
        private List<ExecMethodDto> _queryData;
        private bool _sortAscending;
        public Form1()
        {
            InitializeComponent();
        }

        #region Private Methods
        private void QueryData()
        {
            using (var context = new ARCHONEntities())
            {
                _queryData =
                        (
                        from jobStep in context.JobSteps
                        join executionModule in context.ExecutionModules on jobStep.ModuleUID equals executionModule.ModuleUID
                        join job in context.Jobs on jobStep.JobUID equals job.JobUID
                        where !job.IsDeleted && job.IsLive && !jobStep.IsDeleted && jobStep.IsLive
                        select new ExecMethodDto
                        {
                            JobId = job.JobUID,
                            JobName = job.JobName,
                            StepId = jobStep.StepUID,
                            Category = job.Category,
                            ModuleId = jobStep.ModuleUID,
                            ModuleName = executionModule.ModuleName,
                            ModuleAssembly = executionModule.ModuleAssembly,
                            ModuleObject = executionModule.ModuleObject,
                            ConfigFile = jobStep.ConfigurationFile
                        })
                    .OrderBy(dto => dto.ModuleObject)
                    .ToList();
            }
        }

        private void ProcessData()
        {
            foreach (var execMethodDto in _queryData)
            {
                try
                {
                    string parsedPath = JobConversionHelper.ParsePath(execMethodDto.ConfigFile, execMethodDto.Category);
                    var content = File.ReadAllText($@"C:\Users\RoniAxelrad\Desktop\Woodforest\XMLs\{parsedPath}");
                    execMethodDto.ConfigContent = Regex.Replace(content, @"\t|\n|\r", "");
                }
                catch (FileNotFoundException)
                {
                    execMethodDto.ConfigContent = "Config File Is Missing!";
                }
                catch (DirectoryNotFoundException)
                {
                    execMethodDto.ConfigContent = $"Config Folder Is Missing!";
                }

                ValidateData(execMethodDto);
            }
        }

        private void ValidateData(ExecMethodDto execMethodDto)
        {
            if (execMethodDto.ConfigContent.Equals("Config File Is Missing!")) return;
            switch (execMethodDto.ModuleObject)
            {
                case "Archon.Modules.CommandEvent":
                    if (!execMethodDto.ConfigContent.Contains("exec"))
                    {
                        Console.WriteLine(@"missing..");
                    }
                    break;
                case "Archon.Modules.SqlProcessEvent":
                    if (!execMethodDto.ConfigContent.Contains("server") || !execMethodDto.ConfigContent.Contains("database") || !execMethodDto.ConfigContent.Contains("executesql"))
                    {
                        Console.WriteLine(@"missing..");
                    }
                    break;
            }
        }
        

        private void AttachDataSource()
        {
            dataGridView1.DataSource = _queryData;
        }
        #endregion

        #region Event Handlers
        private void Form1_Load(object sender, System.EventArgs e)
        {
            QueryData();
            ProcessData();
            AttachDataSource();
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var execMethodDtos = _queryData.OrderBy(dataGridView1.Columns[e.ColumnIndex].DataPropertyName);

            dataGridView1.DataSource =
                _sortAscending
                    ? execMethodDtos.ToList()
                    : execMethodDtos.Reverse().ToList();

            _sortAscending = !_sortAscending;
        } 
        #endregion
    }
}
