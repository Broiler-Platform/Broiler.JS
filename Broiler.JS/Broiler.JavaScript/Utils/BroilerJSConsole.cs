using System;
using System.Collections.Generic;
using System.Text;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace BroilerJS.Utils
{
    public class BroilerJSConsole
    {

        public static JSValue Log(in Arguments a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a.TryGetAt(i, out var ai))
                {
                    Console.Write(ai);
                    (JSEngine.CurrentContext as JSContext)?.ReportLog(ai);
                }
            }
            Console.WriteLine();
            return JSUndefined.Value;
        }

    }
}
