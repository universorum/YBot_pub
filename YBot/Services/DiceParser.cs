using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YBot.Models;

namespace YBot.Services;

public partial class DiceParser(ILogger<DiceParser> logger)
{
    private string _expression = string.Empty;
    private int    _inBucket; // fetch unclosed bucked
    private int    _index;

    private char Current => _expression[_index];

    // 2(3) => 2*(3)
    [GeneratedRegex(@"([\d)]) *\(")]
    private static partial Regex LeftHiddenMultiplyRegex();

    // (2)3 => (2)*3
    [GeneratedRegex(@"\) *([\d)])")]
    private static partial Regex RightHiddenMultiplyRegex();

    // Entry
    public (int Actual, bool? Judge, int? Target)? Parse(string expression)
    {
        expression  = LeftHiddenMultiplyRegex().Replace(expression, x => $"{x.Groups[1].Value}*(");
        _expression = RightHiddenMultiplyRegex().Replace(expression, x => $")*{x.Groups[1].Value}");
        var result = ParseCompare();
        ExpectEndOfInput();

        if (result == null) { return null; }

        return (result.Value.Actual, result.Value.Judge, result.Value.Target);
    }

    // Priority 0 (higher is priority)
    private (int Actual, bool? Judge, int? Target)? ParseCompare()
    {
        // higher processor method' return excluded all higher possible
        var expr = ParseCloseBucket();
        if (expr == null) { return null; }

        var isMore      = 0;
        var include     = false;
        var needCompare = false;

        SkipWhitespace();
        if (_index < _expression.Length && Current is '<' or '>')
        {
            isMore      = Current == '>' ? 1 : -1;
            needCompare = true;
            _index++;
        }

        SkipWhitespace();
        if (_index < _expression.Length && Current is '=')
        {
            include     = true;
            needCompare = true;
            _index++;
        }

        var actual = Expression.Lambda<Func<int>>(expr).Compile().Invoke();

        // no any compare operator found, return
        if (!needCompare) { return (actual, null, null); }

        var right = ParseCloseBucket();
        ArgumentNullException.ThrowIfNull(right);

        var target = Expression.Lambda<Func<int>>(right).Compile()();
        var judge = isMore switch
        {
            > 0 => include ? actual >= target : actual > target,
            0   => include ? actual == target : throw new ArgumentNullException(),
            < 0 => include ? actual <= target : actual < target
        };

        return (actual, judge, target);
    }

    // Priority 1
    private Expression? ParseCloseBucket()
    {
        // higher processor method' return excluded all higher possible
        var expr = ParseAdditive();

        // only increment value when hit target char
        if (_index < _expression.Length && Current is ')')
        {
            if (_inBucket <= 0)
            {
                logger.LogTrace("Unexpected character at index {index}: '{current}'", _index, Current);
                throw new ArgumentException($"Unexpected character at index {_index}: '{Current}'");
            }

            _index++;
            _inBucket--;
        }

        return expr;
    }

    // Priority 2
    private Expression? ParseAdditive()
    {
        var expr = ParseMultiplicative();
        while (_index < _expression.Length)
        {
            var symbol = Current;
            if (symbol is not ('+' or '-')) { break; }

            _index++;
            var right = ParseMultiplicative();
            expr ??= Expression.Constant(0);
            ArgumentNullException.ThrowIfNull(right);
            expr = symbol switch
            {
                '+' => Expression.Add(expr, right),
                '-' => Expression.Subtract(expr, right),
                _   => throw new ArgumentOutOfRangeException()
            };
        }

        return expr;
    }

    // Priority 3
    private Expression? ParseMultiplicative()
    {
        var expr = ParsePower();
        while (_index < _expression.Length)
        {
            var symbol = Current;
            if (symbol is not ('*' or 'x' or '/' or '\\' or '%')) { break; }

            _index++;
            var right = ParsePower();
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(right);
            expr = symbol switch
            {
                '*'  => Expression.Multiply(expr, right),
                'x'  => Expression.Multiply(expr, right),
                '/'  => Expression.Divide(expr, right),
                '\\' => Expression.Modulo(expr, right),
                '%'  => Expression.Modulo(expr, right),
                _    => throw new ArgumentOutOfRangeException()
            };
        }

        return expr;
    }

    // Priority 4
    private Expression? ParsePower()
    {
        var expr = ParseDice();
        while (_index < _expression.Length)
        {
            if (Current is not '^') { break; }

            _index++;
            var right = ParseDice();
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(right);
            expr  = Expression.Convert(expr, typeof(double));
            right = Expression.Convert(right, typeof(double));
            expr  = Expression.Power(expr, right);
            expr  = Expression.Convert(expr, typeof(int));
        }

        return expr;
    }

    // Priority 5
    private Expression? ParseDice()
    {
        var expr = ParseOpenBucket();
        while (_index < _expression.Length)
        {
            if (Current is not ('d' or 'D')) { break; }

            _index++;
            var left = ParseOpenBucket();
            expr ??= Expression.Constant(1);
            ArgumentNullException.ThrowIfNull(left);
            Expression<Func<int, int, int>> exp = (face, count) => Dice.New(face, count);
            expr = Expression.Invoke(exp, left, expr);
            expr = Expression.Convert(expr, typeof(int));
        }

        return expr;
    }

    // Priority 6
    private Expression? ParseOpenBucket()
    {
        var expr = ParsePrimary();

        if (_index < _expression.Length && Current is '(')
        {
            _index++;
            _inBucket++;
            var right = ParseCloseBucket(); // fetch from '(' until ')'
            ArgumentNullException.ThrowIfNull(right);
            expr = expr != null ? Expression.Multiply(expr, right) : right;
        }

        return expr;
    }

    // Priority 7
    private Expression? ParsePrimary()
    {
        // TODO float 

        SkipWhitespace();
        if (_index < _expression.Length && char.IsDigit(Current))
        {
            var start = _index;
            while (_index < _expression.Length && char.IsDigit(Current)) { _index++; }

            if (start == _index) { return null; }

            var value = int.Parse(_expression[start.._index]);

            SkipWhitespace();

            return Expression.Constant(value);
        }

        return null;
    }

    // When end with invalid char
    private void ExpectEndOfInput()
    {
        SkipWhitespace();
        if (_index == _expression.Length) { return; }

        // have char in end that can't process, query is invalid
        logger.LogTrace("Unexpected character at index {index}: '{current}'", _index, Current);
        throw new ArgumentException($"Unexpected character at index {_index}: '{Current}'");
    }

    private void SkipWhitespace()
    {
        while (_index < _expression.Length && char.IsWhiteSpace(Current)) { _index++; }
    }
}