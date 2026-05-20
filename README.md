# 🏙️ 都市天际线II — 城市数据分析与报告生成系统

> **Cities: Skylines II — City Data Analysis & Government Report Generator**
>
> A complete data pipeline for CS2: an in-game Mod collects city statistics over time, and a WPF desktop app generates government-style Word reports with charts and optional AI-powered narrative analysis.

---

## ✨ 功能亮点

- 📊 **双模板报告** — 模板A《城市发展工作报告》（建市以来全历程）、模板B《城市季度运行分析报告》（同比+环比）
- 🤖 **6种AI接入** — OpenAI / DeepSeek / SiliconFlow / Ollama / Azure OpenAI / 自定义API
- 📈 **自动图表生成** — 人口、经济、交通、教育等多维度 ScottPlot 图表
- 🕐 **精确时间映射** — 游戏内日期与采样点精确对应
- 📋 **政府报告视角** — 红头文件格式、章节结构、数据表格、预警评分

---

## 📦 项目组成

| 组件 | 类型 | 框架 | 说明 |
|------|------|------|------|
| `analysis/` | 游戏Mod | .NET Framework 4.8 | 游戏内运行，定时采集70+项城市指标并导出JSON |
| `DataAnalyzer/` | Windows桌面应用 | .NET 8.0 + WPF | 读取数据，生成含图表的Word政府工作报告 |

---

## 🚀 快速开始

### 安装 Mod

1. 使用 CSII Modding SDK 编译 `analysis/` 项目
2. 将编译产物放入游戏 Mod 目录，或通过 Paradox Mods 上架安装
3. 游戏内 Mod 会自动在 `<文档>/Cities Skylines II/analysis/` 下按城市名创建数据文件夹

### 使用分析软件

1. 安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime)
2. 从 [Releases](https://github.com/StaLincon/CS2-CityAnalysis/releases) 下载并解压
3. 将 `appsettings.example.json` 复制为 `appsettings.json`，填入你的 API Key
4. 运行 `DataAnalyzer.exe`

### 配置说明

```json
{
  "dataPath": "",
  "cityName": "My City",
  "selectedSaveFolder": "",
  "llm": {
    "providerType": "OpenAI",
    "apiKey": "",
    "apiUrl": "https://api.openai.com/v1/chat/completions",
    "model": "gpt-4o",
    "proxyUrl": "",
    "enabled": false
  }
}
```

| 字段 | 说明 |
|------|------|
| `dataPath` | 游戏数据目录，留空则自动定位 |
| `providerType` | AI提供商：`OpenAI` / `DeepSeek` / `SiliconFlow` / `Ollama` / `Azure` / `Custom` |
| `apiKey` | 对应平台的 API Key |
| `apiUrl` | API 端点地址 |
| `model` | 使用的模型名称 |
| `proxyUrl` | 代理地址（可选） |
| `enabled` | 是否启用 AI 分析（关闭则仅生成预览报告） |

---

## 📋 报告模板

### 模板A — 城市发展工作报告

自建市以来的全历程发展报告，包含10个章节：

1. 城市发展综述
2. 人口发展与结构演变
3. 经济发展与财政运行
4. 产业结构与转型升级
5. 就业与民生保障
6. 交通基础设施与出行
7. 社会事业与公共服务
8. 城市治理与安全
9. 居民生活与住房保障
10. 发展展望与战略建议

### 模板B — 城市季度运行分析报告

季度维度的运行分析，核心指标速览 + 同比/环比对比，包含10个章节：

1. 季度运行综述
2. 人口与劳动力
3. 经济运行与财政收支
4. 产业结构与运行
5. 就业与收入
6. 交通运行与基础设施
7. 社会事业与公共服务
8. 城市安全与治理
9. 居民生活与住房
10. 下季度展望与建议

---

## 🏗️ 架构概览

```
游戏引擎 → StatisticCollector → StatisticSnapshot → JSON文件
                                                         │
                                                    DataReader
                                                         │
                                                   AnalysisEngine
                                                    │          │
                                    ┌───────────────┼───────────────┐
                                    │               │               │
                          PreviewReportGen   DevelopmentReportGen  QuarterlyReportGen
                          (无需API)          (模板A+AI)             (模板B+AI)
                                    │               │               │
                                    └───────────────┼───────────────┘
                                                    │
                                              .docx 报告文件
```

---

## 📊 采集指标

Mod 采集 70+ 项城市指标，覆盖：

| 类别 | 示例指标 |
|------|---------|
| 人口 | 总人口、迁入/迁出、出生率/死亡率 |
| 财政 | 资金、收入、支出、贸易 |
| 社会 | 幸福度、健康度、犯罪率、无家可归 |
| 交通 | 公交/地铁/火车/电车/飞机/出租/轮船 客流量 |
| 产业 | 服务业/加工业/办公 财富与就业 |
| 其他 | 旅游、邮政、教育、房地产、福利 |

---

## 🛠️ 技术栈

| 组件 | 技术 |
|------|------|
| Mod | Cities: Skylines II Modding API (.NET Framework 4.8) |
| 桌面应用 | .NET 8.0 + WPF |
| 图表 | ScottPlot 5.x |
| Word文档 | DocumentFormat.OpenXml |
| 图像渲染 | SkiaSharp + HarfBuzzSharp |
| JSON | System.Text.Json |

---

## 📂 项目结构

```
├── analysis/                          # 游戏Mod源码
│   ├── Mod.cs                         # Mod入口
│   ├── AnalysisSystem.cs              # 核心采集系统
│   ├── Setting.cs                     # Mod设置
│   └── Data/
│       ├── StatisticSnapshot.cs       # 快照数据模型
│       ├── StatisticCollector.cs      # 数据采集器
│       ├── SnapshotSerializer.cs      # 序列化
│       └── DataAggregator.cs          # 数据聚合
│
├── DataAnalyzer/                      # Windows应用源码
│   ├── MainWindow.xaml(.cs)           # 主窗口
│   ├── Models/
│   │   ├── StatisticSnapshot.cs       # 快照模型
│   │   ├── FullHistory.cs             # 历史数据模型
│   │   ├── AnalysisModels.cs          # 分析报告模型
│   │   ├── ReportModels.cs            # 报告模板模型
│   │   └── AppConfig.cs              # 配置模型
│   └── Services/
│       ├── AnalysisEngine(.Extensions).cs  # 数据分析引擎
│       ├── ChartRenderer.cs                # 图表渲染
│       ├── DevelopmentReportGenerator.cs   # 模板A生成器
│       ├── QuarterlyReportGenerator.cs     # 模板B生成器
│       ├── DevelopmentPrompts.cs           # 模板A提示词
│       ├── QuarterlyPrompts.cs             # 模板B提示词
│       ├── LlmService.cs                   # LLM调度
│       └── *ApiService.cs                  # 6种API适配器
│
└── DataAnalyzer.sln                   # 解决方案文件
```

---

## 📄 许可证

MIT License

---

## 🙏 致谢

- [Cities: Skylines II](https://www.paradoxinteractive.com/games/cities-skylines-ii) by Colossal Order / Paradox Interactive
- [ScottPlot](https://scottplot.net/) — .NET 图表库
- [Open XML SDK](https://github.com/OfficeDev/Open-XML-SDK) — Word 文档生成
