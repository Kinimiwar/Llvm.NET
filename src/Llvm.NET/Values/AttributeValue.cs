﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Llvm.NET.Native;

namespace Llvm.NET.Values
{
    // TODO: Consider removing this in favor of C#7 Tuple support
    public struct IndexedAttributeValue 
        : IEquatable<IndexedAttributeValue>
    {
        public IndexedAttributeValue( FunctionAttributeIndex index, AttributeValue value )
        {
            Index = index;
            Value = value;
        }

        public FunctionAttributeIndex Index { get; }
        public AttributeValue Value { get; }

        public override string ToString( )
        {
            if( Index < FunctionAttributeIndex.Parameter0 )
                return $"{Index}: {Value}";

            return $"Parameter{Index - FunctionAttributeIndex.Parameter0}: {Value}";
        }
        #region Equality operators
        public bool Equals( IndexedAttributeValue other )
            => other.Index == Index
            && other.Value == Value;

        public override bool Equals( object obj )
        {
            if( !( obj is IndexedAttributeValue ) )
                return false;

            return Equals( ( IndexedAttributeValue )obj );
        }

        public override int GetHashCode( ) => Value.GetHashCode( ) ^ Index.GetHashCode( );

        public static bool operator ==( IndexedAttributeValue left, IndexedAttributeValue right ) => left.Equals( right );

        public static bool operator !=( IndexedAttributeValue left, IndexedAttributeValue right ) => left.Equals( right );
        #endregion
    }

    /// <summary>Single attribute for functions, function returns and function parameters</summary>
    /// <remarks>
    /// This is the equivalent to the underlying llvm::Attribute class. The name was changed to 
    /// AttributeValue in .NET to prevent confusion with the <see cref="Attribute"/> class
    /// that is used throughout .NET libraries. As with the underlying LLVM type, this is an 
    /// immutable value type. 
    /// </remarks>
    [SuppressMessage( "Microsoft.Design"
                    , "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"
                    , Justification = "The native pointer isn't owned by this structure, it is owned by the LLVM context"
                    )
    ]
    public struct AttributeValue
    {
        /// <summary>Creates a simple boolean attribute</summary>
        /// <param name="context">Context for creating the attribute</param>
        /// <param name="kind">Kind of attribute</param>
        public AttributeValue( Context context, AttributeKind kind )
            : this( context, kind, 0ul )
        {
            if( kind.RequiresIntValue() )
                throw new ArgumentException( $"Attribute {kind} requires a value", nameof( kind ) );
        }

        /// <summary>Creates an attribute with an integer value parameter</summary>
        /// <param name="context">Context used for interning attributes</param>
        /// <param name="kind">The kind of attribute</param>
        /// <param name="value">Value for the attribute</param>
        /// <remarks>
        /// <para>Not all attributes support a value and those that do don't all support
        /// a full 64bit value. The following table provides the kinds of attributes
        /// accepting a value and the allowed size of the values.</para>
        /// <list type="table">
        /// <listheader><term><see cref="AttributeKind"/></term><term>Bit Length</term></listheader>
        /// <item><term><see cref="AttributeKind.Alignment"/></term><term>32</term></item>
        /// <item><term><see cref="AttributeKind.StackAlignment"/></term><term>32</term></item>
        /// <item><term><see cref="AttributeKind.Dereferenceable"/></term><term>64</term></item>
        /// <item><term><see cref="AttributeKind.DereferenceableOrNull"/></term><term>64</term></item>
        /// </list>
        /// </remarks>
        public AttributeValue( Context context, AttributeKind kind, UInt64 value )
            : this( NativeMethods.CreateAttribute( context.VerifyAsArg( nameof( context ) ).ContextHandle
                                                 , ( LLVMAttrKind )kind
                                                 , value
                                                 )
                  )
        {
        }

        /// <summary>Adds a valueless named attribute</summary>
        /// <param name="context">Context to use for interning attributes</param>
        /// <param name="name">Attribute name</param>
        public AttributeValue( Context context, string name )
            : this( context, name, string.Empty )
        {
        }

        /// <summary>Adds a Target specific named attribute with value</summary>
        /// <param name="context">Context to use for interning attributes</param>
        /// <param name="name">Name of the attribute</param>
        /// <param name="value">Value of the attribute</param>
        [SuppressMessage( "Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0" )]
        public AttributeValue( Context context, string name, string value )
        {
            context.VerifyAsArg( nameof( context ) );
            if( string.IsNullOrWhiteSpace( name ) )
                throw new ArgumentException( "AttributeValue name cannot be null, Empty or all whitespace" );

            NativeAttribute = NativeMethods.CreateTargetDependentAttribute( context.ContextHandle, name, value );
        }

        internal AttributeValue( UIntPtr nativeValue )
        {
            NativeAttribute = nativeValue;
        }

        #region IEquatable
        public override int GetHashCode( ) => NativeAttribute.GetHashCode( );

        public override bool Equals( object obj )
        {
            if( obj is AttributeValue )
                return Equals( ( LLVMMetadataRef )obj );

            if( obj is UIntPtr )
                return NativeAttribute.Equals( obj );

            return base.Equals( obj );
        }

        public bool Equals( AttributeValue other ) => NativeAttribute == other.NativeAttribute;
        
        public static bool operator ==( AttributeValue left, AttributeValue right ) => left.Equals( right );
        public static bool operator !=( AttributeValue left, AttributeValue right ) => !left.Equals( right );
        #endregion

        /// <summary>Kind of the attribute, or null for target specif named attributes</summary>
        public AttributeKind? Kind => IsString ? (AttributeKind?)null : ( AttributeKind )NativeMethods.GetAttributeKind( NativeAttribute );

        /// <summary>Name of a named attribute or null for other kinds of attributes</summary>
        public string Name
        {
            get
            {
                if( !IsString )
                    return null;

                var llvmString = NativeMethods.GetAttributeName( NativeAttribute );
                return Marshal.PtrToStringAnsi( llvmString );
            }
        }

        /// <summary>StringValue for named attributes with values</summary>
        public string StringValue
        {
            get
            {
                if( !IsString )
                    return null;

                var llvmString = NativeMethods.GetAttributeStringValue( NativeAttribute );
                return Marshal.PtrToStringAnsi( llvmString );
            }
        }

        /// <summary>Integer value of the attribute or null if the attribute doesn't have a value</summary>
        public UInt64? IntegerValue => IsInt ? NativeMethods.GetAttributeValue( NativeAttribute ) : (UInt64?)null;

        /// <summary>Flag to indicate if this attribute is a target specific string value</summary>
        public bool IsString => NativeMethods.IsStringAttribute( NativeAttribute );

        /// <summary>Flag to indicate if this attribute has an integer attribute</summary>
        public bool IsInt => NativeMethods.IsIntAttribute( NativeAttribute );

        /// <summary>Flag to indicate if this attribute is a simple enumeration value</summary>
        public bool IsEnum => NativeMethods.IsEnumAttribute( NativeAttribute );

        public bool IsValidOn( FunctionAttributeIndex index, Function function )
        {
            // for now all string attributes are valid everywhere as they are target dependent
            // (e.g. no way to verify the validity of an arbitrary without knowing the target)
            if( IsString )
                return true;

            return Kind.Value.CheckAttributeUsage( index, function );
        }

        public override string ToString( )
        {
            if( IsString )
            {
                if( !string.IsNullOrWhiteSpace( StringValue ) )
                    return $"\"{ Name }\" = \"{ StringValue }\"";

                return Name;
            }

            if( IsInt )
            {
                return $"{ Kind } = { IntegerValue }";
            }

            return Kind.ToString( );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources" )]
        internal readonly UIntPtr NativeAttribute;
    }

    public static class AttributeValueMixins
    {
        /// <summary>Implicitly cast an <see cref="AttributeKind"/> to an <see cref="AttributeValue"/></summary>
        /// <param name="kind">Kind of attribute to create</param>
        /// <param name="ctx">Context that should own the attribute value</param>
        public static AttributeValue ToAttributeValue( this AttributeKind kind, Context ctx ) => new AttributeValue( ctx, kind );

        /// <summary>Implicitly cast a string to an named <see cref="AttributeValue"/></summary>
        /// <param name="kind">Attribute name</param>
        /// <param name="ctx">Context that should own the attribute value</param>
        public static AttributeValue ToAttributeValue( this string kind, Context ctx ) => new AttributeValue( ctx, kind );
    }
}
