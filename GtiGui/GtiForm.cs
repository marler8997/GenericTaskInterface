using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Gti
{
    public class TaskGui
    {
        readonly Task task;

        TableLayoutPanel tablePanel;
        GtiGuiParameter[] guiParams;

        public TaskGui(Task task)
        {
            this.task = task;
        }
        public TableLayoutPanel GetOrCreatePanel()
        {
            if (tablePanel == null)
            {
                guiParams = new GtiGuiParameter[task.Parameters.SafeLength()];
                for (int i = 0; i < guiParams.Length; i++)
                {
                    guiParams[i] = GtiGuiParameter.Create(task.Parameters[i]);
                }

                tablePanel = new TableLayoutPanel();
                tablePanel.ColumnCount = 4;
                tablePanel.Dock = DockStyle.Fill;
                int rowIndex = 0;

                foreach (GtiGuiParameter parameter in guiParams)
                {
                    parameter.GenerateControls(tablePanel, rowIndex);
                    rowIndex++;
                }

                tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, GtiForm.SmallButtonCellWidth));
                tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, GtiForm.SmallButtonCellWidth));
            }
            return tablePanel;
        }
    }


    abstract class TaskAppImplementationGui
    {
        public readonly GtiForm form;
        public readonly TaskGui taskGui;
        protected TaskAppImplementationGui(GtiForm form, TaskGui taskGui)
        {
            this.form = form;
            this.taskGui = taskGui;
        }
        public abstract void Selected();
    }
    class NoAppTaskImplementationGui : TaskAppImplementationGui
    {
        SplitContainer splitContainer;

        public NoAppTaskImplementationGui(GtiForm form, TaskGui taskGui)
            : base(form, taskGui)
        {
        }
        public override void Selected()
        {
            if (splitContainer == null)
            {
                splitContainer = new SplitContainer();
                splitContainer.Dock = DockStyle.Fill;
                splitContainer.Orientation = Orientation.Horizontal;
                splitContainer.Panel1.Controls.Add(taskGui.GetOrCreatePanel());

                Label label = new Label();
                label.Dock = DockStyle.Fill;
                label.Text = "No app to run this task";
                splitContainer.Panel2.Controls.Add(label);
            }

            form.SetContent(splitContainer);
        }
    }
    class ConsoleTaskImplementationGui : TaskAppImplementationGui
    {
        public readonly ConsoleApplication application;
        public readonly ConsoleTaskImplementation implementation;
        SplitContainer splitContainer;

        public ConsoleTaskImplementationGui(GtiForm form, TaskGui taskGui,
            ConsoleApplication application, ConsoleTaskImplementation implementation)
            : base(form, taskGui)
        {
            this.application = application;
            this.implementation = implementation;
        }
        public override void Selected()
        {
            if (splitContainer == null)
            {
                splitContainer = new SplitContainer();
                splitContainer.Dock = DockStyle.Fill;
                splitContainer.Orientation = Orientation.Horizontal;
                splitContainer.Panel1.Controls.Add(taskGui.GetOrCreatePanel());

                Button runButton = new Button();
                runButton.Text = "Run";
                splitContainer.Panel2.Controls.Add(runButton);
            }

            form.SetContent(splitContainer);
        }
    }

    // Prevents visual studio from opening this file in designer mode
    [System.ComponentModel.DesignerCategory("")]
    public class GtiForm : Form
    {
        public static void Start(String titleMessage, GtiXml gtiXml)
        {
            Application.EnableVisualStyles();
            Application.Run(new GtiForm(titleMessage,gtiXml));
        }

        public const int SmallButtonWidth = 24;
        public const int SmallButtonCellWidth = 30;

        readonly SplitContainer splitContainer;
        int taskAppGridPreferredWidth;

        public GtiForm(String titleMessage, GtiXml gtiXml)
        {
            Width = 600;
            if (String.IsNullOrEmpty(titleMessage))
            {
                Text = "GTI GUI";
            }
            else
            {
                Text = "GTI GUI - " + titleMessage;
            }

            Padding = new Padding(3);

            splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            Controls.Add(splitContainer);

            {
                DataGridView taskAppsGrid = CreateTaskAppGrid(gtiXml, out taskAppsByRow);
                taskAppsGrid.Dock = DockStyle.Fill;
                taskAppsGrid.BackgroundColor = Color.White;
                taskAppsGrid.CellClick += TaskAppPanelClicked;

                taskAppGridPreferredWidth = 3 + taskAppsGrid.Columns[0].GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true) +
                    taskAppsGrid.Columns[1].GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true);
                splitContainer.SplitterDistance = taskAppGridPreferredWidth;

                splitContainer.Panel1.Controls.Add(taskAppsGrid);
                splitContainer.Panel1.BackColor = Color.White;
                splitContainer.FixedPanel = FixedPanel.Panel1;
            }

            Width = taskAppGridPreferredWidth + 500;
        }

        IList<TaskAppImplementationGui> taskAppsByRow;
        void TaskAppPanelClicked(Object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex <= taskAppsByRow.Count)
            {
                taskAppsByRow[e.RowIndex].Selected();
            }
        }

        DataGridView CreateTaskAppGrid(GtiXml gtiXml, out IList<TaskAppImplementationGui> taskAppsByRow)
        {
            DataGridView dataGridView = new DataGridView();
            dataGridView.ColumnCount = 2;
            dataGridView.ReadOnly = true;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.RowHeadersVisible = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MultiSelect = false;
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.AllowUserToResizeColumns = true;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dataGridView.Columns[0].Name = "Task";
            dataGridView.Columns[1].Name = "App";
            dataGridView.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            taskAppsByRow = new List<TaskAppImplementationGui>();

            foreach (var task in gtiXml.Tasks.SafeEnumerable())
            {
                TaskGui taskGui = new TaskGui(task);
                Int32 appImplementationCount = 0;
                foreach (var app in gtiXml.ConsoleApplications.SafeEnumerable())
                {
                    foreach (var impl in app.TaskImplementations)
                    {
                        if (impl.TaskName.Equals(task.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            dataGridView.Rows.Add(new String[] { task.Name, app.Name });
                            taskAppsByRow.Add(new ConsoleTaskImplementationGui(this, taskGui, app, impl));
                            appImplementationCount++;
                        }
                    }
                }
                if (appImplementationCount == 0)
                {
                    dataGridView.Rows.Add(new String[] { task.Name, "<no-app>" });
                    taskAppsByRow.Add(new NoAppTaskImplementationGui(this, taskGui));
                }
            }
            return dataGridView;
        }
        public void SetContent(Control control)
        {
            splitContainer.Panel2.Controls.Clear();
            splitContainer.Panel2.Controls.Add(control);
        }
    }
}