﻿namespace Llvm.NET.Instructions
{
    public class LandingPad
        : Instruction
    {
        internal LandingPad( LLVMValueRef valueRef, bool preValidated )
            : base( preValidated ? valueRef : ValidateConversion( valueRef, NativeMethods.IsAExtractElementInst ) )
        {
        }
    }
}
