
// we want to avoid recursion in code generation, so we use a stack and make each SExpression add to the stack sub operations to the stack.
// this also means we need another class that represents a subcomponent of an SExpression, so we can add to the stack without recursion
// each SExpression will expand


namespace BaD;

public class CodeGenerationExpression
{
    // function to take in an IGenerator, return nothing
    public Action<IGenerator> OnGenerate { get; }

    
    public CodeGenerationExpression(SExpression expression)
    {
        OnGenerate = (generator) =>
        {
            List<CodeGenerationExpression> expressions = expression.GenerateCode(generator);
            generator.PushOperations(expressions);
        };
    }

    public CodeGenerationExpression(Action<IGenerator> onGenerate)
    {
        OnGenerate = onGenerate;
    }

    public void GenerateCode(IGenerator generator)
    {
        OnGenerate(generator);
    }
}