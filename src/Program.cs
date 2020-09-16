namespace RobustCopy
{
    using System.Threading.Tasks;

    using CliFx;

    public class Program
    {
        public static async Task<int> Main(string[] args) =>
            await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .Build()
            .RunAsync();
    }
}
