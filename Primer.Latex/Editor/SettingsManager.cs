using LatexRenderer;
using UnityEditor.Callbacks;
using UnityEditor.SettingsManagement;

namespace UnityEditor.LatexRenderer.UserSettings
{
    internal static class Manager
    {
        private static Settings _instance;

        internal static Settings Instance
        {
            get
            {
                if (_instance is null) Initialize();

                return _instance;
            }
        }

        private static void Initialize()
        {
            _instance = new Settings("org.primerlearning.latex-renderer");
            _instance.afterSettingsSaved += SyncPaths;
            SyncPaths();
        }

        [DidReloadScripts]
        private static void SyncPaths()
        {
            LatexToSvgConverter.LatexExecutablePath = Instance.Get<string>(
                "general.xelatexExecutablePath", SettingsScope.User);
            LatexToSvgConverter.DvisvgmExecutablePath = Instance.Get<string>(
                "general.dvisvgmExecutablePath", SettingsScope.User);
        }
    }
}