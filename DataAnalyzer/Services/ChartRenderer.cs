using System;
using System.Collections.Generic;
using System.Linq;
using DataAnalyzer.Models;
using ScottPlot;

namespace DataAnalyzer.Services
{
    public class ChartRenderer
    {
        private readonly List<StatisticSnapshot> m_History;
        private readonly StatisticSnapshot m_Current;
        private readonly CityAnalysisReport m_Analysis;
        private readonly string m_ChineseFontName = "Microsoft YaHei";

        public ChartRenderer(List<StatisticSnapshot> history, StatisticSnapshot current, CityAnalysisReport analysis)
        {
            m_History = history;
            m_Current = current;
            m_Analysis = analysis;
            ScottPlot.Fonts.Default = m_ChineseFontName;
        }

        private void SetChineseFonts(Plot plt)
        {
            // ScottPlot 5.x 使用全局字体设置
        }

        /// <summary>
        /// 应用图表布局优化，解决坐标轴重叠问题
        /// </summary>
        private void ApplyLayoutOptimizations(Plot plt)
        {
            // 设置图例位置为顶部右侧，避免覆盖数据
            plt.Legend.Alignment = ScottPlot.Alignment.UpperRight;
            plt.Legend.FontSize = 10;
            
            // 禁用不需要的轴，防止重复显示
            plt.Axes.Top.IsVisible = false;
            plt.Axes.Right.IsVisible = false;
        }

        /// <summary>
        /// 优化Y轴显示，自动处理大数格式
        /// </summary>
        private void OptimizeYAxis(Plot plt, bool isPercentage = false)
        {
            if (isPercentage)
            {
                // 百分比数据固定范围
                plt.Axes.Left.Min = 0;
                plt.Axes.Left.Max = 100;
            }
        }

        /// <summary>
        /// 获取游戏年月标签对列表（根据数据量动态调整间隔）
        /// 完整逻辑：强制包含首尾、均匀分布、避免重叠、边界保护
        /// </summary>
        private List<(double Position, string Label)> GetGameYearMonthLabels()
        {
            var labels = new List<(double Position, string Label)>();
            int count = m_History.Count;
            
            // 边界情况：数据太少时不显示标签
            if (count < 2) return labels;
            
            // 根据数据量动态确定最大标签数
            int maxLabels = GetOptimalLabelCount(count);
            
            // 如果数据量少于等于最大标签数，显示所有标签
            if (count <= maxLabels)
            {
                for (int i = 0; i < count; i++)
                {
                    var snap = m_History[i];
                    string label = FormatTimeLabel(snap, i);
                    labels.Add((i, label));
                }
                return labels;
            }
            
            // 核心算法：均匀分布标签，强制包含首尾
            // 确保首尾标签始终存在，中间均匀分布
            labels.Add((0, FormatTimeLabel(m_History[0], 0))); // 第一个点
            
            if (maxLabels > 2)
            {
                // 计算中间标签的步长
                double step = (count - 1) / (double)(maxLabels - 1);
                
                // 添加中间标签（不包括首尾）
                for (int i = 1; i < maxLabels - 1; i++)
                {
                    // 计算索引位置
                    int index = (int)Math.Round(i * step);
                    // 确保索引在有效范围内且不与相邻标签重叠
                    index = Math.Max(1, Math.Min(index, count - 2));
                    
                    var snap = m_History[index];
                    string label = FormatTimeLabel(snap, index);
                    labels.Add((index, label));
                }
            }
            
            // 强制添加最后一个点（确保始终显示最后一个时间）
            labels.Add((count - 1, FormatTimeLabel(m_History[count - 1], count - 1)));
            
            return labels;
        }
        
        /// <summary>
        /// 根据数据量获取最优标签数量，避免标签重叠
        /// </summary>
        private int GetOptimalLabelCount(int dataCount)
        {
            if (dataCount <= 10) return 5;      // 少量数据：最多5个标签
            if (dataCount <= 50) return 6;      // 中等数据：最多6个标签
            if (dataCount <= 100) return 7;     // 较多数据：最多7个标签
            return 8;                           // 大量数据：最多8个标签
        }
        
        /// <summary>
        /// 格式化时间标签，如果时间数据无效则使用索引作为备选
        /// </summary>
        private string FormatTimeLabel(StatisticSnapshot snap, int index)
        {
            // 如果时间数据有效（年份>0或月份>0），使用年月格式
            if (snap.GameYear > 0 || snap.GameMonth > 0)
            {
                return $"{snap.GameYear}年{snap.GameMonth}月";
            }
            // 否则使用索引作为备选标签
            return $"周期{index + 1}";
        }

        /// <summary>
        /// 根据数据量获取最优标签间隔，确保标签不重叠
        /// </summary>
        private int GetOptimalLabelInterval(int dataCount)
        {
            // 最大可见标签数设置为8，避免标签重叠
            const int maxVisibleLabels = 8;
            
            if (dataCount <= maxVisibleLabels) return 1;  // 数据量少，显示所有标签
            if (dataCount <= 16) return 2;                // 少量数据：每2个点显示一个标签
            if (dataCount <= 24) return 3;                // 中等数据：每3个点显示一个标签
            if (dataCount <= 40) return 5;                // 较多数据：每5个点显示一个标签
            if (dataCount <= 80) return 10;               // 大量数据：每10个点显示一个标签
            if (dataCount <= 160) return 20;              // 海量数据：每20个点显示一个标签
            if (dataCount <= 400) return 50;              // 超大量数据：每50个点显示一个标签
            return 80;                                    // 极端数据：每80个点显示一个标签
        }

        /// <summary>
        /// 根据数据量获取最优平滑窗口大小（增大窗口获得更平滑的曲线）
        /// </summary>
        private int GetOptimalSmoothWindow(int dataCount)
        {
            if (dataCount <= 20) return 5;     // 少量数据：中等窗口
            if (dataCount <= 50) return 9;     // 中等数据：较大窗口
            if (dataCount <= 100) return 15;   // 较多数据：大窗口
            if (dataCount <= 300) return 25;   // 大量数据：特大窗口
            return 40;                         // 海量数据：超特大窗口
        }

        /// <summary>
        /// 根据数据量决定是否需要降采样
        /// </summary>
        private (double[] xs, double[] ys) DownsampleData(double[] xs, double[] ys, int maxPoints = 200)
        {
            if (xs == null || ys == null || xs.Length <= maxPoints)
                return (xs, ys);

            int step = (int)Math.Ceiling((double)xs.Length / maxPoints);
            var newXs = new List<double>();
            var newYs = new List<double>();
            
            for (int i = 0; i < xs.Length; i += step)
            {
                newXs.Add(xs[i]);
                newYs.Add(ys[i]);
            }
            
            // 确保包含最后一个点
            if (newXs.Count > 0 && newXs[newXs.Count - 1] != xs[xs.Length - 1])
            {
                newXs.Add(xs[xs.Length - 1]);
                newYs.Add(ys[xs.Length - 1]);
            }
            
            return (newXs.ToArray(), newYs.ToArray());
        }

        private void ApplyYearMonthLabels(Plot plt)
        {
            var labels = GetGameYearMonthLabels();
            if (labels.Count < 2) return;

            // 使用 NumericManual 设置自定义标签
            var positions = labels.Select(l => l.Position).ToArray();
            var texts = labels.Select(l => l.Label).ToArray();
            
            // 设置自定义刻度生成器
            var tickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, texts);
            plt.Axes.Bottom.TickGenerator = tickGenerator;
            
            // 计算X轴范围，确保所有标签完全显示
            // 1. 获取数据的实际范围
            double dataMin = positions.Min();
            double dataMax = positions.Max();
            
            // 2. 计算边距（为标签预留空间）
            double range = dataMax - dataMin;
            double margin = range * 0.05; // 5% 的边距
            
            // 3. 设置X轴范围，确保最后一个标签完全显示
            plt.Axes.Bottom.Min = Math.Max(0, dataMin - margin);
            plt.Axes.Bottom.Max = dataMax + margin;
            
            // 4. 确保范围至少覆盖完整数据索引范围
            if (m_History.Count > 0)
            {
                plt.Axes.Bottom.Max = Math.Max(plt.Axes.Bottom.Max, m_History.Count - 1);
            }
        }

        /// <summary>
        /// 移动平均平滑算法（根据数据量动态调整窗口大小）
        /// </summary>
        private double[] SmoothData(double[] data)
        {
            if (data == null || data.Length < 3)
                return data;

            int windowSize = GetOptimalSmoothWindow(data.Length);
            var smoothed = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                int start = Math.Max(0, i - windowSize / 2);
                int end = Math.Min(data.Length - 1, i + windowSize / 2);
                double sum = 0;
                int count = 0;
                for (int j = start; j <= end; j++)
                {
                    sum += data[j];
                    count++;
                }
                smoothed[i] = sum / count;
            }
            return smoothed;
        }

        public byte[] GeneratePopulationChart()
        {
            if (m_History.Count < 2) return null;

            var plt = new Plot();
            SetChineseFonts(plt);

            var xs = Enumerable.Range(0, m_History.Count).Select(x => (double)x).ToArray();
            var pop = m_History.Select(h => (double)h.Population).ToArray();
            
            // 只保留平滑趋势曲线（隐藏标记点）
            var smoothedPop = SmoothData(pop);

            var smoothLine = plt.Add.Scatter(xs, smoothedPop);
            smoothLine.LegendText = "人口趋势";
            smoothLine.LineWidth = 2;
            smoothLine.Color = ScottPlot.Color.FromHex("#3B82F6");
            smoothLine.MarkerSize = 0;

            plt.Title("人口变化趋势");
            plt.XLabel("游戏时间");
            plt.YLabel("人口数");
            ApplyYearMonthLabels(plt);
            ApplyLayoutOptimizations(plt);
            OptimizeYAxis(plt);
            plt.ShowLegend();

            return plt.GetImage(800, 450).GetImageBytes();
        }

        public byte[] GenerateIncomeExpenseChart()
        {
            if (m_History.Count < 2) return null;

            var plt = new Plot();
            SetChineseFonts(plt);

            var xs = Enumerable.Range(0, m_History.Count).Select(x => (double)x).ToArray();
            var income = m_History.Select(h => (double)h.Income).ToArray();
            var expense = m_History.Select(h => (double)h.Expense).ToArray();
            var smoothedIncome = SmoothData(income);
            var smoothedExpense = SmoothData(expense);

            var smoothIncLine = plt.Add.Scatter(xs, smoothedIncome);
            smoothIncLine.LegendText = "收入";
            smoothIncLine.LineWidth = 2;
            smoothIncLine.Color = ScottPlot.Color.FromHex("#22C55E");
            smoothIncLine.MarkerSize = 0;

            var smoothExpLine = plt.Add.Scatter(xs, smoothedExpense);
            smoothExpLine.LegendText = "支出";
            smoothExpLine.LineWidth = 2;
            smoothExpLine.Color = ScottPlot.Color.FromHex("#EF4444");
            smoothExpLine.MarkerSize = 0;

            plt.Title("财政收支趋势");
            plt.XLabel("游戏时间");
            plt.YLabel("金额 (₡)");
            ApplyYearMonthLabels(plt);
            ApplyLayoutOptimizations(plt);
            OptimizeYAxis(plt);
            plt.ShowLegend();

            return plt.GetImage(800, 450).GetImageBytes();
        }

        public byte[] GenerateTaxChart()
        {
            if (m_History.Count < 2) return null;

            var plt = new Plot();
            SetChineseFonts(plt);

            var xs = Enumerable.Range(0, m_History.Count).Select(x => (double)x).ToArray();
            var resTax = m_History.Select(h => (double)h.ResidentialTaxableIncome).ToArray();
            var comTax = m_History.Select(h => (double)h.CommercialTaxableIncome).ToArray();
            var indTax = m_History.Select(h => (double)h.IndustrialTaxableIncome).ToArray();
            var offTax = m_History.Select(h => (double)h.OfficeTaxableIncome).ToArray();
            
            var smoothResTax = SmoothData(resTax);
            var smoothComTax = SmoothData(comTax);
            var smoothIndTax = SmoothData(indTax);
            var smoothOffTax = SmoothData(offTax);

            var smoothResLine = plt.Add.Scatter(xs, smoothResTax);
            smoothResLine.LegendText = "住宅税";
            smoothResLine.LineWidth = 2;
            smoothResLine.Color = ScottPlot.Color.FromHex("#1F4E79");
            smoothResLine.MarkerSize = 0;

            var smoothComLine = plt.Add.Scatter(xs, smoothComTax);
            smoothComLine.LegendText = "商业税";
            smoothComLine.LineWidth = 2;
            smoothComLine.Color = ScottPlot.Color.FromHex("#22C55E");
            smoothComLine.MarkerSize = 0;

            var smoothIndLine = plt.Add.Scatter(xs, smoothIndTax);
            smoothIndLine.LegendText = "工业税";
            smoothIndLine.LineWidth = 2;
            smoothIndLine.Color = ScottPlot.Color.FromHex("#F59E0B");
            smoothIndLine.MarkerSize = 0;

            var smoothOffLine = plt.Add.Scatter(xs, smoothOffTax);
            smoothOffLine.LegendText = "办公税";
            smoothOffLine.LineWidth = 2;
            smoothOffLine.Color = ScottPlot.Color.FromHex("#3B82F6");
            smoothOffLine.MarkerSize = 0;

            plt.Title("税收结构趋势");
            plt.XLabel("游戏时间");
            plt.YLabel("税收金额 (₡)");
            ApplyYearMonthLabels(plt);
            ApplyLayoutOptimizations(plt);
            OptimizeYAxis(plt);
            plt.ShowLegend();

            return plt.GetImage(800, 450).GetImageBytes();
        }

        public byte[] GenerateSectorChart()
        {
            if (m_History.Count < 2) return null;

            var plt = new Plot();
            SetChineseFonts(plt);

            var xs = Enumerable.Range(0, m_History.Count).Select(x => (double)x).ToArray();
            var service = m_History.Select(h => (double)h.ServiceWealth).ToArray();
            var processing = m_History.Select(h => (double)h.ProcessingWealth).ToArray();
            var office = m_History.Select(h => (double)h.OfficeWealth).ToArray();
            
            var smoothService = SmoothData(service);
            var smoothProcessing = SmoothData(processing);
            var smoothOffice = SmoothData(office);

            var smoothServiceLine = plt.Add.Scatter(xs, smoothService);
            smoothServiceLine.LegendText = "服务业";
            smoothServiceLine.LineWidth = 2;
            smoothServiceLine.Color = ScottPlot.Color.FromHex("#3B82F6");
            smoothServiceLine.MarkerSize = 0;

            var smoothProcessingLine = plt.Add.Scatter(xs, smoothProcessing);
            smoothProcessingLine.LegendText = "加工业";
            smoothProcessingLine.LineWidth = 2;
            smoothProcessingLine.Color = ScottPlot.Color.FromHex("#F59E0B");
            smoothProcessingLine.MarkerSize = 0;

            var smoothOfficeLine = plt.Add.Scatter(xs, smoothOffice);
            smoothOfficeLine.LegendText = "办公业";
            smoothOfficeLine.LineWidth = 2;
            smoothOfficeLine.Color = ScottPlot.Color.FromHex("#1F4E79");
            smoothOfficeLine.MarkerSize = 0;

            plt.Title("产业结构趋势");
            plt.XLabel("游戏时间");
            plt.YLabel("产业财富 (₡)");
            ApplyYearMonthLabels(plt);
            ApplyLayoutOptimizations(plt);
            OptimizeYAxis(plt);
            plt.ShowLegend();

            return plt.GetImage(800, 450).GetImageBytes();
        }

        public byte[] GenerateTransportChart()
        {
            var t = m_Analysis.Transport;

            if (t.TotalPassengers <= 0) return null;

            var plt = new Plot();
            SetChineseFonts(plt);

            var values = new double[]
            {
                t.Bus.Passengers,
                t.Subway.Passengers,
                t.Tram.Passengers,
                t.Train.Passengers,
                t.Taxi.Passengers,
                t.Airplane.Passengers,
                t.Ship.Passengers
            };

            var bars = plt.Add.Bars(values);
            bars.Color = ScottPlot.Color.FromHex("#1F4E79");

            double[] positions = { 0, 1, 2, 3, 4, 5, 6 };
            string[] labels = {
                $"公交\n{t.Bus.Share:F1}%",
                $"地铁\n{t.Subway.Share:F1}%",
                $"电车\n{t.Tram.Share:F1}%",
                $"火车\n{t.Train.Share:F1}%",
                $"出租\n{t.Taxi.Share:F1}%",
                $"飞机\n{t.Airplane.Share:F1}%",
                $"轮船\n{t.Ship.Share:F1}%"
            };

            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, labels);
            plt.Axes.Bottom.Label.Text = "交通方式";

            plt.Title("公共交通客流分布");
            plt.YLabel("客运量 (人次)");
            
            ApplyLayoutOptimizations(plt);
            OptimizeYAxis(plt);

            return plt.GetImage(800, 450).GetImageBytes();
        }

        public byte[] GenerateWellbeingChart()
        {
            if (m_History.Count < 2) return null;

            var plt = new Plot();
            SetChineseFonts(plt);

            var xs = Enumerable.Range(0, m_History.Count).Select(x => (double)x).ToArray();
            
            var wellbeing = m_History.Select(h => ComputeCompositeHappiness(h)).ToArray();
            var health = m_History.Select(h => ComputeCompositeHealth(h)).ToArray();
            var smoothedWellbeing = SmoothData(wellbeing);
            var smoothedHealth = SmoothData(health);

            var smoothWLine = plt.Add.Scatter(xs, smoothedWellbeing);
            smoothWLine.LegendText = "综合幸福指数";
            smoothWLine.LineWidth = 2;
            smoothWLine.Color = ScottPlot.Color.FromHex("#22C55E");
            smoothWLine.MarkerSize = 0;

            var smoothHLine = plt.Add.Scatter(xs, smoothedHealth);
            smoothHLine.LegendText = "综合健康指数";
            smoothHLine.LineWidth = 2;
            smoothHLine.Color = ScottPlot.Color.FromHex("#3B82F6");
            smoothHLine.MarkerSize = 0;

            plt.Title("社会幸福度与健康趋势");
            plt.XLabel("游戏时间");
            plt.YLabel("综合指数 (满分100)");
            ApplyYearMonthLabels(plt);
            ApplyLayoutOptimizations(plt);
            OptimizeYAxis(plt, isPercentage: true);
            plt.ShowLegend();

            return plt.GetImage(800, 450).GetImageBytes();
        }

        private double ComputeCompositeHappiness(StatisticSnapshot snap)
        {
            var pop = snap.Population;
            var adults = snap.AdultsCount;
            var workers = snap.WorkerCount;
            
            var employmentRate = pop > 0 && workers > 0 ? Math.Min((double)workers / pop * 100, 100) : 0;
            var educationRate = adults > 0 ? (double)snap.EducationCount / adults * 100 : 0;
            var homelessRate = pop > 0 ? (double)snap.HomelessCount / pop * 100 : 0;

            var baseWellbeing = Math.Min(snap.Wellbeing, 100) / 100.0;
            var employmentScore = Math.Min(employmentRate, 100) / 100.0;
            var crimeScore = Math.Max(0, (50 - snap.CrimeRate) / 50.0);
            var educationScore = Math.Min(educationRate, 100) / 100.0;
            var homelessScore = Math.Max(0, (10 - homelessRate) / 10.0);

            var happinessIndex = (baseWellbeing * 0.35 + 
                                  employmentScore * 0.25 + 
                                  crimeScore * 0.20 + 
                                  educationScore * 0.15 + 
                                  homelessScore * 0.05) * 100;

            return Math.Max(0, Math.Min(happinessIndex, 100));
        }

        private double ComputeCompositeHealth(StatisticSnapshot snap)
        {
            var pop = snap.Population;
            var adults = snap.AdultsCount;
            
            var educationRate = adults > 0 ? (double)snap.EducationCount / adults * 100 : 0;
            var homelessRate = pop > 0 ? (double)snap.HomelessCount / pop * 100 : 0;
            var naturalGrowth = snap.NaturalGrowthCount;

            var baseHealth = Math.Min(snap.Health, 100) / 100.0;
            var growthScore = Math.Max(0, Math.Min((naturalGrowth + 50) / 100.0, 1));
            var homelessScore = Math.Max(0, (10 - homelessRate) / 10.0);
            var educationScore = Math.Min(educationRate, 100) / 100.0;
            var serviceScore = snap.CityServiceWorkers > 0 ? 
                Math.Min((double)snap.CityServiceWorkers / Math.Max(snap.CityServiceMaxWorkers, 1), 1) : 0;

            var healthIndex = (baseHealth * 0.40 + 
                               growthScore * 0.20 + 
                               homelessScore * 0.15 + 
                               educationScore * 0.15 + 
                               serviceScore * 0.10) * 100;

            return Math.Max(0, Math.Min(healthIndex, 100));
        }
    }
}