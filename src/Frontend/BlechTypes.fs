﻿// Copyright (c) 2019 - for information on the respective copyright owner
// see the NOTICE file and/or the repository 
// https://github.com/blech-lang/blech.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/// Defines F# types, i.e. data structures that represent typed Blech.
/// The individual F# types depend on one another and form an (abstract syntax) tree.
/// The main sections in the following definitions are the following:
///     - Blech types and Blech type declarations
///     - Blech declarations which declare some data containers (variables, objects, arguments, events)
///     - Blech code capsule declarations (subprograms, modules)
///     - Blech expressions
///     - Blech statments
module Blech.Frontend.BlechTypes

open System.Collections.Generic

open Blech.Common.PPrint
open Blech.Common.Range
open Blech.Common.TranslationUnitPath

open Constants
open CommonTypes
open DocPrint


//=============================================================================
// Types
//=============================================================================

/// The different mutability capabilities of data in Blech
[<RequireQualifiedAccess>]
type Mutability =
    | Variable // a local, mutable variable, or output parameter
    | Immutable // a local, immutable variable, or input parameter
    | CompileTimeConstant // a value known at compile time; need not be represented in memory at run time
    | StaticParameter // constant data which may be later modified in the HEX file when tuning the software

    override this.ToString() =
        match this with
        | Variable -> "var"
        | Immutable -> "let"
        | StaticParameter -> "param"
        | CompileTimeConstant -> "const"

    member this.ToDoc = txt <| this.ToString()

type ProcedureKind =
    | Activity
    | LocalFunction
    | ExternFunction
    | OpaqueProcedure

    override this.ToString() =
        match this with
        | Activity -> "activity"
        | LocalFunction -> "function"
        | ExternFunction -> "extern function"
        | OpaqueProcedure -> ""

    member this.ToDoc = txt <| this.ToString()

/// Data types
/// Only value-typed data may be returned from functions or activities
type ValueTypes =
    // simple types
    | Void // e.g. function return type
    | BoolType    
    | IntType of IntType
    | NatType of NatType
    | BitsType of BitsType
    | FloatType of FloatType
    //structured
    | ArrayType of size: Size * datatype: ValueTypes // we use uint64 for size to represent any positive integer                                                      
    | StructType of name:QName * VarDecl list  // value typed structs may only contain value typed fields
                                                             // these may be mutable or not
    | OpaqueSimple of QName
    | OpaqueArray of QName
    | OpaqueStruct of QName
    
    member this.ToDoc = txt <| this.ToString()
    
    override this.ToString () =
        match this with
        | Void -> "void"
        | BoolType -> "bool"
        | IntType i -> i.ToString()
        | NatType i -> i.ToString()
        | BitsType b -> b.ToString()    
        | FloatType f -> f.ToString()
        | ArrayType (s, e) -> sprintf "[%s]%s" (s.ToString()) (e.ToString())
        | StructType (q, _) -> q.ToString()
        | OpaqueArray n
        | OpaqueStruct n
        | OpaqueSimple n -> sprintf "type %s" (n.ToString())
    
    member this.IsPrimitive =
        match this with
        | Void | BoolType | IntType _ | NatType _ | BitsType _ | FloatType _ | OpaqueSimple _ -> true
        | ArrayType _ | StructType _ | OpaqueArray _ | OpaqueStruct _ -> false

    member this.IsOpaque =
        match this with
        | OpaqueArray _ | OpaqueStruct _ | OpaqueSimple _ -> true
        | Void | BoolType | IntType _ | NatType _ | BitsType _ | FloatType _
        | ArrayType _ | StructType _ -> false
    
/// Reference Types are not used anywhere as of the first release 2019/2020
/// Only introduced as a reminder that not all types are value types and
/// subsequent phases should already pay attention to this
and ReferenceTypes =
    | Ref of range:range * ValueTypes // this lifts a value type to a reference type as in "var ref x: int8 = y" where the type of x is reference of int8
    | StructType of range:range * QName * VarDecl list // reference typed structs may contain any typed fields
                                                       // these may be mutable or not
    member this.ToDoc = txt <| this.ToString()
    
    override this.ToString () =
        match this with
        | Ref (_,vt) -> "ref " + vt.ToString()
        | StructType (_, q, _) -> q.ToString()
    
    member this.TryRange =
        match this with
        | Ref (r, _)
        | StructType (r,_,_) -> 
            Some r


and Types = 
    | ValueTypes of ValueTypes
    | ReferenceTypes of ReferenceTypes
    | Any // used for wildcard
    | AnyComposite // compound literals
    | AnyInt // used for untyped integer literals
    | AnyBits // of Bits // used for untyped bits literals 
    | AnyFloat // of Float // used for untyped float literals
    
    member this.ToDoc =
        match this with
        | ValueTypes f -> f.ToDoc
        | ReferenceTypes r -> r.ToDoc
        | Any -> txt "wildcard"
        | AnyComposite -> txt "any composite"
        | AnyInt -> txt "any int"
        | AnyBits -> txt "any bits"
        | AnyFloat -> txt "any float"

    override this.ToString() = render None <| this.ToDoc
    
    member this.IsValueType = 
        match this with
        | ValueTypes _ -> true
        | _ -> false
    
    member this.IsPrimitive =
        match this with
        | ValueTypes v -> v.IsPrimitive
        | _ -> false

    member this.IsWildcard = 
        this = Any

    member this.IsCompoundLiteral =
        this = AnyComposite

    member this.IsPrimitiveAny = 
        match this with
        | AnyInt | AnyBits | AnyFloat -> true
        | _ -> false
    
    member this.IsSomeAny = 
        match this with
        | Any | AnyComposite | AnyInt | AnyBits | AnyFloat -> true
        | ValueTypes _ | ReferenceTypes _ -> false

    member this.IsOpaque =
        match this with 
        | ValueTypes v -> v.IsOpaque
        | Any | AnyComposite | AnyInt | AnyBits | AnyFloat
        | ReferenceTypes _ -> false


//=============================================================================
// Data declarations 
//=============================================================================

// Declaration of a new type (note that type aliases do not exist in the typed Blech, they are resolved!)
and NewTypeDecl =
    {
        representation: Types
        annotation: Attribute.OtherDecl
    }
    member this.ToDoc = 
        this.annotation.ToDoc @ [this.representation.ToDoc]
        |> dpToplevelClose
    
    override this.ToString () = this.representation.ToString()

/// Variable Declaration is used to represent a declaration of global and 
/// local, mutable and immutable data. Of course, not all combinations are
/// allowed: no mutable global variables in Blech!
/// Formal parameters of functions and activities are represented by ParamDecl.
and VarDecl = 
    {
        pos: range
        name: QName
        datatype: Types
        mutability: Mutability
        initValue: TypedRhs // after type checking every declaration must have an initial value
        annotation: Attribute.VarDecl
        allReferences: HashSet<range> // used for language server
    }
            
    member this.IsConst =
        this.mutability.Equals Mutability.CompileTimeConstant
    
    member this.IsParam =
        this.mutability.Equals Mutability.StaticParameter

    member this.ToDoc =
        let vdDoc = 
            this.mutability.ToDoc
            <+> match this.datatype with | ReferenceTypes _ -> txt "ref" <+> empty | _ -> empty
            <^> txt (this.name.ToString())
            <^> txt ":" <+> this.datatype.ToDoc
            <+> txt "=" <+> this.initValue.ToDoc
    
        this.annotation.ToDoc @ [vdDoc]
        |> dpToplevelClose

    override this.ToString () = render None <| this.ToDoc
    
/// variables and constants bound to an external C declaration
and ExternalVarDecl =
    {
        pos: range
        name: QName
        datatype: Types
        mutability: Mutability
        // no init value for external variables!
        annotation: Attribute.VarDecl
        allReferences: HashSet<range>
    }

    member this.ToDoc =
        let vdDoc =
            txt "extern"
            <+> this.mutability.ToDoc
            <+> match this.datatype with | ReferenceTypes _ -> txt "ref" <+> empty | _ -> empty
            <^> txt (this.name.ToString())
            <^> txt ":" <+> this.datatype.ToDoc
        this.annotation.ToDoc @ [vdDoc]
        |> dpToplevelClose

    override this.ToString () = render None <| this.ToDoc
    
    member this.IsConst =
        this.mutability.Equals Mutability.CompileTimeConstant
    
    member this.IsParam =
        this.mutability.Equals Mutability.StaticParameter

/// A parameter declaration consists of a name and datatype
/// unlike a variable declaration, an argument declaration has no init value
and ParamDecl =  // TODO: add annotations, fjg 21.03.19
    {
        pos: range
        name : QName
        datatype: Types
        isMutable: bool
        allReferences: HashSet<range>
    }

    member this.ToDoc =
        // if this.isMutable then txt "var" else txt "let" <-- this was outdated
        // <+> 
        txt (this.name.ToString())
        <^> txt ":" <+> this.datatype.ToDoc

    override this.ToString () = render None <| this.ToDoc 

/// A location declaration occurs inside a statement or a match condition.
/// It introduces a local variable.
/// It is either mutable 'let' or immutable 'var'.
//and LocationDecl = 
//    {
//        pos: range
//        name: QName
//        datatype: Types
//        isMutable: bool
//        // no init value for locations 
//        // no annotations for locations
//        allReferences: HashSet<range>
//    }

//    member this.ToDoc =
//        if this.isMutable then txt "var" else txt "let"
//        <+> txt (this.name.ToString())
//        <^> txt ":" <+> this.datatype.ToDoc

//    override this.ToString () = render None <| this.ToDoc 

//=============================================================================   
// Code capsules 
//=============================================================================

/// We use "procedure" to refer to a function or activity
/// The data structure is split in a prototype (as represented in signatures) and
/// implementation (when there is a body as in blc files)

and ProcedurePrototype =
    {
        pos: range
        kind: ProcedureKind // [extern] function or activity
        singletons: QName list // Singletons called from this procedure. Includes its own name iff declared as singleton.
        name: QName
        inputs: ParamDecl list
        outputs: ParamDecl list
        returns: ValueTypes
        annotation: Attribute.FunctionPrototype
        allReferences: HashSet<range>
    }

    member this.IsFunction =
        match this.kind with
        | Activity | OpaqueProcedure -> false
        | LocalFunction | ExternFunction -> true

    member this.IsActivity =
        match this.kind with
        | Activity -> true
        | OpaqueProcedure | LocalFunction | ExternFunction -> false

    //member this.IsExtern =
    //    match this.kind with
    //    | Activity | LocalFunction -> false
    //    | ExternFunction -> true

    member this.IsSingleton = not this.singletons.IsEmpty

    member this.ToDoc =
        let ins = this.inputs |> List.map (fun i -> i.ToDoc) |> dpCommaSeparatedInParens
        let outs = this.outputs|> List.map (fun i -> i.ToDoc) |> dpCommaSeparatedInParens
        let protoDoc = 
            this.kind.ToDoc
            <+> txt (this.name.ToString())
            <^> ( ins
                  <..> outs
                  <.> match this.returns with | ValueTypes.Void -> empty | _ -> txt "returns" <+> this.returns.ToDoc
                  |> align
                  |> group )
        let protoDoc = if this.IsSingleton then txt "singleton" <+> protoDoc else protoDoc
        this.annotation.ToDoc @ [protoDoc]
        |> dpToplevelClose

    override this.ToString() = 
        render None <| this.ToDoc
    
    member this.IsDirectCCall = 
        this.annotation.isDirectCCall

and ProcedureImpl =
    {
        pos: range // contains the range of the protoype and the code block
        prototype: ProcedurePrototype
        globalInputs: ExternalVarDecl list
        globalOutputsInScope: ExternalVarDecl list // for code generation
        body: Stmt list // TODO: maybe turn into stmt?
        annotation: Attribute.SubProgram
        allReferences: HashSet<range>
    }

    member this.Singletons = this.prototype.singletons
    member this.Name = this.prototype.name
    member this.Inputs = this.prototype.inputs
    member this.Outputs = this.prototype.outputs
    member this.Returns = this.prototype.returns
    
    member this.IsFunction = this.prototype.IsFunction
    member this.IsActivity = this.prototype.IsActivity
    member this.IsSingleton = this.prototype.IsSingleton

    member this.ToDoc =
        let ins = this.Inputs |> List.map (fun i -> i.ToDoc) |> dpCommaSeparatedInParens
        let outs = this.Outputs|> List.map (fun i -> i.ToDoc) |> dpCommaSeparatedInParens
        let spdoc = 
            this.prototype.kind.ToDoc
            <+> txt (this.Name.ToString())
            <^> ( ins
                  <..> outs
                  <.> match this.Returns with | ValueTypes.Void -> empty | _ -> txt "returns" <+> this.Returns.ToDoc
                  |> align
                  |> group )
            <.> (this.body |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
            <.> txt "end"
        let spdoc =
            if this.IsSingleton then 
                let singletonsBlock = 
                    List.map (fun n -> txt (n.ToString())) this.Singletons
                    |> dpCommaSeparatedInBrackets
                txt "singleton"
                <+> singletonsBlock
                <+> spdoc 
            else spdoc

        this.annotation.ToDoc @ [spdoc]
        |> dpToplevelClose

    override this.ToString() = 
        render None <| this.ToDoc

    member this.IsEntryPoint =
        Option.isSome this.annotation.entryPoint


/// A Blech module corresponds to a file    
and BlechModule =
    {
        name: TranslationUnitPath
        documentation: Attribute.OtherDecl option
        types: Types list
        funPrototypes: ProcedurePrototype list
        funacts: ProcedureImpl list
        variables: VarDecl list
        externalVariables: ExternalVarDecl list
        memberPragmas: Attribute.MemberPragma list
        entryPoint: ProcedureImpl option
    }

    // creates an empty type checking result for test purposes
    static member MakeEmpty moduleName =
        { 
            name = moduleName
            documentation = None
            types = List.empty 
            funPrototypes = List.empty 
            funacts = List.empty 
            variables = List.empty 
            externalVariables = List.empty 
            memberPragmas = List.empty 
            entryPoint = None 
        }

    override this.ToString() = 
        render (Some 72) <| this.ToDoc
    
    
    member this.ToDoc =
        [ txt (this.name.ToString()) ]
        |> List.append <| (this.memberPragmas |> List.map (fun mp -> mp.ToDoc))
        |> List.append <| (this.types |> List.map (fun t -> t.ToDoc))
        |> List.append <| (this.variables |> List.map (fun v -> v.ToDoc))
        |> List.append <| (this.externalVariables |> List.map (fun v -> v.ToDoc))
        |> List.append <| (this.funPrototypes |> List.map (fun f -> f.ToDoc))
        |> List.append <| (this.funacts |> List.map (fun f -> f.ToDoc))
        |> punctuate line 
        |> vsep

    member this.IsAProgram = 
        Option.isSome this.entryPoint

//=============================================================================
// Expressions 
//=============================================================================

/// Typed memory location (a name, a field, an array cell)
and TypedMemLoc =
    | Loc of QName
    | FieldAccess of TypedMemLoc * Identifier // fst is the path leading to the struct, snd is the field being accessed
                                              // accessing a field in a struct of struct s1.f1.f2 amounts to
                                              // TML(TML(Loc s1, f1), f2)
    | ArrayAccess of TypedMemLoc * TypedRhs
    
    /// Fully qualified name as a string
    override this.ToString () =
        match this with
        | Loc qname -> qname.ToString()
        | FieldAccess (tml, ident) -> tml.ToString() + "." + ident
        | ArrayAccess (tml, idx) -> sprintf "%s[%s]" (tml.ToString()) (idx.ToString())
    
    /// Use when printing the name without full qualification
    member this.ToBasicString () =
        match this with
        | Loc qname -> qname.basicId.ToString() // here basicId
        | FieldAccess (tml, ident) -> tml.ToBasicString() + "." + ident
        | ArrayAccess (tml, idx) -> sprintf "%s[%s]" (tml.ToBasicString()) (idx.ToString()) // TODO: problem, idx.ToString will give the long (not basic) string

    /// Use when printing the state
    member this.ToUnderscoreString () =
        match this with
        | Loc qname -> qname.ToUnderscoreString() 
        | FieldAccess (tml, ident) -> tml.ToUnderscoreString() + "." + ident
        | ArrayAccess (tml, idx) -> sprintf "%s[%s]" (tml.ToUnderscoreString()) (idx.ToString()) // TODO: problem, idx.ToString will give the long (not basic) string
    
    /// Fully qualified name as a Doc
    member this.ToDoc = txt <| this.ToString()
    
    /// Fully qualified name prefix of this location.
    /// Leaves out field or array accesses.
    member this.QNamePrefix =
        match this with
        | Loc qname -> qname
        | FieldAccess (tml, _) 
        | ArrayAccess (tml, _) -> tml.QNamePrefix

    /// Given a QName "ctx" and a TML "a.b.c" produce "ctx.a.b.c"
    member this.PrependFieldAccess newLoc =
        match this with
        | Loc qname -> FieldAccess(Loc newLoc, qname.basicId)
        | FieldAccess (tml, ident) -> FieldAccess(tml.PrependFieldAccess newLoc, ident) 
        | ArrayAccess (tml, idx) -> ArrayAccess(tml.PrependFieldAccess newLoc, idx)
    
    member this.AddFieldAccess ident = FieldAccess (this, ident)
    
    member this.AddArrayAccess idx = ArrayAccess (this, idx)
    
    member this.GetPrefixBeforeArrayAccess =
        match this with
        | Loc _ -> None
        | FieldAccess (preTml, _) -> preTml.GetPrefixBeforeArrayAccess
        | ArrayAccess (preTml, _) ->
            match preTml.GetPrefixBeforeArrayAccess with
            | None _ -> Some preTml
            | x -> x
    
    member this.FindAllIndexExpr =
        match this with
        | Loc _ -> []
        | FieldAccess (subtml, _) -> subtml.FindAllIndexExpr
        | ArrayAccess (subtml, idxExpr) -> idxExpr :: subtml.FindAllIndexExpr 

/// left hand side expressions, must represent a memory location that is written to
and LhsStructure =
    // discard the assigned rhs value
    | Wildcard 
    // return location for activity calls
    | ReturnVar 
    // locations
    | LhsCur of TypedMemLoc
    | LhsNext of TypedMemLoc
    
    member this.ToDoc =
        match this with
        | Wildcard -> txt "_"
        | ReturnVar -> txt "return"
        | LhsCur t -> t.ToDoc
        | LhsNext t -> txt "next" <+> t.ToDoc
    
    override this.ToString () = render None <| this.ToDoc
    
    member this.AddFieldAccess ident = 
        match this with
        | Wildcard -> failwith "Cannot add a field access to a wildcard."
        | ReturnVar -> failwith "Cannot add a field access to a return."
        | LhsCur t -> LhsCur (t.AddFieldAccess ident)
        | LhsNext t -> LhsNext (t.AddFieldAccess ident)
    
    member this.AddArrayAccess (idx: TypedRhs) = 
        match this with
        | Wildcard -> failwith "Cannot add a field access to a wildcard."
        | ReturnVar -> failwith "Cannot add a field access to a wildcard."
        | LhsCur t -> LhsCur (t.AddArrayAccess idx)
        | LhsNext t -> LhsNext (t.AddArrayAccess idx)
    
    //member this.AddArrayAccess (idx: Int) = 
    //    let rhs = {rhs = IntConst idx; typ = ValueTypes (IntType (IntType.RequiredType idx)); range = range0}
    //    this.AddArrayAccess rhs

    //member this.AddArrayAccess (idx: Nat) = 
    //    let rhs = {rhs = NatConst idx; typ = ValueTypes (NatType (NatType.RequiredType idx)); range = range0}
    //    this.AddArrayAccess rhs

/// right hand side expression, in assignments, arguments to subprograms or index of array access    
and RhsStructure =
    // locations
    | RhsCur of TypedMemLoc
    | Prev of TypedMemLoc
    // call
    | FunCall of QName * TypedRhs list * TypedLhs list
    // constants and literals
    | BoolConst of bool
    | NatConst of Constants.Nat
    | IntConst of Constants.Int
    | BitsConst of Constants.Bits
    | FloatConst of Constants.Float
    | ResetConst // empty struct or array, reset to default values
    | StructConst of (Identifier * TypedRhs) list
    | ArrayConst of (Constants.Size * TypedRhs) list
    //
    | Convert of TypedRhs * Types * Behaviour
    // logical
    | Neg of TypedRhs
    | Conj of TypedRhs * TypedRhs
    | Disj of TypedRhs * TypedRhs
    // bitwise
    | Bnot of TypedRhs
    | Band of TypedRhs * TypedRhs
    | Bor of TypedRhs * TypedRhs
    | Bxor of TypedRhs * TypedRhs
    | Shl of TypedRhs * TypedRhs
    | Shr of TypedRhs * TypedRhs
    | Sshr of TypedRhs * TypedRhs
    | Rotl of TypedRhs * TypedRhs
    | Rotr of TypedRhs * TypedRhs
    // relational
    | Les of TypedRhs * TypedRhs
    | Leq of TypedRhs * TypedRhs
    | Equ of TypedRhs * TypedRhs
    // arithmetic
    | Add of TypedRhs * TypedRhs
    | Sub of TypedRhs * TypedRhs
    | Mul of TypedRhs * TypedRhs
    | Div of TypedRhs * TypedRhs
    | Mod of TypedRhs * TypedRhs

    member this.ToDoc = this.ppExpr dpPrec.["min"]

    member this.IsCompoundConst =
        match this with
        | ResetConst | StructConst _ | ArrayConst _ -> true
        | _ -> false

    member this.GetIntConst: Int =
        match this with
        | IntConst i -> i
        | _ -> failwith "expected an IntConst"

    member this.GetNatConst: Nat =
        match this with
        | NatConst n -> n
        | _ -> failwith "expected a NatConst"
    
    member this.GetBitsConst: Bits =
        match this with
        | BitsConst b -> b
        | _ -> failwith "expected a BitsConst"
    
    member this.GetFloatConst: Float =
        match this with
        | FloatConst f -> f
        | _ -> failwith "expected a FloatConst"
    
    member this.ppExpr outerPrec =
        match this with
        // names
        //| Ref name
        | RhsCur t -> t.ToDoc
        | Prev t -> txt "prev" <+> t.ToDoc
        | FunCall (name, ins, outs) ->
            let name = txt <| name.ToString()
            let ins =  ins |> List.map (fun i -> i.ToDoc)
            let outs = outs |> List.map (fun o -> o.ToDoc)
            dpBlechCall name ins outs
        // constants and literals
        | BoolConst c -> if c then txt "true" else txt "false"
        | IntConst i -> txt <| i.ToString()
        | BitsConst b -> txt <| b.ToString()
        | NatConst n -> txt <| n.ToString()
        | FloatConst f -> txt <| f.ToString()
        | ResetConst -> [empty] |> dpCommaSeparatedInBraces
        | StructConst structFieldExprList ->
            structFieldExprList
            |> List.map (fun (ident, typedRhs) -> txt ident <+> chr '=' <+> typedRhs.rhs.ppExpr outerPrec)
            |> dpCommaSeparatedInBraces
        | ArrayConst elems ->
            elems
            |> List.map (fun elem -> (snd elem).rhs.ppExpr outerPrec)
            |> dpCommaSeparatedInBraces
        // subexpressions
        // type conversion
        | Convert (e, t, b)->
            fun p -> e.rhs.ppExpr p <.> txt ("as" + string b) <+> t.ToDoc
            |> dpPrecedence outerPrec dpPrec.["as"]
        // logical
        | Neg expr ->
            fun p -> txt "not" <+> expr.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["not"]
        | Conj (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "and" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["and"]
        | Disj (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "or" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["or"]
        // bitwise
        | Bnot expr ->
            fun p -> txt "~" <+> expr.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["~"]
        | Band (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "&" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["&"]
        | Bor (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "|" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["|"]
        | Bxor (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "^" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["^"]
        | Shl (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "<<" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["<<"]
        | Shr (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt ">>" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.[">>"]
        | Sshr (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "+>>" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["+>>"]
        | Rotl (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "<<>" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["<<>"]
        | Rotr (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "<>>" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["<>>"]
        // relational
        | Les (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "<" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["<"]
        | Leq (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "<=" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["<="]
        | Equ (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "==" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["=="]
        // arithmetic
        | Add (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "+" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["+"]
        | Sub (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "-" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["-"]
        | Mul (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "*" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["*"]
        | Div (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "/" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["/"]
        | Mod (e1, e2) ->
            fun p -> e1.rhs.ppExpr p <.> txt "%" <+> e2.rhs.ppExpr p 
            |> dpPrecedence outerPrec dpPrec.["%"]
       

    override this.ToString () = render None <| this.ToDoc

and TypedRhs =
    {
        rhs: RhsStructure
        typ: Types
        range: range
    }
    member this.ToDoc = this.rhs.ToDoc
    override this.ToString () = render None <| this.ToDoc
    member this.Range = this.range
    member this.SetRange r = {this with range = r}
    
and TypedLhs =
    {
        lhs: LhsStructure
        typ: Types
        range: range
    }
    member this.ToDoc = this.lhs.ToDoc
    override this.ToString () = render None <| this.ToDoc
    member this.Range = this.range
    member this.SetRange r = {this with range = r}


//=============================================================================
// Receiver: Adds locations declarations to run and emit statements on top of TypedLhs
// Condition: Adds matching and condition list on top of TypedRhs // to be defined
//=============================================================================


and Receiver =
    | UsedLoc of TypedLhs
    | FreshLoc of VarDecl
    | ReturnLoc of TypedLhs

    member this.ToDoc =
       match this with
       | UsedLoc tlhs -> tlhs.lhs.ToDoc
       | FreshLoc ldecl -> ldecl.ToDoc
       | ReturnLoc tlhs -> tlhs.lhs.ToDoc
    
    override this.ToString() = render None <| this.ToDoc
    
    member this.Range = 
        match this with
        | UsedLoc tlhs -> tlhs.Range
        | FreshLoc ldecl -> ldecl.pos
        | ReturnLoc tlhs -> tlhs.Range
    
    member this.Typ =
        match this with
        | UsedLoc tlhs -> tlhs.typ
        | FreshLoc vdecl -> vdecl.datatype
        | ReturnLoc tlhs -> tlhs.typ
    

//=============================================================================
// Statements 
//=============================================================================

/// Individual statments
and Stmt =
    // local variable or object declaration
    | VarDecl of VarDecl
    | ExternalVarDecl of ExternalVarDecl
    // actions
    | Assign of range * TypedLhs * TypedRhs
    | Assert of range * TypedRhs * string
    | Assume of range * TypedRhs * string
    | Print of range * string * (TypedRhs list)
    // pause
    | Await of range * TypedRhs
    // control flow
    | ITE of range * TypedRhs * Stmt list * Stmt list // line, condition, if-block, else-block (each possibly empty!)
    | Cobegin of range * (Strength * Stmt list) list // line, list of threads each being weak/strong with a code block
    | WhileRepeat of range * TypedRhs * Stmt list // line, condition, loop body
    | RepeatUntil of range * Stmt list * TypedRhs * bool // line, loop body, condition, isEndlessLoop
    | Preempt of range * Preemption * TypedRhs * Moment * Stmt list // line, kind of preemption, abort condition, moment of preemption, body
    // scoping
    | StmtSequence of Stmt list // DO block, ...for scoping reasons
    // calling
    | ActivityCall of range * QName * Receiver option * TypedRhs list * TypedLhs list // line, who to call, result, inputs, outputs
    | FunctionCall of range * QName * TypedRhs list * TypedLhs list // line, who to call, inputs, outputs
    // | Emit of range * Receiver * TypedRhs option // line, event, payload
    | Return of range * TypedRhs option // line, expressions to return
        
    member this.ToDoc =
        match this with
        | VarDecl v -> v.ToDoc
        | ExternalVarDecl v -> v.ToDoc
        | Assign (_, l, r) -> l.ToDoc <+> (txt "=" <.> r.ToDoc |> gnest dpTabsize)
        | Assert (_, r, m) -> txt "assert" <+> r.ToDoc <.> (dquotes <| txt m)
        | Assume (_, r, m) -> txt "assume" <+> r.ToDoc <.> (dquotes <| txt m)
        | Print (_, m, rs) ->
            txt "printf" 
            <+> ( txt m :: (rs |> List.map (fun r -> r.ToDoc)) 
                  |> dpCommaSeparatedInParens)
        | Await (_, c) -> txt "await" <+> c.ToDoc
        | ITE (_, c, bIf, bElse) ->
            txt "if" <+> c.ToDoc <+> txt "then"
            <.> ( bIf |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
            <.> txt "else" 
            <.> ( bElse |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
            <.> txt "end"
        | Cobegin (_, blocks) ->
            let ppStrength = function
                | CommonTypes.Weak -> txt "weak"
                | CommonTypes.Strong -> empty
            let ppWithBlock (withStrength, (withBranch: Stmt list)) =
                txt "with" <+> ppStrength withStrength
                <.> (withBranch |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
            match blocks with
            | [] -> empty
            | (cobStrength, cobBranch) :: withBlocks -> 
                txt "cobegin" <+> ppStrength cobStrength
                <.> (cobBranch |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
                <.> (withBlocks |> List.map ppWithBlock |> vsep)
                <.> txt "end"
        | WhileRepeat (_, c, b) ->
            txt "while" <+> c.ToDoc <+> txt "repeat"
            <.> ( b |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
            <.> txt "end"
        | RepeatUntil (_, s, c, i) ->
            txt "repeat" 
            <.> (s |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
            <.> if i then txt "end"
                else txt "until" <+> c.ToDoc <+> txt "end"
        | Preempt (_, p, c, m, b) ->
            let ppPreemption = function
                | CommonTypes.Abort -> txt "abort"
                | CommonTypes.Reset -> txt "reset"
                | CommonTypes.Suspend -> txt "suspend"
            let ppMoment = function 
                | CommonTypes.After -> txt "after"
                | CommonTypes.Before -> txt "before"
                | CommonTypes.OnNext -> txt "next"
            ppPreemption p <+> txt "when" <+> c.ToDoc <+> ppMoment m
            <.> (b |> List.map(fun s -> s.ToDoc) |> vsep |> indent dpTabsize)
            <.> txt "end"
        | StmtSequence ss ->
            ss
            |> List.map (fun s -> s.ToDoc)
            |> vsep
        | ActivityCall (_, qname, retvar, ins, outs) ->
            let run = 
                match retvar with
                | None -> txt "run"
                | Some (ReturnLoc _) ->
                    txt "return run"
                | Some (UsedLoc tlhs) ->
                    txt "run" <+> tlhs.ToDoc <+> txt "="
                | Some (FreshLoc vdecl) ->
                    txt "run" <+> vdecl.ToDoc <+> txt "="
            let qname = txt <| qname.ToString()
            let ins =  ins |> List.map (fun i -> i.ToDoc)
            let outs = outs |> List.map (fun o -> o.ToDoc)
            run <+> dpBlechCall qname ins outs
        | FunctionCall (_, qname, ins, outs) ->
            let qname = txt <| qname.ToString()
            let ins =  ins |> List.map (fun i -> i.ToDoc)
            let outs = outs |> List.map (fun o -> o.ToDoc)
            dpBlechCall qname ins outs
        | Return (_, exprOpt) ->
            match exprOpt with
            | None -> txt "return"
            | Some expr -> txt "return" <+> expr.ToDoc
            
    override this.ToString () = 
        render None <| this.ToDoc

// end of Blech types definition

/// Assuming the argument has been reduced as far as possible,
/// simply check whether the structure is a _Const with only _Const fields
let rec public isLiteral expr =
    match expr.rhs with
    | IntConst _ 
    | BoolConst _
    | BitsConst _
    | FloatConst _
    | ResetConst -> true
    | StructConst fields -> 
        fields |> List.forall (snd >> isLiteral)
    | ArrayConst fields -> 
        fields |> List.forall (snd >> isLiteral)
    | _ -> false // location names, binary expressions, function calls are not constants under above assumption