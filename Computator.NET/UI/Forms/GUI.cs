﻿#define PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Computator.NET.Benchmarking;
using Computator.NET.Charting;
using Computator.NET.Charting.Chart3D;
using Computator.NET.Charting.ComplexCharting;
using Computator.NET.Charting.RealCharting;
using Computator.NET.Compilation;
using Computator.NET.Config;
using Computator.NET.Evaluation;
using Computator.NET.Localization;
using Computator.NET.Logging;
using Computator.NET.NumericalCalculations;
using Computator.NET.Properties;
using Computator.NET.Transformations;
using Computator.NET.UI.AutocompleteMenu;
using Computator.NET.UI.CodeEditors;
using EditChartWindow = Computator.NET.Charting.RealCharting.EditChartWindow;
using File = System.IO.File;
using Settings = Computator.NET.Properties.Settings;

namespace Computator.NET
{
    public partial class GUI : LocalizedForm
    {
        private readonly CultureInfo[] AllCultures =
            CultureInfo.GetCultures(CultureTypes.NeutralCultures);

        private readonly Dictionary<CalculationsMode, IChart> charts = new Dictionary<CalculationsMode, IChart>
        {
            {CalculationsMode.Real, new Chart2D()},
            {CalculationsMode.Complex, new ComplexChart()},
            {CalculationsMode.Fxy, new Chart3DControl()}
        };

        //  private readonly FunctionComplexEvaluator complexEvaluator;
        private readonly ExpressionsEvaluator expressionsEvaluator;
        // private readonly Function2DEvaluator evaluator2d;
        // private readonly Function3DEvaluator evaluator3d;
        private readonly SimpleLogger logger;
        private readonly WebBrowserForm menuFunctionsToolTip;

        private CodeEditorControlWrapper customFunctionsCodeEditor;
        private List<Action<object, EventArgs>> defaultActions;
        private ElementHost elementHostChart3d;

        private CodeEditorControlWrapper scriptingCodeEditor;
        private IChart currentChart => charts[_calculationsMode];

        private Chart2D chart2d => charts[CalculationsMode.Real] as Chart2D;
        private Chart3DControl chart3d => charts[CalculationsMode.Fxy] as Chart3DControl;
        private ComplexChart complexChart => charts[CalculationsMode.Complex] as ComplexChart;

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = tabControl1.SelectedIndex;

            transformToolStripMenuItem.Enabled = chartToolStripMenuItem.Enabled =
                chart3dToolStripMenuItem.Enabled = comlexChartToolStripMenuItem.Enabled = index == 0;

            openToolStripMenuItem.Enabled = index == 0 || index == 5 || index == 4;

            saveToolStripMenuItem.Enabled = index == 4 || index == 5;

            //expressionTextBox.Visible = !(index ==5||index==4);
            tableLayoutPanel1.Visible = !(index == 5 || index == 4);
        }

        private void languageToolStripComboBox_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            var selectedCulture = AllCultures.First(c => c.NativeName == (string) languageToolStripComboBox.SelectedItem);
            Thread.CurrentThread.CurrentCulture = selectedCulture;
            LocalizationManager.GlobalUICulture = selectedCulture;
            Settings.Default.Language = selectedCulture;
            Settings.Default.Save();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Config.Settings().Show();
        }

        private void logsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(SimpleLogger.LogsDirectory))
                Process.Start(SimpleLogger.LogsDirectory);
            else
                MessageBox.Show(
                    Strings.GUI_logsToolStripMenuItem_Click_You_dont_have_any_logs_yet_);
        }

        private void modeRealToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var txt = (sender as ToolStripMenuItem).Text;
            switch (txt)
            {
                case "Real : f(x)":
                    SetMode(CalculationsMode.Real);
                    break;
                case "Complex : f(z)":
                    SetMode(CalculationsMode.Complex);
                    break;
                case "3D : f(x,y)":
                    SetMode(CalculationsMode.Fxy);
                    break;
            }
        }

        private void runToolStripButton_Click(object sender, EventArgs e)
        {
            defaultActions[tabControl1.SelectedIndex].Invoke(sender, e);
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            switch (tabControl1.SelectedIndex)
            {
                case 4:
                    if (scriptingCodeEditor.Focused)
                        scriptingCodeEditor.Cut();
                    else
                        SendKeys.Send("^X");
                    break;
                case 5:
                    if (customFunctionsCodeEditor.Focused)
                        customFunctionsCodeEditor.Cut();
                    else
                        SendKeys.Send("^X");
                    break;

                default: //if (tabControl1.SelectedIndex < 4)
                    SendKeys.Send("^X"); //expressionTextBox.Cut();
                    break;
            }
#else
            SendKeys.Send("^X");
#endif
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            if (tabControl1.SelectedIndex < 4)
                SendKeys.Send("^Z"); //expressionTextBox.Undo();
            else if (tabControl1.SelectedIndex == 4)
            {
                if (scriptingCodeEditor.Focused)
                    scriptingCodeEditor.Undo();
                else
                    SendKeys.Send("^Z");
            }
            else
            {
                if (customFunctionsCodeEditor.Focused)
                    customFunctionsCodeEditor.Undo();
                else
                    SendKeys.Send("^Z");
            }
#else
            SendKeys.Send("^Z");

#endif
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            if (tabControl1.SelectedIndex < 4)
            {
                SendKeys.Send("^Y");
                //expressionTextBox.do()
            }
            else if (tabControl1.SelectedIndex == 4)
                //scriptingCodeEditor.Focus();
            {
                if (scriptingCodeEditor.Focused)
                    scriptingCodeEditor.Redo();
                else
                    SendKeys.Send("^Y");
            }
            else
            {
                if (customFunctionsCodeEditor.Focused)
                    customFunctionsCodeEditor.Redo();
                else
                    SendKeys.Send("^Y");
            }
#else
              SendKeys.Send("^Y");
#endif
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            if (tabControl1.SelectedIndex < 4)
            {
                SendKeys.Send("^C"); //expressionTextBox.Copy();
            }
            else if (tabControl1.SelectedIndex == 4)
            {
                if (scriptingCodeEditor.Focused)
                    scriptingCodeEditor.Copy();
                else
                    SendKeys.Send("^C");
            }
            else
            {
                if (customFunctionsCodeEditor.Focused)
                    customFunctionsCodeEditor.Copy();
                else
                    SendKeys.Send("^C");
            }
#else
            SendKeys.Send("^C");
#endif
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            if (tabControl1.SelectedIndex < 4)
            {
                SendKeys.Send("^V"); //expressionTextBox.Paste();
            }
            else if (tabControl1.SelectedIndex == 4)
            {
                if (scriptingCodeEditor.Focused)
                    scriptingCodeEditor.Paste();
                else
                    SendKeys.Send("^V");
            }
            else
            {
                if (customFunctionsCodeEditor.Focused)
                    customFunctionsCodeEditor.Paste();
                else
                    SendKeys.Send("^V");
            }

#else
            SendKeys.Send("^V");
#endif
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            if (tabControl1.SelectedIndex < 4)
            {
                SendKeys.Send("^A"); //expressionTextBox.SelectAll();
            }
            else if (tabControl1.SelectedIndex == 4)
            {
                if (scriptingCodeEditor.Focused)
                    scriptingCodeEditor.SelectAll();
                else
                    SendKeys.Send("^A");
            }
            else
            {
                if (customFunctionsCodeEditor.Focused)
                    customFunctionsCodeEditor.SelectAll();
                else
                    SendKeys.Send("^A");
            }
#else
            SendKeys.Send("^A");
#endif
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            switch (tabControl1.SelectedIndex)
            {
                case 0:

                    //SendKeys.Send("^S");
                    break;

                case 4:
                    scriptingCodeEditor.NewDocument();
                    break;

                case 5:
                    customFunctionsCodeEditor.NewDocument();
                    break;

                default:
                    //SendKeys.Send("^S");
                    break;
            }
#else
    //SendKeys.Send("^S");
#endif
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog {Filter = GlobalConfig.tslFilesFIlter};

            if (ofd.ShowDialog() != DialogResult.OK)
                return;


#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            switch (tabControl1.SelectedIndex)
            {
                case 0:

                    //SendKeys.Send("^S");
                    break;

                case 4:
                    scriptingCodeEditor.NewDocument(ofd.FileName);
                    break;

                case 5:
                    customFunctionsCodeEditor.NewDocument(ofd.FileName);
                    break;

                default:
                    //SendKeys.Send("^S");
                    break;
            }
#else
    //SendKeys.Send("^S");
#endif
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            switch (tabControl1.SelectedIndex)
            {
                case 0:

                    //SendKeys.Send("^S");
                    break;

                case 4:
                    scriptingCodeEditor.Save();
                    break;

                case 5:
                    customFunctionsCodeEditor.Save();
                    break;

                default:
                    //SendKeys.Send("^S");
                    break;
            }
#else
            SendKeys.Send("^S");
#endif
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            switch (tabControl1.SelectedIndex)
            {
                case 0:

                    //SendKeys.Send("^S");
                    break;

                case 4:
                    scriptingCodeEditor.SaveAs();
                    break;

                case 5:
                    customFunctionsCodeEditor.SaveAs();
                    break;

                default:
                    //SendKeys.Send("^S");
                    break;
            }
#else
            SendKeys.Send("^S");
#endif
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    //if (_calculationsMode == CalculationsMode.Real)
                    currentChart.Print();
                    // else
                    //  SendKeys.Send("^P");
                    break;

                case 4:
                    //scriptingCodeEditor();
                    break;

                case 5:
                    //this.customFunctionsCodeEditor
                    break;

                default:
                    SendKeys.Send("^P"); //this.chart2d.Printing.PrintPreview();
                    break;
            }
#else
            SendKeys.Send("^P");
#endif
        }

        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if PREFER_NATIVE_METHODS_OVER_SENDKING_SHORTCUT_KEYS
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    currentChart.PrintPreview();
                    /*if (_calculationsMode == CalculationsMode.Real)
                        chart2d.Printing.PrintPreview();
                    else
                        SendKeys.Send("^P");*/
                    break;

                case 4:
                    //scriptingCodeEditor();
                    break;

                case 5:
                    //this.customFunctionsCodeEditor
                    break;

                default:
                    SendKeys.Send("^P"); //this.chart2d.Printing.PrintPreview();
                    break;
            }
#else
            SendKeys.Send("^P");
#endif
        }

        private void exponentiationToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        #region initialization and construction

        private readonly ModeDeterminer modeDeterminer;

        public GUI()
        {
            logger = new SimpleLogger(this);
            //evaluator2d = new Function2DEvaluator();
            //evaluator3d = new Function3DEvaluator();
            //complexEvaluator = new FunctionComplexEvaluator();
            expressionsEvaluator = new ExpressionsEvaluator();

            menuFunctionsToolTip = new WebBrowserForm();
            modeDeterminer = new ModeDeterminer();

            InitializeComponent();
            InitializeFunctions();
            InitializeCharts(); //takes more time then it should
            expressionTextBox.RefreshAutoComplete();
            SetupAllComboBoxes();
            attachEventHandlers();
            toolStripStatusLabel1.Text = GlobalConfig.version;
            customFunctionsDirectoryTree.Path = GlobalConfig.FullPath("TSL Examples", "_CustomFunctions");
            directoryTree1.Path = GlobalConfig.FullPath("TSL Examples", "_Scripts");
            UpdateXyRatio();
            InitializeScripting(); //takes a lot of time, TODO: optimize
            SetMathFonts();
            InitializeDataBindings();
            BringToFront();
            Focus();
            Icon = Resources.computator_net_icon;


            Settings.Default.PropertyChanged += Default_PropertyChanged;

            tabPage4.Enabled = false;

            expressionTextBox.TextChanged += ExpressionTextBox_TextChanged;
            HandleCommandLine();
        }

        private CalculationsMode _calculationsMode = CalculationsMode.Fxy;

        private void ExpressionTextBox_TextChanged(object sender, EventArgs e)
        {
            var mode = modeDeterminer.DetermineMode(expressionTextBox.Text);
            if (mode == _calculationsMode) return;

            SetMode(mode);
        }

        private void SetMode(CalculationsMode mode)
        {
            chartToolStripMenuItem.Visible =
                chart2d.Visible = mode == CalculationsMode.Real;

            comlexChartToolStripMenuItem.Visible =
                calculationsComplexLabel.Visible =
                    calculationsImZnumericUpDown.Visible =
                        complexChart.Visible = mode == CalculationsMode.Complex;

            chart3dToolStripMenuItem.Visible =
                elementHostChart3d.Visible = mode == CalculationsMode.Fxy;

            switch (mode)
            {
                case CalculationsMode.Complex:
                    calculationsRealLabel.Text = "Re(z) =";
                    calculationsComplexLabel.Text = "Im(z) =";
                    addToChartButton.Text = Strings.DrawChart;
                    // chart2d.ClearAll();
                    //chart3d.Clear();
                    modeToolStripDropDownButton.Text = "Mode[Complex : f(z)]";
                    break;
                case CalculationsMode.Fxy:
                    calculationsComplexLabel.Visible = calculationsImZnumericUpDown.Visible = true;
                    calculationsRealLabel.Text = "       x =";
                    calculationsComplexLabel.Text = "       y =";
                    addToChartButton.Text = Strings.AddToChart;
                    //complexChart.ClearAll();
                    //chart2d.ClearAll();
                    modeToolStripDropDownButton.Text = "Mode[3D : f(x,y)]";
                    break;
                case CalculationsMode.Real:
                    calculationsRealLabel.Text = "       x =";
                    addToChartButton.Text = Strings.AddToChart;
                    //complexChart.ClearAll();
                    //chart3d.Clear();
                    modeToolStripDropDownButton.Text = "Mode[Real : f(x)]";
                    break;
            }

            _calculationsMode = mode;
            UpdateXyRatio();
        }

        private void HandleCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length < 2) return;
            if (!args[1].Contains(".tsl")) return;

            if (args[1].Contains(".tslf"))
            {
                customFunctionsCodeEditor.NewDocument(args[1]);
                //customFunctionsCodeEditor.CurrentFileName = args[1];
                // customFunctionsCodeEditor.Text = code;
                tabControl1.SelectedIndex = 5;
                tabControl1_SelectedIndexChanged(null, null);
            }
            else
            {
                scriptingCodeEditor.NewDocument(args[1]);
                //scriptingCodeEditor.CurrentFileName = args[1];
                //scriptingCodeEditor.Text = code;

                tabControl1.SelectedIndex = 4;
                tabControl1_SelectedIndexChanged(null, null);
            }
        }

        private void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Language":
                    Thread.CurrentThread.CurrentCulture = Settings.Default.Language;
                    LocalizationManager.GlobalUICulture = Settings.Default.Language;
                    break;

                case "CodeEditor":
                    scriptingCodeEditor.ChangeEditorType();
                    customFunctionsCodeEditor.ChangeEditorType();
                    break;

                case "FunctionsOrder":
                    expressionTextBox.RefreshAutoComplete();
                    break;

                case "ExpressionFont":
                    expressionTextBox.SetFont(Settings.Default.ExpressionFont);
                    break;


                case "ScriptingFont":
                    scriptingCodeEditor.SetFont(Settings.Default.ScriptingFont);
                    customFunctionsCodeEditor.SetFont(Settings.Default.ScriptingFont);
                    break;
            }
        }

        private void SetMathFonts()
        {
            chart2d.Legends[0].Font = MathCustomFonts.GetMathFont(chart2d.Legends[0].Font.Size);
            const float fontsize = 17.0F;

            chart2d.Font = MathCustomFonts.GetMathFont(fontsize);

            function.DefaultCellStyle.Font = MathCustomFonts.GetMathFont(function.DefaultCellStyle.Font.Size);
            result.DefaultCellStyle.Font = MathCustomFonts.GetMathFont(result.DefaultCellStyle.Font.Size);

            calculationValueTextBox.Font = MathCustomFonts.GetMathFont(calculationValueTextBox.Font.Size);
            resultNumericalCalculationsTextBox.Font =
                MathCustomFonts.GetMathFont(resultNumericalCalculationsTextBox.Font.Size);

            consoleOutputTextBox.Font = MathCustomFonts.GetMathFont(consoleOutputTextBox.Font.Size);

            calculationsHistoryDataGridView.Columns[0].DefaultCellStyle.Font =
                MathCustomFonts.GetMathFont(calculationsHistoryDataGridView.Columns[0].DefaultCellStyle.Font.Size);

            calculationsHistoryDataGridView.Columns[calculationsHistoryDataGridView.Columns.Count - 1].DefaultCellStyle
                .Font =
                MathCustomFonts.GetMathFont(
                    calculationsHistoryDataGridView.Columns[calculationsHistoryDataGridView.Columns.Count - 1]
                        .DefaultCellStyle.Font.Size);
        }

        private void InitializeFunctions()
        {
            var functions = GlobalConfig.functionsDetails.ToArray();

            var dict = new Dictionary<string, ToolStripMenuItem>
            {
                {"ElementaryFunctions", elementaryFunctionsToolStripMenuItem},
                {"SpecialFunctions", specialFunctionsToolStripMenuItem},
                {"MathematicalConstants", mathematicalConstantsToolStripMenuItem},
                {"PhysicalConstants", physicalConstantsToolStripMenuItem}
            };

            foreach (var f in functions)
            {
                if (f.Value.Category == "")
                    f.Value.Category = "_empty_";

                if (!dict[f.Value.Type].DropDownItems.ContainsKey(f.Value.Category))
                {
                    var cat = new ToolStripMenuItem(f.Value.Category) {Name = f.Value.Category};
                    dict[f.Value.Type].DropDownItems.Add(cat);
                }

                var item = new ToolStripMenuItem
                {
                    Text = f.Value.Signature,
                    ToolTipText = f.Value.Title
                };
                //item.Click += Item_Click;
                item.MouseDown += Item_Click;

                (dict[f.Value.Type].DropDownItems[f.Value.Category] as ToolStripMenuItem)
                    .DropDownItems.Add(item);
            }
        }

        private void InitializeDataBindings()
        {
            exponentiationToolStripMenuItem.DataBindings.Add("Checked", expressionTextBox, "ExponentMode", false,
                DataSourceUpdateMode.OnPropertyChanged);

            //TODO: somehow manage to get codeeditors ExponentMode bind to exponentToolStripMenuItem Checked property
            // scriptingCodeEditor.DataBindings.Add("ExponentMode", exponentiationToolStripMenuItem, "ExponentMode",false,DataSourceUpdateMode.OnPropertyChanged);
            // customFunctionsCodeEditor.DataBindings.Add("ExponentMode", scriptingCodeEditor, "ExponentMode",false,DataSourceUpdateMode.OnPropertyChanged);


            y0NumericUpDown.DataBindings.Add("Value", chart3d, "YMin", false,
                DataSourceUpdateMode.OnPropertyChanged);
            yNNumericUpDown.DataBindings.Add("Value", chart3d, "YMax", false,
                DataSourceUpdateMode.OnPropertyChanged);
            x0NumericUpDown.DataBindings.Add("Value", chart3d, "XMin", false,
                DataSourceUpdateMode.OnPropertyChanged);
            xnNumericUpDown.DataBindings.Add("Value", chart3d, "XMax", false,
                DataSourceUpdateMode.OnPropertyChanged);
            //working OK
            complexChart.DataBindings.Add("YMin", y0NumericUpDown, "Value", false,
                DataSourceUpdateMode.OnPropertyChanged);
            complexChart.DataBindings.Add("YMax", yNNumericUpDown, "Value", false,
                DataSourceUpdateMode.OnPropertyChanged);
            complexChart.DataBindings.Add("XMin", x0NumericUpDown, "Value", false,
                DataSourceUpdateMode.OnPropertyChanged);
            complexChart.DataBindings.Add("XMax", xnNumericUpDown, "Value", false,
                DataSourceUpdateMode.OnPropertyChanged);

            BindField(chart2d, "YMin", y0NumericUpDown, "Value");
            BindField(chart2d, "YMax", yNNumericUpDown, "Value");
            BindField(chart2d, "XMin", x0NumericUpDown, "Value");
            BindField(chart2d, "XMax", xnNumericUpDown, "Value");

            chart2d.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "XyRatio")
                    XYRatioToolStripStatusLabel.Text = string.Format(Strings.XYRatio0, chart2d.XyRatio);
            };
        }

        private void InitializeCharts()
        {
            elementHostChart3d = new ElementHost
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Child = chart3d
            };
            ((ISupportInitialize) chart2d).BeginInit();

            panel2.Controls.Add(chart2d);
            panel2.Controls.Add(complexChart);
            panel2.Controls.Add(elementHostChart3d);
            chart2d.BringToFront();
            complexChart.BringToFront();
            elementHostChart3d.BringToFront();
            complexChart.Visible = false;
            elementHostChart3d.Visible = false;
            ((ISupportInitialize) chart2d).EndInit();


            foreach (var chart in charts)
            {
                chart.Value.SetChartAreaValues((double) x0NumericUpDown.Value, (double) xnNumericUpDown.Value,
                    (double) y0NumericUpDown.Value, (double) yNNumericUpDown.Value);
                complexChart.SetChartAreaValues((double) x0NumericUpDown.Value, (double) xnNumericUpDown.Value,
                    (double) y0NumericUpDown.Value, (double) yNNumericUpDown.Value);
            }
        }

        private void InitializeScripting()
        {
            scriptingCodeEditor = new CodeEditorControlWrapper();
            customFunctionsCodeEditor = new CodeEditorControlWrapper();

            splitContainer2.Panel1.Controls.Add(scriptingCodeEditor);
            scriptingCodeEditor.Name = "scriptingCodeEditor";
            scriptingCodeEditor.Dock = DockStyle.Fill;
            scriptingCodeEditor.BringToFront();

            splitContainer3.Panel1.Controls.Add(customFunctionsCodeEditor);
            customFunctionsCodeEditor.Dock = DockStyle.Fill;
            customFunctionsCodeEditor.Name = "customFunctionsCodeEditor";
        }

        private void attachEventHandlers()
        {
            Resize += (o, e) => UpdateXyRatio();
            defaultActions = new List<Action<object, EventArgs>>
            {
                addToChartButton_Click,
                calculateButton_Click,
                numericalOperationButton_Click,
                symbolicOperationButton_Click,
                processButton_Click
            };
        }

        private void SetupAllComboBoxes()
        {
            chart2d.setupComboBoxes(typeOfChartComboBox, seriesOfChartComboBox, colorsOfChartComboBox,
                positionLegendComboBox, aligmentLegendComboBox);
            complexChart.setupComboBoxes(countourLinesToolStripComboBox, colorAssignmentToolStripComboBox);

            NumericalCalculation.setupOperations(operationNumericalCalculationsComboBox);
            NumericalCalculation.setupMethods(methodNumericalCalculationsComboBox,
                operationNumericalCalculationsComboBox);
            NumericalCalculation.setupGroupBoxes(operationNumericalCalculationsComboBox,
                derivativeAtPointGroupBox,
                intervalGroupBox, maxErrorGroupBox, stepsGroupBox);


            languageToolStripComboBox.Items.Add(new CultureInfo("en").NativeName);
            languageToolStripComboBox.Items.Add(new CultureInfo("pl").NativeName);
            languageToolStripComboBox.Items.Add(new CultureInfo("de").NativeName);
            languageToolStripComboBox.Items.Add(new CultureInfo("cs").NativeName);


            languageToolStripComboBox.AutoSize = true;
            languageToolStripComboBox.Invalidate();

            languageToolStripComboBox.SelectedItem =
                AllCultures.First(c =>
                    c.TwoLetterISOLanguageName ==
                    Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName)
                    .NativeName;
        }

        #endregion

        #region helpers

        private void UpdateXyRatio()
        {
            var ratio = 1.0;
            ratio = _calculationsMode != CalculationsMode.Complex ? chart2d.XyRatio : complexChart.XYRatio;
            XYRatioToolStripStatusLabel.Text = string.Format(Strings.XYRatio0, ratio);
        }

        public static void BindField(Control control, string propertyName,
            object dataSource, string dataMember)
        {
            Binding bd;

            for (var index = control.DataBindings.Count - 1; index == 0; index--)
            {
                bd = control.DataBindings[index];
                if (bd.PropertyName == propertyName)
                    control.DataBindings.Remove(bd);
            }
            control.DataBindings.Add(propertyName, dataSource, dataMember, false,
                DataSourceUpdateMode.OnPropertyChanged);
        }

        private void ExportChartImage(string filename, SaveFileDialog saveFileDialog2)
        {
            var srcDC = NativeMethods.GetDC(elementHostChart3d.Handle);

            var bm = new Bitmap(elementHostChart3d.Width, elementHostChart3d.Height);
            var g = Graphics.FromImage(bm);
            var bmDC = g.GetHdc();
            NativeMethods.BitBlt(bmDC, 0, 0, bm.Width, bm.Height, srcDC, 0, 0, 0x00CC0020 /*SRCCOPY*/);
            NativeMethods.ReleaseDC(elementHostChart3d.Handle, srcDC);
            g.ReleaseHdc(bmDC);
            g.Dispose();
            ImageFormat format;

            switch (saveFileDialog2.FilterIndex)
            {
                case 1:
                    format = ImageFormat.Png;
                    break;
                case 2:
                    format = ImageFormat.Gif;
                    break;
                case 3:
                    format = ImageFormat.Jpeg;
                    break;
                case 4:
                    format = ImageFormat.Bmp;
                    break;
                case 5:
                    format = ImageFormat.Tiff;
                    break;
                case 6:
                    format = ImageFormat.Wmf;
                    break;
                default:
                    format = ImageFormat.Png;
                    break;
            }

            bm.Save(filename, format);
        }

        #endregion

        #region eventHandlers

        private void addToChartButton_Click(object sender, EventArgs e)
        {
            if (expressionTextBox.Text != "")
            {
                try
                {
                    currentChart.addFx(expressionsEvaluator.Evaluate(expressionTextBox.Text,
                        customFunctionsCodeEditor.Text, _calculationsMode));
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }
            else
                MessageBox.Show(
                    Strings.GUI_addToChartButton_Click_Expression_should_not_be_empty_);
        }

        private void HandleException(Exception ex)
        {
            var message = ex.Message + Environment.NewLine + (ex.InnerException?.Message ?? "");
            MessageBox.Show(message, Strings.Error);

            if (!ex.IsInternal())
            {
                logger.MethodName = MethodBase.GetCurrentMethod().Name;
                logger.Log(message, ErrorType.General, ex);
            }
        }


        private void calculateButton_Click(object sender, EventArgs e)
        {
            if (expressionTextBox.Text == "")
                MessageBox.Show(
                    Strings.GUI_addToChartButton_Click_Expression_should_not_be_empty_);
            else
            {
                try
                {
                    var function = expressionsEvaluator.Evaluate(expressionTextBox.Text,
                        customFunctionsCodeEditor.Text, _calculationsMode);

                    var x = (double) valueForCalculationNumericUpDown.Value;
                    var y = (double) calculationsImZnumericUpDown.Value;
                    var z = new Complex(x, y);

                    dynamic result = function.EvaluateDynamic(x, y);

                    calculationValueTextBox.Text = ScriptingExtensions.ToMathString(result);

                    calculationsHistoryDataGridView.Rows.Insert(0,
                        expressionTextBox.Text,
                        _calculationsMode == CalculationsMode.Complex
                            ? z.ToMathString()
                            : (_calculationsMode == CalculationsMode.Fxy
                                ? $"{x.ToMathString()}, {y.ToMathString()}"
                                : x.ToMathString()),
                        calculationValueTextBox.Text);
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }
        }

        private void editChartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var editChartWindow = new EditChartWindow(chart2d);
            editChartWindow.Show();
        }

        private void exportChartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chart2d.saveImage();
        }

        public void aligmentLegendComboBox_SelectedIndexChanged(object s, EventArgs e)
        {
            chart2d.changeChartLegendAligment(((ToolStripComboBox) s).SelectedItem.ToString());
        }

        public void positionLegendComboBox_SelectedIndexChanged(object s, EventArgs e)
        {
            chart2d.changeChartLegendPosition(((ToolStripComboBox) s).SelectedItem.ToString());
        }

        public void colorsOfChartComboBox_SelectedIndexChanged(object s, EventArgs e)
        {
            chart2d.changeChartColor(((ToolStripComboBox) s).SelectedItem.ToString());
        }

        public void seriesOfChartComboBox_SelectedIndexChanged(object s, EventArgs e)
        {
            chart2d.changeSeries(((ToolStripComboBox) s).SelectedItem.ToString());
        }

        public void typeOfChartComboBox_SelectedIndexChanged(object s, EventArgs e)
        {
            chart2d.changeChartType(((ToolStripComboBox) s).SelectedItem.ToString());
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void expressionTextBox_KeyPress(object s, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) 13)
                defaultActions[tabControl1.SelectedIndex].Invoke(s, e);
        }

        private void symbolicOperationButton_Click(object sender, EventArgs e)
        {
        }

        private void clearChartButton_Click(object sender, EventArgs e)
        {
            // this.chart2d.DataBindings
            chart2d.ClearAll();
            complexChart.ClearAll();
            chart3d.Clear();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            complexChart.saveImage();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var editComplexChartWindow = new EditComplexChartWindow(complexChart);
            editComplexChartWindow.Show();
        }

        private void Item_Click(object sender, MouseEventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (e.Button == MouseButtons.Left)
            {
                if (tabControl1.SelectedIndex < 4)
                    expressionTextBox.AppendText(menuItem.Text);
                else if (tabControl1.SelectedIndex == 4)
                {
                    scriptingCodeEditor.AppendText(menuItem.Text);
                }
                else if (tabControl1.SelectedIndex == 5)
                    customFunctionsCodeEditor.AppendText(menuItem.Text);
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (GlobalConfig.functionsDetails.ContainsKey(menuItem.Text))
                {
                    menuFunctionsToolTip.setFunctionInfo(GlobalConfig.functionsDetails[menuItem.Text]);
                    //menuFunctionsToolTip.Show(this, menuItem.Width + 3, 0);
                    menuFunctionsToolTip.Show();
                }
            }
        }

        private void operationNumericalCalculationsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            NumericalCalculation.setupMethods(methodNumericalCalculationsComboBox,
                operationNumericalCalculationsComboBox);

            NumericalCalculation.setupGroupBoxes(operationNumericalCalculationsComboBox,
                derivativeAtPointGroupBox,
                intervalGroupBox, maxErrorGroupBox, stepsGroupBox);
        }

        private void numericalOperationButton_Click(object sender, EventArgs e)
        {
            var method = methodNumericalCalculationsComboBox.SelectedItem.ToString();
            var operation = operationNumericalCalculationsComboBox.SelectedItem.ToString();


            if (_calculationsMode == CalculationsMode.Real)
            {
                var function = expressionsEvaluator.Evaluate(expressionTextBox.Text,
                    customFunctionsCodeEditor.Text, _calculationsMode);

                Func<double, double> fx = (double x) => function.Evaluate(x);

                var result = double.NaN;
                double eps;

                if (!double.TryParse(epsTextBox.Text, out eps))
                {
                    MessageBox.Show(Strings.GivenΕIsNotValid, Strings.Error);
                    return;
                }
                if (!(eps > 0.0) || !(eps < 1))
                {
                    MessageBox.Show(
                        Strings.GivenΕIsNotValidΕShouldBeSmallPositiveNumber, Strings.Error);
                    return;
                }

                var a = (double) aIntervalNumericUpDown.Value;
                var b = (double) bIntervalNumericUpDown.Value;
                var n = (int) nStepsNumericUpDown.Value;
                var order = (uint) nOrderDerivativeNumericUpDown.Value;
                var xPoint = (double) xDerivativePointNumericUpDown.Value;
                var parametersStr="";

                switch (operation)
                {
                    case "Integral":
                        result = Integral.integrate(method, fx, a, b, n);
                        parametersStr= $"a={a.ToMathString()}; b={b.ToMathString()}; N={n}";
                        break;
                    case "Derivative":
                        result = Derivative.derivative(method, fx,xPoint,order, eps);
                        parametersStr = $"n={order}; x={xPoint.ToMathString()}; ε={eps.ToMathString()}";
                        break;
                    case "Function root":
                        result = FunctionRoot.findRoot(method, fx, a, b, eps, n);
                        parametersStr = $"a={a.ToMathString()}; b={b.ToMathString()}; ε={eps.ToMathString()}; N={n}";
                        break;
                }

                resultNumericalCalculationsTextBox.Text = result.ToMathString();
                numericalCalculationsDataGridView.Rows.Insert(0, expressionTextBox.Text,operation,method, parametersStr, resultNumericalCalculationsTextBox.Text);
            }
            else
            {
                MessageBox.Show(
                    Strings
                        .GUI_numericalOperationButton_Click_Only_Real_mode_is_supported_in_Numerical_calculations_right_now__more_to_come_in_next_versions_ +
                    Environment.NewLine +
                    Strings.GUI_numericalOperationButton_Click__Check__Real___f_x___mode,
                    Strings.GUI_numericalOperationButton_Click_Warning_);
            }
        }


        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Computator.NET "+GlobalConfig.version+"\nthis is beta version, some functions may not work properly\n\nAuthor: Paweł Troka\nE-mail: ptroka@fizyka.dk\nWebsite: http://fizyka.dk", "About Computator.NET");
            var about = new AboutBox1();
            about.ShowDialog();
        }

        private void processButton_Click(object sender, EventArgs e)
        {
            consoleOutputTextBox.Text = Strings.ConsoleOutput;
            try
            {
                scriptingCodeEditor.ProcessScript(consoleOutputTextBox, customFunctionsCodeEditor.Text);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void featuresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(GlobalConfig.features, Strings.Features);
        }

        private void thanksToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                GlobalConfig.betatesters + Environment.NewLine + GlobalConfig.translators +
                Environment.NewLine +
                GlobalConfig.libraries + Environment.NewLine +
                GlobalConfig.others, Strings.SpecialThanksTo);
        }

        private void changelogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sr = new StreamReader(GlobalConfig.FullPath("CHANGELOG")))
            {
                var changelogForm = new Form
                {
                    Text = Strings.GUI_changelogToolStripMenuItem_Click_Changelog,
                    Size = Size
                };
                changelogForm.Controls.Add(new RichTextBox
                {
                    Text = sr.ReadToEnd(),
                    ReadOnly = true,
                    Dock = DockStyle.Fill
                });
                changelogForm.ShowDialog();
            }
        }

        private void bugReportingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Strings.PleaseReportAnyBugsToPawełTrokaPtrokaFizykaDk,
                Strings.BugReporting);
        }

        private void saveScriptButton_Click(object sender, EventArgs e)
        {
            var result = saveScriptFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveScriptFileDialog.FileName))
                {
                    sw.Write(scriptingCodeEditor.Text);
                }
                directoryTree1.Refresh();
                directoryTree1.Invalidate();
            }
        }

        private void openScriptButton_Click(object sender, EventArgs e)
        {
            var result = openScriptFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                using (var sr = new StreamReader(openScriptFileDialog.FileName))
                {
                    scriptingCodeEditor.Text = sr.ReadToEnd();
                }
            }
        }

        private void transformToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuitem = sender as ToolStripDropDownItem;

            if (_calculationsMode == CalculationsMode.Real)
                chart2d.Transform(
                    points => MathematicalTransformations.Transform(points, menuitem.Text),
                    menuitem.Text);
            //  else if (complexNumbersModeRadioBox.Checked)
            //    else if(fxyModeRadioBox.Checked)
        }

        private void openCustomFunctions_Click(object sender, EventArgs e)
        {
            var result = openCustomFunctionsFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                using (var sr = new StreamReader(openCustomFunctionsFileDialog.FileName))
                {
                    customFunctionsCodeEditor.Text = sr.ReadToEnd();
                }
            }
        }

        private void saveCustomFunctions_Click(object sender, EventArgs e)
        {
            var result = saveCustomFunctionsFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveCustomFunctionsFileDialog.FileName))
                {
                    sw.Write(customFunctionsCodeEditor.Text);
                }
                customFunctionsDirectoryTree.Refresh();
                customFunctionsDirectoryTree.Invalidate();
            }
        }

        private void customFunctionsDirectoryTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (File.Exists(customFunctionsDirectoryTree.SelectedNode.FullPath))
            {
                using (var sr = new StreamReader(customFunctionsDirectoryTree.SelectedNode.FullPath))
                {
                    customFunctionsCodeEditor.Text = sr.ReadToEnd();
                }
            }
        }

        private void countourLinesToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            complexChart.countourMode =
                (CountourLinesMode) countourLinesToolStripComboBox.SelectedItem;
            complexChart.reDraw();
        }

        private void colorAssignmentToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            complexChart.colorAssignmentMethod =
                (AssignmentOfColorMethod) colorAssignmentToolStripComboBox.SelectedItem;
            complexChart.reDraw();
        }

        private void directoryTree1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (File.Exists(directoryTree1.SelectedNode.FullPath))
            {
                using (var sr = new StreamReader(directoryTree1.SelectedNode.FullPath))
                {
                    scriptingCodeEditor.Text = sr.ReadToEnd();
                }
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            chart3d.Quality =
                chart2d.Quality = complexChart.Quality = trackBar1.Value/(double) trackBar1.Maximum*100.0;
        }

        private void exportChart3dToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFileDialog2 = new SaveFileDialog
            {
                Filter = "Png Image (.png)|*.png|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|Bitmap Im" +
                         "age (.bmp)|*.bmp|Tiff Image (.tiff)|*.tiff|Wmf Image (.wmf)|*.wmf",
                FileName = Strings.Chart + DateTime.Now.ToShortDateString() + " "
                           + DateTime.Now.ToLongTimeString().Replace(':', '-')
                           + ".png"
            };


            var dialogResult = saveFileDialog2.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                Thread.Sleep(20);
                ExportChartImage(saveFileDialog2.FileName, saveFileDialog2);
            }
        }

        private void editChart3dToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var editChartWindow = new Charting.Chart3D.EditChartWindow(chart3d, elementHostChart3d);
            editChartWindow.ShowDialog();
        }

        private void editChart3dPropertiesToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            var editChartProperties = new EditChartProperties(chart3d);
            if (editChartProperties.ShowDialog() == DialogResult.OK)
            {
            }
        }

        private void chart3dEqualAxesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chart3d.EqualAxes = true;
        }

        private void chart3dFitAxesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chart3d.EqualAxes = false;
        }

        private void editPropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var editChartProperties = new EditChartProperties(complexChart);
            if (editChartProperties.ShowDialog() == DialogResult.OK)
            {
                complexChart.reDraw();
            }
        }

        private void editPropertiesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var editChartProperties = new EditChartProperties(chart2d);
            if (editChartProperties.ShowDialog() == DialogResult.OK)
            {
                chart2d.Invalidate();
            }
        }

        private void benchmarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var bch = new BenchmarkForm();
            bch.Show();
        }

        #endregion
    }
}