using SoloC.Compiler.Runtime;

namespace SoloC.Compiler.Vm;

public sealed class Chunk
{
    private readonly List<byte> _code = [];
    private readonly List<SoloValue> _constants = [];
    private readonly List<int> _lines = [];

    public IReadOnlyList<byte> Code => _code;
    public IReadOnlyList<SoloValue> Constants => _constants;
    public IReadOnlyList<int> Lines => _lines;

    public int Emit(OpCode op, int line)
    {
        _code.Add((byte)op);
        _lines.Add(line);
        return _code.Count - 1;
    }

    public void EmitByte(byte value, int line)
    {
        _code.Add(value);
        _lines.Add(line);
    }

    public int AddConstant(SoloValue value)
    {
        _constants.Add(value);
        return _constants.Count - 1;
    }

    public void EmitConstant(SoloValue value, int line)
    {
        var index = AddConstant(value);
        Emit(OpCode.Constant, line);
        EmitByte((byte)index, line);
    }

    public int EmitJump(OpCode op, int line)
    {
        Emit(op, line);
        EmitByte(0xFF, line);
        EmitByte(0xFF, line);
        return _code.Count - 2;
    }

    public void PatchJump(int offset)
    {
        var jump = _code.Count - offset - 2;
        if (jump > ushort.MaxValue)
            throw new InvalidOperationException("Jump too large.");

        _code[offset] = (byte)((jump >> 8) & 0xFF);
        _code[offset + 1] = (byte)(jump & 0xFF);
    }

    public void EmitLoop(int loopStart, int line)
    {
        Emit(OpCode.Loop, line);
        var jump = _code.Count - loopStart + 2;
        EmitByte((byte)((jump >> 8) & 0xFF), line);
        EmitByte((byte)(jump & 0xFF), line);
    }
}
