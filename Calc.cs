using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public class Evaluator
{
    public double Evaluate(string expression)
    {
        // 3 * 2 * 3 + 10 / 5
        // yields: 
        //   ValueToken 3
        //   OperatorToken *
        //   ValueToken 2
        //   OperatorToken *
        //   ValueToken 3
        //   OperatorToken +
        //   ValueToken 10
        //   OperatorToken /
        //   ValueToken 5
        //
        // The expression essentially means:
        // ((3 * 2) * 3) + (10 / 5))
        // Where () is a Expression.
        List<Token> tokens = Tokenize(expression);

        OperatorToken primaryOperator = tokens.OfType<OperatorToken>().OrderByDescending(x => x).First();
        int idx = tokens.IndexOf(primaryOperator);

        Expression rootExpression = Expression.Partial(
            tokens.Take(idx),
            primaryOperator,
            tokens.Skip(idx + 1)
        );

        Expression reducedExpression = Expression.Reduce(rootExpression);

        double result = Expression.Evaluate(reducedExpression);

        return result;
    }

    private sealed class ParsingContext
    {
        public List<Token> Tokens { get; } = new List<Token>();
        public StringBuilder CurrentToken { get; } = new StringBuilder();
        public char PossiblePendingOperator { get; set; }

        public void AppendToPartiallyCompletedToken(char value)
        {
            this.CurrentToken.Insert(0, value);
        }

        public void AddToken(Token token)
        {
            // We parse RTL, so insert at start
            if (token is PartialCompletedExpression g && g.Tokens.Count == 1)
            {
                token = g.Tokens.First();
            }

            this.Tokens.Insert(0, token);
        }

        public Token LastToken => this.Tokens.Count == 0 ? null : this.Tokens[0];

        public void ReplaceToken(Token token, Token replacement)
        {
            this.Tokens[this.Tokens.IndexOf(token)] = replacement;
        }

        public void ReduceRoot()
        {
            if (this.Tokens.Count == 1)
            {
                Token token = this.LastToken;

                if (token is PartialCompletedExpression g)
                {
                    this.Tokens.Clear();
                    this.Tokens.AddRange(g.Tokens);
                }
            }
        }
    }

    private static List<Token> Tokenize(string expression)
    {
        Stack<ParsingContext> parsingContextStack = new Stack<ParsingContext>();
        ParsingContext parsingContext = new ParsingContext();
        int index;

        void CompleteUnfinishedToken()
        {
            if (parsingContext.CurrentToken.Length > 0)
            {
                parsingContext.AddToken(ValueToken.CreateFromString(parsingContext.CurrentToken.ToString(), index));
            }
        }

        void CompleteNumber()
        {
            StringBuilder currentToken = parsingContext.CurrentToken;

            if (currentToken.Length > 0) {
                parsingContext.AddToken(ValueToken.CreateFromString(currentToken.ToString(), index));
            }

            currentToken.Clear();
        }

        void CompleteMinusExpression(bool isOperator)
        {
            if (parsingContext.PossiblePendingOperator != '-')
            {
                return;
            }

            if (isOperator)
            {
                parsingContext.AddToken(
                    OperatorToken.CreateFromString("-", index + 1)
                );

                parsingContext.PossiblePendingOperator = Char.MinValue;
                return;
            }

            if (parsingContext.LastToken is PartialCompletedExpression g)
            {
                List<Token> tokenList = new List<Token>
                {
                    ValueToken.CreateFromString("-1", index), 
                    OperatorToken.CreateFromString("*", index), 
                    g
                };

                // Last is a group, and "-(1 * 3)" is actually "-1 * (1 * 3)"
                parsingContext.ReplaceToken(
                    parsingContext.LastToken,
                    PartialCompletedExpression.Create(
                        tokenList
                    )
                );
            }
            else
            {
                parsingContext.AppendToPartiallyCompletedToken(parsingContext.PossiblePendingOperator);
            }

            parsingContext.PossiblePendingOperator = Char.MinValue;
        }

        // Parse token from right to left
        for (index = expression.Length - 1; index >= 0; index--)
        {
            char ch = expression[index];

            // Whitespace does not matter
            if (Char.IsWhiteSpace(ch))
            {
                continue;
            }

            // Group take precedence
            if (ch == ')')
            {
                CompleteNumber();

                parsingContextStack.Push(parsingContext);
                parsingContext = new ParsingContext();
                continue;
            }

            // Group end
            if (ch == '(')
            {
                CompleteUnfinishedToken();

                ParsingContext groupContext = parsingContext;
                parsingContext = parsingContextStack.Pop();

                parsingContext.AddToken(
                    PartialCompletedExpression.Create(groupContext.Tokens)
                );

                continue;
            }

            if (ch == '-')
            {
                // Otherwise it is probably part of a number, so just prepend it
                parsingContext.PossiblePendingOperator = ch;
                continue;
            }

            if (Char.IsDigit(ch) || ch == '.')
            {
                CompleteMinusExpression(true);

                parsingContext.AppendToPartiallyCompletedToken(ch);
            }
            else
            {
                CompleteMinusExpression(false);
                CompleteNumber();

                parsingContext.AddToken(OperatorToken.CreateFromString(ch.ToString(), index));
            }
        }

        CompleteUnfinishedToken();

        parsingContext.ReduceRoot();
        return parsingContext.Tokens;
    }


    public class Expression
    {
        public Token Left { get; }

        public OperatorToken OpToken { get; }

        public Token Right { get; }

        public Expression(Token left, OperatorToken opToken, Token right)
        {
            this.Left = left;
            this.OpToken = opToken;
            this.Right = right;
        }

        public static Expression Partial(IEnumerable<Token> left, OperatorToken op, IEnumerable<Token> right)
        {
            return new Expression(
                PartialCompletedExpression.Create(left),
                op,
                PartialCompletedExpression.Create(right)
            );
        }

        public static Expression Reduce(Expression rootExpression)
        {
            Token left = rootExpression.Left;
            if (left is PartialCompletedExpression leftExpr)
            {
                left = Reduce(leftExpr);
            }

            Token right = rootExpression.Right;
            if (right is PartialCompletedExpression rightExpr)
            {
                right = Reduce(rightExpr);
            }

            return new Expression(left, rootExpression.OpToken, right);
        }

        private static ExpressionToken Reduce(PartialCompletedExpression partialExpression)
        {
            var tokens = partialExpression.Tokens.ToArray();
            var op = tokens.OfType<OperatorToken>().OrderByDescending(x => x).First();
            var idx = Array.IndexOf(tokens, op);

            Expression expr = Expression.Partial(
                tokens.Take(idx),
                op,
                tokens.Skip(idx + 1)
            );

            return new ExpressionToken(Reduce(expr));
        }

        public static double Evaluate(Expression expression)
        {
            double left = EvaluateCore(expression.Left);
            double right = EvaluateCore(expression.Right);

            return expression.OpToken.Operator.Execute(left, right);
        }

        private static double EvaluateCore(Token token)
        {
            if (token is ValueToken vt)
            {
                return vt.Value;
            }

            if (token is OperatorToken ot)
            {
                throw new InvalidOperationException($"Invalid state: Attempting to evaluate unreduced token: {ot}");
            }

            if (token is ExpressionToken exprToken)
            {
                return Evaluate(exprToken.Expression);
            }

            throw new InvalidOperationException($"Unknown token type: {token?.GetType()}");
        }

        public override string ToString()
        {
            return $"{this.Left} {this.OpToken} {this.Right}";
        }
    }

    public class ExpressionToken : Token
    {
        public Expression Expression { get; }

        public ExpressionToken(Expression expression) : base(-1) => this.Expression = expression;

        public override string ToString() => $"({this.Expression})";
    }

    public class PartialCompletedExpression : Token
    {
        public ICollection<Token> Tokens { get; }

        private PartialCompletedExpression(ICollection<Token> tokens) : base(-1)
        {
            this.Tokens = tokens.ToArray();
        }

        public static Token Create(IEnumerable<Token> tokens)
        {
            Token[] arr = tokens.ToArray();
            if (arr.Length == 1) return arr[0];
            return new PartialCompletedExpression(arr);
        }

        public override string ToString() => $"[ {String.Join(" , ", this.Tokens)} ]";
    }

    public abstract class Token
    {
        public int Position { get; }

        protected Token(int pos)
        {
            this.Position = pos;
        }
    }

    public class ValueToken : Token
    {
        public double Value { get; }

        private ValueToken(double value, int pos) : base(pos)
        {
            this.Value = value;
        }

        public static ValueToken CreateFromString(string rawString, int pos)
        {
            double val = Double.Parse(rawString);

            return new ValueToken(val, pos);
        }

        public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);
    }

    public class OperatorToken : Token, IComparable<OperatorToken>
    {
        public Operator Operator { get; set; }

        private OperatorToken(Operator op, int pos) : base(pos)
        {
            this.Operator = op;
        }

        public int CompareTo(OperatorToken other)
        {
            int diff = this.Operator.CompareTo(other.Operator);

            if (diff != 0)
            {
                return diff;
            }

            return this.Position - other.Position;
        }

        public static OperatorToken CreateFromString(string rawString, int pos)
        {
            return new OperatorToken(Operator.Get(rawString), pos);
        }

        public override string ToString() => this.Operator.ToString();
    }

    public sealed class Operator : IComparable<Operator>
    {
        private readonly Func<double, double, double> _executeFunc;

        public int Priority { get; }

        public string Symbol { get; }

        private Operator(string symbol, int prio, Func<double, double, double> func)
        {
            this._executeFunc = func;
            this.Symbol = symbol;
            this.Priority = prio;
        }

        public static Operator[] AllOperators = {
            new Operator("*", 01, (left,right) => left * right),
            new Operator("/", 01, (left,right) => left / right),
            new Operator("+", 10, (left, right) => left + right),
            new Operator("-", 10, (left, right) => left - right),
        };

        public int CompareTo(Operator other)
        {
            return this.Priority - other.Priority;
        }

        public static Operator Get(string rawToken)
        {
            foreach (var op in AllOperators)
            {
                if (op.Symbol == rawToken)
                {
                    return op;
                }
            }

            throw new InvalidOperationException($"Invalid token: {rawToken}");
        }

        public override string ToString() => this.Symbol;

        public double Execute(double left, double right)
        {
            return this._executeFunc.Invoke(left, right);
        }
    }
}