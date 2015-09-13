﻿namespace Llvm.NET.Instructions
{
    public class Cmp
        : Instruction
    {
        internal Cmp( LLVMValueRef valueRef )
            : base( ValidateConversion( valueRef, LLVMNative.IsACmpInst ) )
        {
        }

        public Predicate Predicate
        {
            get
            {
                switch( Opcode )
                {
                case Opcode.ICmp:
                    return ( Predicate )LLVMNative.GetICmpPredicate( ValueHandle );

                case Opcode.FCmp:
                    return ( Predicate )LLVMNative.GetFCmpPredicate( ValueHandle );

                default:
                    return Predicate.BadFcmpPredicate;
                }
            }
        }
    }
}
