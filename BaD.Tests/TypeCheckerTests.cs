namespace BaD.Tests;

public class TypeCheckerTests
{
    StreamPosition p = new StreamPosition(0, 0);
    [Fact]
    public void FindTest()
    {
        //Checks that value is not found
        SExpression se = new StringLiteralExpression("", p);
        SExpression[] ses = {se};
        TypeChecker tc = new TypeChecker(ses);
        Assert.Null(tc.Find("true"));
    }

    [Fact]
    public void IsVariableTest()
    {
        //Checks that variable does not exist
        IdentifierExpression ie = new IdentifierExpression("", p);
        SExpression[] ses = {ie};
        TypeChecker tc = new TypeChecker(ses);
        Assert.False(tc.IsVariable("Name"));
    }

    [Fact]
    public void CheckTest()
    {
        //Checks for exception
        SExpression[] ses = {};
        TypeChecker tc = new TypeChecker(ses);
        Assert.Throws<TypeCheckerException>(() => tc.Check());
    }

    [Fact]
    public void OperationsTest()
    {
        SExpression se = new StringLiteralExpression("true", p);
        SExpression[] ses = {se};
        TypeChecker tc = new TypeChecker(ses);
        TypeCheckerExpression tce = new TypeCheckerExpression(se);
        tce.CheckType(tc);
        List<TypeCheckerExpression> ltc = [];
        tc.PushOperations(ltc);
        tc.ProcessOperations();

        //Check for expected function declaration
        Assert.Throws<TypeCheckerException>(() => tc.Check(se));

        //Checks that variable exists and can be returned
        PrimitiveTypes ps = new PrimitiveTypes();
        PrimitiveType pt = new PrimitiveType(ps);
        tc.AddVariable("true", pt);
        Assert.IsType<PrimitiveType>(tc.Find("true"));
        Assert.True(tc.IsVariable("true"));
            
        TypedIdentifier ti = new TypedIdentifier("int", pt);
        TypedIdentifier[] tis = [ti];
        List<SExpression> selist = [se];
        FuncExpression fe = new FuncExpression("int", tis, pt, selist, p);
        tc.Check(fe);
        ltc = [tce];
        tc.scopes = new Stack<TypeChecker.Scope>();
        tc.scopes.Push(new TypeChecker.Scope());

        //Checks if there is no main function found
        ses = [fe];
        TypeChecker tc2 = new TypeChecker(ses);
        Assert.Throws<TypeCheckerException>(() => tc2.Check());

        //Check for invalid signature
        fe = new FuncExpression("main", tis, pt, selist, p);
        ses = [fe];
        tc2 = new TypeChecker(ses);
        Assert.Throws<TypeCheckerException>(() => tc2.Check());

        //Check for no params
        fe = new FuncExpression("main", new TypedIdentifier[0], pt, new List<SExpression>(), p);
        ses = [fe];
        tc2 = new TypeChecker(ses);
        tc2.Check();

        //Check for params
        TypedIdentifier ti2 = new TypedIdentifier("", new PointerType(new PointerType (new PrimitiveType(PrimitiveTypes.Char))));
        TypedIdentifier[] tis2 = [ti, ti2];
        fe = new FuncExpression("main", tis2, pt, selist, p);
        ses = [fe];
        tc2 = new TypeChecker(ses);
        tc2.Check();
        Assert.IsType<FunctionType>(tc2.Find("main"));
    }
}
