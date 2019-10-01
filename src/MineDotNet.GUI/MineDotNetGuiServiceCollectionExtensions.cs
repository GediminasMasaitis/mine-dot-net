using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MineDotNet.GUI.Services;
using MineDotNet.GUI.Tiles;

namespace MineDotNet.GUI
{
    static class MineDotNetGuiServiceCollectionExtensions
    {
        public static IServiceCollection AddMineDotNetGUI(this IServiceCollection services)
        {
            services.AddTransient<ITileLoader, TileLoader>();
            services.AddTransient<ITileResizer, TileResizer>();
            services.AddTransient<ITileGenerator, TileGenerator>();
            services.AddTransient<ITileProvider, TileProvider>();
            services.AddSingleton<IBrushProvider, BrushProvider>();
            services.AddTransient<ICellLocator, CellLocator>();
            services.AddTransient<IDisplayService, DisplayService>();

            return services;
        }
    }
}
