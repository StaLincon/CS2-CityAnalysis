# 都市天际线II - 城市数据分析与报告生成系统

## 项目概述

本项目为一套完整的 **Cities: Skylines II** 城市数据采集、分析与报告生成系统，由两大部分组成：

| 组件 | 类型 | 框架 | 说明 |
|------|------|------|------|
| `analysis/` | **游戏Mod** | .NET Framework 4.8 | 在游戏内运行，采集城市统计数据并导出JSON |
| `DataAnalyzer/` | **Windows桌面应用** | .NET 8.0 + WPF | 读取导出的数据，生成含图表的Word报告 |

---

## 项目文件结构

```
./
├── analysis.sln                          # 解决方案文件
├── analysis/                             # 【游戏Mod】Cities: Skylines II 数据采集插件
│   ├── analysis.csproj                   # .NET Framework 4.8 项目文件
│   ├── Mod.cs                            # Mod入口，实现IMod接口
│   ├── AnalysisSystem.cs                 # 核心数据采集系统，定时采集快照与导出
│   ├── Setting.cs                        # Mod设置面板配置
│   ├── Properties/
│   │   ├── PublishProfiles/              # 发布配置文件
│   │   ├── PublishConfiguration.xml      # Mod发布元数据
│   │   └── Thumbnail.png                 # Mod缩略图
│   └── Data/
│       ├── StatisticSnapshot.cs          # 快照数据模型（约70+字段）
│       ├── StatisticCollector.cs         # 游戏数据采集器
│       ├── SnapshotSerializer.cs         # JSON序列化/反序列化
│       └── DataAggregator.cs             # 数据聚合器
│
├── DataAnalyzer/                         # 【Windows应用】数据分析与报告生成器
│   ├── DataAnalyzer.csproj               # .NET 8.0 WPF 项目文件
│   ├── App.xaml / App.xaml.cs            # WPF 应用入口
│   ├── MainWindow.xaml                   # 主窗口界面布局
│   ├── MainWindow.xaml.cs                # 主窗口逻辑（存档管理/UI交互/报告生成流程）
│   ├── appsettings.json                  # 应用配置文件
│   ├── Models/
│   │   ├── StatisticSnapshot.cs          # 快照数据模型（应用版本，与Mod同步）
│   │   ├── FullHistory.cs                # 全量历史数据模型（列式存储）
│   │   ├── AnalysisModels.cs             # AI分析报告模型（章节结构）
│   │   ├── ReportModels.cs               # 报告模板模型（发展报告/季度报告）
│   │   ├── AppConfig.cs                  # 应用配置（LLM/数据路径）
│   │   └── SaveRecord.cs                 # 存档记录（列表展示用）
│   └── Services/
│       ├── SaveFolderManager.cs          # 存档文件夹扫描器
│       ├── DataReader.cs                 # JSON数据读取器
│       ├── GameMetricConverter.cs        # 游戏指标单位转换
│       ├── AnalysisEngine.cs             # 数据分析引擎（核心）
│       ├── AnalysisEngine.Extensions.cs  # 分析引擎扩展（发展/季度分析）
│       ├── ChartRenderer.cs              # ScottPlot图表渲染器
│       ├── DevelopmentReportGenerator.cs # 模板A：城市发展工作报告生成器
│       ├── QuarterlyReportGenerator.cs   # 模板B：季度运行分析报告生成器
│       ├── WordReportGenerator.cs        # AI完整报告Word生成器
│       ├── PreviewReportGenerator.cs     # 无AI预览报告Word生成器
│       ├── DevelopmentPrompts.cs         # 模板A SystemPrompt + 10章Prompt
│       ├── QuarterlyPrompts.cs           # 模板B SystemPrompt + 10章Prompt
│       ├── PromptTemplates.cs            # 通用AI提示词模板
│       ├── LlmService.cs                 # LLM服务调度器
│       ├── IApiService.cs                # API服务接口
│       ├── BaseApiService.cs             # API基类
│       ├── OpenAiApiService.cs           # OpenAI API适配器
│       ├── AzureOpenAiApiService.cs      # Azure OpenAI适配器
│       ├── DeepSeekApiService.cs         # DeepSeek API适配器
│       ├── OllamaApiService.cs           # Ollama本地模型适配器
│       ├── CustomApiService.cs           # 自定义API适配器
│       ├── SiliconFlowApiService.cs      # SiliconFlow API适配器
│       ├── ApiServiceFactory.cs          # API服务工厂
│       └── ImageHelper.cs                # 图表图片辅助处理
│
└── Cities-Skylines-2-Modding-Guide/      # 游戏Mod开发API参考文档
```

---

## 架构设计

### 1. Mod端 (`analysis/`)

```
┌───────────────────────────────────────────────┐
│                   Mod.cs                       │
│          (IMod 接口实现，入口点)                │
│   - OnLoad: 注册AnalysisSystem到游戏更新循环     │
│   - OnDispose: 清理资源                        │
│   - Setting: 提供设置面板                       │
└──────────────────┬────────────────────────────┘
                   │
┌──────────────────▼────────────────────────────┐
│              AnalysisSystem.cs                 │
│        (GameSystemBase 派生，核心引擎)          │
│                                                │
│  ┌──────────────────────────┐                 │
│  │  OnUpdate() - 定时采集    │  每1024 tick   │
│  │   → StatisticCollector   │  触发一次快照   │
│  └──────────────────────────┘                 │
│                                                │
│  ┌──────────────────────────┐                 │
│  │  ExportData() - 导出数据  │  用户触发      │
│  │   → 保存快照 + 历史       │                 │
│  └──────────────────────────┘                 │
│                                                │
│  ┌──────────────────────────┐                 │
│  │  CheckForSaveChange()    │  定期检测       │
│  │   → 存档切换自动创建      │  城市名称变化   │
│  │     新文件夹              │                 │
│  └──────────────────────────┘                 │
└──────────────────┬────────────────────────────┘
                   │
┌──────────────────▼────────────────────────────┐
│           StatisticCollector.cs                │
│      (游戏CityStatisticsSystem封装)            │
│                                                │
│  采集维度:                                     │
│  - 人口/迁移/出生/死亡                         │
│  - 财政/收支/贸易                              │
│  - 幸福度/健康度/犯罪率                        │
│  - 交通(公交/地铁/火车/电车/飞机/出租/轮船)    │
│  - 产业(服务业/加工业/办公)                    │
│  - 旅游/邮政/教育/房地产/就业                  │
└──────────────────┬────────────────────────────┘
                   │
                   ▼
         数据导出至磁盘
┌──────────────────────────────────────────────┐
│  存储目录: <文档>/Cities Skylines II/analysis/ │
│                                                │
│  ├── 城市名称A/                                │
│  │   ├── current_snapshot.json  (最新快照)     │
│  │   └── full_history.json      (全量历史)     │
│  ├── 城市名称B/                                │
│  │   ├── current_snapshot.json                 │
│  │   └── full_history.json                     │
│  └── ...                                       │
└──────────────────────────────────────────────┘
```

### 2. 应用端 (`DataAnalyzer/`)

```
┌───────────────────────────────────────────────┐
│               MainWindow.xaml                  │
│            (WPF 主窗口，界面层)                 │
│                                                │
│  ┌─────────────────────────────────────────┐  │
│  │  数据配置区                              │  │
│  │  - 数据路径 / 城市名称                   │  │
│  │  - [刷新存档列表] 按钮                   │  │
│  │  - 存档列表 (ListView)                   │  │
│  ├─────────────────────────────────────────┤  │
│  │  LLM配置区                               │  │
│  │  - 提供商选择 / API Key / URL / Model    │  │
│  ├─────────────────────────────────────────┤  │
│  │  操作区                                  │  │
│  │  - [生成预览报告] (无需API)              │  │
│  │  - [AI生成完整报告] (需要API)            │  │
│  │  - 输出路径选择                          │  │
│  ├─────────────────────────────────────────┤  │
│  │  终端输出                                │  │
│  │  - 实时日志 (类似终端风格)                │  │
│  └─────────────────────────────────────────┘  │
└───────┬───────────────────┬───────────────────┘
        │                   │
        ▼                   ▼
┌──────────────┐   ┌──────────────────────────────┐
│ SaveFolder   │   │     报告生成流程               │
│ Manager      │   │                              │
│              │   │  1. DataReader 读取JSON       │
│ - 多存档扫描  │   │  2. AnalysisEngine 分析数据   │
│ - 扁平模式    │   │  3. ChartRenderer 生成图表    │
└──────────────┘   │  4. 选择生成器:               │
                   │     ├─ PreviewReportGenerator │
                   │     │  (模板文本，无需API)      │
                   │     └─ WordReportGenerator    │
                   │        (AI生成，需要LLM)       │
                   │  5. 输出 .docx 文件            │
                   └──────────────────────────────┘
```

### 3. 数据流

```
游戏引擎 → StatisticCollector → StatisticSnapshot → JSON文件
                                                         │
                                                    ┌────▼────┐
                                                    │ DataReader│
                                                    └────┬────┘
                                                         │
                                          ┌──────────────▼──────────────┐
                                          │     AnalysisEngine          │
                                          │  生成 CityAnalysisReport    │
                                          └──────────────┬──────────────┘
                                                         │
                              ┌──────────────────────────┼──────────────────────────┐
                              │                          │                          │
                    ┌─────────▼─────────┐    ┌──────────▼──────────┐    ┌──────────▼──────────┐
                    │ PreviewReportGen   │    │ WordReportGenerator │    │   ChartRenderer     │
                    │ (模板文本+图表)     │    │ (LLM分析+图表)      │    │   (ScottPlot ⠡)    │
                    └─────────┬─────────┘    └──────────┬──────────┘    └──────────┬──────────┘
                              │                          │                          │
                              └──────────────────────────┼──────────────────────────┘
                                                         │
                                                         ▼
                                                   .docx 报告文件
```

---

## 数据模型详解

### StatisticSnapshot（快照数据）
单次采样的完整城市状态，包含70+个字段，覆盖：

| 类别 | 字段 |
|------|------|
| 时间 | `RealTime`, `GameTick`, `GameDay`, `GameMonth`, `GameYear`, `SampleCount` |
| 人口 | `Population`, `CitizensMovedIn`, `CitizensMovedAway`, `BirthRate`, `DeathRate` |
| 财政 | `Money`, `Income`, `Expense`, `Trade` |
| 社会 | `AverageHappiness`, `AverageHealth`, `HomelessCount`, `CrimeRate` |
| 就业 | `WorkerCount`, `Unemployed` |
| 旅游 | `TouristCount`, `TouristIncome`, `LodgingUsed`, `Attractiveness` |
| 交通 | 公交/地铁/火车/电车/飞机/出租/轮船 客流量，货运量 |
| 税收 | 住宅/商业/工业/办公 应税收入 |
| 产业 | 服务业/加工业/办公 财富/数量/工人 |
| 其他 | 教育/邮政/房地产/福利/健康等级 |

### FullHistory（历史数据）
采用**列式存储**结构，每个指标是一个独立列表，适合高效的时间序列分析。

### CityAnalysisReport（分析报告）
面向报告的结构化数据模型，包含10个分析章节：
- **CityOverview** - 城市概览
- **DemographicAnalysis** - 人口统计
- **EconomicAnalysis** - 经济分析
- **SectorAnalysis** - 产业结构
- **EmploymentAnalysis** - 就业分析
- **TransportAnalysis** - 交通分析
- **SocialAnalysis** - 社会指标
- **FiscalAnalysis** - 财政分析
- **HouseholdAnalysis** - 家庭分析
- **TrendSummary** - 趋势总结
- **Alerts** - 预警列表
- **Scores** - 评分卡

---

## 关键设计特性

### 1. 双模式报告生成
| 模式 | 类 | 依赖 | 内容特点 |
|------|-----|------|---------|
| **预览模式** | `PreviewReportGenerator` | 无外部依赖 | 模板文本 + 数据表格 + 图表 |
| **AI模式** | `WordReportGenerator` | LLM API | AI分析文本 + 数据表格 + 图表 |

两种模式共享相同的：
- 红头文件格式（红色标题 + 页眉红线）
- 数据表格结构
- ChartRenderer 生成的图表

### 2. 多LLM支持
通过 `IApiService` 接口 + `ApiServiceFactory` 工厂模式支持：
- **OpenAI** (GPT-4o 等)
- **Azure OpenAI**
- **DeepSeek**
- **Ollama** (本地模型)
- **SiliconFlow** (硅基流动)
- **自定义API** (兼容 OpenAI 格式)

### 3. 存档自动分类
- **Mod端**：获取城市名称，按存档创建独立子文件夹，存档切换时自动检测并创建新目录
- **应用端**：`SaveFolderManager` 自动扫描子文件夹结构，同时兼容扁平模式（当前数据）

### 4. 智能图表渲染
- **自适应标签**：根据数据量动态调整X轴标签间隔
- **平滑曲线**：移动平均算法，自动选择窗口大小
- **降采样**：数据量 > 200点时自动降采样
- **中文支持**：全局设置 Microsoft YaHei 字体

---

## 技术栈

| 组件 | 技术 |
|------|------|
| Mod框架 | Cities: Skylines II Modding API (.NET Framework 4.8) |
| 桌面应用 | .NET 8.0 + WPF |
| 图表 | ScottPlot 5.x |
| Word文档 | DocumentFormat.OpenXml (Open XML SDK) |
| 图像 | SkiaSharp + HarfBuzzSharp |
| HTTP | HttpClient (System.Net.Http) |
| JSON | System.Text.Json |

---

## 构建与部署

### Mod 编译
```powershell
cd analysis
dotnet build -c Debug     # 开发版本 → bin/Debug/net48/
dotnet build -c Release   # 发布版本 → bin/Release/net48/
```

### 应用编译
```powershell
cd DataAnalyzer
dotnet build -c Debug     # → bin/Debug/net8.0-windows/
```

### Mod 部署（使用符号链接）
```powershell
cmd /c mklink /J "%LocalAppData%Low\Colossal Order\Cities Skylines II\Mods\analysis" "编译输出目录"
```

---

## 生成日期
2026-05-20
