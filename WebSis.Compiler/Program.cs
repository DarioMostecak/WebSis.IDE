while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();

    if (string.IsNullOrEmpty(line)) return;

    var lexer = new Lexer(line);

    while (true)
    {
        var token = lexer.NextToken();
        if (token.SyntaxKind == SyntaxKind.EndOfFileToken)
            break;

        Console.Write($"{token.SyntaxKind} : '{token.Text}'");
        if (token.Value != null)
            Console.Write($" {token.Value}");

        Console.WriteLine();
    }

}

/**
 * 
 * Expresson =>  1 + 2 * 3
 * 
 *    Three  
 *    
 *      +
 *     / \
 *    1   *
 *       / \
 *      2   3
 *      
 *      
 *  Expresson =>1 + 2 + 3
 *  
 *     Three
 *     
 *       +
 *      / \
 *     +   3
 *    / \
 *   1   2
 *   
 * /

/*************************************************/



#region Lexser
// what kind of syntax is => operator, number etc..
enum SyntaxKind
{
    NumberToken,
    WhiteSpaceToken,
    PlusToken,
    MinusToken,
    StarToken,
    SlashToken,
    OpenParenthesisToken,
    CloseParenthesisToken,
    BadToken,
    EndOfFileToken,
    NumberExpression,
    BinaryExpression
}

//Represent a noden in a three
class SyntaxToken
{
    public SyntaxToken(SyntaxKind syntaxKind, int position, string text, object value)
    {
        SyntaxKind = syntaxKind;
        Position = position;
        Text = text;
        Value = value;
    }

    public SyntaxKind SyntaxKind { get; }
    public int Position { get; }
    public string Text { get; }
    public object Value { get; }
}

//lexer produces tokens that are nodes in the three
class Lexer
{
    private readonly string _text;
    private int _position;

    public Lexer(string text)
    {
        _text = text;
    }

    private char Current
    {
        get
        {
            if (_position >= _text.Length)
                return '\0';

            return _text[_position];
        }
    }

    private void Next()
    {
        _position++;
    }

    public SyntaxToken NextToken()
    {
        // <numbers>
        // + - * / ( )
        // <whitespace>

        //tells the reader that is end of file
        if (_position >= _text.Length)
            return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "/0", null);


        if (char.IsDigit(Current))
        {
            var start = _position;

            while (char.IsDigit(Current))
                Next();

            var lenght = _position - start;
            var text = _text.Substring(start, lenght);

            //implement error handling
            int.TryParse(text, out var value);

            return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
        }

        if (char.IsWhiteSpace(Current))
        {
            var start = _position;

            while (char.IsWhiteSpace(Current))
                Next();

            var lenght = _position - start;
            var text = _text.Substring(start, lenght);

            return new SyntaxToken(SyntaxKind.WhiteSpaceToken, start, text, null);
        }

        if (Current == '+')
            return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
        else if (Current == '-')
            return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
        else if (Current == '*')
            return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
        else if (Current == '/')
            return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
        else if (Current == '(')
            return new SyntaxToken(SyntaxKind.OpenParenthesisToken, _position++, "(", null);
        else if (Current == ')')
            return new SyntaxToken(SyntaxKind.CloseParenthesisToken, _position++, ")", null);

        return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1, 1), null);
    }
}
#endregion


#region syntax model classes and abstractions (Data structures)
abstract class SyntaxNode
{
    public abstract SyntaxKind Kind { get; }
}

abstract class ExpressionSyntax : SyntaxNode
{

}

sealed class NumberExpressionSyntax : ExpressionSyntax
{
    public NumberExpressionSyntax(SyntaxToken numberToken)
    {
        NumberToken = numberToken;
    }

    public override SyntaxKind Kind => SyntaxKind.NumberExpression;
    public SyntaxToken NumberToken { get; }
}

sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Right { get; }
}
#endregion

//Parser create trees
class Parser
{
    private readonly SyntaxToken[] _tokens;
    private int _position;

    public Parser(string text)
    {
        var tokens = new List<SyntaxToken>();

        var lexer = new Lexer(text);
        SyntaxToken token;

        do
        {
            token = lexer.NextToken();

            if (token.SyntaxKind != SyntaxKind.WhiteSpaceToken &&
                token.SyntaxKind != SyntaxKind.BadToken)
            {
                tokens.Add(token);
            }
        }
        while (token.SyntaxKind != SyntaxKind.EndOfFileToken);

        _tokens = tokens.ToArray();
    }

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Length)
            return _tokens[_tokens.Length - 1];

        return _tokens[index];
    }

    private SyntaxToken Current => Peek(0);

    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken Match(SyntaxKind kind)
    {
        if (Current.SyntaxKind == kind)
            return NextToken();

        return new SyntaxToken(kind, Current.Position, null, null);
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        var numberToken = Match(SyntaxKind.NumberToken);
        return new NumberExpressionSyntax(numberToken);
    }

    public ExpressionSyntax Parse()
    {
        var left = ParsePrimaryExpression();

        while (Current.SyntaxKind == SyntaxKind.PlusToken ||
               Current.SyntaxKind == SyntaxKind.MinusToken)
        {
            var operatorToken = NextToken();
            var right = ParsePrimaryExpression();
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }
}


