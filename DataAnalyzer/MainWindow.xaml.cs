using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DataAnalyzer.Models;
using DataAnalyzer.Services;

namespace DataAnalyzer
{
    public class TerminalLine
    {
        public string Text { get; set; }
        public Brush Foreground { get; set; }

        public TerminalLine(string text, string color = "#00FF00")
        {
            Text = text;
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
    }

    public class NavItem
    {
        public string Glyph { get; set; }
        public string Label { get; set; }
        public string PageName { get; set; }
    }

    public partial class MainWindow : Window
    {
        private AppConfig m_Config;
        private string m_ConfigPath;
        private string m_LastOutputPath;
        private readonly List<TerminalLine> m_TerminalLines = new();
        private readonly SaveFolderManager m_SaveManager = new();
        private List<SaveRecord> m_Saves = new();
        private SaveRecord m_SelectedSave;

        public MainWindow()
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] 开始初始化主窗口");
            InitializeComponent();

            // 初始化侧边栏导航
            var navItems = new List<NavItem>
            {
                new NavItem { Glyph = "□", Label = "数据源管理", PageName = "PageDataSource" },
                new NavItem { Glyph = "◇", Label = "报告设置", PageName = "PageReportSettings" },
                new NavItem { Glyph = "◎", Label = "AI 接口配置", PageName = "PageAiConfig" },
                new NavItem { Glyph = "▶", Label = "生成报告", PageName = "PageGenerate" },
            };
            // 将 NavItem 数据绑定到已有的 ListBoxItem.Tag
            for (int i = 0; i < navItems.Count && i < NavList.Items.Count; i++)
            {
                var item = (ListBoxItem)NavList.Items[i];
                item.DataContext = navItems[i];
            }
            NavList.SelectedIndex = 0;

            m_ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] 配置文件路径: {m_ConfigPath}");

            m_Config = AppConfig.Load(m_ConfigPath);
            System.Diagnostics.Debug.WriteLine("[MainWindow] 配置加载完成");

            InitProviderCombo();
            System.Diagnostics.Debug.WriteLine("[MainWindow] 提供商下拉框初始化完成");

            LoadConfig();
            System.Diagnostics.Debug.WriteLine("[MainWindow] 配置加载到UI完成");

            var defaultOutput = Path.Combine(Environment.CurrentDirectory,
                $"CityGovernmentReport_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
            TxtOutputPath.Text = defaultOutput;
            System.Diagnostics.Debug.WriteLine($"[MainWindow] 默认输出路径: {defaultOutput}");

            TerminalOutput.ItemsSource = m_TerminalLines;
            Log("系统初始化完成", "#00D4FF");
            Log("就绪，请配置参数后点击 [ 生成政府工作报告 ]", "#888888");

            CmbReportTemplate.SelectedIndex = 0;

            System.Diagnostics.Debug.WriteLine("[MainWindow] 主窗口初始化完成");

            RefreshSaveList();
        }

        private void InitProviderCombo()
        {
            CmbProvider.Items.Clear();
            foreach (var (name, type) in ApiServiceFactory.GetSupportedProviders())
            {
                CmbProvider.Items.Add(new ComboBoxItem { Content = name, Tag = type });
            }
        }

        private void LoadConfig()
        {
            TxtDataPath.Text = m_Config.GetEffectiveDataPath();
            TxtApiUrl.Text = m_Config.Llm.ApiUrl;
            TxtApiKey.Password = m_Config.Llm.ApiKey;
            CmbModel.Text = m_Config.Llm.Model;
            TxtProxyUrl.Text = m_Config.Llm.ProxyUrl;
            TxtCityName.Text = m_Config.CityName;

            foreach (ComboBoxItem item in CmbProvider.Items)
            {
                if (item.Content.ToString() == m_Config.Llm.ProviderType)
                {
                    CmbProvider.SelectedItem = item;
                    break;
                }
            }

            UpdateProviderSpecificFields();
        }

        private void CmbProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProviderSpecificFields();
        }

        private void UpdateProviderSpecificFields()
        {
            var providerType = GetSelectedProviderType();
            System.Diagnostics.Debug.WriteLine($"[MainWindow] 更新提供商配置: {providerType}");

            switch (providerType)
            {
                case ApiProviderType.DeepSeek:
                    TxtApiUrl.Text = "https://api.deepseek.com/v1/chat/completions";
                    CmbModel.Text = "deepseek-chat";
                    break;
                case ApiProviderType.SiliconFlow:
                    TxtApiUrl.Text = "https://api.siliconflow.cn/v1/chat/completions";
                    CmbModel.Text = "deepseek-ai/DeepSeek-R1-Chat";
                    break;
                case ApiProviderType.Ollama:
                    TxtApiUrl.Text = "http://localhost:11434/v1/chat/completions";
                    CmbModel.Text = "llama3";
                    break;
                case ApiProviderType.AzureOpenAI:
                    if (string.IsNullOrEmpty(TxtApiUrl.Text))
                        TxtApiUrl.Text = "https://your-resource.openai.azure.com/openai/deployments/YOUR_DEPLOYMENT/chat/completions?api-version=2024-02-15-preview";
                    break;
                case ApiProviderType.OpenAI:
                    if (string.IsNullOrEmpty(TxtApiUrl.Text))
                        TxtApiUrl.Text = "https://api.openai.com/v1/chat/completions";
                    if (string.IsNullOrEmpty(CmbModel.Text))
                        CmbModel.Text = "gpt-4o";
                    break;
            }
        }

        private ApiProviderType GetSelectedProviderType()
        {
            if (CmbProvider.SelectedItem is ComboBoxItem item)
            {
                return (ApiProviderType)item.Tag;
            }
            return ApiProviderType.OpenAI;
        }

        private string GetSelectedModel()
        {
            return CmbModel.Text.Trim();
        }

        private string GetEffectiveDataPath()
        {
            if (m_SelectedSave != null && !string.IsNullOrEmpty(m_SelectedSave.FolderPath))
                return m_SelectedSave.FolderPath;

            return TxtDataPath.Text;
        }

        private ReportTemplate GetSelectedTemplate()
        {
            return ReportTemplate.Development;
        }

        private void CmbReportTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log($"[模板] 切换到: 城市发展工作报告", "#00D4FF");
        }

        private void TxtDataPath_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void BtnRefreshSaves_Click(object sender, RoutedEventArgs e)
        {
            Log("[命令] 刷新存档列表", "#FFFF00");
            RefreshSaveList();
        }

        private void RefreshSaveList()
        {
            var basePath = TxtDataPath.Text;

            m_Saves = m_SaveManager.Scan(basePath);
            SaveListView.ItemsSource = null;
            SaveListView.ItemsSource = m_Saves;

            if (m_Saves.Count == 0)
            {
                TxtSaveCount.Text = $"未扫描到存档 — 请确保数据目录包含 current_snapshot.json";
                TxtSaveCount.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6666"));
                Log($"[存档] 未扫描到存档数据，基础路径: {basePath}", "#FFFF00");
                Log("  > 请确保游戏模组已导出数据到该目录中", "#888888");
                Log("  > 目录结构: <基础路径>/current_snapshot.json 或 <基础路径>/<存档名>/current_snapshot.json", "#888888");
            }
            else if (m_Saves.Count == 1 && m_Saves[0].FolderName == "(当前数据)")
            {
                TxtSaveCount.Text = $"✓ 发现 1 个存档（扁平模式）";
                TxtSaveCount.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                Log($"[存档] 在基础路径直接发现数据文件", "#00FF00");
                Log($"  > {m_Saves[0].Summary}", "#888888");
                SaveListView.SelectedIndex = 0;
            }
            else
            {
                TxtSaveCount.Text = $"✓ 发现 {m_Saves.Count} 个存档";
                TxtSaveCount.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                Log($"[存档] 扫描完成，发现 {m_Saves.Count} 个存档", "#00FF00");

                foreach (var save in m_Saves)
                {
                    Log($"  > [{save.FolderName}] {save.Summary}", "#888888");
                }

                if (!string.IsNullOrEmpty(m_Config.SelectedSaveFolder))
                {
                    var matched = m_Saves.FirstOrDefault(s =>
                        s.FolderName == m_Config.SelectedSaveFolder ||
                        s.FolderPath == m_Config.SelectedSaveFolder);
                    if (matched != null)
                    {
                        SaveListView.SelectedItem = matched;
                        SaveListView.ScrollIntoView(matched);
                        Log($"  > 自动选中上次存档: {matched.FolderName}", "#00D4FF");
                    }
                }

                if (SaveListView.SelectedItem == null && m_Saves.Count > 0)
                {
                    SaveListView.SelectedIndex = 0;
                    SaveListView.ScrollIntoView(SaveListView.SelectedItem);
                }
            }
        }

        private void SaveListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_SelectedSave = SaveListView.SelectedItem as SaveRecord;

            if (m_SelectedSave != null)
            {
                Log($"[选择] 当前存档: {m_SelectedSave.FolderName}", "#00D4FF");
                Log($"  > {m_SelectedSave.Summary}", "#888888");

                if (!string.IsNullOrEmpty(m_SelectedSave.FolderName))
                {
                    TxtCityName.Text = m_SelectedSave.FolderName;
                }

                var outputFile = Path.Combine(Environment.CurrentDirectory,
                    $"CityGovernmentReport_{m_SelectedSave.FolderName}_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
                TxtOutputPath.Text = outputFile;

                SaveConfig();
            }
        }

        private void BtnBrowseDataPath(object sender, RoutedEventArgs e)
        {
            Log("[命令] 打开数据目录选择对话框", "#FFFF00");
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择数据基础目录（包含多个存档子文件夹）",
                SelectedPath = TxtDataPath.Text
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtDataPath.Text = dialog.SelectedPath;
                Log($"[设置] 数据路径 = {dialog.SelectedPath}", "#00FF00");
                RefreshSaveList();
            }
        }

        private void BtnBrowseOutputPath(object sender, RoutedEventArgs e)
        {
            Log("[命令] 打开输出文件选择对话框", "#FFFF00");
            var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "Word文档|*.docx",
                DefaultExt = ".docx",
                FileName = TxtOutputPath.Text
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtOutputPath.Text = dialog.FileName;
                Log($"[设置] 输出路径 = {dialog.FileName}", "#00FF00");
            }
        }

        private async void BtnFetchModels_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();

            var apiConfig = new ApiProviderConfig
            {
                ProviderType = GetSelectedProviderType(),
                ApiKey = TxtApiKey.Password,
                ApiUrl = TxtApiUrl.Text,
                Model = GetSelectedModel(),
                ProxyUrl = TxtProxyUrl.Text
            };

            Log("========================================", "#FFFFFF");
            Log("[模型] 获取可用模型列表", "#00D4FF");
            Log("========================================", "#FFFFFF");

            if (!apiConfig.IsValid)
            {
                Log("[错误] API配置不完整，无法获取模型列表", "#FF0000");
                Log("  > 请先填写 API Key 和 API URL", "#FF6666");
                return;
            }

            Log($"[配置] 提供商: {apiConfig.ProviderType}", "#888888");
            Log($"[配置] API URL: {apiConfig.ApiUrl}", "#888888");
            Log($"[请求] 正在请求 /v1/models ...", "#FFFF00");

            try
            {
                var apiService = ApiServiceFactory.Create(apiConfig);
                var models = await apiService.FetchModelsAsync();

                CmbModel.Items.Clear();

                Log($"[成功] 获取到 {models.Count} 个模型：", "#00FF00");
                foreach (var model in models)
                {
                    CmbModel.Items.Add(model.Id);
                    var ownedInfo = string.IsNullOrEmpty(model.OwnedBy) ? "" : $"（{model.OwnedBy}）";
                    Log($"  > {model.Id} {ownedInfo}", "#888888");
                }

                if (models.Count > 0)
                {
                    CmbModel.Text = models.FirstOrDefault(m => m.Id == apiConfig.Model)?.Id ?? models[0].Id;
                    Log($"[选择] 当前使用模型: {CmbModel.Text}", "#00D4FF");
                }

                SaveConfig();
            }
            catch (Exception ex)
            {
                Log("[异常] 获取模型列表失败", "#FF0000");
                Log($"  > 错误: {ex.Message}", "#FF6666");
                MessageBox.Show($"获取模型列表失败：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            Log($"  > 报告模板: 城市发展工作报告", "#888888");

            SaveConfig();

            BtnPreview.IsEnabled = false;
            BtnGenerate.IsEnabled = false;
            BtnOpenReport.IsEnabled = false;
            Progress.Value = 0;
            m_TerminalLines.Clear();
            TerminalOutput.Items.Refresh();

            var dataPath = GetEffectiveDataPath();
            var cityName = SanitizeFileName(TxtCityName.Text);
            // 强制使用当前目录作为输出目录，避免配置文件中的错误路径
            var outputPath = Path.Combine(Environment.CurrentDirectory, $"Preview_{cityName}_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            try
            {
                Log("========================================", "#FFFFFF");
                Log("[开始] 生成预览报告（无需API）", "#00D4FF");
                Log("========================================", "#FFFFFF");

                if (m_SelectedSave != null)
                {
                    Log($"[存档] {m_SelectedSave.FolderName}", "#00D4FF");
                    Log($"  > 游戏时间: {m_SelectedSave.GameDateDisplay}", "#888888");
                    Log($"  > 人口: {m_SelectedSave.PopulationDisplay}", "#888888");
                }

                Log($"[步骤1] 读取城市数据...", "#FFFF00");
                Log($"  > 数据路径: {dataPath}", "#888888");

                var reader = new DataReader(dataPath);
                var current = reader.ReadCurrentSnapshot();
                var full = reader.ReadFullHistory();
                var history = full != null ? reader.BuildSnapshotsFromFullHistory(full) : new List<StatisticSnapshot>();

                if (current == null)
                {
                    Log("[错误] 未找到城市数据文件！", "#FF0000");
                    Log("  > 请先在游戏中使用模组导出数据", "#888888");
                    MessageBox.Show("未找到城市数据。请先在游戏中使用模组导出数据。\n\n确认路径：\n" + dataPath, "数据缺失",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BtnPreview.IsEnabled = true;
                    BtnGenerate.IsEnabled = true;
                    return;
                }

                Log($"  > 数据加载成功", "#00FF00");
                Log($"    - 人口: {current.Population:N0}", "#888888");
                Log($"    - 发展阶段: {GameMetricConverter.GetGameStageDescription(current)}", "#888888");
                Log($"    - 当前周期: 第{current.GameYear}年 第{current.GameMonth}月", "#888888");
                Log($"    - 历史数据: {history.Count} 条", "#888888");

                Log($"[步骤2] 分析城市数据...", "#FFFF00");
                var engine = new AnalysisEngine(current, history, full?.KUpdatesPerDay ?? 32);
                var analysis = engine.Analyze();
                Log($"  > 分析完成", "#00FF00");
                Log($"    - 幸福度: {analysis.Social.Wellbeing:F1}% ({GameMetricConverter.ToHappinessLevel(analysis.Social.Wellbeing)})", "#888888");
                Log($"    - 健康度: {analysis.Social.Health:F1}%", "#888888");
                Log($"    - 生活质量指数: {analysis.Social.QualityOfLifeIndex:F1}/100", "#888888");
                Log($"    - 告警事项: {analysis.Alerts.Count} 项", "#888888");

                Log($"[步骤3] 生成发展工作报告...", "#FFFF00");
                Log($"  > 输出文件: {outputPath}", "#888888");
                Log($"  > 报告格式: 红头文件（预览版）", "#888888");

                var dAnalysis = engine.AnalyzeDevelopment();
                if (dAnalysis != null)
                {
                    var gen = new DevelopmentReportGenerator(new List<ReportChapter>(), dAnalysis, current, history,
                        full?.KUpdatesPerDay ?? 32, cityName, outputPath);
                    gen.Generate();
                }

                m_LastOutputPath = outputPath;
                var fileSize = new FileInfo(outputPath).Length;

                Log("========================================", "#FFFFFF");
                Log("[成功] 预览报告生成完成！", "#00FF00");
                Log("========================================", "#FFFFFF");
                Log($"  > 文件路径: {outputPath}", "#888888");
                Log($"  > 文件大小: {fileSize / 1024.0:F1} KB", "#888888");
                Log($"  > 城市名称: {cityName}", "#888888");
                Log($"  > 统计周期: 第{current.GameYear}年 第{current.GameMonth}月", "#888888");
                Log($"  > 城市人口: {current.Population:N0}", "#888888");
                Log($"  > 幸福度: {current.Wellbeing:F1}%", "#888888");

                Progress.Value = 100;
                TxtStatus.Text = "预览报告生成完成";
                BtnOpenReport.IsEnabled = true;

                MessageBox.Show($"预览报告生成成功！\n\n文件：{outputPath}\n大小：{fileSize / 1024.0:F1} KB\n\n提示：如需AI润色内容，请点击 [AI生成完整报告]",
                    "生成完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log("========================================", "#FFFFFF");
                Log("[错误] 预览报告生成失败！", "#FF0000");
                Log("========================================", "#FFFFFF");
                Log($"  > 错误类型: {ex.GetType().Name}", "#FF6666");
                Log($"  > 错误信息: {ex.Message}", "#FF6666");
                Log($"  > 堆栈跟踪:", "#888888");
                foreach (var line in ex.StackTrace.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        Log($"    {line.Trim()}", "#666666");
                }
                TxtStatus.Text = "生成失败";
                MessageBox.Show($"预览报告生成失败：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnPreview.IsEnabled = true;
                BtnGenerate.IsEnabled = true;
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            Log($"  > 报告模板: 城市发展工作报告", "#888888");
            SaveConfig();

            BtnPreview.IsEnabled = false;
            BtnGenerate.IsEnabled = false;
            BtnOpenReport.IsEnabled = false;
            Progress.Value = 0;
            m_TerminalLines.Clear();
            TerminalOutput.Items.Refresh();

            var dataPath = GetEffectiveDataPath();
            var outputPath = TxtOutputPath.Text;
            var cityName = TxtCityName.Text;

            try
            {
                Log("========================================", "#FFFFFF");
                Log("[开始] 政府工作报告生成任务启动", "#00D4FF");
                Log("========================================", "#FFFFFF");

                if (m_SelectedSave != null)
                {
                    Log($"[存档] {m_SelectedSave.FolderName}", "#00D4FF");
                    Log($"  > 游戏时间: {m_SelectedSave.GameDateDisplay}", "#888888");
                    Log($"  > 人口: {m_SelectedSave.PopulationDisplay}", "#888888");
                }

                Log($"[步骤1] 读取城市数据...", "#FFFF00");
                Log($"  > 数据路径: {dataPath}", "#888888");

                var reader = new DataReader(dataPath);
                var current = reader.ReadCurrentSnapshot();
                var full = reader.ReadFullHistory();
                var history = full != null ? reader.BuildSnapshotsFromFullHistory(full) : new List<StatisticSnapshot>();

                if (current == null)
                {
                    Log("[错误] 未找到城市数据文件！", "#FF0000");
                    Log("  > 请先在游戏中使用模组导出数据", "#888888");
                    Log("  > 确认目录结构: <基础路径>/<存档名>/current_snapshot.json", "#888888");
                    MessageBox.Show("未找到城市数据。请先在游戏中使用模组导出数据。\n\n确认路径：\n" + dataPath, "数据缺失",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BtnGenerate.IsEnabled = true;
                    return;
                }

                Log($"  > 数据加载成功", "#00FF00");
                Log($"    - 人口: {current.Population:N0}", "#888888");
                Log($"    - 发展阶段: {GameMetricConverter.GetGameStageDescription(current)}", "#888888");
                Log($"    - 当前周期: 第{current.GameYear}年 第{current.GameMonth}月", "#888888");
                Log($"    - 历史数据: {history.Count} 条", "#888888");

                Log($"[步骤2] 分析城市数据...", "#FFFF00");
                var engine = new AnalysisEngine(current, history, full?.KUpdatesPerDay ?? 32);
                var analysis = engine.Analyze();
                Log($"  > 分析完成", "#00FF00");
                Log($"    - 幸福度: {analysis.Social.Wellbeing:F1}% ({GameMetricConverter.ToHappinessLevel(analysis.Social.Wellbeing)})", "#888888");
                Log($"    - 健康度: {analysis.Social.Health:F1}%", "#888888");
                Log($"    - 生活质量指数: {analysis.Social.QualityOfLifeIndex:F1}/100", "#888888");
                Log($"    - 告警事项: {analysis.Alerts.Count} 项", "#888888");

                var apiConfig = new ApiProviderConfig
                {
                    ProviderType = GetSelectedProviderType(),
                    ApiKey = TxtApiKey.Password,
                    ApiUrl = TxtApiUrl.Text,
                    Model = GetSelectedModel(),
                    ProxyUrl = TxtProxyUrl.Text
                };

                Log($"[步骤3] 配置API服务...", "#FFFF00");
                Log($"  > 提供商: {GetSelectedProviderType()}", "#888888");
                Log($"  > API URL: {apiConfig.ApiUrl}", "#888888");
                Log($"  > 模型: {apiConfig.Model}", "#888888");
                if (!string.IsNullOrEmpty(apiConfig.ProxyUrl))
                {
                    Log($"  > 代理: {apiConfig.ProxyUrl}", "#888888");
                }

                var apiService = ApiServiceFactory.Create(apiConfig);

                if (!apiConfig.IsValid)
                {
                    Log("[警告] API配置不完整，可能无法生成AI内容", "#FFFF00");
                    var result = MessageBox.Show(
                        "API Key 或 URL 未配置，AI将无法生成报告内容。\n\n是否继续？（将使用占位文本）",
                        "API 配置",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No)
                    {
                        BtnGenerate.IsEnabled = true;
                        return;
                    }
                }

                Log($"[步骤4] 调用AI生成报告内容...", "#FFFF00");
                Log($"  > 共10个章节，逐章生成中...", "#888888");

                var progressHandler = new Progress<(string chapterId, string title, int current, int total)>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var pct = (double)p.current / p.total * 100;
                        Progress.Value = pct;
                        TxtStatus.Text = $"正在生成：{p.title}（{p.current}/{p.total}）";
                        Log($"  [{p.current}/{p.total}] 正在生成: {p.title}...", "#00D4FF");
                    });
                });

                var chapters = await apiService.GenerateReportChaptersAsync(analysis, current, history, cityName, progressHandler);

                Log($"  > AI内容生成完成", "#00FF00");

                Log($"[步骤5] 生成Word文档...", "#FFFF00");
                Log($"  > 输出文件: {outputPath}", "#888888");
                Log($"  > 报告格式: 红头文件", "#888888");

                var dAnalysis = engine.AnalyzeDevelopment();
                if (dAnalysis != null)
                {
                    var gen = new DevelopmentReportGenerator(chapters, dAnalysis, current, history,
                        full?.KUpdatesPerDay ?? 32, cityName, outputPath);
                    gen.Generate();
                }

                m_LastOutputPath = outputPath;
                var fileSize = new FileInfo(outputPath).Length;

                Log("========================================", "#FFFFFF");
                Log("[成功] 政府工作报告生成完成！", "#00FF00");
                Log("========================================", "#FFFFFF");
                Log($"  > 文件路径: {outputPath}", "#888888");
                Log($"  > 文件大小: {fileSize / 1024.0:F1} KB", "#888888");
                Log($"  > 城市名称: {cityName}", "#888888");
                Log($"  > 统计周期: 第{current.GameYear}年 第{current.GameMonth}月", "#888888");
                Log($"  > 城市人口: {current.Population:N0}", "#888888");
                Log($"  > 幸福度: {current.Wellbeing:F1}%", "#888888");
                Log($"  > 发展阶段: {GameMetricConverter.GetGameStageDescription(current)}", "#888888");

                Progress.Value = 100;
                TxtStatus.Text = "报告生成完成";
                BtnOpenReport.IsEnabled = true;

                MessageBox.Show($"政府工作报告生成成功！\n\n文件：{outputPath}\n大小：{fileSize / 1024.0:F1} KB",
                    "生成完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log("========================================", "#FFFFFF");
                Log("[错误] 报告生成失败！", "#FF0000");
                Log("========================================", "#FFFFFF");
                Log($"  > 错误类型: {ex.GetType().Name}", "#FF6666");
                Log($"  > 错误信息: {ex.Message}", "#FF6666");
                Log($"  > 堆栈跟踪:", "#888888");
                foreach (var line in ex.StackTrace.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        Log($"    {line.Trim()}", "#666666");
                }
                TxtStatus.Text = "生成失败";
                MessageBox.Show($"报告生成失败：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnPreview.IsEnabled = true;
                BtnGenerate.IsEnabled = true;
            }
        }

        private void BtnOpenReport_Click(object sender, RoutedEventArgs e)
        {
            Log("[命令] 打开生成的报告文件", "#FFFF00");
            if (!string.IsNullOrEmpty(m_LastOutputPath) && File.Exists(m_LastOutputPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = m_LastOutputPath,
                    UseShellExecute = true
                });
                Log($"  > 已启动: {m_LastOutputPath}", "#00FF00");
            }
            else
            {
                Log("  > 错误: 文件不存在", "#FF0000");
            }
        }

        private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();

            var apiConfig = new ApiProviderConfig
            {
                ProviderType = GetSelectedProviderType(),
                ApiKey = TxtApiKey.Password,
                ApiUrl = TxtApiUrl.Text,
                Model = GetSelectedModel(),
                ProxyUrl = TxtProxyUrl.Text
            };

            Log("========================================", "#FFFFFF");
            Log("[测试] API连接测试开始", "#00D4FF");
            Log("========================================", "#FFFFFF");

            if (!apiConfig.IsValid)
            {
                Log("[错误] API配置不完整！", "#FF0000");
                Log("  > 请检查：API Key 和 API URL 是否填写", "#FF6666");
                MessageBox.Show("API Key 或 URL 未配置", "配置错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log($"[配置] 提供商: {apiConfig.ProviderType}", "#888888");
            Log($"[配置] API URL: {apiConfig.ApiUrl}", "#888888");
            Log($"[配置] 模型: {apiConfig.Model}", "#888888");

            try
            {
                Log("[连接] 正在建立API连接...", "#FFFF00");
                var apiService = ApiServiceFactory.Create(apiConfig);
                Log("[连接] 发送测试请求...", "#FFFF00");

                var result = await apiService.TestConnectionAsync();

                if (result.Success)
                {
                    Log("[成功] API连接测试通过！", "#00FF00");
                    Log($"  > 延迟: {result.Latency}ms", "#888888");
                    Log($"  > 模型信息: {result.ModelInfo}", "#888888");
                    Log($"  > 提示: 可点击 [ 获取模型列表 ] 查看所有可用模型", "#FFFF00");
                    MessageBox.Show($"API连接测试成功！\n\n延迟: {result.Latency}ms\n模型: {result.ModelInfo}",
                        "连接成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Log("[失败] API连接测试失败！", "#FF0000");
                    Log($"  > 错误信息: {result.ErrorMessage}", "#FF6666");
                    MessageBox.Show($"API连接测试失败：\n{result.ErrorMessage}",
                        "连接失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log("[异常] 测试过程发生异常！", "#FF0000");
                Log($"  > 异常类型: {ex.GetType().Name}", "#FF6666");
                Log($"  > 异常信息: {ex.Message}", "#FF6666");
                MessageBox.Show($"测试过程发生异常：\n{ex.Message}",
                    "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveConfig()
        {
            m_Config.DataPath = TxtDataPath.Text;
            m_Config.CityName = TxtCityName.Text;
            m_Config.Llm.ProviderType = CmbProvider.SelectedItem is ComboBoxItem item ? item.Content.ToString() : "OpenAI";
            m_Config.Llm.ApiUrl = TxtApiUrl.Text;
            m_Config.Llm.ApiKey = TxtApiKey.Password;
            m_Config.Llm.Model = GetSelectedModel();
            m_Config.Llm.ProxyUrl = TxtProxyUrl.Text;
            m_Config.SelectedSaveFolder = m_SelectedSave?.FolderPath ?? "";
            m_Config.Save(m_ConfigPath);
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "City";
            
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            // 移除前后空格，限制长度
            sanitized = sanitized.Trim();
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);
            
            return string.IsNullOrEmpty(sanitized) ? "City" : sanitized;
        }

        private void Log(string message, string color = "#00FF00")
        {
            m_TerminalLines.Add(new TerminalLine(message, color));
            TerminalOutput.Items.Refresh();
            if (TerminalOutput.Items.Count > 0)
                TerminalOutput.ScrollIntoView(TerminalOutput.Items[TerminalOutput.Items.Count - 1]);
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedItem is not ListBoxItem item) return;

            var pageName = "";
            if (item.DataContext is NavItem nav)
                pageName = nav.PageName;

            // 隐藏所有页面
            PageDataSource.Visibility = Visibility.Collapsed;
            PageReportSettings.Visibility = Visibility.Collapsed;
            PageAiConfig.Visibility = Visibility.Collapsed;
            PageGenerate.Visibility = Visibility.Collapsed;

            // 显示选中页面
            switch (pageName)
            {
                case "PageDataSource":
                    PageDataSource.Visibility = Visibility.Visible;
                    break;
                case "PageReportSettings":
                    PageReportSettings.Visibility = Visibility.Visible;
                    break;
                case "PageAiConfig":
                    PageAiConfig.Visibility = Visibility.Visible;
                    break;
                case "PageGenerate":
                    PageGenerate.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}