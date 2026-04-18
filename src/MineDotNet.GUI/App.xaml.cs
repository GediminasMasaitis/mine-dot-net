using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MineDotNet.Common;
using MineDotNet.GUI.Services;
using MineDotNet.GUI.Views;
using MineDotNet.IO;

namespace MineDotNet.GUI
{
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            var collection = new ServiceCollection();
            collection.AddMineDotNetGUI();
            IOCC.ServiceProvider = collection.BuildServiceProvider();

            var window = new MainWindow();
            if (e.Args.Length > 0)
            {
                var parser = new TextMapParser();
                var maps = e.Args
                    .Select(a => a.Replace(";", System.Environment.NewLine))
                    .Select(parser.Parse)
                    .ToList();
                if (maps.Count > 0)
                {
                    var converter = IOCC.GetService<IMaskConverter>();
                    var masks = converter.ConvertToMasks(maps.Skip(1)).ToList();
                    window.SetMapAndMasks(maps[0], masks);
                }
            }
            window.Show();
        }
    }
}
