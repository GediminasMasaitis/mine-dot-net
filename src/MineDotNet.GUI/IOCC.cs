using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MineDotNet.GUI
{
    static class IOCC
    {
        public static IServiceProvider ServiceProvider { private get; set; }

        public static T GetService<T>()
        {
            if (Designer.IsDesignTime)
            {
                return default;
            }

            return ServiceProvider.GetService<T>();
        }
    }
}
