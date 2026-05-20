using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;

namespace analysis
{
    [FileLocation(nameof(analysis))]
    [SettingsUIGroupOrder(kMainGroup)]
    [SettingsUIShowGroupName(kMainGroup)]
    public class Setting : ModSetting
    {
        public const string kMainGroup = "Main";

        private ToolDownloader m_Downloader;

        public Setting(IMod mod) : base(mod)
        {
            m_Downloader = new ToolDownloader();
        }

        [SettingsUIButton]
        [SettingsUISection(kMainGroup, kMainGroup)]
        public bool ExportButton
        {
            set
            {
                Mod.Instance?.System?.ExportData();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(kMainGroup, kMainGroup)]
        public bool DownloadToolButton
        {
            set
            {
                m_Downloader.OpenDownloadPage();
            }
        }

        public override void SetDefaults()
        {
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "City Analysis" },
                { m_Setting.GetOptionTabLocaleID(Setting.kMainGroup), "Tools" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kMainGroup), "City Analysis Tools" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportButton)), "Export City Data" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ExportButton)), "Export city statistics and history data" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DownloadToolButton)), "Download Analysis Tool" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DownloadToolButton)), "Get the tool to generate government reports" },
            };
        }

        public void Unload()
        {
        }
    }
}
