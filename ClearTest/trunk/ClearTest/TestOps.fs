﻿namespace Swensen.ClearTest
open System
open System.Reflection
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Linq.QuotationEvaluation
open Microsoft.FSharp.Metadata

[<AutoOpen>]
module TestOps =
    let binaryOps = [
        //boolean ops
        "op_Equality", "="
        "op_GreaterThan", ">"
        "op_LessThan", "<"
        "op_GreaterThanOrEqual", ">="
        "op_LessThanOrEqual", "<="
        "op_Inequality", "<>"
        //pipe ops
        "op_PipeRight", "|>"
        "op_PipeLeft", "<|"
        //numeric ops
        "op_Addition", "+"
        "op_Subtraction", "-"
        "op_Division", "/"
        "op_Multiply", "*"
        "op_Modulus", "%"
    ]

    //todo: expand to include +, -, *, etc.
    let (|BinaryInfixCall|_|) expr =
        match expr with
        | Call (_, mi, args) ->
            match binaryOps |> List.tryFind (fst>>((=) mi.Name)) with
            | Some(_,opStr) -> let lhs::rhs::_ = args in Some(opStr,lhs,rhs)
            | None -> None
        | _ -> None

    //todo:
    //  remaining binary ops
    //  unary ops
    //  add parens based on precedence <-- big one!
    //  mutable let bindings
    //  new object

    //funny case: <@ "asdf".[2] @> resolves as call to
    //Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicFunctions.GetString
    //same with IntrinsicFunctions.GetArray
    let rec sprintExpr expr =
        
        let sprintArgs delimiter exprs =
            exprs |> List.map sprintExpr |> String.concat delimiter

        let sprintTupledArgs = sprintArgs ", "
        let sprintCurriedArgs = sprintArgs " "

        ///is the top-level FSI module
        let isFsiModule (declaringType:Type) =
            declaringType.Name.StartsWith("FSI_")

        let sourceName (mi:MemberInfo) =
            mi.GetCustomAttributes(true)
            |> Array.tryPick (function 
                                | :? CompilationSourceNameAttribute as csna -> Some(csna)
                                | _ -> None)
            |> (function | Some(csna) -> csna.SourceName | None -> mi.Name)

        match expr with
        | Application (curry, last) -> //not actually sure what an application is
            sprintf "%s %s" (sprintExpr curry) (sprintExpr last)
        | Lambda (var, lambdaOrBody) ->
            let rec loop lambdaOrBody =
                match lambdaOrBody with
                | Lambda(var, lambdaOrBody) -> sprintf "%s %s" var.Name (loop lambdaOrBody)
                | body -> sprintf "-> %s" (sprintExpr body)
            sprintf "(fun %s %s)" (var.Name) (loop lambdaOrBody) //deal with parens latter
        | BinaryInfixCall(opStr, lhs, rhs) -> //must come before Call pattern
            //does it make any difference computing these upfront? or should i place them in recursive positions
            let lhsValue, rhsValue = sprintExpr lhs, sprintExpr rhs
            sprintf "%s %s %s" lhsValue opStr rhsValue
        | Call(calle, mi, args) ->
            match calle with
            | Some(instanceExpr) -> //instance call
                //just assume instance members always have tupled args
                sprintf "%s.%s(%s)" (sprintExpr instanceExpr) mi.Name (sprintTupledArgs args)
            | None -> //static call
                if FSharpType.IsModule mi.DeclaringType then
                    let methodName = sourceName mi
                    let sprintedArgs = sprintCurriedArgs args
                    if isFsiModule mi.DeclaringType then 
                        sprintf "%s %s" methodName sprintedArgs
                    else 
                        sprintf "%s.%s %s" (sourceName mi.DeclaringType) methodName sprintedArgs
                else //assume CompiledName same as SourceName for static members
                    sprintf "%s.%s(%s)" mi.DeclaringType.Name mi.Name (sprintTupledArgs args)
                    
        | PropertyGet(calle, pi, args) -> 
            match calle with
            | Some(instanceExpr) -> //instance call 
                match pi.Name, args with
                | _, [] -> sprintf "%s.%s" (sprintExpr instanceExpr) pi.Name
                | "Item", _ -> sprintf "%s.[%s]" (sprintExpr instanceExpr) (sprintTupledArgs args)
                | _, _ -> sprintf "%s.%s(%s)" (sprintExpr instanceExpr) pi.Name (sprintTupledArgs args)
            | None -> //static call (note: can't accept params
                if isFsiModule pi.DeclaringType then 
                    sprintf "%s" pi.Name
                else
                    sprintf "%s.%s" pi.DeclaringType.Name pi.Name 
        | Value(obj, typeObj) ->
            if typeObj = typeof<Unit> then "()"
            elif obj = null then "null"
            else sprintf "%A" obj
        | NewTuple (args) -> //tuples have ad least two elements
            args |> sprintTupledArgs |> sprintf "(%s)"
        | NewUnionCase(_,_) | NewArray(_,_)  ->
            expr.EvalUntyped() |> sprintf "%A"
        | Coerce(target, _) ->
            //don't even "mention" anything about the coersion
            sprintExpr target
        | Let(var, e1, e2) ->
            //todo: this needs to be handled better for curried functions
            sprintf "let %s = %s in %s" var.Name (e1 |> sprintExpr) (e2 |> sprintExpr)
        | _ -> 
            sprintf "%A" (expr)

    //this should return expr, with one one reduction applied
    let rec reduce (expr:Expr) = expr.EvalUntyped()
        
    let reduceSteps (expr:Expr<bool>) =
        let rec loop expr acc =
            //let next = expr |> reduce |> sprintExpr
            let next = expr |> sprintExpr
            match next with
            | "true" | "false" | _ when next = List.head acc -> acc
            | _ -> loop expr (next::acc)

        loop expr [expr |> sprintExpr] |> List.rev

    
    let fsiTestFailed (expr:Expr<bool>) =
        printfn "\nEXPRESSION FALSE:" 
        for str in reduceSteps expr do
            printfn "\t%s" str 
        printfn ""
        
    //making inline ensures stacktraces originate from method called from
    let inline test (expr:Expr<bool>) =
        match expr.Eval() with
        | false -> 
            #if INTERACTIVE
                fsiTestFailed expr
            #else
                //implement as call to testing framework assert
                failwith "non-interactive test runner not yet implemented"
            #endif
        | true -> ()

    let inline (=?) x y = test <@ x = y @>
    let inline (<?) x y = test <@ x < y @>
    let inline (>?) x y = test <@ x > y @>
    let inline (<=?) x y = test <@ x <= y @>
    let inline (>=?) x y = test <@ x >= y @>
    let inline (<>?) x y = test <@ x <> y @>