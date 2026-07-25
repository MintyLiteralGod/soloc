using SoloLua.Compiler;

public class SoloLuaTests
{
    [Fact]
    public void Locals_by_default_and_interpolation()
    {
        var result = SoloLuaCompiler.Compile(
            """
            name = "SoloLua"
            print "Hello, {name}"
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("local name = \"SoloLua\"", result.Lua);
        Assert.Contains("tostring(name)", result.Lua);
    }

    [Fact]
    public void Operators_and_continue()
    {
        var result = SoloLuaCompiler.Compile(
            """
            for i in 1..5
              if i != 2 && i != 4
                print i
              else
                continue
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("~=", result.Lua);
        Assert.Contains(" and ", result.Lua);
        Assert.Contains("goto __solo_cont_", result.Lua);
    }

    [Fact]
    public void List_map_class_and_try()
    {
        var result = SoloLuaCompiler.Compile(
            """
            nums = list 1, 2, 3
            nums.push(4)

            person = map
              name: "Ada"

            class Counter
              fn new(start)
                self.n = start ?? 0
                return self
              fn inc()
                self.n += 1
                return self.n

            c = Counter.new(1)
            print c.inc()

            try
              error("boom")
            catch err
              print err
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("solo.list(", result.Lua);
        Assert.Contains("nums:push(", result.Lua);
        Assert.Contains("solo.map({", result.Lua);
        Assert.Contains("solo.class({", result.Lua);
        Assert.Contains("solo.nullish(", result.Lua);
        Assert.Contains("solo.try(", result.Lua);
        Assert.Contains("Counter.new(", result.Lua);
        Assert.Contains("c:inc(", result.Lua);
    }

    [Fact]
    public void Global_is_explicit()
    {
        var result = SoloLuaCompiler.Compile(
            """
            global CONFIG = "prod"
            x = 1
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("_G.CONFIG = ", result.Lua);
        Assert.Contains("local x = ", result.Lua);
    }
}
