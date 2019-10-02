using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var collection = new ServiceCollection();
            collection.AddMineDotNetGUI();
            IOCC.ServiceProvider = collection.BuildServiceProvider();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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
                form = new LauncherForm();
            }

            Application.Run(form);
        }
    }
}
