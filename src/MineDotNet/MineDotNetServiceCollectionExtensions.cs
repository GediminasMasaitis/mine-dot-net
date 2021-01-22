using Microsoft.Extensions.DependencyInjection;
using MineDotNet.Game;

namespace MineDotNet
{
    public static class MineDotNetServiceCollectionExtensions
    {
        public static IServiceCollection AddMineDotNet(IServiceCollection services)
        {
            services.AddTransient<IGameMapGenerator, GameMapGenerator>();

            return services;
        }
    }
}
