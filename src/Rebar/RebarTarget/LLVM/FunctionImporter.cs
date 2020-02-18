using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionImporter
    {
        private readonly Dictionary<string, LLVMValueRef> _importedFunctions = new Dictionary<string, LLVMValueRef>();
        private readonly Module _module;

        public FunctionImporter(Module module)
        {
            _module = module;
        }

        public LLVMValueRef GetImportedCommonFunction(string functionName)
        {
            return GetCachedFunction(functionName, () =>
            {
                LLVMValueRef function = _module.AddFunction(functionName, CommonModules.CommonModuleSignatures[functionName]);
                function.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
                return function;
            });
        }

        public LLVMValueRef GetCachedFunction(string specializedFunctionName, Func<LLVMValueRef> createFunction)
        {
            LLVMValueRef function;
            if (!_importedFunctions.TryGetValue(specializedFunctionName, out function))
            {
                function = createFunction();
                _importedFunctions[specializedFunctionName] = function;
            }
            return function;
        }
    }
}
