using System.Reflection.PortableExecutable;


namespace BaD;
public class Program
{
    
    const string source = @"
    (extern MessageBoxA (int (*char) (*char) int) int ""user32.dll"")
    (extern GetLastError () int ""kernel32.dll"")
    (extern printf (...) int ""msvcrt.dll"")

    (func myadd ((int x) (int y)) int 
        (return (+ x y)))

    (func main ( (int argc) ((*(*char)) argv) ) int 
        (vardec int x)

        
        (vardec (*int) lvalue)
        (vardec (*int) rvalue)

        // lvalue = (lvalue + rvalue)
        (assign lvalue (list 1 2 3 4 5))
        (assign rvalue (list 6 7 8 9 10))

        (assign x 0)
        (while (< x 5)
            (block
                (vardec int myl)
                (vardec int myr)
                
                (assign myl (@(index lvalue x)))
                (assign myr (@(index rvalue x)))

                (call printf ""%d\n"" (call myadd myl myr))
                // now assign the result back to lvalue list
                (assignptr (index lvalue x) (call myadd myl myr))

                (assign x (+ x 1))
            )
        )

        // test print out the list
        (assign x 0)
        (while (< x 5)
            (block
                (call printf ""%d\n"" (@(index lvalue x)))
                (assign x (+ x 1))
            )
        )
        
        // print out the arguments
        (assign x 0)
        (call printf ""%d\n"" argc)
        (while (< x argc)
            (block
                (call printf ""%s\n"" (@(index argv x)))
                (assign x (+ x 1))
            )
        )

        (assign x 0)

        (while (< x 10)
            (block
                (call printf ""%d"" x)
                (assign x (+ x 1))
            )
        )

        (call MessageBoxA 0 ""Hello World!"" ""Hello World!"" (call myadd 0 0))

        (return 0)
    ))";


    // test types smaller than 8 bytes
    const string source2 = @"
    (extern printf (...) int ""msvcrt.dll"")
    (extern malloc (int) (*void) ""msvcrt.dll"")
    (extern free ((*void)) void ""msvcrt.dll"")
    (extern memcpy ((*void) (*void) int) (*void) ""msvcrt.dll"")

    (func main () int
        (vardec int16 x)
        (vardec int32 y)
        (vardec char z)
        (vardec bool b)

        (assign x 0)
        (assign y 0)
        (assign z 0)
        (assign b 0)

        (vardec (*char) px)
        (assign px (list 'a' 'b' 'c' 'd' 'e' 'f' 'g' 'h' 'i' 'j' 'k' 'l' 'm' 'n' 'o' 'p' 'q' 'r' 's' 't' 'u' 'v' 'w' 'x' 'y' 'z' '\n' '\0'))

        (call printf ""%s"" px)


        // attempt to change a single character partway through the string
        (assignptr (index px 5) 'X')

        (call printf ""%s"" px)

        // now lets try to print out a single character from adding two different types, should be 'e'
        (assign z (+ 'a' 4))

        (call printf ""%c%c"" z '\n')

        // now lets try casting a new pointer from the char pointer
        (vardec int pi)
        (assign pi 10821) // should be hex 2A45

        // get the lower byte
        (assign z (cast char pi))

        // print out the lower byte, should be 45 (depending on endianness)
        (call printf ""%hhx%c"" z '\n')

        // get the upper byte (we'll need to use a pointer to do this), should be 2A (depending on endianness)
        (vardec (*char) ppi)
        (assign ppi (+ (cast (*char) (& pi)) 1))

        (assign z (@ppi))

        (call printf ""%hhx%c"" z '\n')

        // attempt a malloc
        (vardec (*char) p)
        (assign p (call malloc 14))

        // lets attempt to copy the string into the malloc'd space
        (vardec (*char) helloWorld)
        (assign helloWorld ""Hello World!"")
        (call memcpy p helloWorld 14)

        (call printf ""%s%c"" p '\n')

        // print out each char as hex
        (assign x 0)
        (while (< x 14)
            (block
                (call printf ""%hhx%c"" (@(index p x)) ' ')
                (assign x (+ x 1))
            )
        )
        
        // set a char to 256
        (assign z 255)
        // should be 0
        (call printf ""\n%hhx%c"" (+ 1 z) '\n')

        // test downcasting
        (assign y 305419896) //0x12345678
        (assign z (cast char y))
        (call printf ""%hhx%c"" z '\n') // should be 78

        // test upcasting
        (call printf ""%s"" ""\nUpcasting\n"")
        (assign z 120) // 0x78
        (assign y (cast int32 z))
        (call printf ""%x%c"" y '\n') 

        // test 32->64

        (vardec int i64)
        (assign y 305419896)
        (assign i64 (cast int y))

        (call printf ""%x%c"" i64 '\n')

        (vardec uint ui64)
        (assign ui64 305419896)
        (assign i64 (cast int ui64))
        (call printf ""%x%c"" i64 '\n')

        // test signed casting
        (call printf ""%s"" ""\nSigned casting\n"")
        (vardec int32 i32)

        (assign i32 -1)
        (assign z (cast char i32)) 
        (call printf ""%x%c"" z '\n')

        // free
        (call free p)


        (return 0)
    )";


    public static void Main(string[] args)
    {
        // print out all the tokens
        ICharacterSource testCS = new StringCharacterSource(source);
        Lexer testLexer = new(testCS);
        Token testToken = testLexer.NextToken();

        while (testToken.Type != TokenType.Eof)
        {
            Console.WriteLine(testToken.Type + " " + testToken.Value);
            testToken = testLexer.NextToken();
        }


        ICharacterSource cs = new StringCharacterSource(source2);

        
        Lexer lexer = new(cs);

        Parser parser = new(lexer);

        List<SExpression> sexp = new List<SExpression>();
        while(!parser.IsEof)
        {
            sexp.Add(parser.Parse()!);
            if(sexp.Last() != null)
                Console.WriteLine(sexp.Last().ToString());
        }

        foreach (var s in sexp)
        {
            if(s != null)
                Console.WriteLine(s.ToString());
        }

        Console.WriteLine("Parsing complete");

        TypeChecker typeChecker = new TypeChecker( sexp.ToArray() );
        typeChecker.Check();

        Console.WriteLine("Type checking complete");
        

        x86_64CodeGenerator codeGenerator = new x86_64CodeGenerator("output.s", sexp);
        codeGenerator.Generate();
        Console.WriteLine("Code generation complete");
    }
}