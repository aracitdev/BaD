namespace BaD;


public enum PrimitiveTypes
{
    Int, // 64 bit integer
    Int32, // 32 bit integer
    Int16, // 16 bit integer
    UInt, // 64 bit unsigned integer
    UInt32, // 32 bit unsigned integer
    UInt16, // 16 bit unsigned integer

    IntLiteral, // lets us automatically cast literals without needing to specify the type
    UChar, // 8 bit unsigned integer
    Char, // 8 bit integer
    Bool,
    Void
}

public abstract class Type
{
    // force Equals to be implemented
    public override abstract bool Equals(object? obj);
    public override abstract int GetHashCode();
    public abstract bool CanCastTo(Type other);
    public abstract int SizeOf();
    public abstract int LocalSize(); // size of the type on the stack
    public bool IsSigned() // whether the type should be sign extended 
    {
        if(this is PrimitiveType prim)
        {
            return prim.Primitive == PrimitiveTypes.Int || prim.Primitive == PrimitiveTypes.Int32 || prim.Primitive == PrimitiveTypes.Int16 || prim.Primitive == PrimitiveTypes.Char || prim.Primitive == PrimitiveTypes.IntLiteral;
        }
        return false;
    }
    
}

public class PrimitiveType : Type
{
    public readonly PrimitiveTypes Primitive;

    public PrimitiveType(PrimitiveTypes type)
    {
        this.Primitive = type;
    }

    public override string ToString()
    {
        return Primitive.ToString().ToLower();
    }

    public override bool Equals(object? obj)
    {
        if (obj is PrimitiveType prim)
        {
            return prim.Primitive == Primitive;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Primitive.GetHashCode();
    }


    // for implicit casting between types, checks if we can cast this to the specified type
    public override bool CanCastTo(Type other)
    {
        if(other is PrimitiveType prim)
        {
            if(Primitive == PrimitiveTypes.IntLiteral)
            {
                return true;
            }
        }


        // don't allow implicit casting between different types except for int literals
        return false;
    }

    public bool IsIntType()
    {
        return Primitive == PrimitiveTypes.Int || Primitive == PrimitiveTypes.Int32 || Primitive == PrimitiveTypes.Int16 || Primitive == PrimitiveTypes.IntLiteral;
    }

    public override int SizeOf()
    {
        switch(Primitive)
        {
            case PrimitiveTypes.Int:
            case PrimitiveTypes.UInt:
                return 8;
            case PrimitiveTypes.Int32:
            case PrimitiveTypes.UInt32:
                return 4;
            case PrimitiveTypes.Int16:
            case PrimitiveTypes.UInt16:
                return 2;
            case PrimitiveTypes.Char:
            case PrimitiveTypes.UChar:
                return 1;
            case PrimitiveTypes.Bool:
                return 1;
            case PrimitiveTypes.Void:
                return 0;


            case PrimitiveTypes.IntLiteral:
                return 8; // int literals are 8 bytes
            default:
                return 0;
        }
    }

    public override int LocalSize()
    {
        // on the stack, all primitive types are 8 bytes
        return 8;
    }
}

public class PointerType : Type
{
    public Type Type { get; }

    public PointerType(Type type)
    {
        this.Type = type;
    }

    public override string ToString()
    {
        return "(* " + Type + ")";
    }

    public override bool Equals(object? other)
    {
        if(other is PointerType ptr)
        {
            return ptr.Type.Equals(Type);
        }
        return false;
    }

    public override bool CanCastTo(Type other)
    {
        if(other is PointerType ptr)
        {
            // check if we are casting to a void pointer
            if(ptr.Type is PrimitiveType prim && prim.Primitive == PrimitiveTypes.Void)
            {
                return true;
            }

            // check if we are a void pointer
            if(Type is PrimitiveType prim2 && prim2.Primitive == PrimitiveTypes.Void)
            {
                return true;
            }

            return ptr.Type.Equals(Type);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }

    public override int SizeOf()
    {
        return 8; // all pointers are 8 bytes for now
    }

    public override int LocalSize()
    {
        return 8;
    }
}

// represents a function pointer type
public class FunctionType : Type
{
    public readonly Type returnType;
    public readonly List<Type> parameterTypes;

    public bool IsVarArgs { get; } = false;

    public FunctionType(Type returnType, List<Type> parameterTypes, bool isVarArgs = false)
    {
        this.returnType = returnType;
        this.parameterTypes = parameterTypes;
        this.IsVarArgs = isVarArgs;
    }

    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("(func (");
        if(IsVarArgs)
        {
            sb.Append("...");
        }
        else
        foreach (var param in parameterTypes)
        {
            sb.Append(param + " ");
        }
        sb.Append(") " + returnType + ")");
        return sb.ToString();
    }
        
    public override bool Equals(object? other)
    {
        if (other is FunctionType otherFunc)
        {
            if (otherFunc.returnType.Equals(returnType) && otherFunc.parameterTypes.Count == parameterTypes.Count)
            {
                for (int i = 0; i < parameterTypes.Count; i++)
                {
                    if (!otherFunc.parameterTypes[i].Equals(parameterTypes[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    public override bool CanCastTo(Type other)
    {
        // all parameters and return type must match
        if(other is FunctionType otherFunc)
        {
            if(otherFunc.returnType.Equals(returnType) && otherFunc.parameterTypes.Count == parameterTypes.Count)
            {
                for(int i = 0; i < parameterTypes.Count; i++)
                {
                    if(!otherFunc.parameterTypes[i].Equals(parameterTypes[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    public override int SizeOf()
    {
        return 8; // all function pointers are 8 bytes for now
    }

    public override int LocalSize()
    {
        return 8;
    }

    public override int GetHashCode()
    {
        return returnType.GetHashCode() + parameterTypes.GetHashCode();
    }
}

public class TypedIdentifier
{
    public string Name { get; }
    public Type Type { get; }

    public TypedIdentifier(string name, Type type)
    {
        Name = name;
        Type = type;
    }

    public override string ToString()
    {
        return "(" + Type + " " + Name + ")";
    }
}

public class ListType : Type
{
    public Type ElementType { get; }
    public int Length { get; }

    public ListType(Type elementType, int length)
    {
        ElementType = elementType;
        Length = length;
    }

    public override string ToString()
    {
        return "(list " + ElementType + " " + Length + ")";
    }

    public override bool Equals(object? other)
    {
        if(other is ListType list)
        {
            return list.ElementType.Equals(ElementType) && list.Length == Length;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return ElementType.GetHashCode() + Length.GetHashCode();
    }

    public override bool CanCastTo(Type other)
    {
        if(other is ListType list)
        {
            return list.ElementType.Equals(ElementType) && list.Length == Length;
        }

        // can cast to a pointer of the same type
        if(other is PointerType ptr)
        {
            return ptr.Type.Equals(ElementType);
        }
        return false;
    }

    public override int SizeOf()
    {
        int size = ElementType.SizeOf() * Length;
        // we need to align the size to 8 bytes
        if(size % 8 != 0)
        {
            size += 8 - (size % 8);
        }
        return size;
    }

    public override int LocalSize()
    {
        // localsize doesn't use the element local size, but it's actual size, since arrays don't have padding
        // normal stack values are always padded to 8 bytes
        int localSize = ElementType.SizeOf() * Length;
        // we need to align the size to 8 bytes
        if(localSize % 8 != 0)
        {
            localSize += 8 - (localSize % 8);
        }
        return localSize;
    }
}