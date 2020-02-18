using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionImporter
    {
        private readonly Dictionary<string, LLVMValueRef> _importedFunctions = new Dictionary<string, LLVMValueRef>();
        private readonly Module _module;
        private readonly HashSet<string> _commonModuleDependencies = new HashSet<string>();

        public FunctionImporter(Module module)
        {
            _module = module;
        }

        public IEnumerable<string> CommonModuleDependencies => _commonModuleDependencies;

        public LLVMValueRef GetImportedCommonFunction(string functionName)
        {
            return GetCachedFunction(functionName, () =>
            {
                var commonFunctionTuple = CommonModules.GetCommonFunction(functionName);
                LLVMTypeRef functionType = commonFunctionTuple.Item1;
                string functionModuleName = commonFunctionTuple.Item2;
                LLVMValueRef function = _module.AddFunction(functionName, functionType);
                _commonModuleDependencies.Add(functionModuleName);
                // TODO: need a better way to capture transitive dependencies
                if (functionModuleName == CommonModules.FileModuleName)
                {
                    _commonModuleDependencies.Add(CommonModules.StringModuleName);
                }
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
