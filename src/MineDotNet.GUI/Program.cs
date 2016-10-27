using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MineDotNet.Common;

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
            IList<Map> maps = new List<Map>();
            for(var i = 0; i < args.Length; i++)
            {
                var mapStr = args[i].Replace(";", Environment.NewLine);
                var parser = new TextMapParser();
                var map = parser.Parse(mapStr);
                maps.Add(map);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(maps));
        }
    }
}
