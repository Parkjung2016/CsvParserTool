using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CSVParserTool;

internal static class Program
{
    static int failures;
    static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            using (var form = new Form1())
            {
                typeof(Form1).GetField("startupDialogsScheduled", InstancePrivate).SetValue(form, true);
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-30000, -30000);
                form.Show();
                Application.DoEvents();
                CreateHandles(form);
                TestSizesAndThemes(form);
                TestExportResultsResize(form);
                TestInteractiveResizePerformance(form);
                TestEngineSelectionDialog(form);
                TestInfoDialogs(form);
                TestPerEngineWorkspaceSettings();
            }
        }
        catch (Exception ex) { Fail("Form initialization", ex.GetBaseException().Message); }
        Console.WriteLine(failures == 0 ? "UI LAYOUT PASS" : $"UI LAYOUT FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    static void TestSizesAndThemes(Form1 form)
    {
        Type themeType = typeof(Form1).Assembly.GetType("CSVParserTool.UITheme", true);
        Type appThemeType = typeof(Form1).Assembly.GetType("CSVParserTool.AppTheme", true);
        MethodInfo setTheme = themeType.GetMethod("SetTheme", BindingFlags.Static | BindingFlags.Public);
        MethodInfo applyTheme = typeof(Form1).GetMethod("ApplyUITheme", InstancePrivate);
        MethodInfo layout = typeof(Form1).GetMethod("LayoutSplitContainers", InstancePrivate);
        string[] themes = { "Default", "Grassland", "Ocean" };
        Size[] sizes = { new Size(920, 580), new Size(1024, 720), new Size(1280, 800), new Size(1600, 1000) };
        foreach (string theme in themes)
            foreach (bool dark in new[] { false, true })
                foreach (Size size in sizes)
                {
                    setTheme.Invoke(null, new[] { Enum.Parse(appThemeType, theme), (object)dark });
                    applyTheme.Invoke(form, null);
                    form.ClientSize = size;
                    layout.Invoke(form, new object[] { true, true });
                    form.PerformLayout(); Application.DoEvents();
                    string state = $"{theme}/{(dark ? "dark" : "light")}/{size.Width}x{size.Height}";
                    CheckKeyBounds(form, state);
                    CheckProgressWidth(form, state);
                    CheckSplitMinimums(form, state);
                    CheckButtons(form, state);
                }
        Pass($"themes/sizes: {themes.Length * 2 * sizes.Length} states");
    }

    static void TestExportResultsResize(Form1 form)
    {
        Type themeType = typeof(Form1).Assembly.GetType("CSVParserTool.UITheme", true);
        Type appThemeType = typeof(Form1).Assembly.GetType("CSVParserTool.AppTheme", true);
        themeType.GetMethod("SetTheme", BindingFlags.Static | BindingFlags.Public).Invoke(null, new[] { Enum.Parse(appThemeType, "Default"), (object)false });
        typeof(Form1).GetMethod("ApplyUITheme", InstancePrivate).Invoke(form, null);
        Invoke(form, "ShowExportResultsPanel");
        Invoke(form, "LayoutSplitContainers", true, true);
        Invoke(form, "InitializeExportResultSplitterDistance");
        var grid = Field<DataGridView>(form, "Grid_ExportResults");
        grid.Rows.Clear();
        for (int i = 0; i < 40; i++) grid.Rows.Add("DT_Table_" + i, "완료", "OK");
        foreach (Size size in new[] { new Size(920, 580), new Size(1600, 1000), new Size(980, 640), new Size(1400, 900) })
        {
            form.ClientSize = size; Invoke(form, "LayoutSplitContainers", true, true); form.PerformLayout(); Invoke(form, "Grid_ExportResults_SizeChanged", grid, EventArgs.Empty); Application.DoEvents();
            if (grid.Rows.Count != 40) Fail("Export grid row retention", $"{size}: {grid.Rows.Count}/40");
            if (grid.ClientSize.Height <= grid.ColumnHeadersHeight) { var exportPanel = Field<Panel>(form, "Panel_ExportProgress"); var top = Field<Panel>(form, "Panel_ExportProgressTop"); var splitDiag = Field<SplitContainer>(form, "splitExportAndLog"); Fail("Export grid viewport", $"{size}: grid={grid.Bounds}, export={exportPanel.Bounds}, top={top.Bounds}, split={splitDiag.ClientSize}, distance={splitDiag.SplitterDistance}, p1={splitDiag.Panel1.ClientSize}, p2={splitDiag.Panel2.ClientSize}"); }
            if (grid.Columns.Cast<DataGridViewColumn>().Any(c => c.Width <= 0)) Fail("Export grid columns", size.ToString());
            if (size.Width <= 980 && grid.Rows.Count > 25) grid.FirstDisplayedScrollingRowIndex = 20;
            if (size.Width >= 1400 && grid.FirstDisplayedScrollingRowIndex != 0) Fail("Export grid scroll reset", $"{size}: first={grid.FirstDisplayedScrollingRowIndex}");
        }
        Invoke(form, "CloseExportResultsPanel");
        var split = Field<SplitContainer>(form, "splitExportAndLog");
        if (!split.Panel1Collapsed) Fail("Close export results", "Panel1 was not collapsed."); else Pass("Export results resize/close");
    }

    static void TestInfoDialogs(Form owner)
    {
        Type dialogType = typeof(Form1).Assembly.GetType("CSVParserTool.ToolInfoForm", true);
        Type platformType = typeof(DataExportService).Assembly.GetType("CSVParserTool.Exporting.ExportPlatform", true);
        foreach (string platformName in new[] { "Unity", "Unreal" })
        {
            object platform = Enum.Parse(platformType, platformName);
            using (var dialog = (Form)Activator.CreateInstance(
                dialogType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { platform },
                null))
            {
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = new Point(-30000, -30000);
                dialog.Show(owner);
                dialog.PerformLayout();
                Application.DoEvents();

                string expectedEngine = platformName == "Unreal" ? "Unreal Engine" : "Unity";
                if (!Descendants(dialog).OfType<Label>().Any(label => label.Text.Contains(expectedEngine)))
                    Fail("Info engine subtitle", expectedEngine);

                Button codeButton = Descendants(dialog).OfType<Button>().FirstOrDefault(button => button.Text == "코드 사용");
                Button cliButton = Descendants(dialog).OfType<Button>().FirstOrDefault(button => button.Text == "CLI");
                if (codeButton == null || cliButton == null)
                {
                    Fail("Info navigation", platformName);
                    continue;
                }

                codeButton.PerformClick();
                Application.DoEvents();
                string codeText = string.Join("\n", Descendants(dialog).Select(control => control.Text));
                if (platformName == "Unity" && !codeText.Contains("GlobalDataContainer"))
                    Fail("Unity code guide", "GlobalDataContainer example missing");
                if (platformName == "Unreal" && !codeText.Contains("UGlobalDataStorage"))
                    Fail("Unreal code guide", "UGlobalDataStorage example missing");

                cliButton.PerformClick();
                Application.DoEvents();
                string cliText = string.Join("\n", Descendants(dialog).Select(control => control.Text));
                string expectedSwitch = platformName == "Unreal" ? "--engine unreal" : "--engine unity";
                if (!cliText.Contains(expectedSwitch))
                    Fail("Info CLI guide", expectedSwitch + " missing");
                dialog.Close();
            }
        }
        Pass("engine-specific Info/code/CLI guides");
    }
    static void TestPerEngineWorkspaceSettings()
    {
        Type store = typeof(Form1).Assembly.GetType("CSVParserTool.ToolSettingsStore", true);
        MethodInfo setProject = store.GetMethod("SetProjectRootPath", BindingFlags.Static | BindingFlags.Public);
        MethodInfo getProject = store.GetMethod("GetProjectRootPath", BindingFlags.Static | BindingFlags.Public);
        MethodInfo setExcel = store.GetMethod("SetExcelSourceFolderPath", BindingFlags.Static | BindingFlags.Public);
        MethodInfo getExcel = store.GetMethod("GetExcelSourceFolderPath", BindingFlags.Static | BindingFlags.Public);

        setProject.Invoke(null, new object[] { "Unity", "U_PROJECT" });
        setExcel.Invoke(null, new object[] { "Unity", "U_XLSX" });
        setProject.Invoke(null, new object[] { "Unreal", "UE_PROJECT" });
        setExcel.Invoke(null, new object[] { "Unreal", "UE_XLSX" });

        if ((string)getProject.Invoke(null, new object[] { "Unity" }) != "U_PROJECT"
            || (string)getExcel.Invoke(null, new object[] { "Unity" }) != "U_XLSX"
            || (string)getProject.Invoke(null, new object[] { "Unreal" }) != "UE_PROJECT"
            || (string)getExcel.Invoke(null, new object[] { "Unreal" }) != "UE_XLSX")
            Fail("Per-engine settings", "Unity and Unreal workspace values overlapped.");
        else
            Pass("per-engine project/XLSX settings");
    }
    static void TestEngineSelectionDialog(Form owner)
    {
        Type dialogType = typeof(Form1).Assembly.GetType("CSVParserTool.EngineSelectionForm", true);
        Type platformType = typeof(DataExportService).Assembly.GetType("CSVParserTool.Exporting.ExportPlatform", true);
        object unity = Enum.Parse(platformType, "Unity");
        using (var dialog = (Form)Activator.CreateInstance(
            dialogType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { unity },
            null))
        {
            dialog.StartPosition = FormStartPosition.Manual;
            dialog.Location = new Point(-30000, -30000);
            dialog.Show(owner);
            dialog.PerformLayout();
            Application.DoEvents();
            foreach (Control control in Descendants(dialog).Where(control => control.Visible))
            {
                if (control.Width <= 0 || control.Height <= 0)
                    Fail("Engine dialog bounds", control.Name + " " + control.Bounds);
                if (control.Parent != null && !ContainsWithTolerance(control.Parent.ClientRectangle, control.Bounds, 2))
                    Fail("Engine dialog clipped", control.GetType().Name + " " + control.Bounds);
            }
            dialog.Close();
        }
        Pass("engine selection dialog layout");
    }
    static void TestInteractiveResizePerformance(Form1 form)
    {
        Invoke(form, "OnResizeBegin", EventArgs.Empty);
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 300; i++)
        {
            form.ClientSize = new Size(920 + (i % 181), 620 + (i % 121));
            Application.DoEvents();
        }
        Invoke(form, "OnResizeEnd", EventArgs.Empty);
        Application.DoEvents();
        stopwatch.Stop();

        const long limitMilliseconds = 3000;
        if (stopwatch.ElapsedMilliseconds > limitMilliseconds)
            Fail("Interactive resize performance", $"{stopwatch.ElapsedMilliseconds} ms / limit {limitMilliseconds} ms");
        else
            Pass($"interactive resize 300 frames: {stopwatch.ElapsedMilliseconds} ms");
    }
    static void CheckKeyBounds(Form1 form, string state)
    {
        string[] names = { "Panel_Header", "Panel_Top", "Panel_MainContent", "Panel_Bottom", "Panel_ListCard", "Panel_PreviewCard", "Panel_LogSection", "Btn_DataSetting", "Btn_EngineTarget", "Btn_SelectProjectRoot", "Btn_SelectExcelFolder", "Btn_NewCsv", "Btn_RefreshList" };
        foreach (string name in names)
        {
            Control c = Find(form, name); if (c == null) { Fail("Missing control", name); continue; }
            if (c.Width <= 0 || c.Height <= 0) Fail("Non-positive bounds", state + " " + name + " " + c.Bounds);
            if (c.Parent != null && !ContainsWithTolerance(c.Parent.ClientRectangle, c.Bounds, 2)) Fail("Clipped control", state + " " + name + " " + c.Bounds + " parent=" + c.Parent.ClientRectangle);
        }
    }

    static void CheckProgressWidth(Form1 form, string state)
    {
        var panel = Field<Panel>(form, "Panel_ExportProgressTop"); var progress = Field<Control>(form, "SegmentedExportProgress_Export"); var close = Field<Button>(form, "Btn_CloseExportResults");
        if (progress.Left > panel.Padding.Left + 1 || progress.Right < panel.ClientSize.Width - panel.Padding.Right - 1) Fail("Progress width", state + " progress=" + progress.Bounds + " panel=" + panel.ClientRectangle);
        if (!ContainsWithTolerance(panel.ClientRectangle, close.Bounds, 1)) Fail("Close button bounds", state + " " + close.Bounds);
        if (progress.Bounds.IntersectsWith(close.Bounds)) Fail("Progress/close overlap", state);
    }

    static void CheckButtons(Form1 form, string state)
    {
        var buttons = Descendants(form).OfType<Button>().Where(button => button.Visible && button.Width > 0 && button.Height > 0).ToList();
        foreach (Button button in buttons)
        {
            Size preferred = button.GetPreferredSize(Size.Empty);
            if (!button.AutoSize && (preferred.Width > button.ClientSize.Width + 4 || preferred.Height > button.ClientSize.Height + 4))
                Fail("Button text clipped", state + " " + button.Name + " preferred=" + preferred + " actual=" + button.ClientSize);
        }
        foreach (IGrouping<Control, Button> group in buttons.Where(button => button.Parent != null).GroupBy(button => button.Parent))
        {
            Button[] siblings = group.ToArray();
            for (int i = 0; i < siblings.Length; i++) for (int j = i + 1; j < siblings.Length; j++)
                if (siblings[i].Bounds.IntersectsWith(siblings[j].Bounds))
                    Fail("Button overlap", state + " " + siblings[i].Name + "/" + siblings[j].Name);
        }
    }

    static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in Descendants(child)) yield return nested;
        }
    }
    static void CheckSplitMinimums(Form1 form, string state)
    {
        foreach (string field in new[] { "splitOuter", "splitWork", "splitExportAndLog" })
        {
            var split = Field<SplitContainer>(form, field); if (split.Panel1Collapsed || split.Panel2Collapsed) continue;
            int p1 = split.Orientation == Orientation.Vertical ? split.Panel1.Width : split.Panel1.Height;
            int p2 = split.Orientation == Orientation.Vertical ? split.Panel2.Width : split.Panel2.Height;
            if (p1 < split.Panel1MinSize || p2 < split.Panel2MinSize) Fail("Split minimum", state + " " + field + "=" + p1 + "/" + p2);
        }
    }

    static bool ContainsWithTolerance(Rectangle outer, Rectangle inner, int t) => inner.Left >= outer.Left - t && inner.Top >= outer.Top - t && inner.Right <= outer.Right + t && inner.Bottom <= outer.Bottom + t;
    static void CreateHandles(Control root) { foreach (Control child in root.Controls) { child.CreateControl(); CreateHandles(child); } }
    static Control Find(Control root, string name) { if (root.Name == name) return root; foreach (Control child in root.Controls) { Control found = Find(child, name); if (found != null) return found; } return null; }
    static T Field<T>(Form1 form, string name) where T : class => (T)typeof(Form1).GetField(name, InstancePrivate).GetValue(form);
    static object Invoke(Form1 form, string name, params object[] args) => typeof(Form1).GetMethod(name, InstancePrivate).Invoke(form, args);
    static void Pass(string text) => Console.WriteLine("[OK] " + text);
    static void Fail(string name, string detail) { failures++; Console.WriteLine("[FAIL] " + name + ": " + detail); }
}