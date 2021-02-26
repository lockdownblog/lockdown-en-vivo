namespace Lockdown
{
    using System.IO.Abstractions;
    using AutoMapper;
    using Lockdown.Build;
    using Lockdown.Build.Markdown;
    using Lockdown.Build.Utils;
    using Lockdown.Commands;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Extensions.DependencyInjection;

    [Command("lockdown")]
    [VersionOptionFromMember("--version", MemberName = nameof(LockdownVersion))]
    [Subcommand(typeof(BuildCommand))]
    [Subcommand(typeof(RunCommand))]
    public class Program
    {
        public string LockdownVersion { get; } = "0.0.3";

        public static int Main(string[] args)
        {
            ServiceProvider services = new ServiceCollection()
                .AddSingleton<IMapper>(Build.Mapping.Mapper.GetMapper())
                .AddSingleton<ILiquidRenderer, DotLiquidRenderer>()
                .AddSingleton<ISlugifier, Slugifier>()
                .AddSingleton<IMarkdownRenderer, MarkdownRenderer>()
                .AddSingleton<IYamlParser, YamlParser>()
                .AddSingleton<IFileSystem, FileSystem>()
                .AddSingleton<ISiteBuilder, SiteBuilder>()
                .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                .BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            return app.Execute(args);
        }

        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
