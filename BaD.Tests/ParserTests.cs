using System.ComponentModel;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
namespace BaD.Tests;


public class ParserTests 
{
    StreamPosition p = new StreamPosition(0, 0);

    //S-Expression Tests
    [Fact]
    public void SETest()
    {
        SExpression se = new StringLiteralExpression("true", p);
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        SExpression[] selist = {ile};
        BlockExpression be = new BlockExpression(selist, p);
        Token t = new Token(TokenType.And, "&&", 0, 0); 
        CharLiteralExpression left = new CharLiteralExpression('c', p);
        CharLiteralExpression right = new CharLiteralExpression('a', p);
        LogicalExpression le = new LogicalExpression(t, left, right, p);

        Assert.Equal(0, se.GetTempVarSize());
        Assert.Equal(0, se.GetLocalSize());
        Assert.Equal(0, SExpression.GetTempVarSize(be));
        Assert.Equal(0, SExpression.GetTempVarSize(ile));
        Assert.Equal(8, SExpression.GetTempVarSize(le));
    }

    //StringLiteralExpression Tests
    [Fact]
    public void StringLETest()
    {
        StringLiteralExpression se = new StringLiteralExpression("true", p);
        Assert.Equal("\"true\"", se.ToString());

        SExpression[] ses = [se];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(se.CheckTypes(tc));
        List<TypeCheckerExpression> tl = se.CheckTypes(tc);

        List<SExpression> selist = [se];

        //GenerateCode
        CodeGenerationExpression cge = new CodeGenerationExpression(se);
        IGenerator ig = new x86_64CodeGenerator("true", selist);
        Assert.IsType<List<CodeGenerationExpression>>(se.GenerateCode(ig));
    }

    //CharLiteralExpression Tests
    [Fact]
    public void CharLETest()
    {
        CharLiteralExpression c = new CharLiteralExpression('c', p);
        Assert.Equal("\'c\'", c.ToString());

        SExpression[] ses = [c];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(c.CheckTypes(tc));

        List<SExpression> selist = [c];

        //GenerateCode
        CodeGenerationExpression cge = new CodeGenerationExpression(c);
        IGenerator ig = new x86_64CodeGenerator("c", selist);
        Assert.IsType<List<CodeGenerationExpression>>(c.GenerateCode(ig));
    }

    //IntLiteralExpression Tests
    [Fact]
    public void IntLETest()
    {
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        Assert.Equal("1", ile.ToString());   

        SExpression[] ses = [ile];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ile.CheckTypes(tc));

        List<SExpression> selist = [ile];
        CodeGenerationExpression cge = new CodeGenerationExpression(ile);
        IGenerator ig = new x86_64CodeGenerator("1", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ile.GenerateCode(ig));
    }

    //BoolLiteralExpression Tests
    [Fact]
    public void BoolLETest()
    {
        BoolLiteralExpression b = new BoolLiteralExpression(true, p);
        Assert.Equal("true", b.ToString());

        SExpression[] ses = [b];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(b.CheckTypes(tc));

        List<SExpression> selist = [b];
        CodeGenerationExpression cge = new CodeGenerationExpression(b);
        IGenerator ig = new x86_64CodeGenerator("b", selist);
        Assert.IsType<List<CodeGenerationExpression>>(b.GenerateCode(ig));
    }

    //NullLiteralExpression Tests
    [Fact]
    public void NullLETest()
    {
        NullLiteralExpression n = new NullLiteralExpression(p);
        Assert.Equal("null", n.ToString());

        SExpression[] ses = [n];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(n.CheckTypes(tc));

        List<SExpression> selist = [n];
        CodeGenerationExpression cge = new CodeGenerationExpression(n);
        IGenerator ig = new x86_64CodeGenerator("null", selist);
        Assert.IsType<List<CodeGenerationExpression>>(n.GenerateCode(ig));
    }

    //IdentifierExpression Tests
    [Fact]
    public void IdETest()
    {
        IdentifierExpression ie = new IdentifierExpression("Name", p);
        Assert.Equal("Name", ie.ToString());

        SExpression[] ses = [ie];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ie.CheckTypes(tc));

        List<SExpression> selist = [ie];
        CodeGenerationExpression cge = new CodeGenerationExpression(ie);
        IGenerator ig = new x86_64CodeGenerator("Name", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ie.GenerateCode(ig));
    }

    //AssignExpression Tests
    [Fact]
    public void AETest()
    {
        IdentifierExpression ie = new IdentifierExpression("Name", p);
        AssignExpression ae = new AssignExpression("Name", ie, p);
        Assert.Equal("(assign Name Name)", ae.ToString());

        SExpression[] ses = [ie];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ie.CheckTypes(tc));

        List<SExpression> selist = [ae];
        CodeGenerationExpression cge = new CodeGenerationExpression(ae);
        IGenerator ig = new x86_64CodeGenerator("Assign Name", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ae.GenerateCode(ig));
    }

    //AddressOfExpression Tests
    [Fact]
    public void AOETest()
    {
        AddressOfExpression ad = new AddressOfExpression("1", p);
        Assert.Equal("(& 1)", ad.ToString());

        SExpression[] ses = [ad];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ad.CheckTypes(tc));

        List<SExpression> selist = [ad];
        CodeGenerationExpression cge = new CodeGenerationExpression(ad);
        IGenerator ig = new x86_64CodeGenerator("AddressOf", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ad.GenerateCode(ig));
    }

    [Fact]
    public void DerefETest()
    {
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        DereferenceExpression de = new DereferenceExpression(ile, p);
        Assert.Equal("(* 1)", de.ToString());

        SExpression[] ses = [de];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(de.CheckTypes(tc));

        List<SExpression> selist = [de];
        CodeGenerationExpression cge = new CodeGenerationExpression(de);
        IGenerator ig = new x86_64CodeGenerator("Deref", selist);
        Assert.Throws<TypeCheckerException>(() => (de.GenerateCode(ig)));

        Assert.Equal(0, de.GetLocalSize());

    }

    //AssignPtrExpression Tests
    [Fact]
    public void AssignPtrETest()
    {
        CharLiteralExpression c = new CharLiteralExpression('c', p);
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        AssignPtrExpression ap = new AssignPtrExpression(ile, c, p);
        Assert.Equal("(assignptr 1 'c')", ap.ToString());

        SExpression[] ses = [ap];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ap.CheckTypes(tc));

        List<SExpression> selist = [ap];
        CodeGenerationExpression cge = new CodeGenerationExpression(ap);
        IGenerator ig = new x86_64CodeGenerator("AssignPtr", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ap.GenerateCode(ig));

        Assert.Equal(8, ap.GetTempVarSize());
    }

    [Fact]
    public void ArithmeticETest()
    {
        Token t = new Token(TokenType.Plus, "+", 0, 0);
        IntLiteralExpression left = new IntLiteralExpression(1, p);
        IntLiteralExpression right = new IntLiteralExpression(2, p);
        ArithmeticExpression ae = new ArithmeticExpression(t, left, right, p);

        Assert.Equal("(+ 1 2)", ae.ToString());

        SExpression sae = ae.SimplifyArithmeticExpression();

        SExpression[] ses = [sae];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(sae.CheckTypes(tc));
        Assert.IsType<List<TypeCheckerExpression>>(ae.CheckTypes(tc));

        List<SExpression> selist = [ae];
        CodeGenerationExpression cge = new CodeGenerationExpression(ae);
        IGenerator ig = new x86_64CodeGenerator("ArithmeticE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ae.GenerateCode(ig));

        Assert.Equal(0, ae.GetTempVarSize());
    }

    [Fact]
    public void ComparisonETest()
    {
        Token t = new Token(TokenType.LessThan, "<", 0, 0);
        IntLiteralExpression left = new IntLiteralExpression(1, p);
        IntLiteralExpression right = new IntLiteralExpression(2, p);
        ComparisonExpression ce = new ComparisonExpression(t, left, right, p);

        Assert.Equal("(< 1 2)", ce.ToString());

        SExpression[] ses = [ce];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ce.CheckTypes(tc));

        List<SExpression> selist = [ce];
        CodeGenerationExpression cge = new CodeGenerationExpression(ce);
        IGenerator ig = new x86_64CodeGenerator("ComparisonE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ce.GenerateCode(ig));

        Assert.Equal(8, ce.GetTempVarSize());
    }

    [Fact]
    public void LogicalETest()
    {
        Token t = new Token(TokenType.And, "&&", 0, 0); 
        CharLiteralExpression left = new CharLiteralExpression('c', p);
        CharLiteralExpression right = new CharLiteralExpression('a', p);
        LogicalExpression le = new LogicalExpression(t, left, right, p);

        Assert.Equal("(&& 'c' 'a')", le.ToString());

        SExpression[] ses = [le];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(le.CheckTypes(tc));

        List<SExpression> selist = [le];
        CodeGenerationExpression cge = new CodeGenerationExpression(le);
        IGenerator ig = new x86_64CodeGenerator("LogicalE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(le.GenerateCode(ig));
    }

    [Fact]
    public void FuncETest()
    {
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);
        TypedIdentifier ti = new TypedIdentifier("int", pt);
        TypedIdentifier[] tis = [ti];
        List<SExpression> selist = [ile];
        FuncExpression fe = new FuncExpression("int", tis, pt, selist, p);

        SExpression[] ses = [fe];

        Assert.Equal("(func int ((int int) ) int 1 )", fe.ToString());

        FunctionCallExpression fce = new FunctionCallExpression(ile, ses, p);
        Assert.Equal("(call 1 (func int ((int int) ) int 1 ) )", fce.ToString());

        Assert.Equal(0, fce.GetTempVarSize());


        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(fe.CheckTypes(tc));

        List<SExpression> listSE = [fe];
        CodeGenerationExpression cge = new CodeGenerationExpression(fe);
        IGenerator ig = new x86_64CodeGenerator("FE", listSE);
        Assert.IsType<List<CodeGenerationExpression>>(fe.GenerateCode(ig));

        ses = [fce];
        tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(fce.CheckTypes(tc));
    }

    [Fact]
    public void IfETest()
    {
        BoolLiteralExpression b = new BoolLiteralExpression(true, p);
        NullLiteralExpression n = new NullLiteralExpression(p);
        StringLiteralExpression se = new StringLiteralExpression("true", p);
        IfExpression ife = new IfExpression(b, se, n, p);
        Assert.Equal("(if true \"true\" null)", ife.ToString());

        SExpression[] ses = [ife];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ife.CheckTypes(tc));

        List<SExpression> selist = [ife];
        CodeGenerationExpression cge = new CodeGenerationExpression(ife);
        IGenerator ig = new x86_64CodeGenerator("IfE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ife.GenerateCode(ig));

    }

    [Fact]
    public void ReturnETest()
    {
        BoolLiteralExpression b = new BoolLiteralExpression(true, p);
        ReturnExpression re = new ReturnExpression(b, p);
        Assert.Equal("true", re.ToString());

        SExpression[] ses = [re];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(re.CheckTypes(tc));

        List<SExpression> selist = [re];
        CodeGenerationExpression cge = new CodeGenerationExpression(re);
        IGenerator ig = new x86_64CodeGenerator("ReturnE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(re.GenerateCode(ig));
    }

    [Fact]
    public void NotETest()
    {
        BoolLiteralExpression b = new BoolLiteralExpression(true, p);
        NotExpression note = new NotExpression(b, p);
        Assert.Equal("(! true)", note.ToString());

        SExpression[] ses = [note];
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(note.CheckTypes(tc));

        List<SExpression> selist = [note];
        CodeGenerationExpression cge = new CodeGenerationExpression(note);
        IGenerator ig = new x86_64CodeGenerator("NotE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(note.GenerateCode(ig));
    }

    [Fact]
    public void ListETest()
    {
        CharLiteralExpression c = new CharLiteralExpression('c', p);
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        SExpression[] selist = [ile, c];
        ListExpression liste = new ListExpression(selist, p);
        Assert.Equal("(array 1 'c')", liste.ToString());

        TypeChecker tc = new TypeChecker(selist);
        Assert.IsType<List<TypeCheckerExpression>>(liste.CheckTypes(tc));

        List<SExpression> listSE = [liste];
        CodeGenerationExpression cge = new CodeGenerationExpression(liste);
        IGenerator ig = new x86_64CodeGenerator("ListE", listSE);
        Assert.IsType<List<CodeGenerationExpression>>(liste.GenerateCode(ig));
    }

    [Fact]
    public void BlockETest()
    {
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        SExpression[] selist = {ile};
        BlockExpression be = new BlockExpression(selist, p);
        Assert.Equal("(block 1)", be.ToString());

        TypeChecker tc = new TypeChecker(selist);
        Assert.IsType<List<TypeCheckerExpression>>(be.CheckTypes(tc));

        List<SExpression> ses = [be];
        CodeGenerationExpression cge = new CodeGenerationExpression(be);
        IGenerator ig = new x86_64CodeGenerator("BlockE", ses);
        Assert.IsType<List<CodeGenerationExpression>>(be.GenerateCode(ig));
    }

    [Fact]
    public void ExternETest()
    {
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);
        Type[] ts = [pt];
        List<SExpression> selist = [ile];
        ExternExpression ee = new ExternExpression("int", ts, pt, p, "int", true);
        Assert.Equal("(extern int (int ) int \"int\")", ee.ToString());

        SExpression[] ses = {ee};
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ee.CheckTypes(tc));

        CodeGenerationExpression cge = new CodeGenerationExpression(ee);
        IGenerator ig = new x86_64CodeGenerator("ExternE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ee.GenerateCode(ig));
    }

    [Fact]
    public void WhileETest()
    {
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        BoolLiteralExpression b = new BoolLiteralExpression(true, p);
        WhileExpression we = new WhileExpression(ile, b, p);
        Assert.Equal("(while 1 true)", we.ToString());

        List<SExpression> selist = [we];
        SExpression[] ses = {we};
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(we.CheckTypes(tc));

        CodeGenerationExpression cge = new CodeGenerationExpression(we);
        IGenerator ig = new x86_64CodeGenerator("WhileE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(we.GenerateCode(ig));
    }

    [Fact]
    public void IndexETest()
    {
        CharLiteralExpression c = new CharLiteralExpression('c', p);
        IntLiteralExpression ile = new IntLiteralExpression(1, p);
        IndexExpression inde = new IndexExpression(c, ile, p);
        Assert.Equal("(index 'c' 1)", inde.ToString());

        List<SExpression> selist = [inde];
        SExpression[] ses = {inde};
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(inde.CheckTypes(tc));

        CodeGenerationExpression cge = new CodeGenerationExpression(inde);
        IGenerator ig = new x86_64CodeGenerator("IndexE", selist);
        Assert.Throws<TypeCheckerException>(() => inde.GenerateCode(ig));

        Assert.Equal(8, inde.GetTempVarSize());
    }

    //CastExpression Tests
    [Fact]
    public void CastETest()
    {
        IdentifierExpression ile = new IdentifierExpression("A", p);
        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);
        CastExpression ce = new CastExpression(pt, ile, p);
        Assert.Equal("(cast int A)", ce.ToString());


        List<SExpression> selist = [ce];
        SExpression[] ses = {ce};
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ce.CheckTypes(tc));

        CodeGenerationExpression cge = new CodeGenerationExpression(ce);
        IGenerator ig = new x86_64CodeGenerator("CastE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ce.GenerateCode(ig));
    }

    //VardecExpression Tests
    [Fact]
    public void VardecETest()
    {
        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);
        VardecExpression ve = new VardecExpression("v", pt,  p);
        Assert.Equal("(vardec int v)", ve.ToString());

        List<SExpression> selist = [ve];
        SExpression[] ses = {ve};
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(ve.CheckTypes(tc));

        CodeGenerationExpression cge = new CodeGenerationExpression(ve);
        IGenerator ig = new x86_64CodeGenerator("VardecE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(ve.GenerateCode(ig));

        Assert.Equal(8, ve.GetLocalSize());
    }

    //SizeOfExpression Tests
    [Fact]
    public void SizeOfETest()
    {
        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);
        SizeOfExpression soe = new SizeOfExpression(pt, p);
        Assert.Equal("(sizeof int)", soe.ToString());

        List<SExpression> selist = [soe];
        SExpression[] ses = {soe};
        TypeChecker tc = new TypeChecker(ses);
        Assert.IsType<List<TypeCheckerExpression>>(soe.CheckTypes(tc));

        CodeGenerationExpression cge = new CodeGenerationExpression(soe);
        IGenerator ig = new x86_64CodeGenerator("SizeOfE", selist);
        Assert.IsType<List<CodeGenerationExpression>>(soe.GenerateCode(ig));

    }

    //Type Tests
    [Fact]
    public void TypeTest()
    {
        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);
        PrimitiveType pt2 = new PrimitiveType(ps);
        PrimitiveType pt3 = new PrimitiveType(PrimitiveTypes.IntLiteral);
        PrimitiveType pt4 = new PrimitiveType(PrimitiveTypes.IntLiteral);
        List<Type> lt = [pt];
        FunctionType fs = new FunctionType(pt, lt);
        PointerType ptt = new PointerType(pt);
        ListType listt = new ListType(pt, 1);
        TypedIdentifier ti = new TypedIdentifier("int", pt);

        Assert.Equal("int", pt.ToString());
        Assert.Equal("(func (int ) int)", fs.ToString());
        Assert.Equal("(* int)", ptt.ToString());
        Assert.Equal("(list int 1)", listt.ToString());
        Assert.Equal("(int int)", ti.ToString());

        Assert.False(pt.Equals("string"));
        Assert.True(pt.Equals(pt2));
        Assert.False(fs.Equals("a"));
        Assert.False(ptt.Equals("b"));
        Assert.False(listt.Equals("c"));

        Assert.False(pt.CanCastTo(fs));
        Assert.True(pt4.CanCastTo(pt3));
        Assert.False(fs.CanCastTo(pt));
        Assert.False(ptt.CanCastTo(fs));
        Assert.False(listt.CanCastTo(fs));

        Assert.False(fs.IsSigned());
        Assert.True(pt.IsSigned());


        Assert.True(pt.IsIntType());

        Assert.IsType<int>(pt.GetHashCode());
        Assert.IsType<int>(fs.GetHashCode());
        Assert.IsType<int>(ptt.GetHashCode());
        Assert.IsType<int>(listt.GetHashCode());

        Assert.Equal(8, pt.SizeOf());
        Assert.Equal(8, pt.LocalSize());
        Assert.Equal(8, ptt.SizeOf());
        Assert.Equal(8, ptt.LocalSize());
        Assert.Equal(8, fs.SizeOf());
        Assert.Equal(8, fs.LocalSize());
        Assert.Equal(8, listt.LocalSize());
        Assert.Equal(8, listt.SizeOf());
        
    }

    [Fact]
    public void PrimitiveTTest()
    {
        PrimitiveType ptl = new PrimitiveType(PrimitiveTypes.IntLiteral);
        PrimitiveType pt32 = new PrimitiveType(PrimitiveTypes.Int32);
        PrimitiveType pt16 = new PrimitiveType(PrimitiveTypes.Int16);
        PrimitiveType ptc = new PrimitiveType(PrimitiveTypes.Char);
        PrimitiveType ptb = new PrimitiveType(PrimitiveTypes.Bool);
        PrimitiveType ptv = new PrimitiveType(PrimitiveTypes.Void);

        Assert.Equal(4, pt32.SizeOf());
        Assert.Equal(8, ptl.SizeOf());
        Assert.Equal(2, pt16.SizeOf());
        Assert.Equal(1, ptc.SizeOf());
        Assert.Equal(1, ptb.SizeOf());
        Assert.Equal(0, ptv.SizeOf());

        Assert.False(pt32.CanCastTo(pt16));
    }

    //PointerType Tests
    [Fact]
    public void PointerTTest()
    {
        PrimitiveType pt = new PrimitiveType(new PrimitiveTypes());
        PointerType ptt = new PointerType(pt);
        PointerType ptt2 = new PointerType(pt);
        PointerType pttv = new PointerType(new PrimitiveType(PrimitiveTypes.Void));
        PointerType pttv2 = new PointerType(new PrimitiveType(PrimitiveTypes.Void));
        PointerType pttb = new PointerType(new PrimitiveType(PrimitiveTypes.Bool));

        Assert.True(ptt.Equals(ptt2));
        Assert.True(pttv.CanCastTo(pttv));
        Assert.True(pttv.CanCastTo(pttb));
        Assert.True(pttb.CanCastTo(pttb));
    }

    //FunctionType Tests
    [Fact]
    public void FuncTTest()
    {
        PrimitiveType pt = new PrimitiveType(new PrimitiveTypes());
        PointerType pttv = new PointerType(new PrimitiveType(PrimitiveTypes.Void));
        List<Type> lt = [pt];
        List<Type> lt2 = [pttv];
        FunctionType fs = new FunctionType(pt, lt, true);
        FunctionType fs2 = new FunctionType(pt, lt2, true);
        FunctionType fs3 = new FunctionType(pttv, lt2, true);

        Assert.Equal("(func (...) int)", fs.ToString());
        Assert.True(fs.Equals(fs));
        Assert.False(fs.Equals(fs2));
        Assert.False(fs.Equals(fs3));
        Assert.True(fs.CanCastTo(fs));
        Assert.False(fs.CanCastTo(fs2));
        Assert.False(fs.CanCastTo(fs3));

    }

    //ListType Tests
    [Fact]
    public void ListTTest()
    {
        PrimitiveType pt = new PrimitiveType(new PrimitiveTypes());
        PrimitiveType ptc = new PrimitiveType(PrimitiveTypes.Char);
        ListType listt = new ListType(pt, 1);
        ListType listt2 = new ListType(ptc, 2);
        PointerType ptt = new PointerType(pt);

        Assert.True(listt.Equals(listt));
        Assert.True(listt.CanCastTo(listt));
        Assert.True(listt.CanCastTo(ptt));

        Assert.NotEqual(0, listt2.SizeOf());
        Assert.NotEqual(0, listt2.LocalSize());
    }

    [Fact]
    public void ParseTest()
    {
        string s1 = "hello world";
        string s2 = @"
        (while (< x 5)
            (block
                (call printf ""%d\n"" (@(index lvalue x)))
                (assign x (+ x 1))
            )
        )";
        ICharacterSource scs = new StringCharacterSource(s1);
        StringLiteralExpression se = new StringLiteralExpression("string", p);
        Token t = new Token(TokenType.Char, "s", 0, 0);

        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);


        Lexer l1 = new Lexer(scs);
        Parser par1 = new Parser(l1);
        Assert.NotEqual(se, par1.Parse());
        //Assert.Equal(false, par.IsEof());

        Assert.NotEqual(par1.ParseSExpression(), par1.Parse());

        scs = new StringCharacterSource("(if 2 2)");
        l1 = new Lexer(scs);
        par1 = new Parser(l1);
        Assert.NotEqual(par1.ParseSExpression(), par1.Parse());

        scs = new StringCharacterSource("(< 2 2");
        l1 = new Lexer(scs);
        par1 = new Parser(l1);
        Assert.NotEqual(par1.ParseSExpression(), par1.Parse());

        scs = new StringCharacterSource("(&& 1 2");
        l1 = new Lexer(scs);
        par1 = new Parser(l1);
        Assert.NotEqual(par1.ParseSExpression(), par1.Parse());

        scs = new StringCharacterSource("(!");
        l1 = new Lexer(scs);
        par1 = new Parser(l1);
        Assert.NotEqual(par1.ParseSExpression(), par1.Parse());


        scs = new StringCharacterSource("int");
        l1 = new Lexer(scs);
        par1 = new Parser(l1);


        l1 = new Lexer(new StringCharacterSource("("));
        t = new Token(TokenType.LeftParen, "(", 0, 0);
        Assert.NotEqual(se, par1.ParseCast(t));

        l1 = new Lexer(new StringCharacterSource("(!1)"));
        t = new Token(TokenType.LeftParen, "(", 0, 0);
        Assert.NotEqual(se, par1.ParseNot(t));

        l1 = new Lexer(new StringCharacterSource("(while true)"));
        t = new Token(TokenType.LeftParen, "(", 0, 0);
        Assert.NotEqual(se, par1.ParseWhile(t));


        l1 = new Lexer(new StringCharacterSource("1"));
        t = new Token(TokenType.Not, "!", 0, 0);
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseLogical(t));

        l1 = new Lexer(new StringCharacterSource("1 2"));
        t = new Token(TokenType.LessThan, "<", 0, 0);
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseComparison(t));

        l1 = new Lexer(new StringCharacterSource("(+ 1 2"));
        t = new Token(TokenType.Plus, "+", 0, 0);
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseArithmetic(t));

        l1 = new Lexer(new StringCharacterSource("(@ 1)"));
        t = new Token(TokenType.AtSymbol, "@", 0, 0);
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseDereference(t));

        l1 = new Lexer(new StringCharacterSource("(assignptr 1 2"));
        t = new Token(TokenType.LeftParen, "(", 0, 0);
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseAssignPtr(t));
        Assert.NotEqual(se, par1.ParseIdentifier());

        l1 = new Lexer(new StringCharacterSource("char"));
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseSizeOf(t));

        Assert.Throws<Parser.ParserException>(() => par1.ParseVarDec(t));

        l1 = new Lexer(new StringCharacterSource("bool a"));
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseVarDec(t));

        Assert.Throws<Parser.ParserException>(() => par1.ParseFunction(t));

        l1 = new Lexer(new StringCharacterSource("a void a int16"));
        par1 = new Parser(l1);
        Assert.Throws<Parser.ParserException>(() => par1.ParseFunction(t));

        l1 = new Lexer(new StringCharacterSource("a ((void a) (int16 b)) int32 c))"));
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseFunction(t));

        l1 = new Lexer(new StringCharacterSource("(return 1"));
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseReturn(t));

        l1 = new Lexer(new StringCharacterSource("(if 2 2)"));
        par1 = new Parser(l1);
        Assert.NotEqual(se, par1.ParseReturn(t));
    }
}