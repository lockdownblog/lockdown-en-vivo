namespace Lockdown.Build.Utils
{
    public interface ILiquidRenderer
    {
        void SetRoot(string root);

        string Render(string content, object variables);
    }
}
