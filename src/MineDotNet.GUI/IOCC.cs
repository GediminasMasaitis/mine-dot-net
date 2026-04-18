using System;
using Microsoft.Extensions.DependencyInjection;

namespace MineDotNet.GUI
{
    static class IOCC
    {
        public static IServiceProvider ServiceProvider { private get; set; }

        public static T GetService<T>() => ServiceProvider.GetService<T>();
    }
}
