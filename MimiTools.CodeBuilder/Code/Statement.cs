namespace MimiTools.CodeBuilder.Code
{
    public abstract class Statement : NodeBuilder
    {
        private protected Statement() { }

        public sealed override void Clear()
        {
            throw new System.InvalidOperationException("Statements are immutable!");
        }

        public static Statement Assign(Expression target, Expression value)
            => new AssignmentStatement(target, value);

        public static Statement ExitSwitch { get => ExitSwitchStatement.Instance; }

        public static IfStatement If(Expression condition, params Statement[] if_true)
            => new IfStatement(condition, if_true);

        public static Statement LocalDeclaration(string name, ReferenceBuilder type, Expression initializer = null, bool is_const = false)
            => new FullLocalDeclarationStatement(name, type, initializer, is_const);

        public static Statement LocalDeclaration(string name, Expression initializer)
            => new VarLocalDeclarationStatement(name, initializer);

        public static Statement Lock(Expression lock_obj, params Statement[] body)
            => new LockStatement(lock_obj, body);

        //public static Statement Switch(Expression value, params Statement[] statements)

        public static Statement Throw(Expression exception = null)
            => new ThrowStatement(exception);

        public static TryStatement Try(params Statement[] body)
            => new TryStatement(body);

        public static Statement Using(Expression obj, params Statement[] body)
            => new NoVarUsingStatement(obj, body);

        public static Statement Using(string name, Expression obj, params Statement[] body)
            => new VarUsingStatement(name, obj, body);

        public static Statement Using(ReferenceBuilder type, string name, Expression obj, params Statement[] body)
            => new FullUsingStatement(type, name, obj, body);

        public static Statement VariableDeclaration(string name, ReferenceBuilder type, Expression initializer = null, bool is_const = false)
            => new FullLocalDeclarationStatement(name, type, initializer, is_const);

        public static Statement VariableDeclaration(string name, Expression initializer)
            => new VarLocalDeclarationStatement(name, initializer);

        public static Statement While(Expression condition, params Statement[] body)
            => new WhileStatement(condition, body);
    }
}
