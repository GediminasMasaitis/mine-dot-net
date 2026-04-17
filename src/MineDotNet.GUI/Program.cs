using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using MineDotNet.Common;
using MineDotNet.GUI.Forms;
using MineDotNet.GUI.Services;
using MineDotNet.IO;

namespace MineDotNet.GUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Designer.IsDesignTime = false;

            // Per-monitor DPI awareness so the app scales correctly on mixed-DPI
            // setups and 4K displays instead of looking blurry.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Default to Segoe UI 9pt (Windows' standard shell font) instead of
            // the legacy MS Sans Serif WinForms picks by default.
            Application.SetDefaultFont(new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point));

            var collection = new ServiceCollection();
            collection.AddMineDotNetGUI();
            IOCC.ServiceProvider = collection.BuildServiceProvider();

            IList<Map> maps = new List<Map>();
            for(var i = 0; i < args.Length; i++)
            {
                var mapStr = args[i].Replace(";", Environment.NewLine);
                var parser = new TextMapParser();
                var map = parser.Parse(mapStr);
                maps.Add(map);
            }

            Form form;
            if(maps.Count > 0)
            {
                var converter = IOCC.GetService<IMaskConverter>();
                var map = maps[0];
                var masks = converter.ConvertToMasks(maps.Skip(1)).ToList();

                var mainForm = new MainForm();
                form = mainForm;
                mainForm.SetMapAndMasks(map, masks);
            }
            else
            {
                form = new MainForm();
            }

            Application.Run(form);
        }
    }
}
