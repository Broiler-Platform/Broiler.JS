

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool Program(out AstProgram program)
    {
        program = default;

        if (Block(out var block, isProgramRoot: true))
            program = new AstProgram(block.Start, block.End, block.Statements, isAsync) { HoistingScope = block.HoistingScope, AnnexBFunctionNames = block.AnnexBFunctionNames };

        return true;
    }
}
