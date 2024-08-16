
// we want to avoid recursion in code generation, so we use a stack and make each SExpression add to the stack sub operations to the stack.
// this also means we need another class that represents a subcomponent of an SExpression, so we can add to the stack without recursion
// each SExpression will expand


namespace BaD;

public class TypeCheckerExpression
{
    // function to take in an IGenerator, return nothing
    public Action<TypeChecker> OnCheck { get; }

    
    public TypeCheckerExpression(SExpression expression)
    {
        OnCheck = (typeChecker) =>
        {
            List<TypeCheckerExpression> expressions = expression.CheckTypes(typeChecker);
            typeChecker.PushOperations(expressions);
        };
    }

    public TypeCheckerExpression(Action<TypeChecker> onCheck)
    {
        OnCheck = onCheck;
    }

    public void CheckType(TypeChecker generator)
    {
        OnCheck(generator);
    }
}