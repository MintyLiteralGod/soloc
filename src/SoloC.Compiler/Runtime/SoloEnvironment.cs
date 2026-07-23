namespace SoloC.Compiler.Runtime;

public sealed class SoloEnvironment
{
    private readonly Dictionary<string, Variable> _values = new(StringComparer.Ordinal);

    public SoloEnvironment(SoloEnvironment? enclosing = null)
    {
        Enclosing = enclosing;
    }

    public SoloEnvironment? Enclosing { get; }

    public void Define(string name, SoloValue value, bool isImmutable = false)
    {
        if (_values.ContainsKey(name))
            throw new RuntimeException($"Variable '{name}' is already defined in this scope.");

        _values[name] = new Variable(value, isImmutable);
    }

    public SoloValue Get(string name)
    {
        if (_values.TryGetValue(name, out var variable))
            return variable.Value;

        if (Enclosing is not null)
            return Enclosing.Get(name);

        throw new RuntimeException($"Undefined variable '{name}'.");
    }

    public void Assign(string name, SoloValue value)
    {
        if (_values.TryGetValue(name, out var variable))
        {
            if (variable.IsImmutable)
                throw new RuntimeException($"Cannot assign to immutable variable '{name}'.");

            _values[name] = variable with { Value = value };
            return;
        }

        if (Enclosing is not null)
        {
            Enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeException($"Undefined variable '{name}'.");
    }

    public bool TryGet(string name, out SoloValue value)
    {
        if (_values.TryGetValue(name, out var variable))
        {
            value = variable.Value;
            return true;
        }

        if (Enclosing is not null)
            return Enclosing.TryGet(name, out value);

        value = SoloValue.Null;
        return false;
    }

    private readonly record struct Variable(SoloValue Value, bool IsImmutable);
}

public sealed class RuntimeException : Exception
{
    public RuntimeException(string message) : base(message)
    {
    }
}

public sealed class ReturnException : Exception
{
    public ReturnException(SoloValue value)
    {
        Value = value;
    }

    public SoloValue Value { get; }
}
