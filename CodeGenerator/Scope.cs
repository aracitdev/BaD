using System.Text;

namespace BaD;

class Scope
{
    public Dictionary<string, KeyValuePair<int, Type>> variables = new();
    public Stack<int> tmpVariableSizes = new();

    public Scope()
    {
    }

    public void GenerateStack(SExpression expression, IGenerator codeGen, bool isBlock)
    {
        // [arguments, shadow space, return address, local variables, arguments to child functions, shadow space]

        // find the number of local variables
        int localSize = 0;
        int maxArgCount = 0; // if there is no function call, this will be zero, but otherwise it'll be at least 4
        
        SExpressionVisitor visitor = new(
            (SExpression expression) =>
            {
                localSize += expression.GetLocalSize(); // the ammount of stack space this expression needs
                if(expression is FunctionCallExpression fnCall)
                    maxArgCount = Math.Max(Math.Max(maxArgCount, fnCall.Args.Length), 4); // reserve space for 4 arguments
                // ignore block expressions, as they make their own scope
                if(expression is BlockExpression)
                    return new();
                return expression.Children;
            }
        );

        for(int i=0; i<expression.Children.Count; i++)
        {
            if (expression.Children[i] != null)
            {
                visitor.Visit(expression.Children[i]);
                tmpSize = Math.Max(tmpSize, SExpression.GetTempVarSize(expression.Children[i]!));
            }
        }

        // find the maximum number of arguments to a child function (minimum is 4, this is shadow space for the callee)
        scopeSize = maxArgCount + (localSize / 8) + (tmpSize / 8); // shadow space is already in maxArgCount, tmpsize is the max of temporary variables

        // double check that scopeSize + 1 is a multiple of 2
        scopeSize += (scopeSize + 1) % 2; // align the stack

        // now we need to set up the different spaces
        arguments = new(0, maxArgCount * 8 - 8); // arguments to children (including shadow space)
        tmpSpace = new(maxArgCount * 8, maxArgCount * 8 + tmpSize); // temporary variables
        localVariables = new(maxArgCount * 8 + tmpSize, maxArgCount * 8 + tmpSize + localSize); // local variables


        offset = scopeSize * 8; // start at the top of the stack
        tmpOffset = scopeSize * 8 - localSize; // start right after the local variables
        this.isBlock = isBlock;
    }

    public int GetArgOffset(int i)
    {
        // we need to move the full stack size + 1 to get to the arguments
        return (scopeSize + 1) * 8 + i * 8;
    }

    public void AddParameter(string name, int i, Type type )
    {
        variables[name] = new(GetArgOffset(i), type);
    }

    public int GetChildArgOffset(int i)
    {
        // these are the bottom of the stack, so offset is zero
        int v = i * 8;
        //double check bounds
        if(!arguments.Bound(v))
            throw new Exception("Tried to access an argument outside of the bounds of the argument space");
        return v;
    }

    public int GetLocalOffset(string name)
    {
        return variables[name].Key;
    }

    public Type GetLocalType(string name)
    {
        return variables[name].Value;
    }

    public void AddLocal(string name,Type type)
    {
        if(string.IsNullOrEmpty(name))
            throw new Exception("Tried to add a variable with no name");
        if(variables.ContainsKey(name))
            throw new Exception("Variable already exists: " + name);
        // double check that offset is 8 byte aligned
        if(offset % 8 != 0)
            throw new Exception("Tried to add a local variable that is not 8 byte aligned");

        // check that, if for whatever reason, we are adding a local variable to a block, that we have enough space
        if(offset - type.LocalSize() < 0)
            throw new Exception($"Tried to add a local variable to a block with not enough space {offset} < {type.LocalSize()} {name} {type}");

        // double check local variables bounds
        offset -= type.LocalSize();
        if(!localVariables.Bound(offset))
            throw new Exception($"Tried to add a local variable outside of the bounds of the local variable space {offset} < {localVariables.min}, {name} {type}");
        variables[name] = new(offset, type);
    }

    public void PushTmp(int size)
    {
        if(allocatedTempSize + size > tmpSize)
            throw new Exception($"Tried to push a temporary variable when temporary stack space is full {allocatedTempSize} + {size} > {tmpSize}");
        // double check that offset is 8 byte aligned
        if(tmpOffset % 8 != 0)
            throw new Exception("Tried to push a temporary variable that is not 8 byte aligned");
        allocatedTempSize += size;
        tmpVariableSizes.Push(size);
        tmpOffset -= size;
        if(tmpOffset < tmpSpace.min)
            throw new Exception($"Tried to push a temporary variable outside of the bounds of the temporary variable space {tmpOffset} < {tmpSpace.min}");
    }

    public void PopTmp(int size)
    {
        if(tmpVariableSizes.Pop() != size)
            throw new Exception("Tried to pop a temporary variable of the wrong size");
        tmpOffset += size;
        allocatedTempSize -= size;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        // start from the top
        sb.AppendLine($"Scope: {scopeSize * 8} bytes");
        sb.AppendLine($"Arguments: {arguments.min} - {arguments.max}");
        sb.AppendLine($"Temporary Variables: {tmpSpace.min} - {tmpSpace.max}");
        sb.AppendLine($"Local Variables: {localVariables.min} - {localVariables.max}");
        // now list the variables
        foreach(var pair in variables)
        {
            sb.AppendLine($"{pair.Key} : {pair.Value}");
        }
        return sb.ToString();
    }

    public int offset = 0; // currentlty used offset on the stack, decremented by 8 for each variable
    public int scopeSize = 0; // size of the scope
    public int tmpOffset = 0; // offset for temporary stack variables that are pushed and popped on demand
    public int tmpSize = 0; // size of the temporary stack variables
    public int allocatedTempSize = 0; // size of the temporary stack variables that have been allocated
    public bool isBlock = false; // if this is a block, we need to generate the stack differently, since we have no return address

    // min and max values of different spaces on the stack
    public struct StackSpace
    {
        public StackSpace(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public int min;
        public int max;
        public bool Bound(int value)
        {
            return value >= min && value <= max;
        } // true if the value is within the bounds
    }

    StackSpace arguments = new(0, 0); // arguments to children (including shadow space)
    StackSpace tmpSpace = new(0, 0); // temporary variables
    StackSpace localVariables = new(0, 0); // local variables
}
