using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebar.RebarTarget.LLVM
{
    internal static class FunctionNames
    {
        public static string GetSynchronousFunctionName(string functionName)
        {
            return $"{functionName}::sync";
        }

        public static string GetInitializeStateFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::InitializeState";
        }

        public static string GetPollFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::Poll";
        }
    }
}
