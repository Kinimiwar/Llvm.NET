#include <llvm/IR/Module.h>
#include "ModuleBindings.h"

using namespace llvm;

extern "C"
{
    void LLVMAddModuleFlag( LLVMModuleRef M
                            , LLVMModFlagBehavior behavior
                            , const char *name
                            , uint32_t value
                            )
    {
        unwrap( M )->addModuleFlag( (Module::ModFlagBehavior)behavior, name, value );
    }

    LLVMValueRef LLVMGetOrInsertFunction( LLVMModuleRef module, const char* name, LLVMTypeRef functionType )
    {
        auto pModule = unwrap( module );
        auto pSignature = cast< FunctionType >( unwrap( functionType ) );
        return wrap( pModule->getOrInsertFunction( name, pSignature ) );
    }
}