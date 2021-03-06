﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Llvm.NET.DebugInfo;
using Llvm.NET.Native;
using Llvm.NET.Types;
using Llvm.NET.Values;

namespace Llvm.NET
{
    /// <summary>LLVM Bitcode module</summary>
    /// <remarks>
    /// A module is the basic unit for containing code in LLVM. Modules are an in memory
    /// representation of the LLVM bit-code.
    /// </remarks>
    public sealed class NativeModule
        : IDisposable
        , IExtensiblePropertyContainer
    {
        /// <summary>Creates an unnamed module without debug information</summary>
        public NativeModule( )
            : this( string.Empty, null )
        {
        }

        /// <summary>Creates a new module with the specified id in a new context</summary>
        /// <param name="moduleId">Module's ID</param>
        public NativeModule( string moduleId )
            : this( moduleId, null )
        {
        }

        /// <summary>Creates an named module in a given context</summary>
        /// <param name="moduleId">Module's ID</param>
        /// <param name="context">Context for the module</param>
        public NativeModule( string moduleId, Context context )
        {
            if( moduleId == null )
            {
                moduleId = string.Empty;
            }

            if( context == null )
            {
                context = new Context( );
                OwnsContext = true;
            }

            Func<bool> onException = ( ) =>
            {
                if( OwnsContext && context != null )
                {
                    context.Dispose( );
                }

                return false;
            };

            try
            {
                ModuleHandle = NativeMethods.ModuleCreateWithNameInContext( moduleId, context.ContextHandle );
                if( ModuleHandle.Pointer == IntPtr.Zero )
                {
                    throw new InternalCodeGeneratorException( "Could not create module in context" );
                }

                LazyDiBuilder = new Lazy<DebugInfoBuilder>( ( ) => new DebugInfoBuilder( this ) );
                Context.AddModule( this );
                Comdats = new ComdatCollection( this );
            }
            catch when ( onException() )
            {
                // NOP
            }
        }

        /// <summary>Creates a named module with a root <see cref="DICompileUnit"/> to contain debugging information</summary>
        /// <param name="moduleId">Module name</param>
        /// <param name="language">Language to store in the debugging information</param>
        /// <param name="srcFilePath">path of source file to set for the compilation unit</param>
        /// <param name="producer">Name of the application producing this module</param>
        /// <param name="optimized">Flag to indicate if the module is optimized</param>
        /// <param name="flags">Additional flags</param>
        /// <param name="runtimeVersion">Runtime version if any (use 0 if the runtime version has no meaning)</param>
        public NativeModule( string moduleId
                           , SourceLanguage language
                           , string srcFilePath
                           , string producer
                           , bool optimized = false
                           , string flags = ""
                           , uint runtimeVersion = 0
                           )
            : this( moduleId
                  , null
                  , language
                  , srcFilePath
                  , producer
                  , optimized
                  , flags
                  , runtimeVersion
                  )
        {
        }

        /// <summary>Creates a named module with a root <see cref="DICompileUnit"/> to contain debugging information</summary>
        /// <param name="moduleId">Module name</param>
        /// <param name="context">Context for the module</param>
        /// <param name="language">Language to store in the debugging information</param>
        /// <param name="srcFilePath">path of source file to set for the compilation unit</param>
        /// <param name="producer">Name of the application producing this module</param>
        /// <param name="optimized">Flag to indicate if the module is optimized</param>
        /// <param name="compilationFlags">Additional flags</param>
        /// <param name="runtimeVersion">Runtime version if any (use 0 if the runtime version has no meaning)</param>
        public NativeModule( string moduleId
                           , Context context
                           , SourceLanguage language
                           , string srcFilePath
                           , string producer
                           , bool optimized = false
                           , string compilationFlags = ""
                           , uint runtimeVersion = 0
                           )
            : this( moduleId, context )
        {
            DICompileUnit = DIBuilder.CreateCompileUnit( language
                                                       , srcFilePath
                                                       , producer
                                                       , optimized
                                                       , compilationFlags
                                                       , runtimeVersion
                                                       );
        }

        public bool IsDisposed => ModuleHandle.Pointer.IsNull();

        public void Dispose( )
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        ~NativeModule( )
        {
            Dispose( false );
        }

        /// <summary>Name of the Debug Version information module flag</summary>
        public const string DebugVersionValue = "Debug Info Version";

        /// <summary>Name of the Dwarf Version module flag</summary>
        public const string DwarfVersionValue = "Dwarf Version";

        /// <summary>Version of the Debug information Metadata</summary>
        public const UInt32 DebugMetadataVersion = 3; /* DEBUG_METADATA_VERSION (for LLVM v3.7.0) */

        /// <summary>Comdats for this module</summary>
        public ComdatCollection Comdats { get; }

        /// <summary><see cref="Context"/> this module belongs to</summary>
        public Context Context
        {
            get
            {
                if( ModuleHandle.Pointer == IntPtr.Zero )
                {
                    return null;
                }

                return Context.GetContextFor( ModuleHandle );
            }
        }

        public NamedMDNode ModuleFlags
        {
            get
            {
                var handle = NativeMethods.ModuleGetModuleFlagsMetadata( ModuleHandle );
                return new NamedMDNode( handle );
            }
        }

        /// <summary><see cref="DebugInfoBuilder"/> to create debug information for this module</summary>
        public DebugInfoBuilder DIBuilder => LazyDiBuilder.Value;

        /// <summary>Debug Compile unit for this module</summary>
        public DICompileUnit DICompileUnit { get; internal set; }

        /// <summary>Data layout string</summary>
        /// <remarks>
        /// Note the data layout string doesn't do what seems obvious.
        /// That is, it doesn't force the target back-end to generate code
        /// or types with a particular layout. Rather, the layout string has
        /// to match the implicit layout of the target. Thus it should only
        /// come from the actual <see cref="TargetMachine"/> the code is
        /// targeting.
        /// </remarks>
        public string DataLayoutString => Layout?.ToString( ) ?? string.Empty;

        /// <summary>Target data layout for this module</summary>
        /// <remarks>The layout is produced by parsing the <see cref="DataLayoutString"/>
        /// therefore this property changes anytime the <see cref="DataLayoutString"/> is
        /// set. Furthermore, setting this property will change the value of <see cref="DataLayoutString"/>.
        /// In other words, Layout and <see cref="DataLayoutString"/> are two different views
        /// of the same information.
        /// </remarks>
        public DataLayout Layout
        {
            get => Layout_;

            set
            {
                if( Layout_ != null )
                {
                    Layout_.Dispose( );
                }

                Layout_ = value;
                NativeMethods.SetDataLayout( ModuleHandle, value?.ToString( ) ?? string.Empty );
            }
        }

        [SuppressMessage( "StyleCop.CSharp.NamingRules"
                        , "SA1310:Field names must not contain underscore"
                        , Justification = "Trailing _ indicates it must not be written to directly even directly"
                        )
        ]
        private DataLayout Layout_;

        /// <summary>Target Triple describing the target, ABI and OS</summary>
        public string TargetTriple
        {
            get => NativeMethods.GetTarget( ModuleHandle );
            set => NativeMethods.SetTarget( ModuleHandle, value );
        }

        /// <summary>Globals contained by this module</summary>
        public IEnumerable<Value> Globals
        {
            get
            {
                var current = NativeMethods.GetFirstGlobal( ModuleHandle );
                while( current.Pointer != IntPtr.Zero )
                {
                    yield return Value.FromHandle( current );
                    current = NativeMethods.GetNextGlobal( current );
                }
            }
        }

        /// <summary>Enumerable collection of functions contained in this module</summary>
        public IEnumerable<Function> Functions
        {
            get
            {
                var current = NativeMethods.GetFirstFunction( ModuleHandle );
                while( current.Pointer != IntPtr.Zero )
                {
                    yield return Value.FromHandle<Function>( current );
                    current = NativeMethods.GetNextFunction( current );
                }
            }
        }

        // TODO: Add enumerator for GlobalAlias(s)
        // TODO: Add enumerator for NamedMDNode(s)

        /// <summary>Name of the module</summary>
        public string Name => NativeMethods.GetModuleName( ModuleHandle );

        public void Link( NativeModule otherModule )
        {
            if( otherModule == null )
            {
                throw new ArgumentNullException( nameof( otherModule ) );
            }

            if( otherModule.Context != Context )
            {
                throw new ArgumentException( "Linking modules with different contexts is not allowed", nameof( otherModule ) );
            }

            if( !NativeMethods.LinkModules2( ModuleHandle, otherModule.ModuleHandle ).Succeeded )
            {
                throw new InternalCodeGeneratorException( "Module link error" );
            }

            Context.RemoveModule( otherModule );
            otherModule.ModuleHandle = new LLVMModuleRef( IntPtr.Zero );
        }

        /// <summary>Run optimization passes on the module</summary>
        /// <param name="targetMachine"><see cref="TargetMachine"/> for use during optimizations</param>
        /// <remarks>
        /// Options configuring optimization are provided by calling <see cref="StaticState.ParseCommandLineOptions(string[], string)"/>.
        /// The current implementation uses the legacy pass manager architecture, thus the options for controlling passes
        /// are the same as used for the LLVM 'opt' tool. Once LLVM stabilizes on the new pass manager support this
        /// will be obsoleted in favor of a new method that takes a string representation of the optimizations in a manner
        /// consistent with the support in the 'opt' tool.
        /// </remarks>
        public void Optimize( TargetMachine targetMachine )
        {
            if( targetMachine == null )
            {
                throw new ArgumentNullException( nameof( targetMachine ) );
            }

            NativeMethods.RunLegacyOptimizer( ModuleHandle, targetMachine.TargetMachineHandle );
        }

        /// <summary>Verifies a bit-code module</summary>
        /// <param name="errmsg">Error messages describing any issues found in the bit-code</param>
        /// <returns>true if the verification succeeded and false if not.</returns>
        public bool Verify( out string errmsg )
        {
            return NativeMethods.VerifyModule( ModuleHandle, LLVMVerifierFailureAction.LLVMReturnStatusAction, out errmsg );
        }

        /// <summary>Gets a function by name from this module</summary>
        /// <param name="name">Name of the function to get</param>
        /// <returns>The function or null if not found</returns>
        public Function GetFunction( string name )
        {
            var funcRef = NativeMethods.GetNamedFunction( ModuleHandle, name );
            if( funcRef.Pointer == IntPtr.Zero )
            {
                return null;
            }

            return Value.FromHandle<Function>( funcRef );
        }

        /// <summary>Add a function with the specified signature to the module</summary>
        /// <param name="name">Name of the function to add</param>
        /// <param name="signature">Signature of the function</param>
        /// <returns><see cref="Function"/>matching the specified signature and name</returns>
        /// <remarks>
        /// If a matching function already exists it is returned, and therefore the returned
        /// <see cref="Function"/> may have a body and additional attributes. If a function of
        /// the same name exists with a different signature an exception is thrown as LLVM does
        /// not perform any function overloading.
        /// </remarks>
        [SuppressMessage( "Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call" )]
        [SuppressMessage( "Language", "CSE0003:Use expression-bodied members", Justification = "Line too long" )]
        public Function AddFunction( string name, IFunctionType signature )
        {
            return Value.FromHandle<Function>( NativeMethods.GetOrInsertFunction( ModuleHandle, name, signature.GetTypeRef( ) ) );
        }

        /// <summary>Writes a bit-code module to a file</summary>
        /// <param name="path">Path to write the bit-code into</param>
        /// <remarks>
        /// This is a blind write. (e.g. no verification is performed)
        /// So if an invalid module is saved it might not work with any
        /// later stage processing tools.
        /// </remarks>
        public void WriteToFile( string path )
        {
            LLVMStatus status = NativeMethods.WriteBitcodeToFile( ModuleHandle, path );
            if( status.Failed )
            {
                throw new IOException( $"Error reading bitcode file '{path}'", status.ErrorCode );
            }
        }

        /// <summary>Writes this module as LLVM IR source to a file</summary>
        /// <param name="path">File to write the LLVM IR source to</param>
        /// <param name="errMsg">Error messages encountered, if any</param>
        /// <returns><see langword="true"/> if successful or <see langword="false"/> if not</returns>
        public bool WriteToTextFile( string path, out string errMsg )
        {
            return NativeMethods.PrintModuleToFile( ModuleHandle, path, out errMsg );
        }

        /// <summary>Creates a string representation of the module</summary>
        /// <returns>LLVM textual representation of the module</returns>
        /// <remarks>
        /// This is intentionally NOT an override of ToString() as that is
        /// used by debuggers to show the value of a type and this can take
        /// an extremely long time (up to many seconds depending on complexity
        /// of the module) which is bad for the debugger.
        /// </remarks>
        public string WriteToString( ) => NativeMethods.PrintModuleToString( ModuleHandle );

        public MemoryBuffer WriteToBuffer()
        {
            return new MemoryBuffer( NativeMethods.WriteBitcodeToMemoryBuffer( ModuleHandle ) );
        }

        /// <summary>Add an alias to the module</summary>
        /// <param name="aliasee">Value being aliased</param>
        /// <param name="aliasName">Name of the alias</param>
        /// <returns><see cref="GlobalAlias"/> for the alias</returns>
        public GlobalAlias AddAlias( Value aliasee, string aliasName )
        {
            if( aliasee == null )
            {
                throw new ArgumentNullException( nameof( aliasee ) );
            }

            var handle = NativeMethods.AddAlias( ModuleHandle, aliasee.NativeType.GetTypeRef( ), aliasee.ValueHandle, aliasName );
            return Value.FromHandle<GlobalAlias>( handle );
        }

        /// <summary>Get an alias by name</summary>
        /// <param name="name">name of the alias to get</param>
        /// <returns>Alias matching <paramref name="name"/> or null if no such alias exists</returns>
        public GlobalAlias GetAlias( string name )
        {
            var handle = NativeMethods.GetGlobalAlias( ModuleHandle, name );
            return Value.FromHandle<GlobalAlias>( handle );
        }

        /// <summary>Adds a global to this module with a specific address space</summary>
        /// <param name="addressSpace"></param>
        /// <param name="typeRef">Type of the global's value</param>
        /// <param name="name">Name of the global</param>
        /// <returns>The new <see cref="GlobalVariable"/></returns>
        /// <openissues>
        /// - What does LLVM do if creating a second Global with the same name (return null, throw, crash??,...)
        /// </openissues>
        public GlobalVariable AddGlobalInAddressSpace( uint addressSpace, ITypeRef typeRef, string name )
        {
            var handle = NativeMethods.AddGlobalInAddressSpace( ModuleHandle, typeRef.GetTypeRef( ), name, addressSpace );
            return Value.FromHandle<GlobalVariable>( handle );
        }

        /// <summary>Adds a global to this module</summary>
        /// <param name="addressSpace"></param>
        /// <param name="typeRef">Type of the global's value</param>
        /// <param name="isConst">Flag to indicate if this global is a constant</param>
        /// <param name="linkage">Linkage type for this global</param>
        /// <param name="constVal">Initial value for the global</param>
        /// <returns>New global variable</returns>
        [SuppressMessage( "Language", "CSE0003:Use expression-bodied members", Justification = "Line too long" )]
        public GlobalVariable AddGlobalInAddressSpace( uint addressSpace, ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal )
        {
            return AddGlobalInAddressSpace( addressSpace, typeRef, isConst, linkage, constVal, string.Empty );
        }

        /// <summary>Adds a global to this module</summary>
        /// <param name="addressSpace"></param>
        /// <param name="typeRef">Type of the global's value</param>
        /// <param name="isConst">Flag to indicate if this global is a constant</param>
        /// <param name="linkage">Linkage type for this global</param>
        /// <param name="constVal">Initial value for the global</param>
        /// <param name="name">Name of the variable</param>
        /// <returns>New global variable</returns>
        public GlobalVariable AddGlobalInAddressSpace( uint addressSpace, ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal, string name )
        {
            var retVal = AddGlobalInAddressSpace( addressSpace, typeRef, name );
            retVal.IsConstant = isConst;
            retVal.Linkage = linkage;
            retVal.Initializer = constVal;
            return retVal;
        }

        /// <summary>Adds a global to this module</summary>
        /// <param name="typeRef">Type of the global's value</param>
        /// <param name="name">Name of the global</param>
        /// <returns>The new <see cref="GlobalVariable"/></returns>
        /// <openissues>
        /// - What does LLVM do if creating a second Global with the same name (return null, throw, crash??,...)
        /// </openissues>
        public GlobalVariable AddGlobal( ITypeRef typeRef, string name )
        {
            var handle = NativeMethods.AddGlobal( ModuleHandle, typeRef.GetTypeRef( ), name );
            return Value.FromHandle<GlobalVariable>( handle );
        }

        /// <summary>Adds a global to this module</summary>
        /// <param name="typeRef">Type of the global's value</param>
        /// <param name="isConst">Flag to indicate if this global is a constant</param>
        /// <param name="linkage">Linkage type for this global</param>
        /// <param name="constVal">Initial value for the global</param>
        /// <returns>New global variable</returns>
        [SuppressMessage( "Language", "CSE0003:Use expression-bodied members", Justification = "Line too long" )]
        public GlobalVariable AddGlobal( ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal )
        {
            return AddGlobal( typeRef, isConst, linkage, constVal, string.Empty );
        }

        /// <summary>Adds a global to this module</summary>
        /// <param name="typeRef">Type of the global's value</param>
        /// <param name="isConst">Flag to indicate if this global is a constant</param>
        /// <param name="linkage">Linkage type for this global</param>
        /// <param name="constVal">Initial value for the global</param>
        /// <param name="name">Name of the variable</param>
        /// <returns>New global variable</returns>
        public GlobalVariable AddGlobal( ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal, string name )
        {
            var retVal = AddGlobal( typeRef, name );
            retVal.IsConstant = isConst;
            retVal.Linkage = linkage;
            retVal.Initializer = constVal;
            return retVal;
        }

        /// <summary>Retrieves a <see cref="ITypeRef"/> by name from the module</summary>
        /// <param name="name">Name of the type</param>
        /// <returns>The type or null if no type with the specified name exists in the module</returns>
        public ITypeRef GetTypeByName( string name )
        {
            var hType = NativeMethods.GetTypeByName( ModuleHandle, name );
            return hType.Pointer == IntPtr.Zero ? null : TypeRef.FromHandle( hType );
        }

        /// <summary>Retrieves a named global from the module</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GlobalVariable GetNamedGlobal( string name )
        {
            var hGlobal = NativeMethods.GetNamedGlobal( ModuleHandle, name );
            if( hGlobal.Pointer == IntPtr.Zero )
            {
                return null;
            }

            return Value.FromHandle<GlobalVariable>( hGlobal );
        }

        /// <summary>Adds a module flag to the module</summary>
        /// <param name="behavior">Module flag behavior for this flag</param>
        /// <param name="name">Name of the flag</param>
        /// <param name="value">Value of the flag</param>
        public void AddModuleFlag( ModuleFlagBehavior behavior, string name, UInt32 value )
        {
            // AddModuleFlag comes from custom LLVMDebug-C API
            NativeMethods.AddModuleFlag( ModuleHandle, ( LLVMModFlagBehavior )behavior, name, value );
        }

        /// <summary>Adds a module flag to the module</summary>
        /// <param name="behavior">Module flag behavior for this flag</param>
        /// <param name="name">Name of the flag</param>
        /// <param name="value">Value of the flag</param>
        public void AddModuleFlag( ModuleFlagBehavior behavior, string name, LlvmMetadata value )
        {
            if( value == null )
            {
                throw new ArgumentNullException( nameof( value ) );
            }

            // AddModuleFlag comes from custom LLVMDebug-C API
            NativeMethods.AddModuleFlag( ModuleHandle, ( LLVMModFlagBehavior )behavior, name, value.MetadataHandle );
        }

        /// <summary>Adds operand value to named metadata</summary>
        /// <param name="name">Name of the metadata</param>
        /// <param name="value">operand value</param>
        public void AddNamedMetadataOperand( string name, LlvmMetadata value )
        {
            NativeMethods.AddNamedMetadataOperand2( ModuleHandle, name, value?.MetadataHandle ?? LLVMMetadataRef.Zero );
        }

        /// <summary>Adds an llvm.ident metadata string to the module</summary>
        /// <param name="version">version information to place in the llvm.ident metadata</param>
        public void AddVersionIdentMetadata( string version )
        {
            var stringNode = CreateMDNode( version );
            NativeMethods.AddNamedMetadataOperand2( ModuleHandle, "llvm.ident", stringNode.MetadataHandle );
        }

        /// <summary>Create an <see cref="MDNode"/> from a string</summary>
        /// <param name="value">String value</param>
        /// <returns>New node with the string as <see cref="MDNode.Operands"/>[0] (as an MDString)</returns>
        public MDNode CreateMDNode( string value )
        {
            var elements = new LLVMMetadataRef[ ] { NativeMethods.MDString2( Context.ContextHandle, value, ( uint )( value?.Length ?? 0 ) ) };
            var hNode = NativeMethods.MDNode2( Context.ContextHandle, out elements[ 0 ], ( uint )elements.Length );
            return MDNode.FromHandle<MDNode>( hNode );
        }

        /// <summary>Creates a Function definition with Debug information</summary>
        /// <param name="scope">Containing scope for the function</param>
        /// <param name="name">Name of the function in source language form</param>
        /// <param name="linkageName">Mangled linker visible name of the function (may be same as <paramref name="name"/> if mangling not required by source language</param>
        /// <param name="file">File containing the function definition</param>
        /// <param name="line">Line number of the function definition</param>
        /// <param name="signature">LLVM Function type for the signature of the function</param>
        /// <param name="isLocalToUnit">Flag to indicate if this function is local to the compilation unit</param>
        /// <param name="isDefinition">Flag to indicate if this is a definition</param>
        /// <param name="scopeLine">First line of the function's outermost scope, this may not be the same as the first line of the function definition due to source formatting</param>
        /// <param name="debugFlags">Additional flags describing this function</param>
        /// <param name="isOptimized">Flag to indicate if this function is optimized</param>
        /// <param name="tParam"></param>
        /// <param name="decl"></param>
        /// <returns>Function described by the arguments</returns>
        public Function CreateFunction( DIScope scope
                                      , string name
                                      , string linkageName
                                      , DIFile file
                                      , uint line
                                      , DebugFunctionType signature
                                      , bool isLocalToUnit
                                      , bool isDefinition
                                      , uint scopeLine
                                      , DebugInfoFlags debugFlags
                                      , bool isOptimized
                                      , MDNode tParam = null
                                      , MDNode decl = null
                                      )
        {
            if( scope == null )
            {
                throw new ArgumentNullException( nameof( scope ) );
            }

            if( string.IsNullOrWhiteSpace( name ) )
            {
                throw new ArgumentException( "Name cannot be null, empty or whitespace", nameof( name ) );
            }

            if( signature == null )
            {
                throw new ArgumentNullException( nameof( signature ) );
            }

            var func = AddFunction( linkageName ?? name, signature );
            var diSignature = signature.DIType;
            Debug.Assert( diSignature != null );
            var diFunc = DIBuilder.CreateFunction( scope: scope
                                                 , name: name
                                                 , mangledName: linkageName
                                                 , file: file
                                                 , line: line
                                                 , signatureType: diSignature
                                                 , isLocalToUnit: isLocalToUnit
                                                 , isDefinition: isDefinition
                                                 , scopeLine: scopeLine
                                                 , debugFlags: debugFlags
                                                 , isOptimized: isOptimized
                                                 , function: func
                                                 , typeParameter: tParam
                                                 , declaration: decl
                                                 );
            Debug.Assert( diFunc.Describes( func ) );
            func.DISubProgram = diFunc;
            return func;
        }

        public Function CreateFunction( string name
                                      , bool isVarArg
                                      , IDebugType<ITypeRef, DIType> returnType
                                      , params IDebugType<ITypeRef, DIType>[ ] argumentTypes
                                      )
        {
            IFunctionType signature = Context.CreateFunctionType( DIBuilder, isVarArg, returnType, argumentTypes );
            return AddFunction( name, signature );
        }

        /// <summary>Clones the current module</summary>
        /// <returns>Cloned module</returns>
        public NativeModule Clone( ) => new NativeModule( NativeMethods.CloneModule( ModuleHandle ) );

        public NativeModule Clone( Context targetContext )
        {
            if( targetContext == null )
            {
                throw new ArgumentNullException( nameof( targetContext ) );
            }

            if( targetContext == Context )
            {
                return Clone( );
            }

            using( var buffer = WriteToBuffer( ) )
            {
                var retVal = LoadFrom( buffer, targetContext );
                Debug.Assert( retVal.Context == targetContext );
                return retVal;
            }
        }

        /// <inheritdoc/>
        [SuppressMessage( "Language", "CSE0003:Use expression-bodied members", Justification = "Line too long" )]
        bool IExtensiblePropertyContainer.TryGetExtendedPropertyValue<T>( string id, out T value )
        {
            return PropertyBag.TryGetExtendedPropertyValue( id, out value );
        }

        /// <inheritdoc/>
        void IExtensiblePropertyContainer.AddExtendedPropertyValue( string id, object value )
        {
            PropertyBag.AddExtendedPropertyValue( id, value );
        }

        /// <summary>Load a bit-code module from a given file</summary>
        /// <param name="path">path of the file to load</param>
        /// <param name="context">Context to use for creating the module</param>
        /// <returns>Loaded <see cref="NativeModule"/></returns>
        public static NativeModule LoadFrom( string path, Context context )
        {
            if( string.IsNullOrWhiteSpace( path ) )
            {
                throw new ArgumentException( "path cannot be null or an empty string", nameof( path ) );
            }

            if( context == null )
            {
                throw new ArgumentNullException( nameof( context ) );
            }

            if( !File.Exists( path ) )
            {
                throw new FileNotFoundException( "Specified bit-code file does not exist", path );
            }

            using( var buffer = new MemoryBuffer( path ) )
            {
                return LoadFrom( buffer, context );
            }
        }

        /// <summary>Load bit code from a memory buffer</summary>
        /// <param name="buffer">Buffer to load from</param>
        /// <param name="context">Context to load the module into</param>
        /// <returns>Loaded <see cref="NativeModule"/></returns>
        /// <remarks>
        /// This along with <see cref="WriteToBuffer"/> are useful for "cloning"
        /// a module from one context to another. This allows creation of multiple
        /// modules on different threads and contexts and later moving them to a
        /// single context in order to link them into a single final module for
        /// optimization.
        /// </remarks>
        public static NativeModule LoadFrom( MemoryBuffer buffer, Context context )
        {
            if( buffer == null )
            {
                throw new ArgumentNullException( nameof( buffer ) );
            }

            if( context == null )
            {
                throw new ArgumentNullException( nameof( buffer ) );
            }

            if( NativeMethods.ParseBitcodeInContext( context.ContextHandle, buffer.BufferHandle, out LLVMModuleRef modRef, out string errMsg ).Failed )
            {
                throw new InternalCodeGeneratorException( errMsg );
            }

            return context.GetModuleFor( modRef );
        }

        internal NativeModule( LLVMModuleRef handle )
        {
            ModuleHandle = handle;
            LazyDiBuilder = new Lazy<DebugInfoBuilder>( ( ) => new DebugInfoBuilder( this ) );
            Context.AddModule( this );
            Comdats = new ComdatCollection( this );
        }

        internal static NativeModule FromHandle( LLVMModuleRef nativeHandle )
        {
            var context = Context.GetContextFor( nativeHandle );
            return context.GetModuleFor( nativeHandle );
        }

        private void Dispose( bool disposing )
        {
            // if not already disposed, dispose the module
            // Do this only on dispose. The containing context
            // will clean up the module when it is disposed or
            // finalized. Since finalization order isn't
            // deterministic it is possible that the module is
            // finalized after the context has already run its
            // finalizer, which would cause an access violation
            // in the native LLVM layer.
            if( disposing && ModuleHandle.Pointer != IntPtr.Zero )
            {
                // remove the module handle from the module cache.
                Context.RemoveModule( this );

                // if this module created the context then just dispose
                // the context as that will clean up the module as well.
                if( OwnsContext )
                {
                    Context.Dispose( );
                }
                else
                {
                    NativeMethods.DisposeModule( ModuleHandle );
                }

                ModuleHandle = default( LLVMModuleRef );
            }
        }

        internal LLVMModuleRef ModuleHandle { get; private set; }

        private readonly ExtensiblePropertyContainer PropertyBag = new ExtensiblePropertyContainer( );
        private readonly Lazy<DebugInfoBuilder> LazyDiBuilder;
        private readonly bool OwnsContext;
    }
}
