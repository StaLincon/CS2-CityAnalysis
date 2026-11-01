# Cities: Skylines II 1.6.2f1 compatibility fix

The published mod loads on 1.6.2f1, but successful export does not mean the exported values are correct. The original publishing metadata still targets `1.0.*`. Issues #1 and #2 report version compatibility and zero-valued fields.

This patch discovers and deduplicates statistic parameters from the game's StatisticsData/StatisticParameterData prefabs, sums the parameter series, and converts trade resource quantities to market values following StatisticsUISystem. It divides wellbeing and health totals by the corresponding population sample, aligns shorter histories at the newest sample, disposes temporary NativeArrays and completes statistics writers before reading. Existing JSON field names and integer types are preserved; large results saturate at the existing int limits.

Dates use the TimeUISystem epoch (starting year, date offset and time offset), with signed frame differences to prevent a pre-founding initial sample from underflowing. Months scale to twelve months even with custom simulation days per year. `gameDay` remains the 1-based simulation day within the year, matching the existing history field convention. The game API's GetCurrentDateTime represents simulation days as January dates and is not suitable for report month labels.

The download button previously referenced ToolDownloader, which is absent from the repository; it now opens the upstream Releases page directly. Publishing metadata recommends game 1.6.2 and mod version 1.0.1.

## Validation

- Built all mod sources against the installed Steam game 1.6.2f1 (767.21d1), build 25127643, using its actual managed references.
- Ten regression checks cover pre-founding frames, year rollover, non-January epochs, custom year lengths, frame wrap, real exported wellbeing/health totals, empty population, history alignment and integer saturation.
- Existing installed-build logs confirm successful loading/export on 1.6.2f1. The compatibility build has **not yet been loaded in game**; the game was already running during preparation. Prefab aggregation and replacement loading still require an in-game smoke test. Older versions are untested.
- No game assemblies, personal exports, saves or credentials are included in the contribution.

## Local build

Run from this directory in PowerShell with an installed .NET SDK:

```powershell
.\build-local.ps1 -GameDir 'E:\SteamLibrary\steamapps\common\Cities Skylines II'
.\test-local.ps1
```

The DLL is written to `release-artifacts/CityAnalysis/analysis.dll`. The normal Modding SDK project remains available for official publishing. Only the original publisher can release an update to mod 144817.

## 本机替换与恢复

1. 正常保存并退出游戏。
2. 在此目录打开 PowerShell，运行 `powershell -ExecutionPolicy Bypass -File .\install-local.ps1`。脚本仅替换已订阅 Mod 的 `analysis.dll`；会先备份原 DLL 和已有导出数据到此目录的 `release-artifacts/backups/`，不修改游戏存档。
3. 启动游戏并加载城市，在“选项 → City Analysis → Export City Data”重新导出。旧 JSON 不会自动修复。
4. 日志 `Logs/analysis.log` 应出现 `compatibility build 1.0.1 (game 1.6.2f1)`。核对导出日期、幸福度/健康度和财政分类数据；真实没有活动的指标仍可为零。
5. 若需恢复，退出游戏后运行 `powershell -ExecutionPolicy Bypass -File .\install-local.ps1 -Restore`。恢复只还原 Mod DLL；备份的导出数据可手动查看。

当前默认安装路径：`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\.cache\Mods\pdx_mods\144817_1\analysis.dll`。若订阅目录版本改变，用 `-ModDirectory` 指定新目录。Paradox Mods 同步、校验或更新可能覆盖本地替换；重新运行安装脚本即可。保留原订阅启用，不要同时加载第二份同名程序集。此方式用于本地验证，作者发布后应恢复原版并通过订阅更新。
