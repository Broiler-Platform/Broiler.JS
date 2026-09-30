using System;

namespace Broiler.JavaScript.Ast.Misc;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public class FastParseException(FastToken token, string message) : Exception(message)
{
    public readonly FastToken Token = token;
}
