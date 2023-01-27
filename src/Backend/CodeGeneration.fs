// Copyright (c) 2019 - for information on the respective copyright owner
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

module Blech.Backend.CodeGeneration

// Concerning header files we follow:
// http://umich.edu/~eecs381/handouts/CppHeaderFileGuidelines.pdf


open Blech.Common
open Blech.Common.TranslationUnitPath
open Blech.Common.PPrint.PPrint

open Blech.Frontend
open Blech.Frontend.BlechTypes
open Blech.Frontend.DocPrint

open CPdataAccess2
open CPrinter
open TraceGenerator


[<RequireQualifiedAccess>]
module Comment =

    let generatedCode =
        cpGeneratedComment <| txt "This is generated code - do not touch!"

    // c file comments

    let cHeaders =
        cpGeneratedComment <| txt "used C headers"
    
    let blechHeader = 
        cpGeneratedComment <| txt "blech types"

    let importHeaders = 
        cpGeneratedComment <| txt "imports"
    
    let selfInclude = 
        cpGeneratedComment <| txt "exports, user types and C wrappers"

    let necessaryHeaders =
        cpGeneratedComment <| txt "necessary C headers"

    let cConstants = 
        cpGeneratedComment <| txt "direct C constants"
    
    let cFunctions = 
        cpGeneratedComment <| txt "direct C functions"
    
    //let constants = 
    //    cpGeneratedComment <| txt "constants"

    let parameters = 
        cpGeneratedComment <| txt "parameters"
    
    let state = 
        cpGeneratedComment <| txt "state"
    
    let compilations =  
        cpGeneratedComment <| txt "activities and functions"
    
    let progam = 
        cpGeneratedComment <| txt "blech program"
    
    // h file comments

    let activityContexts =
        cpGeneratedComment <| txt "activity contexts"
  
    let cPrototypes = 
        cpGeneratedComment <| txt "extern functions to be implemented in C"

    let exportedFunctions =
        cpGeneratedComment <| txt "activities and functions"
    
    let userTypes = 
        cpGeneratedComment <| txt "all user defined types"

    let cProgramFunctions =
        cpGeneratedComment <| txt "program functions: tick, init"

    let statePrinters = 
        cpGeneratedComment <| txt "trace functions: state printers"

    let traceFunction =
        cpGeneratedComment <| txt "trace function: printState"

    // app file comments

    let blechCInclude = 
        cpGeneratedComment <| txt "the generated blech program"
    
    let externalState =
        cpGeneratedComment <| txt "external state for the blech program"

    let testFunction = 
        cpGeneratedComment <| txt "the test main loop"

    // trace printing comments

    
/// Translates all sub programs of a module into a list of compilations
let public translate ctx (pack: BlechModule) =
    // translate all subprograms in order
    pack.funacts
    |> List.fold (fun compilations funact ->
        if funact.IsFunction then FunctionTranslator.translate ctx funact
        else ActivityTranslator.translate ctx funact
        |> List.singleton
        |> List.append compilations) []


/// Common header definitions
let stdioHeader = txt "#include <stdio.h>"

let programHeader = txt "#include <string.h>"

let includeQuotedHfile hfile = 
    txt "#include" <+> (txt hfile |> dquotes)

let blechHeader = includeQuotedHfile "blech.h"

let generateSelfHeader =
    TranslatePath.moduleToInclude >> includeQuotedHfile
        
let generateCProgramHeader =
    TranslatePath.moduleToInclude >> includeQuotedHfile

let generateIncludeGuards moduleName =
    let guard = txt <| TranslatePath.moduleToIncludeGuard moduleName
    let includeGuardBegin = 
        txt "#ifndef" <+> guard
        <.> txt "#define" <+> guard
    let includeGuardEnd = 
        txt "#endif" <+> enclose (txt "/* ", txt " */") guard
    includeGuardBegin, includeGuardEnd

let generateSubmoduleIncludes otherMods =
    otherMods
    |> List.map (fun otherMod -> TranslatePath.moduleToInclude otherMod.name |> includeQuotedHfile)
    |> dpBlock

let generateModuleDocumentation (optModSpecDocComments: Attribute.OtherDecl option) =
    match optModSpecDocComments with
    | None ->
        empty  
    | Some attr ->
        cpModuleDoc attr.doc

let private mkFunctionPrototypes tcc = 
    tcc.nameToDecl.Values
    |> Seq.choose (fun d -> match d with | Declarable.ProcedurePrototype f -> Some f | _ -> None)

let private mkCCalls functionPrototypes moduleName =
    Seq.filter (fun (fp: ProcedurePrototype) -> fp.IsDirectCCall && not (fp.name.IsImported moduleName)) functionPrototypes


/// Emit C code for module as Doc
let private cpModuleCode ctx (moduleName: TranslationUnitPath) 
                             (documentation: Attribute.OtherDecl option)
                             (pragmas: Attribute.MemberPragma list) 
                             importedModules
                             (compilations: Compilation list) 
                             entryPointOpt =

    let moduleDoc = generateModuleDocumentation documentation

    let selfHeader = generateSelfHeader moduleName
        
    // Blech const become #define macros in C
    // right now all go to the module code, because nothing gets exported
    // constants local to subprograms should also work

    let varDecls = 
        ctx.tcc.nameToDecl.Values
        |> Seq.choose (fun d -> match d with | Declarable.VarDecl f -> Some f | _ -> None)

    let externLocalVars = 
        ctx.tcc.nameToDecl.Values
        |> Seq.choose (fun d -> match d with | Declarable.ExternalVarDecl f -> Some f | _ -> None)
        |> Seq.filter (fun (e: ExternalVarDecl) -> not (e.name.IsTopLevel || e.name.IsImported moduleName))

    /// C define macros for external constants / params
    /// e.g. #define blc_MyActivity_myConst &FOO(BAR)
    let externLocalConstMacros = 
        let renderExternConst (ec: ExternalVarDecl) = 
            let cexpr = 
                match ec.annotation.cvardecl with
                | Some (Attribute.CConst (binding = text))
                | Some (Attribute.CParam (binding = text)) ->
                    txt text |> parens
                | _ -> 
                    failwith "This should never happen"            
            
            let macro = 
                txt "#define" <+> (renderCName Current ctx.tcc ec.name) <+> cexpr
                |> groupWith (txt " \\")
            
            cpOptDocComments ec.annotation.doc
            |> dpOptLinePrefix <| macro

        externLocalVars
        |> Seq.filter (fun extVar -> match extVar.mutability with Mutability.CompileTimeConstant | Mutability.StaticParameter -> true | _ -> false)
        |> Seq.map renderExternConst
        |> dpBlock

    let userParams =
        let renderParam (v: VarDecl) =
            let {prereqStmts=prereqStmts; cExpr=cExpr} = cpExpr ctx.tcc v.initValue
            let vname = (cpName (Some Current) ctx.tcc v.name).Render
            assert (List.length prereqStmts = 0)
            let decl = txt "static" <+> cpArrayDeclDoc vname v.datatype <+> txt "=" <+> cExpr.Render <^> semi

            cpOptDocComments v.annotation.doc
            |> dpOptLinePrefix
            <| decl

        varDecls
        |> Seq.filter (fun vd -> vd.mutability.Equals Mutability.StaticParameter)
        |> Seq.map renderParam
        |> dpBlock

    // Translate function prototypes to direct C calls
    let functionPrototypes = mkFunctionPrototypes ctx.tcc
    
    let cCalls = mkCCalls functionPrototypes moduleName

    let cHeaders = 
        let cCalls = Seq.choose (fun (fp: ProcedurePrototype) -> fp.annotation.TryGetCHeader) cCalls
        let extConsts = Seq.choose (fun (vd: ExternalVarDecl) -> vd.annotation.TryGetCHeader) externLocalVars
        let cIncludes = List.choose (fun (mp: Attribute.MemberPragma) -> mp.TryGetCHeader) pragmas

        seq {extConsts; cCalls; cIncludes }
        |> Seq.concat 
        |> Seq.distinct
        |> Seq.map includeQuotedHfile
        |> dpBlock
    
    let importIncludes = generateSubmoduleIncludes importedModules

    // Translated subprograms
    let code = 
        compilations 
        |> List.map (fun c -> dpOptLinePrefix c.doc c.implementation) 
        |> dpToplevel

    // state printers
    let statePrinters = genStatePrinters ctx.tcc compilations entryPointOpt
  
    // only relevant for main (entry point) programs - not modules
    let globVars, mainCallback, mainInit, printState =
        match entryPointOpt with
        | None -> empty, empty, empty, empty
        | Some entryPoint ->
            let entryCompilation = compilations |> List.find (fun c -> c.name = entryPoint)
            (
                // global variables
                cpMainStateAsStatics entryCompilation,
                // tick function
                ProgramGenerator.mainCallback ctx.tcc ctx.cliContext.passPrimitiveByAddress 
                                              (AppName.tick moduleName) 
                                              entryCompilation.name 
                                              entryCompilation,
                // init function
                ProgramGenerator.mainInit ctx (AppName.init moduleName) entryCompilation,
                // state printer
                ProgramGenerator.printState ctx (AppName.printState moduleName) entryCompilation
            )
            // just an idea how to determine static memory usage
            //let printStatistics =
            //    """
            //    void blc_blech_ScatteredLocals_printStats() {
            //        printf("Context size: %d bytes\n", sizeof blc_blech_ctx);
            //    }
            //    """ |> txt
        
    
    // combine all into one Doc
    [ 
        Comment.generatedCode
      
        moduleDoc

        // Guideline #12 in http://umich.edu/~eecs381/handouts/CppHeaderFileGuidelines.pdf
        Comment.selfInclude
        selfHeader
      
        Comment.necessaryHeaders
        programHeader
        if ctx.cliContext.trace then 
            stdioHeader
      
        Comment.cHeaders
        cHeaders
      
        Comment.blechHeader
        blechHeader
      
        Comment.importHeaders 
        importIncludes 

        Comment.cConstants
        externLocalConstMacros
      
          ////Comment.cFunctions // already part of *.h
          //directCCalls
          //Comment.constants
          //userConst
          
        Comment.parameters
        userParams
      
        if entryPointOpt.IsSome then
            Comment.state
            globVars
      
        Comment.compilations
        code
      
        if ctx.cliContext.trace then
            Comment.statePrinters 
            statePrinters

        if entryPointOpt.IsSome then
            Comment.progam
            mainCallback
            mainInit
            if ctx.cliContext.trace then 
                Comment.traceFunction
                printState
    ]
    |> dpRemoveEmpty
    |> dpToplevel

// end of cpModuleCode

/// Emit C header for module as Doc
let private cpModuleHeader ctx (moduleName: TranslationUnitPath) 
                               (documentation: Attribute.OtherDecl option)
                               (pragmas: Attribute.MemberPragma list) 
                               importedModules 
                               (compilations: Compilation list) 
                               entryPointOpt =

    let includeGuardBegin, includeGuardEnd = generateIncludeGuards moduleName
    
    let moduleDoc = generateModuleDocumentation documentation

    // C header
    let importIncludes = generateSubmoduleIncludes importedModules

    // Translate function prototypes to extern functions and direct C calls
    let functionPrototypes = mkFunctionPrototypes ctx.tcc
    
    let cCalls = mkCCalls functionPrototypes moduleName
    
    let externConsts = 
        ctx.tcc.nameToDecl.Values
        |> Seq.choose (fun d -> match d with | Declarable.ExternalVarDecl f -> Some f | _ -> None) 
        |> Seq.filter (fun (ec: ExternalVarDecl) -> ec.name.IsTopLevel && (not <| ec.name.IsImported moduleName))

    let cHeaders = 
        let cCalls = Seq.choose (fun (fp: ProcedurePrototype) -> fp.annotation.TryGetCHeader) cCalls
        let extConsts = Seq.choose (fun (vd: ExternalVarDecl) -> vd.annotation.TryGetCHeader) externConsts
        let cIncludes = List.choose (fun (mp: Attribute.MemberPragma) -> mp.TryGetCHeader) pragmas

        seq {extConsts; cCalls; cIncludes }
        |> Seq.concat 
        |> Seq.distinct
        |> Seq.map includeQuotedHfile
        |> dpBlock

    //let cCallHeaders = 
    //    let hfiles =
    //        cCalls
    //        |> Seq.choose(fun fp -> fp.annotation.TryGetCHeader) 
    //        |> Seq.distinct
        
    //    let includeHfile hfile = 
    //        txt "#include" <+> (txt hfile |> dquotes)
        
    //    Seq.map includeHfile hfiles
    //    |> dpBlock

    // Type Declarations
    let userTypes = 
        ctx.tcc.userTypes
        |> Seq.choose (fun kvp -> if kvp.Key.moduleName = moduleName then Some kvp.Value else None) // make sure only this module's types are printed
        |> Seq.map (snd >> cpUserType)
        |> vsep // keep empty lines

    // Activity Contexts
    let activityContexts =
        compilations
        |> List.map (cpContextTypeDeclaration ctx.tcc) 
        |> dpBlock

    /// e.g. #define blc_MyActivity_myConst &FOO(BAR)
    let externConstMacros =     
        let renderExternConst (ec: ExternalVarDecl) = 
            let cexpr = 
                match ec.annotation.cvardecl with
                | Some (Attribute.CConst (binding = text))
                | Some (Attribute.CParam (binding = text)) ->
                    txt text |> parens
                | _ -> 
                    failwith "This should never happen"            
            
            let macro = 
                txt "#define" <+> (renderCName Current ctx.tcc ec.name) <+> cexpr
                |> groupWith (txt " \\")
            
            cpOptDocComments ec.annotation.doc
            |> dpOptLinePrefix <| macro
        // printfn "Extern Consts: %A" externConsts
        externConsts
        |> Seq.filter (fun extVar -> match extVar.mutability with Mutability.CompileTimeConstant | Mutability.StaticParameter -> true | _ -> false)
        |> Seq.map renderExternConst
        |> dpBlock


    let directCCalls =
        cCalls
        |> Seq.map (fun fp -> cpDirectCCall ctx.tcc fp)
        |> dpBlock


    // Generate function prototypes for implemented functions
    let localFunctions =
        compilations 
        |> List.map (fun c -> c.signature) 
        |> dpToplevel
    
    let programFunctionPrototypes, traceFunctionPrototype =
        match entryPointOpt with
        | None -> empty, empty // no entry point means we are compiling a module, nothing to do here
        | Some entryPoint ->   // we have a main program and thus need tick function etc...
            let entryCompilation = 
                compilations |> List.find (fun c -> c.name = entryPoint)
            let progFunProt =
                let emptyIface =
                    Compilation.mkNew entryPoint // the name is irrelevant here, the point is to make a compilation without inputs, outputs or retvalue
                let voidType = (ValueTypes ValueTypes.Void) // remember that return values are passed via extra parameter
                [ ProgramGenerator.programFunctionPrototype ctx.tcc ctx.cliContext.passPrimitiveByAddress (AppName.tick moduleName) entryCompilation voidType
                  ProgramGenerator.programFunctionPrototype ctx.tcc false (AppName.init moduleName) emptyIface voidType ]
                |> dpToplevel
            let traceFunProt =
                let voidType = (ValueTypes ValueTypes.Void)
                [ ProgramGenerator.programFunctionPrototype ctx.tcc false (AppName.printState moduleName) entryCompilation voidType ]
                |> dpToplevel
            progFunProt, traceFunProt

    // state printers
    let statePrinterPrototypes = 
        genStatePrinterPrototypes ctx.tcc compilations entryPointOpt
  
    // combine all into one Doc
    [ 
        includeGuardBegin
        Comment.generatedCode
          
        moduleDoc

        Comment.cHeaders
        cHeaders
      
        Comment.blechHeader
        blechHeader
      
        Comment.importHeaders
        importIncludes
      
        Comment.userTypes
        userTypes    // all user types are global
      
        Comment.activityContexts
        activityContexts
      
        Comment.cConstants
        externConstMacros
          
        Comment.cFunctions
        directCCalls
          
        Comment.exportedFunctions
        localFunctions // only exposed functions go there, currently all

        if ctx.cliContext.trace then
            Comment.statePrinters 
            statePrinterPrototypes

        // Program functions must not be created and exposed for blech modules
        if entryPointOpt.IsSome then
            Comment.cProgramFunctions
            programFunctionPrototypes

            if ctx.cliContext.trace then
                Comment.traceFunction
                traceFunctionPrototype
    
        includeGuardEnd 
    ]
    |> dpRemoveEmpty
    |> dpToplevel

// end of cpModuleHeader

/// Emit C code for main app as Doc
/// compilations is required to find the entry point name
let private cpApp ctx (moduleName: TranslationUnitPath) (compilations: Compilation list) entryPointName =
    let includeCProgramFile = generateCProgramHeader moduleName
        
    let entryCompilation = compilations |> List.find (fun c -> c.name = entryPointName)
    
    // inputs and outputs of the entry point (are not part of the internal Blech state)
    let staticVars = cpMainParametersAsStatics ctx.tcc entryCompilation

    let mainLoop = 
        ProgramGenerator.appMainLoop ctx (AppName.init moduleName)
                                      (AppName.tick moduleName) 
                                      (AppName.printState moduleName)
                                      entryCompilation

    // combine all into one Doc
    [ Comment.generatedCode

      Comment.necessaryHeaders
      (if ctx.cliContext.trace then stdioHeader else empty)
      
      Comment.blechHeader
      blechHeader
      
      Comment.blechCInclude
      includeCProgramFile
      
      Comment.externalState
      staticVars
      
      Comment.testFunction
      mainLoop ]
    |> dpRemoveEmpty
    |> dpToplevel
// end of cpApp


let private emitAnything (entryPointOpt: ProcedureImpl option) emitter =
    entryPointOpt
    |> Option.map (fun ep -> ep.Name)
    |> emitter 
    |> render (Some 80)

/// Generate contents of the *.c file
/// The choice whether to emit a module or a main program code is based on
/// the option modul.entryPoint
let public emitCode ctx (modul: BlechModule) importedModules compilations =
    emitAnything
        <| modul.entryPoint
        <| cpModuleCode ctx modul.name modul.documentation modul.memberPragmas importedModules compilations


/// Generate contents of the *.h file
/// The choice whether to emit a module or a main program header is based on
/// the option modul.entryPoint
let public emitHeader ctx (modul: BlechModule) importedModules compilations =
    emitAnything
        <| modul.entryPoint
        <| cpModuleHeader ctx modul.name modul.documentation modul.memberPragmas importedModules compilations


let public emitApp ctx (modul: BlechModule) compilations entryPointName =
    cpApp ctx modul.name compilations entryPointName
    |> render (Some 80)