using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BlinForms.Framework;
using Microsoft.Extensions.Hosting;
using Marius.Yoga.Forms.App;

namespace Marius.Yoga.Forms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static async Task Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            await Host.CreateDefaultBuilder(args)
                .AddBlinForms()
                .ConfigureServices((hostContext, services) =>
                {
                    // Register root form content
                    services.AddRootFormContent<MainApp>();
                })
                .Build()
                .RunAsync();
        }
    }
}
