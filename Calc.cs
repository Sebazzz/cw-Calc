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

    private static List<Token> Tokenize(string expression)
    {
        List<Token> tokens = new List<Token>();
        StringBuilder currentToken = new StringBuilder();

        bool LastTokenIsOperatorToken() => tokens.Count == 0 || tokens[tokens.Count - 1] is OperatorToken;

        int index;
        for (index = expression.Length - 1; index >= 0; index--)
        {
            char ch = expression[index];
            if (Char.IsDigit(ch) || ch == '.' || ch == '-' && LastTokenIsOperatorToken() && currentToken.Length == 0)
            {
                currentToken.Append(ch);
            }
            else if (Char.IsWhiteSpace(ch))
            {
                // Ignore whitespace - not significant
            }
            else
            {
                if (currentToken.Length > 0) tokens.Add(ValueToken.CreateFromString(currentToken.ToString(), index));
                currentToken.Clear();

                tokens.Add(OperatorToken.CreateFromString(ch.ToString(), index));
            }
        }

        if (currentToken.Length > 0) tokens.Add(ValueToken.CreateFromString(currentToken.ToString(), index));

        tokens.Reverse();

        return tokens;
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