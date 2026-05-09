using System;
using System.Collections.Generic;
using System.Text;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Clr;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using BroilerJS;

namespace BroilerJS.REPL
{
    internal class BroilerJSRepl: BroilerJSContext
    {

        public BroilerJSRepl(): base(Environment.CurrentDirectory)
        {
            this[KeyStrings.require] = new JSFunction((in Arguments a1) => {
                var r = this.LoadModuleAsync(System.Environment.CurrentDirectory, a1[0].ToString());
                return AsyncPump.Run(() => r);
            });

            this[KeyStrings.import] = new JSFunction((in Arguments a1) => {
                var r = this.LoadModuleAsync(System.Environment.CurrentDirectory, a1[0].ToString());
                return ClrProxy.Marshal(r);
            });

        }

        public void Run()
        {
            InteractivePrompt.Run((command, listCommand, completions) => {
                if (command == ".exit")
                    return null;
                string result;
                try
                {
                    result = CoreScript.Evaluate(command, codeCache: CodeCache).ToString();
                }
                catch (JSException ex1) {
                    result = ex1.Error[KeyStrings.stack].ToString();
                }
                catch (Exception ex)
                {
                    result = ex.ToString();
                }
                return $"{result}\r\n";
            }, "BroilerJS:>", "// Write .exit to stop..");
            //while (true)
            //{
            //    string line = Console.ReadLine();
            //    if (line == ".exit")
            //        break;

            //    Console.KeyAvailable && Console.ReadKey()

            //    var v = CoreScript.Evaluate(line);
            //    Console.WriteLine(v);
            //}

        }



    }
}
