// generates x86_64 assembly code from a given AST, targetting the FASM assembler

using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using System.Text;

namespace BaD;

public class x86_64CodeGenerator : IGenerator
{
    private Dictionary<int, string> SizeToType = new()
    {
        {1, "byte"},
        {2, "word"},
        {4, "dword"},
        {8, "qword"}
    };

    public Stack<CodeGenerationExpression> operationsStack = new Stack<CodeGenerationExpression>();

    static int uniqueVariableCount = 0;

    public string GenerateUniqueName()
    {
        return "BaDVar" + uniqueVariableCount++;
    }

    public void PushOperations(List<CodeGenerationExpression> expressions)
    {
        // since these are in the order we want to execute them, we need to reverse the list while adding them to the stack
        for(int i = expressions.Count - 1; i >= 0; i--)
            operationsStack.Push(expressions[i]);
    }

    public int SizeOf(Type var) // get the size of a type
    {
        return var.SizeOf();
    }

    public void ProcessOperations()
    {
        while(operationsStack.Count > 0)
        {
            operationsStack.Pop().GenerateCode(this);
        }
    }

    int labelCount = 0;

    public string GenerateUniqueLabel()
    {
        return "BaDLabel" + labelCount++;
    }

    public void CreateLabel(string label)
    {
        writer.WriteLine($"{label}:");
    }

    public void Jump(string label)
    {
        writer.WriteLine($"jmp {label} ; jump to label {label}");
    }

    public void JumpIfZero(IGenerator.RegisterUses reg, string label)
    {
        writer.WriteLine($"test {RegisterMapping[reg]}, {RegisterMapping[reg]} ; test if {RegisterMapping[reg]} is zero");
        writer.WriteLine($"jz {label} ; jump to label {label} if {RegisterMapping[reg]} is zero");
    }

    enum Registers
    {
        RAX,RCX,RDX,RBX,RSP,RBP,RSI,RDI,R8,R9,R10,R11,R12,R13,R14,R15,

        // 8 bit registers
        AL,CL,DL,BL,SPL,BPL,SIL,DIL,R8L,R9L,R10L,R11L,R12L,R13L,R14L,R15L,

        // 16 bit registers
        AX,CX,DX,BX,SP,BP,SI,DI,R8W,R9W,R10W,R11W,R12W,R13W,R14W,R15W,

        // 32 bit registers
        EAX,ECX,EDX,EBX,ESP,EBP,ESI,EDI,R8D,R9D,R10D,R11D,R12D,R13D,R14D,R15D,
    }

    public string MapRegister(IGenerator.RegisterUses reg, int size)
    {
        return size switch
        {
            1 => ByteRegisterMapping[reg].ToString(),
            2 => WordRegisterMapping[reg].ToString(),
            4 => DWordRegisterMapping[reg].ToString(),
            8 => RegisterMapping[reg].ToString(),
            _ => throw new Exception($"Invalid register size {size} for register {reg}")
        };
    }

    private readonly Dictionary<IGenerator.RegisterUses, Registers> RegisterMapping = new()
    {
        { IGenerator.RegisterUses.Return, Registers.RAX },
        { IGenerator.RegisterUses.Arg1, Registers.RCX },
        { IGenerator.RegisterUses.Arg2, Registers.RDX },
        { IGenerator.RegisterUses.Arg3, Registers.R8 },
        { IGenerator.RegisterUses.Arg4, Registers.R9 },
        { IGenerator.RegisterUses.Scratch1, Registers.R10 },
        { IGenerator.RegisterUses.Scratch2, Registers.R11 },
        { IGenerator.RegisterUses.Stack, Registers.RSP }
    };

    private readonly Dictionary<IGenerator.RegisterUses, Registers> ByteRegisterMapping = new()
    {
        { IGenerator.RegisterUses.Return, Registers.AL },
        { IGenerator.RegisterUses.Arg1, Registers.CL },
        { IGenerator.RegisterUses.Arg2, Registers.DL },
        { IGenerator.RegisterUses.Arg3, Registers.R8L },
        { IGenerator.RegisterUses.Arg4, Registers.R9L },
        { IGenerator.RegisterUses.Scratch1, Registers.R10L },
        { IGenerator.RegisterUses.Scratch2, Registers.R11L },
        { IGenerator.RegisterUses.Stack, Registers.SPL }
    };

    private readonly Dictionary<IGenerator.RegisterUses, Registers> WordRegisterMapping = new()
    {
        { IGenerator.RegisterUses.Return, Registers.AX },
        { IGenerator.RegisterUses.Arg1, Registers.CX },
        { IGenerator.RegisterUses.Arg2, Registers.DX },
        { IGenerator.RegisterUses.Arg3, Registers.R8W },
        { IGenerator.RegisterUses.Arg4, Registers.R9W },
        { IGenerator.RegisterUses.Scratch1, Registers.R10W },
        { IGenerator.RegisterUses.Scratch2, Registers.R11W },
        { IGenerator.RegisterUses.Stack, Registers.SP }
    };

    private readonly Dictionary<IGenerator.RegisterUses, Registers> DWordRegisterMapping = new()
    {
        { IGenerator.RegisterUses.Return, Registers.EAX },
        { IGenerator.RegisterUses.Arg1, Registers.ECX },
        { IGenerator.RegisterUses.Arg2, Registers.EDX },
        { IGenerator.RegisterUses.Arg3, Registers.R8D },
        { IGenerator.RegisterUses.Arg4, Registers.R9D },
        { IGenerator.RegisterUses.Scratch1, Registers.R10D },
        { IGenerator.RegisterUses.Scratch2, Registers.R11D },
        { IGenerator.RegisterUses.Stack, Registers.ESP }
    };

    public int PushTmpSize(int size)
    {
        scopes.Last().PushTmp(size);
        // we want to return the rsp offset for the value
        return scopes.Last().tmpOffset;
    }

    public int PopTmpSize(int size)
    {
        int offset = scopes.Last().tmpOffset;
        scopes.Last().PopTmp(size);
        // we want to return the rsp offset for the value
        return offset;
    }

    public void PushTmp(Int64 value)
    {
        // doesn't actually adjust the stack pointer, moves the value into a temp position on the stack
        scopes.Last().PushTmp(8);
        writer.WriteLine($"mov [rsp + {scopes.Last().tmpOffset}], {value} ; push the integer literal {value} onto the stack");
    }

    public void PushTmp(IGenerator.RegisterUses reg)
    {
        scopes.Last().PushTmp(8);
        writer.WriteLine($"mov [rsp + {scopes.Last().tmpOffset}], {RegisterMapping[reg]} ; push the register {RegisterMapping[reg]} onto the stack");
    }

    public void PushTmp(string value)
    {
        scopes.Last().PushTmp(8);
        writer.WriteLine($"mov [rsp + {scopes.Last().tmpOffset}], stringLiteral{stringLiterals[value]} ; push the string literal {value} onto the stack");
    }

    public void PopTmp(IGenerator.RegisterUses reg)
    {       
        writer.WriteLine($"mov {RegisterMapping[reg]}, [rsp + {scopes.Last().tmpOffset}] ; pop the value from the stack into {RegisterMapping[reg]}");
        scopes.Last().PopTmp(8);
    }

    public void LoadFromMemory(IGenerator.RegisterUses reg, string variable)
    {
        KeyValuePair<int, Type> offset = FindVariable(variable);
        // get the size of the type
        int size = SizeOf(offset.Value);
        bool IsSigned = offset.Value.IsSigned();
        string typeIdentifier = SizeToType[size];
        if(size == 8)
        {
            if(IsSigned)
                writer.WriteLine($"mov {RegisterMapping[reg]}, [rsp + {offset.Key}] ; load the value in {variable} into {RegisterMapping[reg]}");
            else
                writer.WriteLine($"mov {RegisterMapping[reg]}, [rsp + {offset.Key}] ; load the value in {variable} into {RegisterMapping[reg]}");
        }
        else
        if(size == 4)
        {
            if(IsSigned)
                writer.WriteLine($"movsxd {RegisterMapping[reg]}, [rsp + {offset.Key}] ; sign extend {variable} into {RegisterMapping[reg]}");
            else
                writer.WriteLine($"mov {RegisterMapping[reg]}, [rsp + {offset.Key}] ; load the value in {variable} into {RegisterMapping[reg]}");
        }
        else
        {
            if(IsSigned)
                writer.WriteLine($"movsx {RegisterMapping[reg]}, {typeIdentifier} [rsp + {offset.Key}] ; load the value in {variable} into {RegisterMapping[reg]}");
            else
                writer.WriteLine($"movzx {RegisterMapping[reg]}, {typeIdentifier} [rsp + {offset.Key}] ; load the value in {variable} into {RegisterMapping[reg]}");
        }

    }

    public void LoadFromMemory(IGenerator.RegisterUses reg, IGenerator.RegisterUses pointer, Type tp)
    {
        int size = SizeOf(tp);
        bool IsSigned = tp.IsSigned();
        string typeIdentifier = SizeToType[size];


        if(size == 8)
        {
            if(IsSigned)
                writer.WriteLine($"mov {RegisterMapping[reg]}, [{RegisterMapping[pointer]}] ; load the value in {RegisterMapping[pointer]} into {RegisterMapping[reg]}");
            else
                writer.WriteLine($"mov {RegisterMapping[reg]}, [{RegisterMapping[pointer]}] ; load the value in {RegisterMapping[pointer]} into {RegisterMapping[reg]}");
        }
        else
        if(size == 4) // special case for 32 bit to 64 bit, since we don't need to clear the upper bits
        {
            if(IsSigned)
                writer.WriteLine($"movsxd {RegisterMapping[reg]}, [{RegisterMapping[pointer]}] ; sign extend {RegisterMapping[pointer]} into {RegisterMapping[reg]}");
            else
                writer.WriteLine($"mov {RegisterMapping[reg]}, [{RegisterMapping[pointer]}] ; load the value in {RegisterMapping[pointer]} into {RegisterMapping[reg]}");
        }
        else
        {
            if(IsSigned)
                writer.WriteLine($"movsx {RegisterMapping[reg]}, {typeIdentifier} [{RegisterMapping[pointer]}] ; load the value in {RegisterMapping[pointer]} into {RegisterMapping[reg]}");
            else
                writer.WriteLine($"movzx {RegisterMapping[reg]}, {typeIdentifier} [{RegisterMapping[pointer]}] ; load the value in {RegisterMapping[pointer]} into {RegisterMapping[reg]}");
        }
    }

    public List<CodeGenerationExpression> LoadToRegister(IGenerator.RegisterUses reg, SExpression expression)
    {
        switch (expression)
        {
            case IntLiteralExpression ile:
                return new() { new CodeGenerationExpression( (generator) => generator.LoadToRegister(reg, ile.Value)) };
            case StringLiteralExpression sle:
                return new() { new CodeGenerationExpression( (generator) => generator.LoadToRegister(reg, sle.Value)) };
            case IdentifierExpression ie:
                if(IsVariable(ie.Name))
                {
                    return new() { new CodeGenerationExpression( (generator) => 
                    { 
                            generator.LoadFromMemory(reg, ie.Name); 
                    } ) };
                }
                else
                {
                    return new() { new CodeGenerationExpression( (generator) => generator.LoadFunctionAddress(reg, ie.Name)) };
                }
            default:
                List<CodeGenerationExpression> expressions = new()
                {
                    new CodeGenerationExpression(expression)
                };
                // expect the result to be in RAX
                // test comment in asm
                if(reg != IGenerator.RegisterUses.Return)
                    expressions.Add(new CodeGenerationExpression( (generator) => generator.LoadToRegister(reg, IGenerator.RegisterUses.Return)));
                return expressions;
        }
    }

    
    public void Cast(IGenerator.RegisterUses reg, Type from, Type to)
    {
        if(from == to)
            return;
        
        int lSize = SizeOf(from);
        int rSize = SizeOf(to);

        string fromRegister = MapRegister(reg, lSize);
        string toRegister = MapRegister(reg, rSize);

        // if fromSize < toSize, we need to zero/sign extend (depending on the sign of the type)
        // if fromSize > toSize, we need to truncate. we want to clear the upper bits and keep the lower bits, so instruction movzx/movsx [64 bit to register] [mapped right (can be 8, 16, 32 bit register)]

        if(lSize < rSize)
        {            
            // upcast, we need to sign/zero extend
            string register64 = MapRegister(reg, 8); // we want to move to the 64 bit register from the 8, 16, 32 bit register and sign/zero extend
            string instruction = from.IsSigned() ? (lSize == 4 ? "movsxd" : "movsx") : (lSize == 4 ? "mov" : "movzx"); // 32->64 bit is special case
            writer.WriteLine($"{instruction} {register64}, {fromRegister} ; sign/zero extend {fromRegister} into {register64}");
        }
        else
        if(lSize > rSize)
        {
            string register64 = MapRegister(reg, 8); // we want to move to the 64 bit register from the 8, 16, 32 bit register and sign/zero extend
            string instruction = from.IsSigned() ? (rSize == 4 ? "movsxd" : "movsx") : (rSize == 4 ? "mov" : "movzx"); // 32->64 bit is special case
            // toRegister is our target (downcasted) size, fromRegister is the 64 bit register
            // we know both represent the same register, so just move toRegister into the target and sign/zero extend

            writer.WriteLine($"{instruction} {register64}, {toRegister} ; move {register64} into {toRegister} ; truncate {register64} into {toRegister}");
        }
        else
        {
            writer.WriteLine($"mov {toRegister}, {fromRegister} ; move {fromRegister} into {toRegister}");
        }
    }



    public void LoadToRegister(IGenerator.RegisterUses reg, Int64 value)
    {
        writer.WriteLine($"mov {RegisterMapping[reg]}, {value} ; load the integer literal {value} into {RegisterMapping[reg]}");
    }

    public void LoadToRegister(IGenerator.RegisterUses reg, string value)
    {
        string commentName = value.Replace("\n", "\\n").Replace("\r", "\\r");
        writer.WriteLine($"mov {RegisterMapping[reg]}, stringLiteral{stringLiterals[value]} ; load the string literal {commentName} into {RegisterMapping[reg]}");
    }

    public void LoadToRegister(IGenerator.RegisterUses reg, IGenerator.RegisterUses value)
    {
        writer.WriteLine($"mov {RegisterMapping[reg]}, {RegisterMapping[value]} ; load the register {RegisterMapping[value]} into {RegisterMapping[reg]}");
    }

    public void LoadFunctionAddress(IGenerator.RegisterUses reg, string name)
    {
        writer.WriteLine($"mov {RegisterMapping[reg]}, {name} ; load the address of the function {name} into {RegisterMapping[reg]}");
    }

    public void LoadVariableAddress(IGenerator.RegisterUses reg, string name)
    {
        KeyValuePair<int, Type> offset = FindVariable(name);
        writer.WriteLine($"lea {RegisterMapping[reg]}, [rsp + {offset.Key}] ; load the address of the variable {name} into {RegisterMapping[reg]}");
    }

    public int AllocateVariable(string name, Type type)
    {
        scopes.Last().AddLocal(name, type);
        return scopes.Last().GetLocalOffset(name);
    }

    public void ReturnFromFunction()
    {
        writer.WriteLine($"add rsp, {scopes.Last().scopeSize * 8} ; clean up the stack");
        writer.WriteLine("ret ; return from the function");
    }


    public void Add(IGenerator.RegisterUses left, IGenerator.RegisterUses right) // add two registers together
    {
        writer.WriteLine($"add {RegisterMapping[left]}, {RegisterMapping[right]} ; add {RegisterMapping[right]} to {RegisterMapping[left]}");
    }

    public void Subtract(IGenerator.RegisterUses left, IGenerator.RegisterUses right)
    {
        writer.WriteLine($"sub {RegisterMapping[left]}, {RegisterMapping[right]} ; subtract {RegisterMapping[right]} from {RegisterMapping[left]}");
    }

    public void Multiply(IGenerator.RegisterUses left, IGenerator.RegisterUses right)
    {
        writer.WriteLine($"imul {RegisterMapping[left]}, {RegisterMapping[right]} ; multiply {RegisterMapping[left]} by {RegisterMapping[right]}");
    }

    public void Divide(IGenerator.RegisterUses left, IGenerator.RegisterUses right)
    {
        // we need to deal with signed division, so we need to sign extend rax into rdx
        writer.WriteLine($"cqo ; sign extend rax into rdx");
        writer.WriteLine($"idiv {RegisterMapping[right]} ; divide {RegisterMapping[left]} by {RegisterMapping[right]}");
    }

    public void Compare(IGenerator.RegisterUses left, IGenerator.RegisterUses right, TokenType type)
    {
        string op = type switch
        {
            TokenType.LessThan => "l",
            TokenType.LessThanOrEqual => "le",
            TokenType.GreaterThan => "g",
            TokenType.GreaterThanOrEqual => "ge",
            TokenType.Equal => "e",
            TokenType.NotEqual => "ne",
            _ => throw new Exception("Invalid comparison type")
        };
        writer.WriteLine($"cmp {RegisterMapping[left]}, {RegisterMapping[right]} ; compare {RegisterMapping[left]} and {RegisterMapping[right]}");
        writer.WriteLine($"set{op} al ; set al to 1 if the comparison is true, 0 otherwise");
        // we need the result in rax, so we need to move it there
        writer.WriteLine($"movzx rax, al ; move the result into rax");
    }

    public void And(IGenerator.RegisterUses left, IGenerator.RegisterUses right)
    {
        writer.WriteLine($"and {RegisterMapping[left]}, {RegisterMapping[right]} ; and {RegisterMapping[left]} and {RegisterMapping[right]}");
    }

    public void Or(IGenerator.RegisterUses left, IGenerator.RegisterUses right)
    {
        writer.WriteLine($"or {RegisterMapping[left]}, {RegisterMapping[right]} ; or {RegisterMapping[left]} and {RegisterMapping[right]}");
    }

    public void Not(IGenerator.RegisterUses reg)
    {
        // this is a logical not
        // if the register is zero, we want to set it to 1, otherwise we want to set it to zero
        writer.WriteLine($"test {RegisterMapping[reg]}, {RegisterMapping[reg]} ; test if {RegisterMapping[reg]} is zero");
        writer.WriteLine($"setz al ; set al to 1 if {RegisterMapping[reg]} is zero, 0 otherwise");
        writer.WriteLine($"movzx {RegisterMapping[reg]}, al ; move the result into {RegisterMapping[reg]}");
    }

    public List<CodeGenerationExpression> Block(BlockExpression ce)
    {
        // blocks create a new scope / stack frame
        List<CodeGenerationExpression> expressions = new()
        {
            new CodeGenerationExpression((generator) =>
            {
                scopes.Add(new Scope());
                Scope currentScope = scopes.Last();
                currentScope.GenerateStack(ce, this, true);
                // reserve space for the stack, add 1 to the scope size to account for the return address even though we don't use it
                writer.WriteLine($";block start, scope size: { (currentScope.scopeSize+1) * 8}");
                writer.WriteLine($"sub rsp, {(currentScope.scopeSize+1) * 8} ; allocate space on the stack for local variables/args for children");
            })
        };

        // generate the block


        for(int i = 0; i < ce.Children.Count; i++)
        {
            expressions.Add(new CodeGenerationExpression(ce.Children[i]!));
        }

        // remove the scope / stack frame
        expressions.Add(new CodeGenerationExpression((generator) =>
        {
            Scope currentScope = scopes.Last();

            writer.WriteLine($"add rsp, {(currentScope.scopeSize+1) * 8} ; clean up the stack");
            writer.WriteLine($";block end, scope size: {(currentScope.scopeSize+1) * 8}");
            // remove the scope
            scopes.RemoveAt(scopes.Count - 1);
        }));

        return expressions;
    }


    public List<CodeGenerationExpression> Call(FunctionCallExpression ce)
    {
        int argCount = ce.Args.Length; // number of arguments, 4 or less are passed in registers, the rest on the stack
        Scope currentScope = scopes.Last();

        IGenerator.RegisterUses[] regEnum = new[] { IGenerator.RegisterUses.Arg1, IGenerator.RegisterUses.Arg2, IGenerator.RegisterUses.Arg3, IGenerator.RegisterUses.Arg4 };

        List<CodeGenerationExpression> expressions = new();

        // each argument can be modified by deeper expressions, so we need to generate the code and push the results onto the stack, then load them into the appropriate registers by popping them off the stack
        // we are using the temporary stack space to hold them during this time

        // first push in reverse order

        if(FunctionCallExpression.HasSubCall(ce))
        {
            for(int i = argCount - 1; i >= 0; i--)
            {
                int j = i;
                if(ce.Args[j] != null)
                {
                    // generate the arg
                    List<CodeGenerationExpression> argExpressions = LoadToRegister(IGenerator.RegisterUses.Return, ce.Args[j]);
                    expressions.AddRange(argExpressions);
                    // push it into the temporary stack space, assuming the result is in rax
                    expressions.Add(new CodeGenerationExpression( (generator) => 
                    {
                        generator.PushTmp(IGenerator.RegisterUses.Return);
                    }));
                }
            }

            // now pop them into the registers (or memory if there are more than 4 arguments)
            for(int i = 0; i < argCount; i++)
            {
                int j = i;
                if(j < 4)
                {
                    expressions.Add(new CodeGenerationExpression( (generator) => 
                    {
                        generator.PopTmp(regEnum[j]);
                    }));
                }
                else
                {
                    // load the argument from the stack
                    expressions.Add(new CodeGenerationExpression( (generator) => 
                    {
                        generator.LoadFromMemory(IGenerator.RegisterUses.Return, IGenerator.RegisterUses.Stack, ce.Args[j].ResultType!);
                    }));
                    // store it in the stack space for the child function
                    expressions.Add(new CodeGenerationExpression( (generator) => 
                    {
                        // get the offset for the argument
                        int offset = currentScope.GetChildArgOffset(j);
                        // store it using the offset
                        generator.StoreToMemory(offset, IGenerator.RegisterUses.Return, ce.Args[j].ResultType!);
                    }));
                }
            }
        }
        else
        {
            // we don't need to worry about pushing and popping temporary variables, we can just load the arguments into the registers/the stack directly
            for(int i = 0; i < argCount; i++)
            {
                int j = i;
                if(j < 4)
                {
                    // generate the arg
                    List<CodeGenerationExpression> argExpressions = LoadToRegister(regEnum[j], ce.Args[j]);
                    expressions.AddRange(argExpressions);
                }
                else
                {
                    // generate the arg
                    List<CodeGenerationExpression> argExpressions = LoadToRegister(IGenerator.RegisterUses.Return, ce.Args[j]);
                    expressions.AddRange(argExpressions);
                    // store it in the stack space for the child function
                    expressions.Add(new CodeGenerationExpression( (generator) => 
                    {
                        // get the offset for the argument
                        int offset = currentScope.GetChildArgOffset(j);
                        // store it using the offset
                        generator.StoreToMemory(offset, IGenerator.RegisterUses.Return, ce.Args[j].ResultType!);
                    }));
                }
            }
        }



        // call the function
        if(ce.Func is IdentifierExpression expression)
        {
            // check if it's an external function
            if(externalFunctions.Any(x => x.Value.Contains(expression.Name)))
            {
                expressions.Add(new CodeGenerationExpression( (generator) => 
                {
                    writer.WriteLine($"call [{expression.Name}]");
                }));
            }
            else
            if(IsFunction(expression.Name) == false)
            {
                // it must be a variable that holds a function pointer
                KeyValuePair<int, Type> offset = FindVariable(expression.Name);
                expressions.Add(new CodeGenerationExpression( (generator) => 
                {
                    writer.WriteLine($"mov rax, [rsp + {offset.Key}] ; load the function pointer into rax");
                    // call
                    writer.WriteLine($"call rax ; call the function");
                }));
            }
            else
                expressions.Add(new CodeGenerationExpression( (generator) => 
                {
                    writer.WriteLine($"call {expression.Name}");
                }));
        }
        else
        if(ce.Func.ResultType is FunctionType)
        {
            // we are calling a function pointer
            expressions.Add(new CodeGenerationExpression(ce.Func));

            // assume the result is in rax
            expressions.Add(new CodeGenerationExpression( (generator) => 
            {
                // call
                writer.WriteLine($"call rax ; call the function");
            }));
        }

        return expressions;
    }



    // calls and function generation are extremely platform specific, so we need to generate the function in a platform specific way, so it's best to handle it here
    public List<CodeGenerationExpression> Function(FuncExpression fde)
    {
        List<CodeGenerationExpression> expressions = new();

        expressions.Add( new CodeGenerationExpression( (generator) => 
        {
            // add to the list of functions
            functions[fde.Name] = 0;
            // generate the function prologue
            writer.WriteLine($"{fde.Name}:");

            int argCount = fde.Params.Length;
            // every argument is 8 bytes, we are assuming that we put the arguments on the stack as local variables

            var registers = new string[] { "rcx", "rdx", "r8", "r9" };

            scopes.Add(new Scope());
            Scope currentScope = scopes.Last();
            currentScope.GenerateStack(fde, this, false);

            // calculate the space needed for the stack
            if(currentScope.scopeSize > 0)
                writer.WriteLine($"sub rsp, {currentScope.scopeSize * 8} ; allocate space on the stack for local variables/args for children");

            // pull register arguments into arguments on the stack
            for(int i = 0; i < argCount; i++)
            {
                // first pull the arguments from the registers
                // add the variable to the current scope

                currentScope.AddParameter(fde.Params[i].Name, i, fde.Params[i].Type);
                int paramOffset = currentScope.GetArgOffset(i);
                if(i < 4)
                    writer.WriteLine($"mov [rsp + {paramOffset}], {registers[i]} ; move the argument {i} into the local variable");
            }
        }));

        // generate the function body
        for(int i = 0; i < fde.Body.Count; i++)
        {
            expressions.Add(new CodeGenerationExpression(fde.Children[i]!));
        }

        expressions.Add(new CodeGenerationExpression( (generator) => 
        {
            // generate the function epilogue
            Scope currentScope = scopes.Last();
            if(currentScope.scopeSize > 0)
                writer.WriteLine($"add rsp, {currentScope.scopeSize * 8} ; clean up the stack");
            writer.WriteLine("ret ; return from the function");
            scopes.RemoveAt(scopes.Count - 1);
        }));
        return expressions;
    }

    static readonly string[] RegisterNames = new string[] { "rax", "rcx", "rdx", "rbx", "rsp", "rbp", "rsi", "rdi", "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15" };

    // stream to write the assembly code to
    readonly private StreamWriter writer;
    private readonly List<SExpression> expressions;

    readonly private Dictionary<string, int> stringLiterals = new(); // string literals are stored in the data section
    readonly private Dictionary<string, int> functions = new(); // functions are stored in the text section
    readonly private List<KeyValuePair<string, List<string>>> externalFunctions = new(); // external functions are stored in the import section

    // keep track of scope


    public KeyValuePair<int, Type> FindVariable(string name)
    {
        int priorScopeSize = 0;
        // search the stack for the variable in reverse order, add the scope sizes together

        for(int i = scopes.Count - 1; i >= 0; i--)
        {
            if (scopes[i].variables.TryGetValue(name, out KeyValuePair<int, Type> result))
                return new KeyValuePair<int, Type>(priorScopeSize + result.Key, result.Value);
            priorScopeSize += scopes[i].scopeSize * 8;
            priorScopeSize += 8; // return address
        }
        

        throw new Exception("Variable not found: " + name);
    }

    // stack of scopes
    private readonly List<Scope> scopes = new ();

    public bool IsFunction(string name)
    {
        if(functions.ContainsKey(name))
        {
            return true;
        }
        return false;
    }

    public bool IsVariable(string name)
    {
        // check all the scopes for the variable
        for(int i = 0; i < scopes.Count; i++)
        {
            if(scopes[i].variables.ContainsKey(name))
                return true;
        }
        return false;
    }

    public x86_64CodeGenerator(string filename, List<SExpression> expressions)
    {
        // set up the writer to write to the given file
        writer = new StreamWriter(filename);
        this.expressions = expressions;

        // we need this for exitprocess, so add ExitProcess to the list of imports
        AddExtern("ExitProcess", "kernel32.dll");
        // we need this for getmainargs, so add __getmainargs to the list of imports
        AddExtern("__getmainargs", "msvcrt.dll");

        stringLiterals = new Dictionary<string, int>();
    }


    public void GenerateDataSection()
    {
        // string literals end up in the data section
        writer.WriteLine(@"section '.data' data readable writeable");
        
        // arguments to the main function
        writer.WriteLine("argc dq ? ; argc");
        writer.WriteLine("argv dq ? ; argv");
        writer.WriteLine("env dq ? ; env");
        writer.WriteLine("startInfo STARTUPINFO ; startInfo");


        SExpressionVisitor dataVisitor = new SExpressionVisitor(
            (SExpression expression) =>
            {
                if(expression is StringLiteralExpression sle)
                {
                    // first check if we already have this string literal
                    if(stringLiterals.ContainsKey(sle.Value))
                        return new(); // we already have this string literal

                    // fasm doesn't let us have newlines in string literals, so we need to replace them with , 0x0A, 0x0D
                    
                    string stringValue = sle.Value;
                    stringValue = stringValue.Replace("\n", "\", 0x0A, 0x0D, \"");

                    int currentString = stringLiterals.Count; // the current string literal count
                    writer.WriteLine($"stringLiteral{currentString}: db \"{stringValue}\", 0");
                    stringLiterals[sle.Value] = currentString;
                    return new();
                }
                else
                {
                    return expression.Children;
                }
            }
        );

        for(int i = 0; i < expressions.Count; i++)
            dataVisitor.Visit(expressions[i]);
    }

    public void AddExtern(string fn, string dll)
    {
        // add the function to the list of functions
        functions[fn] = 0;
        // add the function to the list of imports, if we already have the dll, add the function to the list of functions for that dll
        bool found = externalFunctions.Any(x => x.Key == dll.ToLower());

        if(found)
        {
            var pair = externalFunctions.First(x => x.Key == dll.ToLower());
            pair.Value.Add(fn);
        }
        else
        {
            externalFunctions.Add(new KeyValuePair<string, List<string>>(dll.ToLower(), new List<string> { fn }));
        }
    }

    public void StoreToMemory(string variable, IGenerator.RegisterUses reg)
    {
        KeyValuePair<int, Type> offset = FindVariable(variable);

        // get the size of the type
        int size = SizeOf(offset.Value);

        // we need to use different registers for different sizes
        string regName = MapRegister(reg, size);

        // we don't need to sign extend, since we are storing the value
        writer.WriteLine($"mov [rsp + {offset.Key}], {regName} ; store the value in {regName} into the variable {variable}");
    }

    public void StoreToMemory(IGenerator.RegisterUses pointer, IGenerator.RegisterUses reg, Type type)
    {
        // assume pointer is a pointer to some address in memory (gotten from lea)
        int size = SizeOf(type);
        
        string regName = MapRegister(reg, size);

        writer.WriteLine($"mov [{RegisterMapping[pointer]}], {regName} ; store the value in {regName} into the address in {RegisterMapping[pointer]}");
    }

    public void StoreToMemory(int offset, IGenerator.RegisterUses reg, Type type)
    {
        int size = SizeOf(type);
        string regName = MapRegister(reg, size);

        writer.WriteLine($"mov [rsp + {offset}], {RegisterMapping[reg]} ; store the value in {regName} into the variable at offset {offset}");
    }

    public void GenerateFunctions(SExpression fde)
    {
        if(fde == null)
            return; // do nothing for null expressions

        if(fde is FuncExpression || fde is ExternExpression)
        {
            PushOperations(new() { new CodeGenerationExpression(fde) });
            ProcessOperations();
        }
        else
        {
            throw new Exception("Only function and extern expressions are allowed at the top level"); 
        }
    }

    public void GenerateImportSection()
    {
        // import section
        writer.WriteLine(@"section '.idata' import data readable writeable");

        writer.Write("library ");
        for(int i = 0; i < externalFunctions.Count; i++)
        {
            string cleanedName = externalFunctions[i].Key.Replace(".dll", "");
            writer.Write(cleanedName + ", '" + externalFunctions[i].Key + "'");
            if(i < externalFunctions.Count - 1)
                writer.Write(",\\");
            writer.WriteLine();
        }

        writer.WriteLine();

        for(int i = 0; i < externalFunctions.Count; i++)
        {
            string cleanedName = externalFunctions[i].Key.Replace(".dll", "");
            writer.WriteLine("import " + cleanedName + ",\\");
            for(int j = 0; j < externalFunctions[i].Value.Count; j++)
            {
                writer.Write("    " + externalFunctions[i].Value[j] + ", '" + externalFunctions[i].Value[j] + "'");
                if(j < externalFunctions[i].Value.Count - 1)
                    writer.Write(",\\");
                writer.WriteLine();
            }
            writer.WriteLine();
        }
    }

    public void GenerateCodeSection()
    {
        writer.WriteLine(@"section '.text' code readable executable");        
        
        // generate code for each function
        for(int i = 0; i < expressions.Count; i++)
            GenerateFunctions(expressions[i]);
        
        // now generate the entrypoint function
        // align stack, get command line arguments, pass argc and argv to main, call main, exit
        writer.WriteLine(@"
_start:
    sub rsp, 40 ; align the stack and reserve shadow space for the callee
    ; we need to get the command line arguments from __getmainargs

    ; _getmainargs signature is int __cdecl _getmainargs(int *argc, char ***argv, char ***env, int doWildCard, _startupinfo *startInfo)

    lea rcx, [argc] ; pass the address of argc
    lea rdx, [argv] ; pass the address
    lea r8, [env] ; pass the address of env
    mov r9, 0 ; pass the address of the startInfo

    lea r11, [startInfo] ; pass the address of the startInfo
    mov [rsp + 32], r11 ; pass the address of the startInfo
    call [__getmainargs] ; get the command line arguments

    mov rcx, [argc] ; call main with argc
    mov rdx, [argv] ; call main with argv
    ; call main
    call main
    call [ExitProcess] ; exit
    add rsp, 40 ; clean up the stack
    ret ; return from the function
");
    }

    public void Generate()
    {
        // simplify all the arithmetic expressions
        for(int i = 0; i < expressions.Count; i++)
        {
            SExpressionVisitor visitor = new SExpressionVisitor(
                (SExpression expression) =>
                {
                    if(expression is ArithmeticExpression ae)
                    {
                        ae.SimplifyArithmeticExpression();
                        return new(); // we don't want to visit the children, we've already simplified the expression
                    }
                    return expression.Children;
                }
            );
            visitor.Visit(expressions[i]);
        }

        // windows executable header
        writer.WriteLine(@"format PE64 console 6.0
entry _start
include 'win64a.inc'");

        // data section
        GenerateDataSection();
        // add a test string to the data section, since we need to ALWAYS have a data section (for some reason windows doesn't like it if we don't)
        GenerateCodeSection();

    // exitprocess expects an aligned stack (for some reason)
    writer.WriteLine(@" ; ExitProcess(0)
    mov rcx, 0
    call [ExitProcess]");

    // now we return from the main function and clean up the stack
        writer.WriteLine(@"
    add rsp, 8 ; clean up the stack
    ret ; return from the function");

        GenerateImportSection();


        writer.Close();
    }
}