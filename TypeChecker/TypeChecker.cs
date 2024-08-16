using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq.Expressions;
using Type = BaD.Type;

namespace BaD;

public class TypeCheckerException : Exception
{
    public TypeCheckerException(string message, StreamPosition position) : base(message + " at " + position) { }
}

public class TypeChecker
{
    SExpression[] expressions;

    public struct Scope
    {
        public Dictionary<string, Type> variables;
        public Dictionary<string, Type> functions;
    }

    // scope stack
    public Stack<Scope> scopes = new();

    // find item in the current scope
    public Type? Find(string name)
    {

        // check all scopes to see if the variable exists

        foreach (var scope in scopes)
        {
            if (scope.variables.TryGetValue(name, out Type? value))
                return value;
            if (scope.functions.TryGetValue(name, out Type? value2))
                return value2;
        }
        return null;
    }

    public bool IsVariable(string name)
    {
        foreach (var scope in scopes)
        {
            if (scope.variables.ContainsKey(name))
                return true;
        }
        return false;
    }

    public TypeChecker(SExpression[] sexp)
    {
        expressions = sexp;
        // add the global scope
        scopes.Push(new Scope()
        {
            variables = new Dictionary<string, Type>(),
            functions = new Dictionary<string, Type>()
        });
        LastFunction = null;
    }


    public Scope CurrentScope => scopes.Peek();

    public FunctionType? LastFunction;

    // since expressions are nested, we need to check the type of each expression
    public void Check()
    {
        foreach (var e in expressions)
        {
            if(e != null)
                Check(e);
        }

        // now just double check that we have a valid main function
        if(!scopes.Peek().functions.ContainsKey("main"))
            throw new TypeCheckerException("No main function found", new StreamPosition(0, 0));

        // now check that the main function has the correct signature, either int main() or int main(int argc, char** argv)
        if(scopes.Peek().functions["main"] is FunctionType mainType)
        {
            if(mainType.parameterTypes.Count == 0 && mainType.returnType.Equals(new PrimitiveType(PrimitiveTypes.Int)))
                return; // int main()
            if(mainType.parameterTypes.Count == 2 && mainType.parameterTypes[0].Equals(new PrimitiveType(PrimitiveTypes.Int)) && mainType.parameterTypes[1].Equals(new PointerType(new PointerType (new PrimitiveType(PrimitiveTypes.Char)))))
                return; // int main(int argc, char** argv)
            throw new TypeCheckerException("Main function has invalid signature", new StreamPosition(0, 0));
        }
    }

    Stack<TypeCheckerExpression> operations = new();

    public void PushOperations(List<TypeCheckerExpression> ops)
    {
        // push in reverse order
        for (int i = ops.Count - 1; i >= 0; i--)
            operations.Push(ops[i]);
    }

    public void ProcessOperations()
    {
        while (operations.Count > 0)
        {
            operations.Pop().CheckType(this);
        }
    }

    public void AddVariable(string name, Type type)
    {
        CurrentScope.variables.Add(name, type);
    }


    public void Check(SExpression fde)
    {
        if(fde == null)
            return;
        if(fde is ExternExpression || fde is FuncExpression)
        {
            PushOperations(new() { new TypeCheckerExpression(fde) });
            ProcessOperations();
        }
        else
        {
            throw new TypeCheckerException("Expected function declaration", fde.StartPosition);
        }
    }
}