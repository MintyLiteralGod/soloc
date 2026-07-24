using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Runtime;

namespace SoloC.Compiler.Vm;

public sealed class VirtualMachine
{
    private readonly DiagnosticBag _diagnostics;
    private readonly TextWriter _output;
    private readonly Dictionary<string, SoloValue> _globals = new(StringComparer.Ordinal);
    private readonly SoloValue[] _stack = new SoloValue[256];
    private int _stackTop;

    public VirtualMachine(TextWriter? output = null, DiagnosticBag? diagnostics = null)
    {
        _output = output ?? Console.Out;
        _diagnostics = diagnostics ?? new DiagnosticBag();
        InstallBuiltins();
    }

    public void ImportModule(string name)
    {
        if (!StdModules.TryImport(name, _globals, out var error))
            _diagnostics.Error(error ?? $"Unknown module '{name}'.", new TextSpan(0, 0));
    }

    public SoloValue Execute(BytecodeProgram program)
    {
        try
        {
            return Run(program.Chunk);
        }
        catch (RuntimeException ex)
        {
            _diagnostics.Error(ex.Message, ex.Span ?? new TextSpan(0, 0), ex.Tip);
            return SoloValue.Null;
        }
    }

    private void InstallBuiltins()
    {
        _globals["print"] = SoloValue.FromNative(new NativeFunction("print", args =>
        {
            _output.WriteLine(string.Join(" ", args.Select(a => a.ToString())));
            return SoloValue.Void;
        }));
    }

    private SoloValue Run(Chunk chunk)
    {
        var ip = 0;
        _stackTop = 0;

        while (ip < chunk.Code.Count)
        {
            var instruction = (OpCode)chunk.Code[ip++];
            switch (instruction)
            {
                case OpCode.Constant:
                    Push(chunk.Constants[chunk.Code[ip++]]);
                    break;
                case OpCode.Null:
                    Push(SoloValue.Null);
                    break;
                case OpCode.True:
                    Push(SoloValue.FromBool(true));
                    break;
                case OpCode.False:
                    Push(SoloValue.FromBool(false));
                    break;
                case OpCode.Pop:
                    Pop();
                    break;
                case OpCode.DefineGlobal:
                {
                    var name = chunk.Constants[chunk.Code[ip++]].AsString();
                    _globals[name] = Pop();
                    break;
                }
                case OpCode.GetGlobal:
                {
                    var name = chunk.Constants[chunk.Code[ip++]].AsString();
                    if (!_globals.TryGetValue(name, out var value))
                        throw new RuntimeException($"I can't find a variable named '{name}'.");
                    Push(value);
                    break;
                }
                case OpCode.SetGlobal:
                {
                    var name = chunk.Constants[chunk.Code[ip++]].AsString();
                    if (!_globals.ContainsKey(name))
                        throw new RuntimeException($"I can't find a variable named '{name}'.");
                    _globals[name] = Peek();
                    break;
                }
                case OpCode.GetLocal:
                    Push(_stack[chunk.Code[ip++]]);
                    break;
                case OpCode.SetLocal:
                    _stack[chunk.Code[ip++]] = Peek();
                    break;
                case OpCode.Equal:
                {
                    var b = Pop();
                    var a = Pop();
                    Push(SoloValue.FromBool(EqualsValues(a, b)));
                    break;
                }
                case OpCode.NotEqual:
                {
                    var b = Pop();
                    var a = Pop();
                    Push(SoloValue.FromBool(!EqualsValues(a, b)));
                    break;
                }
                case OpCode.Greater:
                case OpCode.GreaterEqual:
                case OpCode.Less:
                case OpCode.LessEqual:
                {
                    var b = Pop();
                    var a = Pop();
                    var cmp = a.AsDouble().CompareTo(b.AsDouble());
                    Push(SoloValue.FromBool(instruction switch
                    {
                        OpCode.Greater => cmp > 0,
                        OpCode.GreaterEqual => cmp >= 0,
                        OpCode.Less => cmp < 0,
                        _ => cmp <= 0,
                    }));
                    break;
                }
                case OpCode.Add:
                {
                    var b = Pop();
                    var a = Pop();
                    if (a.Kind == SoloValueKind.String || b.Kind == SoloValueKind.String)
                        Push(SoloValue.FromString(a.ToString() + b.ToString()));
                    else if (a.Kind == SoloValueKind.Int && b.Kind == SoloValueKind.Int)
                        Push(SoloValue.FromInt(a.AsInt() + b.AsInt()));
                    else
                        Push(SoloValue.FromDouble(a.AsDouble() + b.AsDouble()));
                    break;
                }
                case OpCode.Subtract:
                case OpCode.Multiply:
                case OpCode.Divide:
                case OpCode.Modulo:
                {
                    var b = Pop();
                    var a = Pop();
                    if (a.Kind == SoloValueKind.Int && b.Kind == SoloValueKind.Int)
                    {
                        Push(SoloValue.FromInt(instruction switch
                        {
                            OpCode.Subtract => a.AsInt() - b.AsInt(),
                            OpCode.Multiply => a.AsInt() * b.AsInt(),
                            OpCode.Divide => a.AsInt() / b.AsInt(),
                            _ => a.AsInt() % b.AsInt(),
                        }));
                    }
                    else
                    {
                        Push(SoloValue.FromDouble(instruction switch
                        {
                            OpCode.Subtract => a.AsDouble() - b.AsDouble(),
                            OpCode.Multiply => a.AsDouble() * b.AsDouble(),
                            OpCode.Divide => a.AsDouble() / b.AsDouble(),
                            _ => a.AsDouble() % b.AsDouble(),
                        }));
                    }

                    break;
                }
                case OpCode.Not:
                    Push(SoloValue.FromBool(!Pop().IsTruthy()));
                    break;
                case OpCode.Negate:
                {
                    var value = Pop();
                    Push(value.Kind == SoloValueKind.Int
                        ? SoloValue.FromInt(-value.AsInt())
                        : SoloValue.FromDouble(-value.AsDouble()));
                    break;
                }
                case OpCode.Print:
                    _output.WriteLine(Pop().ToString());
                    break;
                case OpCode.Jump:
                {
                    // Read offset first — do not use `ip += ReadShort(ref ip)` (C# eval order bug).
                    var offset = ReadShort(chunk, ref ip);
                    ip += offset;
                    break;
                }
                case OpCode.JumpIfFalse:
                {
                    var offset = ReadShort(chunk, ref ip);
                    if (!Peek().IsTruthy())
                        ip += offset;
                    break;
                }
                case OpCode.Loop:
                {
                    var offset = ReadShort(chunk, ref ip);
                    ip -= offset;
                    break;
                }
                case OpCode.Call:
                {
                    var argCount = chunk.Code[ip++];
                    var callee = Pop();
                    var args = new SoloValue[argCount];
                    for (var i = argCount - 1; i >= 0; i--)
                        args[i] = Pop();

                    if (callee.Kind != SoloValueKind.NativeFunction)
                        throw new RuntimeException("Can only call functions.");

                    Push(((NativeFunction)callee.Raw!).Implementation(args));
                    break;
                }
                case OpCode.Return:
                    return _stackTop > 0 ? Pop() : SoloValue.Null;
                case OpCode.MakeArray:
                {
                    var count = chunk.Code[ip++];
                    var elements = new SoloValue[count];
                    for (var i = count - 1; i >= 0; i--)
                        elements[i] = Pop();
                    Push(SoloValue.FromArray(new SoloArray(elements)));
                    break;
                }
                case OpCode.GetIndex:
                {
                    var index = Pop();
                    var array = Pop();
                    Push(array.AsArray().Get(index.AsInt()));
                    break;
                }
                case OpCode.SetIndex:
                {
                    var value = Pop();
                    var index = Pop();
                    var array = Pop();
                    array.AsArray().Set(index.AsInt(), value);
                    Push(value);
                    break;
                }
                case OpCode.GetLength:
                {
                    var value = Pop();
                    Push(value.Kind switch
                    {
                        SoloValueKind.Array => SoloValue.FromInt(value.AsArray().Length),
                        SoloValueKind.String => SoloValue.FromInt(value.AsString().Length),
                        _ => throw new RuntimeException("Only arrays and strings have .Length."),
                    });
                    break;
                }
                default:
                    throw new RuntimeException($"Unknown opcode '{instruction}'.");
            }
        }

        return SoloValue.Null;
    }

    private static int ReadShort(Chunk chunk, ref int ip)
    {
        var high = chunk.Code[ip];
        var low = chunk.Code[ip + 1];
        ip += 2;
        return (high << 8) | low;
    }

    private static bool EqualsValues(SoloValue a, SoloValue b)
    {
        if (a.Kind is SoloValueKind.Int or SoloValueKind.Double &&
            b.Kind is SoloValueKind.Int or SoloValueKind.Double)
            return a.AsDouble().Equals(b.AsDouble());
        return a.Equals(b);
    }

    private void Push(SoloValue value)
    {
        if (_stackTop >= _stack.Length)
            throw new RuntimeException("Stack overflow.");
        _stack[_stackTop++] = value;
    }

    private SoloValue Pop()
    {
        if (_stackTop == 0)
            throw new RuntimeException("Stack underflow.");
        return _stack[--_stackTop];
    }

    private SoloValue Peek()
    {
        if (_stackTop == 0)
            throw new RuntimeException("Stack underflow.");
        return _stack[_stackTop - 1];
    }
}
