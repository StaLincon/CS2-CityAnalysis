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
    [SettingsUIGroupOrder(kButtonGroup, kMainGroup)]
    [SettingsUIShowGroupName(kButtonGroup, kMainGroup)]
    public class Setting : ModSetting
    {
        public const string kMainGroup = "Main";
        public const string kButtonGroup = "Export";

        public Setting(IMod mod) : base(mod)
        {
        }

        [SettingsUISection(kMainGroup, kMainGroup)]
        public bool ExportData { get; set; }

        [SettingsUIButton]
        [SettingsUISection(kButtonGroup, kButtonGroup)]
        public bool ExportButton
        {
            set
            {
                Mod.Instance?.System?.ExportData();
            }
        }

        public override void SetDefaults()
        {
            ExportData = false;
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
                { m_Setting.GetSettingsLocaleID(), "City Analysis Data" },
                { m_Setting.GetOptionTabLocaleID(Setting.kMainGroup), "Main" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Data Export" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportButton)), "Export City Data" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ExportButton)), "Export all city statistics and full history to data files" },
            };
        }

        public void Unload()
        {
        }
    }
}