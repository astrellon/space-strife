namespace LysitheaVM
{
    public class Script
    {
        #region Fields
        public static readonly Script Empty = new(Scope.Empty, Function.Empty);

        public readonly IReadOnlyScope BuiltinScope;
        public readonly Function Code;

        public bool IsEmpty => this.Code.IsEmpty && this.BuiltinScope.IsEmpty;
        #endregion

        #region Constructor
        public Script(IReadOnlyScope builtinScope, Function code)
        {
            this.BuiltinScope = builtinScope;
            this.Code = code;
        }
        #endregion

        #region Methods
        public bool TryFindFunction(string functionLookupName, out Function result)
        {
            if (this.BuiltinScope.TryGetKey(functionLookupName, out var value))
            {
                if (value is FunctionValue funcValue)
                {
                    result = funcValue.Value;
                    return true;
                }
            }

            result = Function.Empty;
            return false;
        }
        #endregion
    }
}