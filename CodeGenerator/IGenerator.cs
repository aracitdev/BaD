namespace BaD;
public interface IGenerator
{
    // we abstract away the registers into their usage cases, so we can easily allow for other architectures
    // assume 4 arg registers, 1 return register, 2 scratch registers
    // we want to identify each unique mapping of a register to a use case
    enum RegisterUses
    {
        Return, Arg1, Arg2, Arg3, Arg4, Scratch1, Scratch2, Stack
    }

    void Generate();
    void PushTmp(Int64 value);
    void PushTmp(RegisterUses reg);
    void PushTmp(string value);
    void PopTmp(RegisterUses reg);


    void LoadToRegister(RegisterUses reg, Int64 value);
    void LoadToRegister(RegisterUses reg, string value);
    void LoadToRegister(RegisterUses reg, RegisterUses value);

    void Cast(IGenerator.RegisterUses reg, Type from, Type to);

    // optomized for the platform, so we need to generate the code in a platform specific way
    List<CodeGenerationExpression> LoadToRegister(RegisterUses reg, SExpression expression);

    void LoadFromMemory(RegisterUses reg, string variable);
    public void LoadFromMemory(IGenerator.RegisterUses reg, IGenerator.RegisterUses pointer, Type type);


    void StoreToMemory(string variable, RegisterUses reg); // store a value in a variable on the stack, should resolve type
    void StoreToMemory(int offset, RegisterUses reg, Type type); // store a value in a variable on the stack
    void StoreToMemory(RegisterUses pointer, RegisterUses reg, Type type); // store a value in a variable on the stack

    public void LoadFunctionAddress(RegisterUses reg, string name);
    public void LoadVariableAddress(RegisterUses reg, string name);

    KeyValuePair<int, Type> FindVariable(string name); // find a variable on the stack

    public int PushTmpSize(int size);
    public int PopTmpSize(int size);

    bool IsFunction(string name); // check if a name is a function
    bool IsVariable(string name); // check if a name is a variable

    int AllocateVariable(string name, Type typeSize); // allocate a variable on the stack
    void Add(RegisterUses left, RegisterUses right); // add two registers together
    void Subtract(RegisterUses left, RegisterUses right); // subtract two registers
    void Multiply(RegisterUses left, RegisterUses right); // multiply two registers
    void Divide(RegisterUses left, RegisterUses right); // divide two registers

    // Logical operations
    public void Not(RegisterUses reg); // not a register
    public void And(RegisterUses left, RegisterUses right);
    public void Or(RegisterUses left, RegisterUses right);
    public void Compare(IGenerator.RegisterUses left, IGenerator.RegisterUses right, TokenType type);

    public List<CodeGenerationExpression> Function(FuncExpression ce);

    // extremely platform specific, so we need to generate the function in a platform specific way, so it's best to handle it here
    List<CodeGenerationExpression> Call(FunctionCallExpression ce); // call a function
    List<CodeGenerationExpression> Block(BlockExpression ce); // generate a block of code
    void ReturnFromFunction(); // return from the current function
    void AddExtern(string fn, string dll); // add an external function to the list of imports
    public void PushOperations(List<CodeGenerationExpression> expressions);

    public string GenerateUniqueName(); // generate a unique name for a variable
    public string GenerateUniqueLabel(); // generate a unique label for a jump
    public void Jump(string label); // jump to a label 
    public void JumpIfZero(RegisterUses reg, string label); // jump to a label if the register is zero  
    public void CreateLabel(string label); // create a label

    int SizeOf(Type var); // get the size of a type
}