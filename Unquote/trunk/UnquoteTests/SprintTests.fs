﻿module Test.Swensen.Unquote.SprintTests
open Xunit
open Swensen.Unquote

//I would love to see using test to test itself, but for now, Eval() can't handle qouted qoutations.
//would love to create F# specific unit testing framework.

let sprint = Sprint.sprint

[<Fact>]
let ``literal int`` () =
    sprint <@ 1 @> =? "1"

[<Fact>]
let ``literal long`` () =
    sprint <@ 1L @> =? "1L"

[<Fact>]
let ``unit`` () =
    sprint <@ () @> =? "()"

[<Fact>]
let ``2-tuple`` () =
    sprint <@ (1,2) @> =? "(1, 2)"

[<Fact>]
let ``5-tuple`` () =
    sprint <@ (1,2,3,4,5) @> =? "(1, 2, 3, 4, 5)"

[<Fact>]
let ``tuple of tuples (i.e. tuple containing sub-expressions)`` () =
    sprint <@ ((1,2,3), (2,3)) @> =? "((1, 2, 3), (2, 3))"

[<Fact>]
let ``literal list`` () =
    sprint <@ [1;2;3;] @> =? "[1; 2; 3]"

[<Fact>]
let ``literal array`` () =
    sprint <@ [|1;2;3;|] @> =? "[|1; 2; 3|]"

[<Fact>]
let ``lambda expression with two args`` () =
    sprint <@ (fun i j -> i + j)@> =? "fun i j -> i + j"

[<Fact>]
let ``instance call on literal string value`` () =
    sprint <@ "hi".ToString() @> =? "\"hi\".ToString()"

[<Fact>]
let ``module and function call with CompiledNames differing from SourceNames`` () =
    sprint <@ List.mapi (fun i j -> i + j) [1;2;3] @> =? "List.mapi (fun i j -> i + j) [1; 2; 3]"

module NonSourceNameModule = let nonSourceNameFunc (x:int) = x

[<Fact>]
let ``module and function with non-source name`` () =
    sprint <@ NonSourceNameModule.nonSourceNameFunc 3  @> =? "NonSourceNameModule.nonSourceNameFunc 3"

[<Fact>]
let ``simple let binding`` () =
    sprint <@ let x = 3 in () @> =? "let x = 3 in ()"

[<Fact>]
let ``item getter with single arg`` () =
    let table = System.Collections.Generic.Dictionary<int,int>()
    sprint <@ table.[0] @> =? "seq [].[0]" //might want to fix up dict value sprinting later

[<Fact>]
let ``named getter with single arg`` () =
    sprint <@ "asdf".Chars(0) @> =? "\"asdf\".Chars(0)"

[<Fact>]
let ``auto open modules are not qualified`` () =
    sprint <@ snd (1, 2) @> =? "snd (1, 2)"

[<Fact>]
let ``coerce sprints nothing`` () =
    sprint <@ Set.ofSeq [1;2;3;4] @> =? "Set.ofSeq [1; 2; 3; 4]"

[<Fact>]
let ``arithmetic precedence`` () =
     sprint <@ 2 + 3 - 7 @> =? "2 + 3 - 7"
     sprint <@ 2 + (3 - 7) @> =? "2 + (3 - 7)"
     sprint <@ 2 + (3 - 7) * 9 @> =? "2 + (3 - 7) * 9"
     sprint <@ (2 + (3 - 7)) * 9 @> =? "(2 + (3 - 7)) * 9"

[<Fact>]
let ``lambda precedence`` () =
    sprint <@ (fun i -> i + 1) 3  @> =? "(fun i -> i + 1) 3"

[<Fact>]
let ``lambda with application on lhs of + op call precedence`` () =
    sprint <@ (fun i j k -> i + j + k) (2 + 5) 3 (4 + 17) + 12 @> =? "(fun i j k -> i + j + k) (2 + 5) 3 (4 + 17) + 12"

let f i j k = i + j + k
[<Fact>]
let ``function with curried args on lhs of + op call precedence`` () =
    sprint <@ f (2 + 5) 3 (4 + 17) + 12 @> =? "SprintTests.f (2 + 5) 3 (4 + 17) + 12"


//need to think up some multi-arg item and named getter scenarios

//Future features:

[<Fact(Skip="Future feature")>]
let ``partial application`` () =
    sprint <@  List.mapi (fun i j -> i + j) @> =? "List.mapi (fun i j -> i + j)"

[<Fact(Skip="Future feature")>]
let ``pattern match let binding`` () =
    sprint <@  let (x,y) = 2,3 in () @> =? "let (x, y) = (2, 3)"