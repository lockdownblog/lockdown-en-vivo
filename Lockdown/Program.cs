namespace Lockdown
{
    using Lockdown.Commands;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Extensions.DependencyInjection;

    [Command("lockdown")]
    [VersionOptionFromMember("--version", MemberName = nameof(LockdownVersion))]
    [Subcommand(typeof(BuildCommand))]
    public class Program
    {
        public string LockdownVersion { get; } = "0.0.3";

        public static int Main(string[] args)
        {
            ServiceProvider services = new ServiceCollection()
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
