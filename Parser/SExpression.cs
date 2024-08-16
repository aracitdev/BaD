using System.Net;
using System.Reflection;
using System.Text;

namespace BaD;

// all expressions are s-expressions in this language
// expression types:
// literal: i | c | 'true' | 'false' | 'null'
// identifier: [a-z, A-Z][a-z, A-Z, 0-9]*
// adress of: '(& 'var' )'
// dereference: '(@' 'expr' )'
// pointer to type: '(* 'type' )'
// arithmetic: (+ i i) | (- i i) | (* i i) | (/ i i)
// comparison: (< i i) | (> i i) | (<= i i) | (>= i i) | (== i i) | (!= i i)
// logical: (&& i i) | (|| i i) | (! i)
// vardec: (vardec 'type' 'var')
// assign: (assign 'var' 'expr')
// while: (while 'expr' 'stmt')
// if: (if 'expr' 'stmt' 'stmt')
// return: (return 'expr')
// block: (block 'stmt' 'stmt' ...)
// println: (println 'expr')
// stmt: 'expr' | 'vardec' | 'assign' | 'while' | 'if' | 'return' | 'block' | 'println'
// func: (func var (param*) type stmt*)
// call: (call expr expr*)




// class used to traverse s-expressions generically
public class SExpressionVisitor
{
    readonly Func<SExpression, List<SExpression?>> visitAction;

    public SExpressionVisitor(Func<SExpression, List<SExpression?>> visitAction)
    {
        this.visitAction = visitAction;
    }

    public void Visit(SExpression? expr)
    {
        Stack<SExpression> stack = new();

        if(expr != null)
            stack.Push(expr);

        while (stack.Count > 0)
        {
            SExpression current = stack.Pop();
            if (current == null)
                continue;
            List<SExpression?> expressions = visitAction(current);
            for (int i = expressions.Count - 1; i >= 0; i--)
            {
                if (expressions[i] != null)
                    stack.Push(expressions[i]!);
            }
        }
    }
}


public abstract class SExpression
{
    public SExpression(StreamPosition position, List<SExpression?> children)
    {
        Children = children;
        StartPosition = position;
    }

    public List<SExpression?> Children { get; set; } // all the children of this expression for easy traversal


    public virtual int GetTempVarSize() // get the number of temporary variables needed for this expression, for stack allocation
    {
        return 0; // default to 0
    }

    // local size is stack space that persists within the same scope, vardec and list are examples of expressions that need local size
    public virtual int GetLocalSize()
    {
        return 0; // default to 0
    }

    public abstract List<CodeGenerationExpression> GenerateCode(IGenerator generator);
    public abstract List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker); // check the types of the expression
    
    public Type? ResultType { get; set; }
    abstract override public string ToString();
    public StreamPosition StartPosition {get; private set;}

    // find the ammount of compiler needed temporary variables on the stack
    static public int GetTempVarSize(SExpression expression)
    {
        if(expression is BlockExpression)
            return 0; // block expressions don't affect the current stack
        int expressionSize = expression.GetTempVarSize();

        int maxSize = 0;
        for(int i = 0; i < expression.Children.Count; i++)
        {
            if(expression.Children[i] != null)
                maxSize = Math.Max(maxSize, GetTempVarSize(expression.Children[i]!));
        }
        return maxSize + expressionSize;
    }
}


// literal expressions should almost never have their generate code called directly, instead parent expressions embed them, but they are here for completeness

public class StringLiteralExpression : SExpression
{
    public string Value { get; }

    public StringLiteralExpression(string value, StreamPosition position) : base(position, new List<SExpression?>{})
    {
        Value = value;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, Value))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to string (char*)
        return new()
        {
            new((tc) => ResultType = new PointerType(new PrimitiveType(PrimitiveTypes.Char)))
        };
    }


    public override string ToString()
    {
        return "\"" + Value + "\"";
    }
}

public class IntLiteralExpression : SExpression
{
    public long Value { get; }

    public IntLiteralExpression(long value, StreamPosition position) : base(position, new List<SExpression?>{})
    {
        Value = value;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, Value))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to int
        return  new()
        {
            new((tc) => ResultType = new PrimitiveType(PrimitiveTypes.IntLiteral))
        };
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

public class CharLiteralExpression : SExpression
{
    public char Value { get; }

    public CharLiteralExpression(char value, StreamPosition position) : base(position,  new List<SExpression?>{})
    {
        Value = value;
    }


    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, Value))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to char
        return new()
        {
            new((tc) => ResultType = new PrimitiveType(PrimitiveTypes.Char))
        };
    }

    public override string ToString()
    {
        return "'" + Value + "'";
    }
}

public class BoolLiteralExpression : SExpression
{
    public bool Value { get; }

    public BoolLiteralExpression(bool value, StreamPosition position) : base(position,  new List<SExpression?>{})
    {
        Value = value;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, Value ? 1 : 0))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to bool
        return new()
        {
            new((tc) => ResultType = new PrimitiveType(PrimitiveTypes.Bool))
        };
    }

    public override string ToString()
    {
        return Value.ToString().ToLower();
    }
}

public class NullLiteralExpression : SExpression
{
    public NullLiteralExpression(StreamPosition position) : base(position,  new List<SExpression?>{})
    {
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, 0L))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to null
        return new()
        {
            new((tc) => ResultType = new PointerType(new PrimitiveType(PrimitiveTypes.Void)))
        };
    }

    public override string ToString()
    {
        return "null";
    }
}

public class IdentifierExpression : SExpression
{
    public string Name { get; }

    public IdentifierExpression(string name, StreamPosition position) : base(position,  new List<SExpression?>{})
    {
        Name = name;
    }


    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.LoadFromMemory(IGenerator.RegisterUses.Return, Name))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to the type of the variable
        return new()
        {
            new((tc) => ResultType = tc.Find(Name))
        };
    }

    public override string ToString()
    {
        return Name;
    }
}

public class AddressOfExpression : SExpression
{
    public string Name { get; }

    public AddressOfExpression(string name, StreamPosition position) : base(position,  new List<SExpression?>{})
    {
        Name = name;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();

        // generate the address of the function (addressOf.Name)
        
        if(generator.IsFunction(Name))
            actions.Add(new((gen) => gen.LoadFunctionAddress(IGenerator.RegisterUses.Return, Name)));
        else
            actions.Add(new((gen) => gen.LoadVariableAddress(IGenerator.RegisterUses.Return, Name)));
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to the type of the variable
        return new()
        {
            new((typeChecker) => {

                Type exprType = typeChecker.Find(Name) ?? throw new TypeCheckerException($"Identifier {Name} not declared in this scope", StartPosition);
                if(exprType is FunctionType)
                    ResultType = exprType;
                else
                    ResultType = new PointerType(exprType);
            })
        };
    }

    public override string ToString()
    {
        return "(& " + Name + ")";
    }
}

public class AssignPtrExpression : SExpression
{
    public SExpression Ptr { get; }
    public SExpression Value { get; }

    public AssignPtrExpression(SExpression ptr, SExpression value, StreamPosition position) : base(position, new List<SExpression?>{ptr, value})
    {
        Ptr = ptr;
        Value = value;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Value);
        // we need to push the result because the other operation can modify all the registers
        actions.Add(new((gen) => gen.PushTmp(IGenerator.RegisterUses.Return)));
        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Ptr));
        actions.Add(new((gen) => gen.PopTmp(IGenerator.RegisterUses.Scratch1)));
        actions.Add(new((gen) => gen.StoreToMemory(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch1, Value.ResultType!)));
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to the type of the variable
        return new()
        {
            new(Ptr),
            new(Value),

            new((typeChecker) => {
                if(Ptr.ResultType is not PointerType ptrType)
                    throw new TypeCheckerException("Cannot assign to a non-pointer type", Ptr.StartPosition);
                if(!ptrType.Type.Equals(Value.ResultType))
                    throw new TypeCheckerException("Cannot assign a value of type " + Value.ResultType + " to a pointer of type " + ptrType.Type, StartPosition);
                ResultType = Value.ResultType;
            })
        };
    }

    public override string ToString()
    {
        return "(assignptr " + Ptr + " " + Value + ")";
    }

    public override int GetTempVarSize()
    {
        return 8; // we need to push the value to the stack
    }
}

public class DereferenceExpression : SExpression
{
    public SExpression Expr { get; }

    public DereferenceExpression(SExpression expr, StreamPosition position) : base(position, new List<SExpression?>{expr})
    {
        Expr = expr;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        PointerType? ptrType = Expr.ResultType as PointerType;
        if(ptrType == null)
            throw new TypeCheckerException("Cannot dereference a non-pointer type, typechecker missed", Expr.StartPosition); // this should never happen, since the type checker should catch this
        List<CodeGenerationExpression> actions = new()
        {
            new CodeGenerationExpression(Expr),
            new((gen) => gen.LoadFromMemory(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Return, ptrType.Type))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to the type of the variable
        return new()
        {
            new(Expr),
            new((typeChecker) => {
                if(Expr.ResultType is not PointerType ptrType)
                    throw new TypeCheckerException("Cannot dereference a non-pointer type", Expr.StartPosition);
                ResultType = ptrType.Type;
            })
        };
    }

    public override string ToString()
    {
        return "(* " + Expr + ")";
    }
}

public class VardecExpression : SExpression
{
    public string Name { get; }
    public Type Type { get; }

    public VardecExpression(string name, Type type, StreamPosition position) : base(position, new List<SExpression?>{})
    {
        Name = name;
        Type = type;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.AllocateVariable(Name, Type))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to the type of the variable
        return new()
        {
            new((typeChecker) => {
                typeChecker.AddVariable(Name, Type);
                ResultType = Type;
            })
        };
    }

    public override int GetLocalSize()
    {
        return Type.LocalSize();
    }

    public override string ToString()
    {
        return "(vardec " + Type + " " + Name + ")";
    }
}

public class AssignExpression : SExpression
{
    public string Name { get; }
    public SExpression Expr { get; }

    public AssignExpression(string name, SExpression expr, StreamPosition position) : base(position, new List<SExpression?>{expr})
    {
        Name = name;
        Expr = expr;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Expr);
        actions.Add(new((gen) => gen.StoreToMemory(Name, IGenerator.RegisterUses.Return)));
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just set ResultType to the type of the variable
        return new()
        {
            new(Expr),
            new((typeChecker) => {
                Type? varType = typeChecker.Find(Name) ?? throw new TypeCheckerException("Variable " + Name + " not declared in this scope", StartPosition); 
                if(!varType.Equals(Expr.ResultType) && !Expr.ResultType!.CanCastTo(varType))
                    throw new TypeCheckerException("Cannot assign a value of type " + Expr.ResultType + " to a variable of type " + varType, StartPosition);
                ResultType = Expr.ResultType;
            })
        };
    }

    public override string ToString()
    {
        return "(assign " + Name + " " + Expr + ")";
    }
}

public class WhileExpression : SExpression
{
    public SExpression Expr { get; }
    public SExpression Stmt { get; }

    public WhileExpression(SExpression expr, SExpression stmt, StreamPosition position) : base(position, new List<SExpression?>{expr, stmt}) 
    {
        Expr = expr;
        Stmt = stmt;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        string? startLabel = generator.GenerateUniqueLabel();
        string? endLabel = generator.GenerateUniqueLabel();

        // first we create the label
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.CreateLabel(startLabel))
        };
        // then we need to test the condition

        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Expr));
        // if the condition is false, we jump to the end
        actions.Add(new((gen) => gen.JumpIfZero(IGenerator.RegisterUses.Return, endLabel)));

        // if the condition is true, we execute the statement
        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Stmt));
        actions.Add(new((gen) => gen.Jump(startLabel)));
        actions.Add(new((gen) => gen.CreateLabel(endLabel)));
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        return new()
        {
            new(Expr),
            new(Stmt),
            new((typeChecker) => {
                if(Expr.ResultType is not PrimitiveType primType || primType.Primitive != PrimitiveTypes.Bool)
                    throw new TypeCheckerException("While expression must be of type bool, not " + Expr.ResultType, StartPosition);
            })
        };
    }

    public override string ToString()
    {
        return "(while " + Expr + " " + Stmt + ")";
    }
}

public class IfExpression : SExpression
{
    public SExpression Expr { get; }
    public SExpression ThenStmt { get; }
    public SExpression? ElseStmt { get; }

    public IfExpression(SExpression expr, SExpression thenStmt, SExpression? elseStmt, StreamPosition position) : base(position, new List<SExpression?>{expr, thenStmt, elseStmt})
    {
        Expr = expr;
        ThenStmt = thenStmt;
        ElseStmt = elseStmt;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        string? endLabel = generator.GenerateUniqueLabel();
        string? elseLabel = null;
        if(ElseStmt != null)
            elseLabel = generator.GenerateUniqueLabel();
        // first we need to test the expression
        List<CodeGenerationExpression> actions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Expr);
        // assume the expression is in the return register)

        if(ElseStmt != null && elseLabel != null)
        {
            actions.Add(new((gen) => gen.JumpIfZero(IGenerator.RegisterUses.Return, elseLabel)));
        }
        else
            actions.Add(new((gen) => gen.JumpIfZero(IGenerator.RegisterUses.Return, endLabel)));
        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, ThenStmt));
        actions.Add(new((gen) => gen.Jump(endLabel)));
        if(ElseStmt != null && elseLabel != null)
        {
            actions.Add(new((gen) => gen.CreateLabel(elseLabel)));
            actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, ElseStmt));
        }
        actions.Add(new((gen) => gen.CreateLabel(endLabel)));
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new()
        {
            // check condition is bool
            new(Expr),
            new(
                (typeChecker) => {
                    if(Expr.ResultType is not PrimitiveType primType || primType.Primitive != PrimitiveTypes.Bool)
                        throw new TypeCheckerException("If expression must be of type bool, not " + Expr.ResultType, StartPosition);
                }
            ),
            new(ThenStmt)
        };

        if(ElseStmt != null)
            actions.Add(new(ElseStmt));

        if(ElseStmt != null)
            actions.Add(new(
                (typeChecker) => {
                    if(!ThenStmt.ResultType!.Equals(ElseStmt.ResultType))
                        throw new TypeCheckerException("If statement then and else branches must have the same type, not " + ThenStmt.ResultType + " and " + ElseStmt.ResultType, StartPosition);
                }
            ));

        actions.Add(
            new((typeChecker) => {
                ResultType = ThenStmt.ResultType;
            })
        );
        
        return actions;
    }

    public override string ToString()
    {
        return "(if " + Expr + " " + ThenStmt + " " + ElseStmt + ")";
    }
}

public class ReturnExpression : SExpression
{
    public SExpression? Expr { get; }

    public ReturnExpression(SExpression? expr, StreamPosition position) : base(position, new List<SExpression?>{expr})
    {
        Expr = expr;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        if(Expr == null)
            return new List<CodeGenerationExpression> { new((gen) => gen.ReturnFromFunction()) }; // just return from the function
        List<CodeGenerationExpression> actions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Expr);
        actions.Add(new((gen) => gen.ReturnFromFunction()));
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();
        if(Expr != null)
            actions.Add(new(Expr));
        actions.Add(new((typeChecker) => {
            if(typeChecker.LastFunction == null)
                throw new TypeCheckerException("Return statement outside of function", StartPosition);
            if(Expr != null)
            {
                if(!typeChecker.LastFunction.returnType.Equals(Expr.ResultType) && !Expr.ResultType!.CanCastTo(typeChecker.LastFunction.returnType))
                    throw new TypeCheckerException("Return type " + Expr.ResultType + " does not match function return type " + typeChecker.LastFunction.returnType, StartPosition);
            }
            else
                ResultType = new PrimitiveType(PrimitiveTypes.Void);
        }));
        return actions;
    }

    public override string ToString()
    {
        return "(return " + Expr != null ? Expr!.ToString() : "" + ")";
    }
}

public class BlockExpression : SExpression
{
    public SExpression[] Stmts { get; }

    public BlockExpression(SExpression[] stmts, StreamPosition position) : base(position, new List<SExpression?>(stmts))
    {
        Stmts = stmts;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        return generator.Block(this);
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();
        foreach (var stmt in Stmts)
        {
            actions.Add(new(stmt));
        }

        actions.Add(new((typeChecker) => {
            if(Stmts.Length > 0)
                ResultType = Stmts[Stmts.Length - 1].ResultType;
            else
                ResultType = new PrimitiveType(PrimitiveTypes.Void);
        }));
        return actions;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("(block");
        foreach (var stmt in Stmts)
        {
            sb.Append(" " + stmt.ToString());
        }

        sb.Append(")");
        return sb.ToString();
    }
}

public class FuncExpression : SExpression
{
    public string Name { get; }
    public TypedIdentifier[] Params { get; }
    public Type ReturnType { get; }
    public List<SExpression> Body { get; }
    public int LocalVarCount { get; set; }

    public FuncExpression(string name, TypedIdentifier[] @params, Type returnType, List<SExpression> body, StreamPosition position) : base(position, new List<SExpression?>(body))
    {
        Name = name;
        Params = @params;
        ReturnType = returnType;
        Body = body;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        return generator.Function(this);
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {

        List<TypeCheckerExpression> actions = new();

        actions.Add(new((typeChecker) => {
            if(typeChecker.Find(Name) != null)
                throw new TypeCheckerException($"Function {Name} already declared in this scope", StartPosition);

            // create a list of the function's parameters
            List<Type> parameters = new();
            foreach (var p in Params)
                parameters.Add(p.Type);
            typeChecker.CurrentScope.functions.Add(Name, new FunctionType(ReturnType, parameters));

            typeChecker.scopes.Push(new TypeChecker.Scope()
            {
                variables = new Dictionary<string, Type>(),
                functions = new Dictionary<string, Type>()
            });
            typeChecker.LastFunction = new FunctionType(ReturnType, parameters);

            // add the function's parameters to the new scope
            foreach (var p in Params)
                typeChecker.CurrentScope.variables.Add(p.Name, p.Type);
        }));

        foreach (var stmt in Body)
            actions.Add(new(stmt));

        actions.Add(new((typeChecker) => {
            typeChecker.scopes.Pop();
            ResultType = new FunctionType(ReturnType, Params.Select(p => p.Type).ToList());
        }));

        return actions;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("(func " + Name + " (");
        foreach (var param in Params)
        {
            sb.Append(param + " ");
        }

        sb.Append(") " + ReturnType + " ");
        foreach (var stmt in Body)
        {
            sb.Append(stmt + " ");
        }

        sb.Append(")");
        return sb.ToString();
    }
}

public class FunctionCallExpression : SExpression
{
    public SExpression Func { get; }
    public SExpression[] Args { get; }

    public FunctionCallExpression(SExpression func, SExpression[] args, StreamPosition position) : base(position, new List<SExpression?>(args.Concat(new SExpression?[]{func})))
    {
        Func = func;
        Args = args;
    }

    public static bool HasSubCall(SExpression expr)
    {
        bool hasCall = false;
        SExpressionVisitor visitor = new((expr) => {
            if(hasCall)
                return new List<SExpression?>(); // we don't need to go deeper if we already have a call expression
            if(expr is FunctionCallExpression)
                hasCall = true;
            return new List<SExpression?>(expr.Children);
        });
        foreach (var child in expr.Children)
        {
            visitor.Visit(child);
        }

        return hasCall;
    }

    public override int GetTempVarSize()
    {
        // we need to allocate space for the arguments if there is a subcall since the arguments may be modified
        return HasSubCall(this) ? Args.Length * 8 : 0; // assume we need a temporary variable for each argument, since subexpressions may change the value of the registers 
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        return generator.Call(this); // code generator handles this, because it is extremely platform specific
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();

        actions.Add(new(Func));

        actions.Add(new((typeChecker) => {
            if(Func.ResultType is not FunctionType funcType)
                throw new TypeCheckerException("Cannot call a non-function type", Func.StartPosition);
            if(!funcType.IsVarArgs && Args.Length != funcType.parameterTypes.Count)
                throw new TypeCheckerException("Function " + Func + " expects " + funcType.parameterTypes.Count + " arguments, but got " + Args.Length, StartPosition);
        }));
        for (int i = 0; i < Args.Length; i++)
        {
            int j = i;
            actions.Add(new(Args[i]));
            actions.Add(new((typeChecker) => {
                FunctionType funcType = (FunctionType)Func.ResultType! ?? throw new TypeCheckerException("Cannot call a non-function type", Func.StartPosition);
            
                if(!funcType.IsVarArgs && !Args[j].ResultType!.Equals(funcType.parameterTypes[j]) && !Args[j].ResultType!.CanCastTo(funcType.parameterTypes[j]))
                    throw new TypeCheckerException("Argument " + i + " of function " + Func + " has type " + Args[j].ResultType + " but was expected to have type " + funcType.parameterTypes[j], Args[j].StartPosition);
            }));
        }

        actions.Add(new((typeChecker) => {
            FunctionType funcType = (FunctionType)Func.ResultType! ?? throw new TypeCheckerException("Cannot call a non-function type", Func.StartPosition);
            ResultType = funcType.returnType;
        }));
        return actions;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("(call " + Func + " ");
        foreach (var arg in Args)
        {
            sb.Append(arg + " ");
        }

        sb.Append(")");
        return sb.ToString();
    }
}

public class ArithmeticExpression : SExpression
{
    public Token Op { get; private set; }
    public SExpression Left { get; private set; }
    public SExpression Right { get; private set; }

    public ArithmeticExpression(Token op, SExpression left, SExpression right, StreamPosition position)
        : base(position, new List<SExpression?>{left, right})
    {
        Op = op;
        Left = left;
        Right = right;
    }


    public SExpression SimplifyArithmeticExpression()
    {
        // simplify expression tree from the bottom up
        if(Left is ArithmeticExpression)
            Left = ((ArithmeticExpression)Left).SimplifyArithmeticExpression();

        if(Right is ArithmeticExpression)
            Right = ((ArithmeticExpression)Right).SimplifyArithmeticExpression();

        if(Left is IntLiteralExpression lLit && Right is IntLiteralExpression rLit)
        {
            // we can do this operation at compile time
            long leftValue = lLit.Value;
            long rightValue = rLit.Value;

            long result = Op.Type switch
            {
                TokenType.Plus => leftValue + rightValue,
                TokenType.Minus => leftValue - rightValue,
                TokenType.Multiply => leftValue * rightValue,
                TokenType.Divide => leftValue / rightValue,
                _ => throw new NotImplementedException("Unknown arithmetic operator: " + Op.Type)
            };
            return new IntLiteralExpression(result, StartPosition); // replace ourself with the result
        }
        return this; // we can't simplify this expression
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();

        actions.Add(new(Left));
        actions.Add(new(Right));

        actions.Add(new((typeChecker) => {
            Type leftType = Left.ResultType ?? throw new TypeCheckerException("Left operand of arithmetic expression has no type", Left.StartPosition);
            Type rightType = Right.ResultType ?? throw new TypeCheckerException("Right operand of arithmetic expression has no type", Right.StartPosition);

            if(!(leftType is PrimitiveType || leftType is PointerType))
                throw new TypeCheckerException($"Left operand of arithmetic expression has type {leftType} but was expected to have type int, pointer, or char", StartPosition);
            // right can be an int, pointer, or char
            if(!(rightType is PrimitiveType || rightType is PointerType))
                throw new TypeCheckerException($"Right operand of arithmetic expression has type {rightType} but was expected to have type int, pointer, or char", StartPosition);

            ResultType = Left.ResultType;
        }));
        return actions;
    }

    CodeGenerationExpression GetOpExpression(IGenerator.RegisterUses left, IGenerator.RegisterUses right)
    {
        return Op.Type switch
        {
            TokenType.Plus => new((gen) => gen.Add(left, right)),
            TokenType.Minus => new((gen) => gen.Subtract(left, right)),
            TokenType.Multiply => new((gen) => gen.Multiply(left, right)),
            TokenType.Divide => new((gen) => gen.Divide(left, right)),
            _ => throw new NotImplementedException("Unknown arithmetic operator: " + Op.Type)
        };

    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();

        if(Left is IntLiteralExpression l && Right is IntLiteralExpression r)
        {
            // we can do this operation at compile time
            long leftValue = l.Value;
            long rightValue = r.Value;

            long result = Op.Type switch
            {
                TokenType.Plus => leftValue + rightValue,
                TokenType.Minus => leftValue - rightValue,
                TokenType.Multiply => leftValue * rightValue,
                TokenType.Divide => leftValue / rightValue,
                _ => throw new NotImplementedException("Unknown arithmetic operator: " + Op.Type)
            };
            return new List<CodeGenerationExpression>
            {
                new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, result))
            };
        }
        else
        if(Left is IntLiteralExpression || Right is IntLiteralExpression)
        {
            bool leftIsLiteral = Left is IntLiteralExpression;


            // generate the code for the non-literal expression
            List<CodeGenerationExpression> expActions = generator.LoadToRegister(IGenerator.RegisterUses.Return, leftIsLiteral ? Right : Left);
            actions.AddRange(expActions);

            // we know the right won't modify our scratch register, so we can use it. move the left to the scratch register
            actions.Add(new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Scratch1, IGenerator.RegisterUses.Return)));

            // now load the literal to the return register
            List<CodeGenerationExpression> litActions = generator.LoadToRegister(IGenerator.RegisterUses.Return, leftIsLiteral ? Left : Right);
            actions.AddRange(litActions);

            // do the operation, make sure the left is the left and the right is the right
            actions.Add(GetOpExpression( leftIsLiteral ? IGenerator.RegisterUses.Return : IGenerator.RegisterUses.Scratch1, leftIsLiteral ? IGenerator.RegisterUses.Scratch1 : IGenerator.RegisterUses.Return));    

            // if we used scratch as the left, move it to the return register
            if(!leftIsLiteral)
                actions.Add(new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch1)));
            return actions;
        }
        else
        {
            List<CodeGenerationExpression> leftActions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Left);
            // we need to push the result because the other operation can modify all the registers
            actions.AddRange(leftActions);
            actions.Add(new((gen) => gen.PushTmp(IGenerator.RegisterUses.Return)));
            List<CodeGenerationExpression> rightActions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Right);
            actions.AddRange(rightActions);
            
            // right is now in the return register, move it to the scratch register
            actions.Add(new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Scratch1, IGenerator.RegisterUses.Return))); //
            actions.Add(new((gen) => gen.PopTmp(IGenerator.RegisterUses.Return))); // pop the left from the stack
            actions.Add(GetOpExpression(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch1));
        }
        return actions;
    }

    // we need 1 temporary variable for the result if the left or right is not a literal
    public override int GetTempVarSize()
    {
        return (Left is IntLiteralExpression || Right is IntLiteralExpression) ? 0 : 8;
    }

    public override string ToString()
    {
        return "(" + Op.Value + " " + Left + " " + Right + ")";
    }
}

public class ComparisonExpression : SExpression
{
    public Token Op { get; }
    public SExpression Left { get; }
    public SExpression Right { get; }

    public ComparisonExpression(Token op, SExpression left, SExpression right, StreamPosition position)
        : base(position, new List<SExpression?>{left, right})
    {
        Op = op;
        Left = left;
        Right = right;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();


        // first generate the code for the left and right expressions
        List<CodeGenerationExpression> leftActions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Left);
        
        // we need to push the result because the other operation can modify all the registers
        actions.AddRange(leftActions);

        actions.Add(new((gen) => gen.PushTmp(IGenerator.RegisterUses.Return)));

        List<CodeGenerationExpression> rightActions = generator.LoadToRegister(IGenerator.RegisterUses.Scratch1, Right);
        actions.AddRange(rightActions);

        // move the left to the return register
        actions.Add(new((gen) => gen.PopTmp(IGenerator.RegisterUses.Return)));

        // now we can do the comparison
        actions.Add( new(
            (gen) =>
            generator.Compare(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch1, Op.Type)
        ));

        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();

        actions.Add(new(Left));
        actions.Add(new(Right));

        actions.Add(new((typeChecker) => {
            Type leftType = Left.ResultType ?? throw new TypeCheckerException("Left operand of comparison expression has no type", Left.StartPosition);
            Type rightType = Right.ResultType ?? throw new TypeCheckerException("Right operand of comparison expression has no type", Right.StartPosition);

            if(!(leftType is PrimitiveType primitiveL || leftType is PointerType))
                throw new TypeCheckerException($"Left operand of comparison expression has type {leftType} but was expected to have type int, pointer, or char", StartPosition);
            // right can be an int, pointer, or char
            if(!(rightType is PrimitiveType primitiveR || rightType is PointerType))
                throw new TypeCheckerException($"Right operand of comparison expression has type {rightType} but was expected to have type int, pointer, or char", StartPosition);

            ResultType = new PrimitiveType(PrimitiveTypes.Bool);
        }));
        return actions;
    }

    public override string ToString()
    {
        return "(" + Op.Value + " " + Left + " " + Right + ")";
    }

    // we use 1 temporary variable for the left, because the right can modify registers
    public override int GetTempVarSize()
    {
        return 8;
    }
}

public class LogicalExpression : SExpression
{
    public Token Op { get; }
    public SExpression Left { get; }
    public SExpression Right { get; }

    public LogicalExpression(Token op, SExpression left, SExpression right, StreamPosition position)
        : base(position, new List<SExpression?>{left, right})
    {
        Op = op;
        Left = left;
        Right = right;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();

        // first generate the code for the left and right expressions
        List<CodeGenerationExpression> leftActions = generator.LoadToRegister(IGenerator.RegisterUses.Return, Left);

        // we need to push the result because the other operation can modify all the registers
        actions.AddRange(leftActions);
        actions.Add(new((gen) => gen.PushTmp(IGenerator.RegisterUses.Return)));

        List<CodeGenerationExpression> rightActions = generator.LoadToRegister(IGenerator.RegisterUses.Scratch1, Right);
        actions.AddRange(rightActions);

        // move the left to the return register
        actions.Add(new((gen) => gen.PopTmp(IGenerator.RegisterUses.Return)));

        // now we can do the comparison
        switch (Op.Type)
        {
            case TokenType.And:
                actions.Add(new((gen) => gen.And(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch1)));
                break;
            case TokenType.Or:
                actions.Add(new((gen) => gen.Or(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch1)));
                break;
            default:
                throw new NotImplementedException("Unknown logical operator: " + Op.Type);
        }

        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();

        actions.Add(new(Left));
        actions.Add(new(Right));

        actions.Add(new((typeChecker) => {
            Type leftType = Left.ResultType ?? throw new TypeCheckerException("Left operand of logical expression has no type", Left.StartPosition);
            Type rightType = Right.ResultType ?? throw new TypeCheckerException("Right operand of logical expression has no type", Right.StartPosition);

            if(leftType is not PrimitiveType primitiveL || primitiveL.Primitive != PrimitiveTypes.Bool)
                throw new TypeCheckerException($"Left operand of logical expression has type {leftType} but was expected to have type bool", StartPosition);
            if(rightType is not PrimitiveType primitiveR || primitiveR.Primitive != PrimitiveTypes.Bool)
                throw new TypeCheckerException($"Right operand of logical expression has type {rightType} but was expected to have type bool", StartPosition);

            ResultType = new PrimitiveType(PrimitiveTypes.Bool);
        }));
        return actions;
    }

    public override string ToString()
    {
        return "(" + Op.Value + " " + Left + " " + Right + ")";
    }

    // we use 1 temporary variable for the left, because the right can modify registers
    public override int GetTempVarSize()
    {
        return 8;
    }
}

public class NotExpression : SExpression
{
    public SExpression Expr { get; }

    public NotExpression(SExpression expr, StreamPosition position) : base(position, new List<SExpression?>{expr})
    {
        Expr = expr;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();

        actions.Add(new(Expr));

        actions.Add(new((typeChecker) => {
            Type exprType = Expr.ResultType ?? throw new TypeCheckerException("Expression has no type", Expr.StartPosition);
            if(exprType is not PrimitiveType primitive || primitive.Primitive != PrimitiveTypes.Bool)
                throw new TypeCheckerException("Expression has type " + exprType + " but was expected to have type bool", StartPosition);
            ResultType = new PrimitiveType(PrimitiveTypes.Bool);
        }));
        return actions;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();
        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Expr));
        actions.Add(new((gen) => gen.Not(IGenerator.RegisterUses.Return)));
        return actions;
    }
    
    public override string ToString()
    {
        return "(! " + Expr + ")";
    }
}

public class ExternExpression : SExpression
{
    public string Name { get; }
    public string Dll { get; }
    public Type[] Params { get; }
    public Type ReturnType { get; }
    public bool IsVarArgs { get; } // variadic functions

    public ExternExpression(string name, Type[] @params, Type returnType, StreamPosition position, string dll, bool isVarArgs) : base(position, new List<SExpression?>{})
    {
        Name = name;
        Params = @params;
        ReturnType = returnType;
        Dll = dll;
        IsVarArgs = isVarArgs;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        // just add the extern to the generator
        List<CodeGenerationExpression> actions = new()
        {
            new((gen) => gen.AddExtern(Name, Dll))
        };
        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        // just add the extern to the type checker
        return new()
        {
            new((typeChecker) => {
                typeChecker.CurrentScope.functions.Add(Name, new FunctionType(ReturnType, Params.ToList(), IsVarArgs));
            })
        };
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("(extern " + Name + " (");
        foreach (var param in Params)
        {
            sb.Append(param + " ");
        }

        sb.Append(") " + ReturnType + " \"" + Dll + "\")");
        return sb.ToString();
    }

}

// Get a pointer to an array element
public class IndexExpression : SExpression
{
    public SExpression Array { get; }
    public SExpression Index { get; }

    public IndexExpression(SExpression array, SExpression index, StreamPosition position) : base(position, new List<SExpression?>{array, index})
    {
        Array = array;
        Index = index;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();

        // we need to get the internal type first
        PointerType type = Array.ResultType! as PointerType ?? throw new TypeCheckerException("Cannot index a non-pointer type", Array.StartPosition);


        // first we need to load the array to the return register
        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Array));

        // we need to push the result because the other operation can modify all the registers
        actions.Add(new((gen) => gen.PushTmp(IGenerator.RegisterUses.Return)));

        // first calculate the index
        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Index));

        // load the size of the type into the second scratch register
        actions.Add(new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Scratch2, generator.SizeOf(type.Type!))));

        // multiply the index by the size of the type
        actions.Add(new((gen) => gen.Multiply(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch2)));

        // pop the array from the stack
        actions.Add(new((gen) => gen.PopTmp(IGenerator.RegisterUses.Scratch1)));

        // add the index to the array
        actions.Add(new((gen) => gen.Add(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Scratch1)));

        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        return new()
        {
            new(Array),
            new(Index),
            new((typeChecker) => {
                if(Array.ResultType is not PointerType ptrType)
                    throw new TypeCheckerException("Cannot index a non-pointer type", Array.StartPosition);
                if(Index.ResultType is not PrimitiveType primType || !primType.IsIntType())
                    throw new TypeCheckerException("Index must be of type int, not " + Index.ResultType, Index.StartPosition);
                ResultType = ptrType;
            })
        };
    }

    public override string ToString()
    {
        return "(index " + Array + " " + Index + ")";
    }

    public override int GetTempVarSize()
    {
        // we need 1 temporary variable for the index
        return 8;
    }
}

public class SizeOfExpression : SExpression
{
    public Type Type { get; }

    public SizeOfExpression(Type type, StreamPosition position) : base(position, new List<SExpression?>{})
    {
        Type = type;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        return new List<CodeGenerationExpression>
        {
            new((gen) => gen.LoadToRegister(IGenerator.RegisterUses.Return, generator.SizeOf(Type)))
        };
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        return new()
        {
            new((typeChecker) => {
                ResultType = new PrimitiveType(PrimitiveTypes.Int);
            })
        };
    }

    public override string ToString()
    {
        return "(sizeof " + Type + ")";
    }
}

public class ListExpression : SExpression
{
    public SExpression[] Items { get; } // this is stored on the stack

    public ListExpression(SExpression[] items, StreamPosition position) : base(position, new List<SExpression?>(items))
    {
        Items = items;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();
        // we need a unique label for the list
        string label = generator.GenerateUniqueLabel();
        // first allocate space for the list
        actions.Add(new((gen) => gen.AllocateVariable(label, new ListType(Items[0].ResultType!, Items.Length))));
        // then we need to store the items in the list
        for (int i = 0; i < Items.Length; i++)
        {
            int j = i;
            actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Items[j]));
            actions.Add(new((gen) => 
                {
                    KeyValuePair<int, Type> offset = gen.FindVariable(label); // offset of the list on the stack
                    gen.StoreToMemory(offset.Key + j * Items[j].ResultType!.SizeOf(), IGenerator.RegisterUses.Return, Items[j].ResultType!);
                }
            ));
        }

        // now we need to load the address of the list into the return register
        actions.Add(new((gen) => gen.LoadVariableAddress(IGenerator.RegisterUses.Return, label)));
        return actions;
    }

    public override int GetLocalSize()
    {
        // list is stored on the stack
        // we can't store this in temp space because this needs to persists across expressions, which temp space does not

        // we must align the size to 8 bytes even if the size is not a multiple of 8 (might happen if sizeof(type) is not a multiple of 8)
        int size = Items.Length * Items[0].ResultType!.SizeOf();
        Console.WriteLine("Size of list: " + size + " aligned to " + (size + (8 - size % 8)));
        return size + (8 - size % 8);
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("(array");
        foreach (var item in Items)
        {
            sb.Append(" " + item);
        }

        sb.Append(")");
        return sb.ToString();
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        List<TypeCheckerExpression> actions = new();
        // check the types of the items
        foreach (var item in Items)
        {
            actions.Add(new(item));
        }

        // check that all the items have the same type
        actions.Add(new((typeChecker) => {
            if(Items.Length > 0)
            {
                Type itemType = Items[0].ResultType ?? throw new TypeCheckerException("List literal has no type", StartPosition);
                for (int i = 1; i < Items.Length; i++)
                {
                    if(!Items[i].ResultType!.Equals(itemType))
                        throw new TypeCheckerException("List literal item " + i + " has type " + Items[i].ResultType + " but was expected to have type " + itemType, Items[i].StartPosition);
                }
                ResultType = new ListType(itemType, Items.Length);
            }
            else
                throw new TypeCheckerException("List literal has no items", StartPosition);
        }));

        return actions;
    }
}

public class CastExpression : SExpression
{
    public Type Type { get; }
    public SExpression Expr { get; }

    public CastExpression(Type type, SExpression expr, StreamPosition position) : base(position, new List<SExpression?>{expr})
    {
        Type = type;
        Expr = expr;
    }

    public override List<CodeGenerationExpression> GenerateCode(IGenerator generator)
    {
        List<CodeGenerationExpression> actions = new();
        actions.AddRange(generator.LoadToRegister(IGenerator.RegisterUses.Return, Expr));

        // now we need to cast the return register to the type
        actions.Add(new((gen) => gen.Cast(IGenerator.RegisterUses.Return, Type, Expr.ResultType!)));

        return actions;
    }

    public override List<TypeCheckerExpression> CheckTypes(TypeChecker typeChecker)
    {
        return new()
        {
            new(Expr),
            new((typeChecker) => {
                // check that the cast is valid, for now only allow primitive types to be cast or from a pointer to a pointer
                if(Expr.ResultType is not PrimitiveType && Expr.ResultType is not PointerType)
                    throw new TypeCheckerException("Cannot cast type " + Expr.ResultType, Expr.StartPosition);
                if(Type is not PrimitiveType && Type is not PointerType)
                    throw new TypeCheckerException("Cannot cast to type " + Type, StartPosition);
                ResultType = Type;
            })
        };
    }

    public override string ToString()
    {
        return "(cast " + Type + " " + Expr + ")";
    }
}