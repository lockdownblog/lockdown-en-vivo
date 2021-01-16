namespace Lockdown
{
    using System;
    using System.IO;

    public class Program
    {
        public static void Main(string[] args)
        {
            var argumentos = string.Join("; ", args);
            var directorioActual = Directory.GetCurrentDirectory();
            Console.WriteLine($"Me estoy ejecutando en {directorioActual}");
            Console.WriteLine(argumentos);
        }
    }
}
