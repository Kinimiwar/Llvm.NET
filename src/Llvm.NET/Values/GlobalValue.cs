﻿namespace Llvm.NET.Values
{
    /// <summary>LLVM Global value </summary>
    public class GlobalValue : Constant
    {
        /// <summary>Visibility of this global value</summary>
        public Visibility Visibility
        {
            get
            {
                return ( Visibility )NativeMethods.GetVisibility( ValueHandle );
            }
            set
            {
                NativeMethods.SetVisibility( ValueHandle, ( LLVMVisibility )value );
            }
        }

        /// <summary>Linkage specification for this symbol</summary>
        public Linkage Linkage
        {
            get
            {
                return ( Linkage )NativeMethods.GetLinkage( ValueHandle );
            }
            set
            {
                NativeMethods.SetLinkage( ValueHandle, ( LLVMLinkage )value );
            }
        }

        /// <summary>Flag to indicate if this is an Unnamed address</summary>
        public bool UnnamedAddress
        {
            get
            {
                return NativeMethods.HasUnnamedAddr( ValueHandle );
            }
            set
            {
                NativeMethods.SetUnnamedAddr( ValueHandle, value );
            }
        }

        /// <summary>Flag to indicate if this is a declaration</summary>
        public bool IsDeclaration => NativeMethods.IsDeclaration( ValueHandle );

        /// <summary>Module containing this global value</summary>
        public Module ParentModule => Type.Context.GetModuleFor( NativeMethods.GetGlobalParent( ValueHandle ) );

        internal GlobalValue( LLVMValueRef valueRef )
            : base( ValidateConversion( valueRef, NativeMethods.IsAGlobalValue ) )
        {
        }
    }

    public static class GlobalValueExtensions
    {
        /// <summary>Visibility of this global value</summary>
        public static T Visibility<T>( this T value, Visibility visibility )
            where T : GlobalValue
        {
            value.Visibility = visibility;
            return value;
        }

        /// <summary>Linkage specification for this symbol</summary>
        public static T Linkage<T>( this T value, Linkage linkage )
            where T : GlobalValue
        {
            value.Linkage = linkage;
            return value;
        }
    }
}
