using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Gti
{
    //
    // TODO: Add DragDrop functionality for file parameters
    //
    public abstract class GtiGuiParameter
    {
        delegate GtiGuiParameter GtiGuiParameterCreator(Parameter parameter);
        static readonly GtiGuiParameterCreator[] Creators;
        static GtiGuiParameter()
        {
            Creators = new GtiGuiParameterCreator[(int)ParameterType.Count];
            Creators[(int)ParameterType.Enum           ] = GtiGuiEnumParameter.CreateInstance;
            Creators[(int)ParameterType.File           ] = GtiGuiInputFileParameter.CreateInstance;
            Creators[(int)ParameterType.InputFile      ] = GtiGuiInputFileParameter.CreateInstance;
            Creators[(int)ParameterType.OutputDirectory] = GtiGuiOutputDirectoryParameter.CreateInstance;
            Creators[(int)ParameterType.Directory      ] = GtiGuiOutputDirectoryParameter.CreateInstance;
        }
        public static GtiGuiParameter Create(Parameter parameter)
        {
            if ((int)parameter.type < Creators.Length)
            {
                var creator = Creators[(int)parameter.type];
                if (creator != null)
                {
                    return creator(parameter);
                }
            }
            throw new NotImplementedException(String.Format("ParameterType '{0}' does not have a Gui class yet", parameter.type));
        }

        public readonly String name;
        public readonly Boolean optional;
        public CheckBox optionalCheckBox;
        public GtiGuiParameter(String name, Boolean optional)
        {
            this.name = name;
            this.optional = optional;
        }

        public void GenerateControls(TableLayoutPanel tablePanel, Int32 rowIndex)
        {
            if (optional)
            {
                optionalCheckBox = new CheckBox();
                optionalCheckBox.TextAlign = ContentAlignment.MiddleCenter;
                optionalCheckBox.CheckedChanged += EnableChanged;
                tablePanel.Controls.Add(optionalCheckBox, 0, rowIndex);
            }
            GenerateTypeControls(tablePanel, rowIndex);
        }
        public abstract void EnableChanged(Object sender, EventArgs e);
        public abstract void GenerateTypeControls(TableLayoutPanel tablePanel, Int32 rowIndex);
    }
    static class ParameterGuiUtil
    {
        static readonly Dictionary<EnumDefinition, String[]> enumStringsMap =
            new Dictionary<EnumDefinition, String[]>();
        public static String[] GetEnumStrings(EnumDefinition definition)
        {
            String[] strings;
            if (!enumStringsMap.TryGetValue(definition, out strings))
            {
                strings = new String[definition.Values.Length];
                for (int i = 0; i < strings.Length; i++)
                {
                    strings[i] = definition.Values[i].Name;
                }
                enumStringsMap.Add(definition, strings);
            }
            return strings;
        }


        public static String FindDirectory(String path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return null;
            }
            while (true)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
                String newPath = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(newPath) || newPath.Equals(path))
                {
                    return null;
                }
                path = newPath;
            }
        }
    }
    public class GtiGuiEnumParameter : GtiGuiParameter
    {
        EnumDefinition enumDefinition;
        ComboBox comboBox;

        public static GtiGuiParameter CreateInstance(Parameter parameter)
        {
            return new GtiGuiEnumParameter(parameter.Name, parameter.optional, parameter.TypeEnumReference.definition);
        }
        public GtiGuiEnumParameter(String name, Boolean optional, EnumDefinition enumDefinition)
            : base(name, optional)
        {
            this.enumDefinition = enumDefinition;
        }
        public override void GenerateTypeControls(TableLayoutPanel tablePanel, Int32 rowIndex)
        {
            Label label = new Label();
            if (String.IsNullOrEmpty(name))
            {
                label.Text = enumDefinition.Name + ":";
            }
            else
            {
                label.Text = name + ":";
            }
            label.TextAlign = ContentAlignment.MiddleRight;
            tablePanel.Controls.Add(label, 1, rowIndex);

            comboBox = new ComboBox();
            comboBox.DataSource = ParameterGuiUtil.GetEnumStrings(enumDefinition);
            comboBox.Dock = DockStyle.Fill;
            tablePanel.Controls.Add(comboBox, 3, rowIndex);

            if (optionalCheckBox != null)
            {
                comboBox.Enabled = false;
            }
        }
        public void InputFileOpenDialogClicked(Object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void EnableChanged(object sender, EventArgs e)
        {
            comboBox.Enabled = optionalCheckBox.Checked;
        }
    }
    public class GtiGuiInputFileParameter : GtiGuiParameter
    {
        TextBox text;

        public static GtiGuiParameter CreateInstance(Parameter parameter)
        {
            return new GtiGuiInputFileParameter(parameter.Name, parameter.optional);
        }
        public GtiGuiInputFileParameter(String name, Boolean optional)
            : base(name, optional)
        {
        }
        public override void GenerateTypeControls(TableLayoutPanel tablePanel, Int32 rowIndex)
        {
            Label label = new Label();
            if (String.IsNullOrEmpty(name))
            {
                label.Text = "Input File:";
            }
            else
            {
                label.Text = name + ":";
            }
            label.TextAlign = ContentAlignment.MiddleRight;
            tablePanel.Controls.Add(label, 1, rowIndex);

            Button openFileButton = new Button();
            openFileButton.Text = "...";
            openFileButton.Size = new Size(GtiForm.SmallButtonWidth, openFileButton.Size.Height);
            openFileButton.Click += InputFileOpenDialogClicked;
            openFileButton.TextAlign = ContentAlignment.MiddleCenter;
            tablePanel.Controls.Add(openFileButton, 2, rowIndex);

            text = new TextBox();
            text.AutoCompleteMode = AutoCompleteMode.Suggest;
            text.AutoCompleteSource = AutoCompleteSource.FileSystem;
            text.Dock = DockStyle.Fill;
            tablePanel.Controls.Add(text, 3, rowIndex);

            if (optionalCheckBox != null)
            {
                text.Enabled = false;
            }
        }

        OpenFileDialog openFileDialog;
        public void InputFileOpenDialogClicked(Object sender, EventArgs e)
        {
            if (openFileDialog == null)
            {
                openFileDialog = new OpenFileDialog();
                openFileDialog.RestoreDirectory = true;
                if (String.IsNullOrEmpty(name))
                {
                    openFileDialog.Title = "Select Input File";
                }
                else
                {
                    openFileDialog.Title = "Select " + name;
                }
            }

            {
                String dir = ParameterGuiUtil.FindDirectory(text.Text);
                if (dir != null)
                {
                    openFileDialog.InitialDirectory = dir;
                }
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                text.Text = openFileDialog.FileName;
            }
        }
        public override void EnableChanged(object sender, EventArgs e)
        {
            text.Enabled = optionalCheckBox.Checked;
        }
    }
    public class GtiGuiOutputDirectoryParameter : GtiGuiParameter
    {
        TextBox text;

        public static GtiGuiParameter CreateInstance(Parameter parameter)
        {
            return new GtiGuiOutputDirectoryParameter(parameter.Name, parameter.optional);
        }
        public GtiGuiOutputDirectoryParameter(String name, Boolean optional)
            : base(name, optional)
        {
        }
        public override void GenerateTypeControls(TableLayoutPanel tablePanel, Int32 rowIndex)
        {
            Label label = new Label();
            if (String.IsNullOrEmpty(name))
            {
                label.Text = "Output Directory:";
            }
            else
            {
                label.Text = name + ":";
            }
            label.TextAlign = ContentAlignment.MiddleRight;
            tablePanel.Controls.Add(label, 1, rowIndex);

            Button openFileButton = new Button();
            openFileButton.Text = "...";
            openFileButton.Size = new Size(GtiForm.SmallButtonWidth, openFileButton.Size.Height);
            openFileButton.Click += SelectDirectoryDialogClicked;
            openFileButton.TextAlign = ContentAlignment.MiddleCenter;
            tablePanel.Controls.Add(openFileButton, 2, rowIndex);

            text = new TextBox();
            text.AutoCompleteMode = AutoCompleteMode.Suggest;
            text.AutoCompleteSource = AutoCompleteSource.FileSystemDirectories;
            text.Dock = DockStyle.Fill;
            tablePanel.Controls.Add(text, 3, rowIndex);

            if (optionalCheckBox != null)
            {
                text.Enabled = false;
            }
        }

        //
        // TODO: share the same browsing dialog for all parameters in the same tool set
        OpenFileDialog openFileDialog;
        public void SelectDirectoryDialogClicked(Object sender, EventArgs e)
        {
            if (openFileDialog == null)
            {
                openFileDialog = new OpenFileDialog();
                openFileDialog.RestoreDirectory = true;
                if (String.IsNullOrEmpty(name))
                {
                    openFileDialog.Title = "Select Output Directory";
                }
                else
                {
                    openFileDialog.Title = "Select " + name;
                }
                openFileDialog.ValidateNames = false;
                openFileDialog.CheckFileExists = false;
                openFileDialog.CheckPathExists = false;
            }

            {
                String dir = ParameterGuiUtil.FindDirectory(text.Text);
                if (dir != null)
                {
                    openFileDialog.InitialDirectory = dir;
                }
            }

            openFileDialog.FileName = SelectFolder;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                String textString = openFileDialog.FileName;
                if (textString.EndsWith(SelectFolder))
                {
                    text.Text = textString.Remove(textString.Length - 1 - SelectFolder.Length);
                }
                else
                {
                    text.Text = textString;
                }
            }
        }
        const String SelectFolder = "(Select Folder)";
        public override void EnableChanged(object sender, EventArgs e)
        {
            text.Enabled = optionalCheckBox.Checked;
        }
    }
}