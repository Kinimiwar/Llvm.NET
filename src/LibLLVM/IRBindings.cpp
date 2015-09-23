//===- IRBindings.cpp - Additional bindings for ir ------------------------===//
//
//                     The LLVM Compiler Infrastructure
//
// This file is distributed under the University of Illinois Open Source
// License. See LICENSE.TXT for details.
//
//===----------------------------------------------------------------------===//
//
// This file defines additional C bindings for the ir component.
//
//===----------------------------------------------------------------------===//

#include "IRBindings.h"

#include "llvm/IR/Attributes.h"
#include "llvm/IR/DebugLoc.h"
#include "llvm/IR/Function.h"
#include "llvm/IR/IRBuilder.h"
#include "llvm/IR/LLVMContext.h"
#include "llvm/IR/Module.h"

using namespace llvm;

extern "C"
{
    void LLVMSetFunctionStackAlignment( LLVMValueRef Fn, uint32_t alignment )
    {
        Function *Func = unwrap<Function>( Fn );
        AttrBuilder builder( Func->getAttributes( ), AttributeSet::AttrIndex::FunctionIndex );
        builder.addStackAlignmentAttr( alignment );
        auto newAttributeSet = AttributeSet::get( Func->getContext( ), AttributeSet::AttrIndex::FunctionIndex, builder );
        Func->setAttributes( newAttributeSet );
    }

    uint32_t LLVMGetFunctionStackAlignment( LLVMValueRef Fn )
    {
        Function *Func = unwrap<Function>( Fn );
        AttributeSet const attributes = Func->getAttributes( );
        return attributes.getStackAlignment( AttributeSet::AttrIndex::FunctionIndex );
    }

    void LLVMAddFunctionAttr2( LLVMValueRef Fn, int index, LLVMAttrKind kind )
    {
        Function *Func = unwrap<Function>( Fn );
        AttrBuilder builder( Func->getAttributes( ), index );
        builder.addAttribute( ( Attribute::AttrKind )kind );
        auto newAttributeSet = AttributeSet::get( Func->getContext( ), index, builder );
        Func->setAttributes( newAttributeSet );
    }

    LLVMBool LLVMHasFunctionAttr2( LLVMValueRef Fn, int index, LLVMAttrKind kind )
    {
        Function *Func = unwrap<Function>( Fn );
        AttributeSet const attributes = Func->getAttributes( );
        return attributes.hasAttribute( index, ( Attribute::AttrKind )kind );
    }

    void LLVMRemoveFunctionAttr2( LLVMValueRef Fn, int index, LLVMAttrKind kind )
    {
        Function *Func = unwrap<Function>( Fn );
        AttrBuilder builder( Func->getAttributes( ), index );
        builder.removeAttribute( ( Attribute::AttrKind )kind );
        auto newAttributeSet = AttributeSet::get( Func->getContext( ), index, builder );
        Func->setAttributes( newAttributeSet );
    }

    LLVMMetadataRef LLVMConstantAsMetadata( LLVMValueRef C )
    {
        return wrap( ConstantAsMetadata::get( unwrap<Constant>( C ) ) );
    }

    LLVMMetadataRef LLVMMDString2( LLVMContextRef C, char const *Str, unsigned SLen )
    {
        return wrap( MDString::get( *unwrap( C ), StringRef( Str, SLen ) ) );
    }

    LLVMMetadataRef LLVMMDNode2( LLVMContextRef C
                                 , LLVMMetadataRef *MDs
                                 , unsigned Count
                                 )
    {
        auto node = MDNode::get( *unwrap( C )
                                 , ArrayRef<Metadata *>( unwrap( MDs ), Count )
                                 );
        return wrap( node );
    }

    LLVMMetadataRef LLVMTemporaryMDNode( LLVMContextRef C
                                         , LLVMMetadataRef *MDs
                                         , unsigned Count
                                         )
    {
        auto node = MDTuple::getTemporary( *unwrap( C )
                                          , ArrayRef<Metadata *>( unwrap( MDs ), Count )
                                          );
        return wrap( node.release() );
    }

    void LLVMAddNamedMetadataOperand2( LLVMModuleRef M
                                       , char const *name
                                       , LLVMMetadataRef Val
                                       )
    {
        NamedMDNode *N = unwrap( M )->getOrInsertNamedMetadata( name );
        if( !N )
            return;

        if( !Val )
            return;

        N->addOperand( unwrap<MDNode>( Val ) );
    }

    void LLVMSetMetadata2( LLVMValueRef Inst, unsigned KindID, LLVMMetadataRef MD )
    {
        MDNode *N = MD ? unwrap<MDNode>( MD ) : nullptr;
        unwrap<Instruction>( Inst )->setMetadata( KindID, N );
    }

    void LLVMMetadataReplaceAllUsesWith( LLVMMetadataRef MD, LLVMMetadataRef New )
    {
        auto *Node = unwrap<MDNode>( MD );
        Node->replaceAllUsesWith( unwrap<Metadata>( New ) );
        MDNode::deleteTemporary( Node );
    }

    void LLVMSetCurrentDebugLocation2( LLVMBuilderRef Bref
                                       , unsigned Line
                                       , unsigned Col
                                       , LLVMMetadataRef Scope
                                       , LLVMMetadataRef InlinedAt
                                       )
    {
        auto loc = DebugLoc::get( Line
                                  , Col
                                  , Scope ? unwrap<MDNode>( Scope ) : nullptr
                                  , InlinedAt ? unwrap<MDNode>( InlinedAt ) : nullptr
                                  );
        unwrap( Bref )->SetCurrentDebugLocation( loc );
    }

    LLVMBool LLVMIsTemporary( LLVMMetadataRef M )
    {
        auto pMetadata = unwrap<MDNode>( M );
        return pMetadata->isTemporary();
    }

    LLVMBool LLVMIsResolved( LLVMMetadataRef M )
    {
        auto pMetadata = unwrap<MDNode>( M );
        return pMetadata->isResolved( );
    }

    char const* LLVMGetMDStringText( LLVMMetadataRef mdstring, unsigned* len )
    {
        MDString const* S = unwrap<MDString>( mdstring );
        *len = S->getString( ).size( );
        return S->getString( ).data( );
    }

    void LLVMMDNodeResolveCycles( LLVMMetadataRef M )
    {
        MDNode* pNode = unwrap<MDNode>( M );
        if( pNode->isResolved() )
            return;

        pNode->resolveCycles( );
    }
}