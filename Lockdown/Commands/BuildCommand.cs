namespace Lockdown.Commands
{
    using McMaster.Extensions.CommandLineUtils;

    public class BuildCommand
    {
        private readonly IConsole console;

        public BuildCommand(IConsole console)
        {
            this.console = console;
        }

        public int OnExecute()
        {
            this.console.WriteLine("This is an alfa version. Do not use me please!");
            return 0;
        }
    }
}
