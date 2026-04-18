using Microsoft.Extensions.DependencyInjection;
using MineDotNet.GUI.Services;
using MineDotNet.IO;

namespace MineDotNet.GUI
{
    static class MineDotNetGuiServiceCollectionExtensions
    {
        public static IServiceCollection AddMineDotNetGUI(this IServiceCollection services)
        {
            services.AddSingleton<ITileSource, TileSource>();
            services.AddSingleton<IPaletteProvider, PaletteProvider>();
            services.AddSingleton<IMaskConverter, MaskConverter>();
            services.AddSingleton<IMapVisualizer, TextMapVisualizer>();
            services.AddSingleton<IStringMapVisualizer, TextMapVisualizer>();
            services.AddSingleton<IMapParser, TextMapParser>();
            services.AddSingleton<IStringMapParser, TextMapParser>();
            return services;
        }
    }
}
