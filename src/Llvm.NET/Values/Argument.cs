﻿using System;
using System.Collections.Generic;

namespace Llvm.NET.Values
{
    /// <summary>An LLVM Value representing an Argument in a function</summary>
    public class Argument
        : Value
    {
        /// <summary>Function this argument belongs to</summary>
        public Function ContainingFunction => FromHandle<Function>( NativeMethods.GetParamParent( ValueHandle ) );
        public uint Index => NativeMethods.GetArgumentIndex( ValueHandle );

        /// <summary>Sets the alignment for the argument</summary>
        /// <param name="value">Alignment value for this argument</param>
        public void SetAlignment( uint value )
        {
            NativeMethods.SetParamAlignment( ValueHandle, value );
        }

        /// <summary>Attributes for this parameter</summary>
        public IAttributeSet Attributes { get; }

        internal Argument( LLVMValueRef valueRef )
            : this( valueRef, false )
        {
        }

        internal Argument( LLVMValueRef valueRef, bool preValidated )
            : base( preValidated ? valueRef : ValidateConversion( valueRef, NativeMethods.IsAArgument ) )
        {
            Attributes = new AttributeSetImpl( ContainingFunction, FunctionAttributeIndex.Parameter0 + (int)Index );
        }
    }
}
