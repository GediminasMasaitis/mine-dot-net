using System.ComponentModel;

namespace MineDotNet.GUI
{
    public static class Designer
    {
        public static bool IsRuntime => LicenseManager.UsageMode == LicenseUsageMode.Runtime;

        public static bool IsDesignTime => LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }
}