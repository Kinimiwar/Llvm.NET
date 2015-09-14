//===- DIBuilderBindings.cpp - Bindings for DIBuilder ---------------------===//
//
//                     The LLVM Compiler Infrastructure
//
// This file is distributed under the University of Illinois Open Source
// License. See LICENSE.TXT for details.
//
//===----------------------------------------------------------------------===//
//
// This file defines C bindings for the DIBuilder class.
//
//===----------------------------------------------------------------------===//

#include "DIBuilderBindings.h"

#include "IRBindings.h"
#include "llvm/IR/DIBuilder.h"
#include "llvm/IR/IRBuilder.h"
#include "llvm/IR/Module.h"
#include "llvm/IR/Constant.h"
#include "llvm/Support/raw_ostream.h"

using namespace llvm;

DEFINE_SIMPLE_CONVERSION_FUNCTIONS( DIBuilder, LLVMDIBuilderRef )

extern "C"
{
    void LLVMSetDebugLoc( LLVMValueRef inst, unsigned line, unsigned column, LLVMMetadataRef scope )
    {
        unwrap<Instruction>( inst )->setDebugLoc( DebugLoc::get( line, column, unwrap<MDNode>( scope ) ) );
    }

    LLVMDIBuilderRef LLVMNewDIBuilder( LLVMModuleRef mref, LLVMBool allowUnresolved )
    {
        Module *m = unwrap( mref );
        return wrap( new DIBuilder( *m, allowUnresolved != 0 ) );
    }

    void LLVMDIBuilderDestroy( LLVMDIBuilderRef dref )
    {
        DIBuilder *d = unwrap( dref );
        delete d;
    }

    void LLVMDIBuilderFinalize( LLVMDIBuilderRef dref )
    {
        unwrap( dref )->finalize( );
    }

    LLVMMetadataRef LLVMDIBuilderCreateCompileUnit( LLVMDIBuilderRef Dref
                                                    , unsigned Lang
                                                    , const char *File
                                                    , const char *Dir
                                                    , const char *Producer
                                                    , int Optimized, const char *Flags
                                                    , unsigned RuntimeVersion
                                                    )
    {
        DIBuilder *D = unwrap( Dref );
        DICompileUnit* CU = D->createCompileUnit( Lang
                                                 , File
                                                 , Dir
                                                 , Producer
                                                 , Optimized
                                                 , Flags
                                                 , RuntimeVersion
                                                 );
        return wrap( CU );
    }

    LLVMMetadataRef LLVMDIBuilderCreateFile( LLVMDIBuilderRef Dref
                                             , const char *File
                                             , const char *Dir
                                             )
    {
        DIBuilder *D = unwrap( Dref );
        DIFile* F = D->createFile( File, Dir );
        return wrap( F );
    }

    LLVMMetadataRef LLVMDIBuilderCreateLexicalBlock( LLVMDIBuilderRef Dref
                                                     , LLVMMetadataRef Scope
                                                     , LLVMMetadataRef File
                                                     , unsigned Line
                                                     , unsigned Column
                                                     )
    {
        DIBuilder *D = unwrap( Dref );
        DILexicalBlock* LB = D->createLexicalBlock( unwrap<DILocalScope>( Scope )
                                                  , unwrap<DIFile>( File )
                                                  , Line
                                                  , Column
                                                  );
        return wrap( LB );
    }

    LLVMMetadataRef LLVMDIBuilderCreateLexicalBlockFile( LLVMDIBuilderRef Dref
                                                         , LLVMMetadataRef Scope
                                                         , LLVMMetadataRef File
                                                         , unsigned Discriminator
                                                         )
    {
        DIBuilder *D = unwrap( Dref );
        DILexicalBlockFile* LBF = D->createLexicalBlockFile( unwrap<DILocalScope>( Scope )
                                                           , unwrap<DIFile>( File )
                                                           , Discriminator
                                                           );
        return wrap( LBF );
    }

    LLVMMetadataRef LLVMDIBuilderCreateFunction( LLVMDIBuilderRef Dref
                                                 , LLVMMetadataRef Scope
                                                 , const char *Name
                                                 , const char *LinkageName
                                                 , LLVMMetadataRef File
                                                 , unsigned Line
                                                 , LLVMMetadataRef CompositeType
                                                 , int IsLocalToUnit
                                                 , int IsDefinition
                                                 , unsigned ScopeLine
                                                 , unsigned Flags
                                                 , int IsOptimized
                                                 , LLVMValueRef Func
                                                 , LLVMMetadataRef /*MDNode* */ TParam /*= nullptr*/
                                                 , LLVMMetadataRef /*MDNode* */ Decl /*= nullptr*/
                                                 )
    {
        DIBuilder *D = unwrap( Dref );
        DISubprogram* SP = D->createFunction( unwrap<DIScope>( Scope )
                                             , Name
                                             , LinkageName
                                             , File ? unwrap<DIFile>( File ) : nullptr
                                             , Line
                                             , unwrap<DISubroutineType>( CompositeType )
                                             , IsLocalToUnit
                                             , IsDefinition
                                             , ScopeLine
                                             , Flags
                                             , IsOptimized
                                             , Func ? unwrap<Function>( Func ) : nullptr
                                             , TParam ? unwrap<MDNode>( TParam ) : nullptr
                                             , Decl ? unwrap<MDNode>( Decl ) : nullptr
                                             );
        return wrap( SP );
    }

    LLVMMetadataRef LLVMDIBuilderCreateTempFunctionFwdDecl( LLVMDIBuilderRef Dref
                                                           , LLVMMetadataRef /*DIScope* */Scope
                                                           , char const* Name
                                                           , char const* LinkageName
                                                           , LLVMMetadataRef /*DIFile* */ File
                                                           , unsigned LineNo
                                                           , LLVMMetadataRef /*DISubroutineType* */ Ty
                                                           , bool isLocalToUnit
                                                           , bool isDefinition
                                                           , unsigned ScopeLine
                                                           , unsigned Flags /*= 0*/
                                                           , bool isOptimized /*= false*/
                                                           , LLVMValueRef /*Function* */ Fn /*= nullptr*/
                                                           , LLVMMetadataRef /*MDNode* */ TParam /*= nullptr*/
                                                           , LLVMMetadataRef /*MDNode* */ Decl /*= nullptr*/
                                                           )
    {
        DIBuilder *D = unwrap( Dref );
        DISubprogram* SP = D->createTempFunctionFwdDecl( unwrap<DIScope>( Scope )
                                                      , Name
                                                      , LinkageName
                                                      , File ? unwrap<DIFile>( File ) : nullptr
                                                      , LineNo
                                                      , unwrap<DISubroutineType>( Ty )
                                                      , isLocalToUnit
                                                      , isDefinition
                                                      , ScopeLine
                                                      , Flags
                                                      , isOptimized
                                                      , Fn ? unwrap<Function>( Fn ) : nullptr
                                                      , TParam ? unwrap<MDNode>( TParam ) : nullptr
                                                      , Decl ? unwrap<MDNode>( Decl ) : nullptr
                                                      );
        return wrap( SP );
    }

    LLVMMetadataRef LLVMDIBuilderCreateLocalVariable( LLVMDIBuilderRef Dref
                                                      , unsigned Tag
                                                      , LLVMMetadataRef Scope
                                                      , const char *Name
                                                      , LLVMMetadataRef File
                                                      , unsigned Line
                                                      , LLVMMetadataRef Ty
                                                      , int AlwaysPreserve
                                                      , unsigned Flags
                                                      , unsigned ArgNo
                                                      )
    {
        DIBuilder *D = unwrap( Dref );
        DIVariable* V = D->createLocalVariable( Tag
                                               , unwrap<DIScope>( Scope )
                                               , Name
                                               , unwrap<DIFile>( File )
                                               , Line
                                               , unwrap<DIType>( Ty )
                                               , AlwaysPreserve
                                               , Flags
                                               , ArgNo
                                               );
        return wrap( V );
    }

    LLVMMetadataRef LLVMDIBuilderCreateBasicType( LLVMDIBuilderRef Dref
                                                  , const char *Name
                                                  , uint64_t SizeInBits
                                                  , uint64_t AlignInBits
                                                  , unsigned Encoding
                                                  )
    {
        DIBuilder *D = unwrap( Dref );
        DIBasicType* T = D->createBasicType( Name, SizeInBits, AlignInBits, Encoding );
        return wrap( T );
    }

    LLVMMetadataRef LLVMDIBuilderCreatePointerType( LLVMDIBuilderRef Dref
                                                    , LLVMMetadataRef PointeeType
                                                    , uint64_t SizeInBits
                                                    , uint64_t AlignInBits
                                                    , const char *Name
                                                    )
    {
        DIBuilder *D = unwrap( Dref );
        DIDerivedType* T = D->createPointerType( unwrap<DIType>( PointeeType )
                                                , SizeInBits
                                                , AlignInBits
                                                , Name
                                                );
        return wrap( T );
    }

    LLVMMetadataRef LLVMDIBuilderCreateQualifiedType( LLVMDIBuilderRef Dref
                                                      , uint32_t Tag
                                                      , LLVMMetadataRef BaseType
                                                      )
    {
        DIBuilder* D = unwrap( Dref );
        DIDerivedType* T = D->createQualifiedType( Tag, unwrap<DIType>( BaseType ) );
        return wrap( T );
    }


    LLVMMetadataRef LLVMDIBuilderCreateSubroutineType( LLVMDIBuilderRef Dref
                                                       , LLVMMetadataRef File
                                                       , LLVMMetadataRef ParameterTypes
                                                       , unsigned Flags
                                                       )
    {
        DIBuilder *D = unwrap( Dref );
        DISubroutineType* sub = D->createSubroutineType( File ? unwrap<DIFile>( File ) : nullptr
                                                       , DITypeRefArray(unwrap<MDTuple>(ParameterTypes))
                                                       , Flags
                                                       );
        return wrap( sub );
    }

    LLVMMetadataRef LLVMDIBuilderCreateStructType( LLVMDIBuilderRef Dref
                                                   , LLVMMetadataRef Scope
                                                   , const char *Name
                                                   , LLVMMetadataRef File
                                                   , unsigned Line
                                                   , uint64_t SizeInBits
                                                   , uint64_t AlignInBits
                                                   , unsigned Flags
                                                   , LLVMMetadataRef DerivedFrom
                                                   , LLVMMetadataRef ElementTypes
                                                   )
    {
        DIBuilder *D = unwrap( Dref );
        DICompositeType* CT = D->createStructType( unwrap<DIScope>( Scope )
                                                  , Name
                                                  , File ? unwrap<DIFile>( File ) : nullptr
                                                  , Line
                                                  , SizeInBits
                                                  , AlignInBits
                                                  , Flags
                                                  , DerivedFrom ? unwrap<DIType>( DerivedFrom ) : nullptr
                                                  , ElementTypes ?  DINodeArray(unwrap<MDTuple>(ElementTypes)) : nullptr 
                                                  );
        return wrap( CT );
    }

LLVMMetadataRef LLVMDIBuilderCreateReplaceableCompositeType( LLVMDIBuilderRef Dref
                                                           , unsigned Tag
                                                           , const char *Name
                                                           , LLVMMetadataRef Scope
                                                           , LLVMMetadataRef File
                                                           , unsigned Line
                                                           , unsigned RuntimeLang
                                                           , uint64_t SizeInBits
                                                           , uint64_t AlignInBits
                                                           , unsigned Flags
                                                           )
{
    DIBuilder *D = unwrap(Dref);
    DICompositeType* type = D->createReplaceableCompositeType( Tag
                                                               , Name
                                                               , unwrap<DIScope>( Scope )
                                                               , File ? unwrap<DIFile>( File ) : nullptr
                                                               , Line
                                                               , RuntimeLang
                                                               , SizeInBits
                                                               , AlignInBits
                                                               , Flags
                                                               );
    return wrap( type );
}
    LLVMMetadataRef LLVMDIBuilderCreateMemberType( LLVMDIBuilderRef Dref
                                                   , LLVMMetadataRef Scope
                                                   , const char *Name
                                                   , LLVMMetadataRef File
                                                   , unsigned Line
                                                   , uint64_t SizeInBits
                                                   , uint64_t AlignInBits
                                                   , uint64_t OffsetInBits
                                                   , unsigned Flags
                                                   , LLVMMetadataRef Ty
                                                   )
    {
        DIBuilder *D = unwrap( Dref );
        DIDerivedType* DT = D->createMemberType( unwrap<DIScope>( Scope )
                                                , Name
                                                , File ? unwrap<DIFile>( File ) : nullptr
                                                , Line
                                                , SizeInBits
                                                , AlignInBits
                                                , OffsetInBits
                                                , Flags
                                                , unwrap<DIType>( Ty )
                                                );
        return wrap( DT );
    }

    LLVMMetadataRef LLVMDIBuilderCreateArrayType( LLVMDIBuilderRef Dref
                                                  , uint64_t SizeInBits
                                                  , uint64_t AlignInBits
                                                  , LLVMMetadataRef ElementType
                                                  , LLVMMetadataRef Subscripts
                                                  )
    {
        DIBuilder *D = unwrap( Dref );
        DICompositeType* CT = D->createArrayType( SizeInBits
                                                 , AlignInBits
                                                 , unwrap<DIType>( ElementType )
                                                 , DINodeArray(unwrap<MDTuple>(Subscripts))
                                                 );
        return wrap( CT );
    }

    LLVMMetadataRef LLVMDIBuilderCreateVectorType( LLVMDIBuilderRef Dref
                                                  , uint64_t SizeInBits
                                                  , uint64_t AlignInBits
                                                  , LLVMMetadataRef ElementType
                                                  , LLVMMetadataRef Subscripts
                                                  )
    {
        DIBuilder *D = unwrap( Dref );
        DICompositeType* CT = D->createVectorType( SizeInBits
                                                 , AlignInBits
                                                 , unwrap<DIType>( ElementType )
                                                 , DINodeArray(unwrap<MDTuple>(Subscripts))
                                                 );
        return wrap( CT );
    }

    LLVMMetadataRef LLVMDIBuilderCreateTypedef( LLVMDIBuilderRef Dref
                                                , LLVMMetadataRef Ty
                                                , const char *Name
                                                , LLVMMetadataRef File
                                                , unsigned Line
                                                , LLVMMetadataRef Context
                                                )
    {
        DIBuilder *D = unwrap( Dref );
        DIDerivedType* DT = D->createTypedef( unwrap<DIType>( Ty )
                                             , Name
                                             , File ? unwrap<DIFile>( File ) : nullptr
                                             , Line
                                             , Context ? unwrap<DIScope>( Context ) : nullptr
                                             );
        return wrap( DT );
    }

    LLVMMetadataRef LLVMDIBuilderGetOrCreateSubrange( LLVMDIBuilderRef Dref
                                                      , int64_t Lo
                                                      , int64_t Count
                                                      )
    {
        DIBuilder *D = unwrap( Dref );
        DISubrange* S = D->getOrCreateSubrange( Lo, Count );
        return wrap( S );
    }

    LLVMMetadataRef LLVMDIBuilderGetOrCreateArray( LLVMDIBuilderRef Dref
                                                   , LLVMMetadataRef *Data
                                                   , size_t Length
                                                   )
    {
        DIBuilder *D = unwrap( Dref );
        Metadata **DataValue = unwrap( Data );
        ArrayRef<Metadata *> Elements( DataValue, Length );
        DINodeArray A = D->getOrCreateArray( Elements );
        return wrap( A.get() );
    }

    LLVMMetadataRef LLVMDIBuilderGetOrCreateTypeArray( LLVMDIBuilderRef Dref
                                                       , LLVMMetadataRef *Data
                                                       , size_t Length
                                                       )
    {
        DIBuilder *D = unwrap( Dref );
        Metadata **DataValue = unwrap( Data );
        ArrayRef<Metadata *> Elements( DataValue, Length );
        DITypeRefArray A = D->getOrCreateTypeArray( Elements );
        return wrap( A.get() );
    }

    LLVMMetadataRef LLVMDIBuilderCreateExpression( LLVMDIBuilderRef Dref
                                                   , int64_t *Addr
                                                   , size_t Length
                                                   )
    {
        DIBuilder *D = unwrap( Dref );
        DIExpression* Expr = D->createExpression( ArrayRef<int64_t>( Addr, Length ) );
        return wrap( Expr );
    }

    LLVMValueRef LLVMDIBuilderInsertDeclareAtEnd( LLVMDIBuilderRef Dref
                                                  , LLVMValueRef Storage
                                                  , LLVMMetadataRef VarInfo
                                                  , LLVMMetadataRef Expr
                                                  , LLVMMetadataRef diLocation
                                                  , LLVMBasicBlockRef Block
                                                  )
    {
        DIBuilder *D = unwrap( Dref );
        Instruction *Instr = D->insertDeclare( unwrap( Storage )
                                               , unwrap<DILocalVariable>( VarInfo )
                                               , unwrap<DIExpression>( Expr )
                                               , unwrap<DILocation>( diLocation )
                                               , unwrap( Block )
                                               );
        return wrap( Instr );
    }

    LLVMValueRef LLVMDIBuilderInsertValueAtEnd( LLVMDIBuilderRef Dref
                                                , LLVMValueRef Val
                                                , uint64_t Offset
                                                , LLVMMetadataRef VarInfo
                                                , LLVMMetadataRef Expr
                                                , LLVMMetadataRef diLocation
                                                , LLVMBasicBlockRef Block
                                                )
    {
        DIBuilder *D = unwrap( Dref );
        Instruction *Instr = D->insertDbgValueIntrinsic( unwrap( Val )
                                                         , Offset
                                                         , unwrap<DILocalVariable>( VarInfo )
                                                         , unwrap<DIExpression>( Expr )
                                                         , unwrap<DILocation>( diLocation )
                                                         , unwrap( Block )
                                                         );
        return wrap( Instr );
    }

    LLVMMetadataRef LLVMDIBuilderCreateEnumerationType( LLVMDIBuilderRef Dref
                                                        , LLVMMetadataRef Scope          // DIScope
                                                        , char const* Name
                                                        , LLVMMetadataRef File           // DIFile
                                                        , unsigned LineNumber
                                                        , uint64_t SizeInBits
                                                        , uint64_t AlignInBits
                                                        , LLVMMetadataRef Elements       // DINodeArray
                                                        , LLVMMetadataRef UnderlyingType // DIType
                                                        , char const* UniqueId
                                                        )
    {
        DIBuilder* D = unwrap( Dref );
        DICompositeType* type = D->createEnumerationType( unwrap<DIScope>( Scope )
                                                        , Name
                                                        , unwrap<DIFile>( File )
                                                        , LineNumber
                                                        , SizeInBits
                                                        , AlignInBits
                                                        , DINodeArray(unwrap<MDTuple>(Elements))
                                                        , unwrap<DIType>( UnderlyingType )
                                                        , UniqueId
                                                        );
        return wrap( type );
    }

    /// createEnumerator - Create a single enumerator value.
    //DIEnumerator createEnumerator( StringRef Name, int64_t Val );
    LLVMMetadataRef LLVMDIBuilderCreateEnumeratorValue( LLVMDIBuilderRef Dref, char const* Name, int64_t Val )
    {
        DIBuilder* D = unwrap( Dref );
        DIEnumerator* enumerator = D->createEnumerator( Name, Val );
        return wrap( enumerator );
    }

    LLVMDwarfTag LLVMDIDescriptorGetTag( LLVMMetadataRef descriptor )
    {
        DINode* desc = unwrap<DINode>( descriptor );
        return (LLVMDwarfTag)desc->getTag( );
    }

    LLVMMetadataRef LLVMDIBuilderCreateGlobalVariable( LLVMDIBuilderRef Dref
                                                       , LLVMMetadataRef Context
                                                       , char const* Name
                                                       , char const* LinkageName
                                                       , LLVMMetadataRef File  // DIFile
                                                       , unsigned LineNo
                                                       , LLVMMetadataRef Ty    //DITypeRef
                                                       , LLVMBool isLocalToUnit
                                                       , LLVMValueRef Val
                                                       , LLVMMetadataRef Decl // = nullptr
                                                       )
    {
        DIBuilder* D = unwrap( Dref );
        DIGlobalVariable* globalVar = D->createGlobalVariable( unwrap<DIScope>( Context )
                                                            , Name
                                                            , LinkageName
                                                            , File ? unwrap<DIFile>( File ) : nullptr
                                                            , LineNo
                                                            , unwrap<DIType>( Ty )
                                                            , isLocalToUnit
                                                            , unwrap<Constant>( Val )
                                                            , Decl ? unwrap<MDNode>( Decl ) : nullptr
                                                            );
        return wrap( globalVar );
    }

    LLVMValueRef LLVMDIBuilderInsertDeclareBefore( LLVMDIBuilderRef Dref
                                                 , LLVMValueRef Storage      // Value
                                                 , LLVMMetadataRef VarInfo   // DIVariable
                                                 , LLVMMetadataRef Expr      // DIExpression
                                                 , LLVMMetadataRef diLocation // DILocation
                                                 , LLVMValueRef InsertBefore // Instruction
                                                 )
    {
        DIBuilder* D = unwrap( Dref );
        Instruction* pInstruction = D->insertDeclare( unwrap( Storage )
                                                    , unwrap<DILocalVariable>( VarInfo )
                                                    , unwrap<DIExpression>( Expr )
                                                    , unwrap<DILocation>( diLocation )
                                                    , unwrap<Instruction>(InsertBefore)
                                                    );
        return wrap( pInstruction );
    }

    char const* LLVMDIDescriptorAsString( LLVMMetadataRef descriptor )
    {
        std::string Messages;
        raw_string_ostream Msg( Messages );
        DINode* d = unwrap<DINode>( descriptor );
        d->print( Msg );
        return LLVMCreateMessage( Msg.str( ).c_str( ) );
    }

    void LLVMMDNodeReplaceAllUsesWith( LLVMMetadataRef oldDescriptor, LLVMMetadataRef newDescriptor )
    {
        MDNode* o = unwrap<MDNode>( oldDescriptor );
        Metadata* n = unwrap<Metadata>( newDescriptor );
        o->replaceAllUsesWith( n );
    }

    unsigned LLVMDITypeGetFlags( LLVMMetadataRef t )
    {
       DIType* type = unwrap<DIType>( t );
       return type->getFlags();
    }

    LLVMMetadataRef LLVMDILocation( LLVMContextRef context, unsigned Line, unsigned Column, LLVMMetadataRef scope, LLVMMetadataRef InlinedAt )
    {
        DILocation* pLoc = DILocation::get( *unwrap( context )
                                          , Line
                                          , Column
                                          , unwrap<DILocalScope>( scope )
                                          , InlinedAt ? unwrap<DILocation>( InlinedAt ) : nullptr
                                          );
        return wrap( pLoc );
    }

    char const* LLVMGetDITypeName( LLVMMetadataRef diType )
    {
        DIType* pType = unwrap<DIType>( diType );
        return LLVMCreateMessage( pType->getName( ).str().c_str() );
    }

    LLVMBool LLVMSubProgramDescribes( LLVMMetadataRef subProgram, LLVMValueRef /*const Function **/F )
    {
        DISubprogram* pSub = unwrap<DISubprogram>( subProgram );
        return pSub->describes( unwrap<Function>( F ) );
    }
}