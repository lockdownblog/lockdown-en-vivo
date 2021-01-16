namespace Lockdown
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using McMaster.Extensions.CommandLineUtils;

    [Command("lockdown")]
    [VersionOptionFromMember("--version", MemberName = nameof(LockdownVersion))]
    public class Program
    {
        public string LockdownVersion { get; } = "0.0.0";

        [Argument(0, Description = "The first operand")]
        [Required]
        public int FirstNumber { get; set; }

        [Argument(1, Description = "Operation to perform")]
        [Required]
        public string Operation { get; set; }

        [Argument(2, Description = "The second operand")]
        [Required]
        public int SecondNumber { get; set; }

        public static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        public int OnExecute(CommandLineApplication app)
        {
            int result;
            switch (this.Operation)
            {
                case "+":
                    result = this.FirstNumber + this.SecondNumber;
                    break;
                case "-":
                    result = this.FirstNumber - this.SecondNumber;
                    break;
                case "/":
                    result = this.FirstNumber / this.SecondNumber;
                    break;
                case "*":
                    result = this.FirstNumber * this.SecondNumber;
                    break;
                default:
                    throw new NotImplementedException($"The operation {this.Operation} is not implemented");
            }

            Console.WriteLine(result);

            return 0;
        }
    }
}
