using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace analysis
{
    public class Mod : IMod
    {
        public static ILog Log = LogManager.GetLogger(nameof(analysis)).SetShowsErrorsInUI(false);
        public static Mod Instance { get; private set; }

        private Setting m_Setting;

        public AnalysisSystem System { get; internal set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                Log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            AssetDatabase.global.LoadSettings(nameof(analysis), m_Setting, new Setting(this));

            updateSystem.UpdateAt<AnalysisSystem>(SystemUpdatePhase.PostSimulation);

            Log.Info("City Analysis Data mod loaded. Use Options -> City Analysis Data to export.");
        }

        public void OnDispose()
        {
            Instance = null;
            Log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}