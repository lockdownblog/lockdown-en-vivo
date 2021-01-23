using Lockdown.Commands;
using System;
using Xunit;
using Lockdown.Test.Utils;
using Shouldly;

namespace Lockdown.Test
{
    public class BuildCommandTests
    {
        [Fact]
        public void TestWriteToConsole()
        {
            // Setup
            var testConsole = new TestConsole();
            var buildCommand = new BuildCommand(testConsole);

            // Act
            buildCommand.OnExecute();

            // Assert
            string writtenText = testConsole.GetWrittenContent();

            writtenText.ShouldBe("This is an alfa version. Do not use me please!" + Environment.NewLine);
        }
    }
}
