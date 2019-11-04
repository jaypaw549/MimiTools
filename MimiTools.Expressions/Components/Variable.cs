namespace MimiTools.Expressions.Components
{
    public struct Variable
    {
        internal Variable(string name, Environment env)
        {
            this.name = name;
            environment = env;
        }

        private readonly string name;
        private readonly Environment environment;
    }
}
