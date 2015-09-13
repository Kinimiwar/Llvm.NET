﻿using System;

namespace Llvm.NET.Values
{
    /// <summary>LLVM Global Alias for a function or global value</summary>
    public class GlobalAlias
        : GlobalValue
    {
        public Constant Aliasee
        {
            get
            {
                if( ValueHandle.Pointer == IntPtr.Zero )
                    return null;

                var handle = LLVMNative.GetAliasee( ValueHandle );
                if( handle.Pointer == IntPtr.Zero )
                    return null;

                return FromHandle<Constant>( handle );
            }
        }

        internal GlobalAlias( LLVMValueRef valueRef )
            : this( valueRef, false )
        {
        }

        internal GlobalAlias( LLVMValueRef valueRef, bool preValidated )
            : base( preValidated ? valueRef : ValidateConversion( valueRef, LLVMNative.IsAGlobalAlias ) )
        {
        }
    }
}
