namespace Lockdown
{
    using System;
    using System.IO;
    using McMaster.Extensions.CommandLineUtils;

    [Command("lockdown")]
    [VersionOptionFromMember("--version", MemberName = nameof(LockdownVersion))]
    public class Program
    {
        public string LockdownVersion { get; } = "0.0.0";

        public static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
