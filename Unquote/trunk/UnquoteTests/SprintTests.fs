﻿[<AutoOpen>] //making auto open allows us not to have to fully qualify module properties
module Test.Swensen.Unquote.SprintTests
open Xunit
open Swensen.Unquote

//I would love to see using test to test itself, but for now, Eval() can't handle qouted qoutations.
//would love to create F# specific unit testing framework.

[<Fact>]
let ``literal int`` () =
    source <@ 1 @> =? "1"

[<Fact>]
let ``literal long`` () =
    source <@ 1L @> =? "1L"

[<Fact>]
let ``unit`` () =
    source <@ () @> =? "()"

[<Fact>]
let ``2-tuple`` () =
    source <@ (1,2) @> =? "(1, 2)"

[<Fact>]
let ``5-tuple`` () =
    source <@ (1,2,3,4,5) @> =? "(1, 2, 3, 4, 5)"

[<Fact>]
let ``tuple of tuples (i.e. tuple containing sub-expressions)`` () =
    source <@ ((1,2,3), (2,3)) @> =? "((1, 2, 3), (2, 3))"

[<Fact>]
let ``literal list`` () =
    source <@ [1;2;3;] @> =? "[1; 2; 3]"

[<Fact>]
let ``literal array`` () =
    source <@ [|1;2;3;|] @> =? "[|1; 2; 3|]"

[<Fact>]
let ``lambda expression with two args`` () =
    source <@ (fun i j -> i + j)@> =? "fun i j -> i + j"

[<Fact>]
let ``instance call on literal string value`` () =
    source <@ "hi".ToString() @> =? "\"hi\".ToString()"

[<Fact>]
let ``module and function call with CompiledNames differing from SourceNames`` () =
    source <@ List.mapi (fun i j -> i + j) [1;2;3] @> =? "List.mapi (fun i j -> i + j) [1; 2; 3]"

module NonSourceNameModule = let nonSourceNameFunc (x:int) = x

[<Fact>]
let ``module and function with non-source name`` () =
    source <@ NonSourceNameModule.nonSourceNameFunc 3  @> =? "NonSourceNameModule.nonSourceNameFunc 3"

[<Fact>]
let ``simple let binding`` () =
    source <@ let x = 3 in () @> =? "let x = 3 in ()"

[<Fact>]
let ``item getter with single arg`` () =
    let table = System.Collections.Generic.Dictionary<int,int>()
    source <@ table.[0] @> =? "seq [].[0]" //might want to fix up dict value sourceing later

[<Fact>]
let ``named getter with single arg`` () =
    source <@ "asdf".Chars(0) @> =? "\"asdf\".Chars(0)"

[<Fact>]
let ``auto open modules are not qualified`` () =
    source <@ snd (1, 2) @> =? "snd (1, 2)"

[<Fact>]
let ``coerce sources nothing`` () =
    source <@ Set.ofSeq [1;2;3;4] @> =? "Set.ofSeq [1; 2; 3; 4]"

[<Fact>]
let ``arithmetic precedence`` () =
     source <@ 2 + 3 - 7 @> =? "2 + 3 - 7"
     source <@ 2 + (3 - 7) @> =? "2 + (3 - 7)"
     source <@ 2 + (3 - 7) * 9 @> =? "2 + (3 - 7) * 9"
     source <@ (2 + (3 - 7)) * 9 @> =? "(2 + (3 - 7)) * 9"

[<Fact>]
let ``lambda precedence`` () =
    source <@ (fun i -> i + 1) 3  @> =? "(fun i -> i + 1) 3"

[<Fact>]
let ``lambda with application on lhs of + op call precedence`` () =
    source <@ (fun i j k -> i + j + k) (2 + 5) 3 (4 + 17) + 12 @> =? "(fun i j k -> i + j + k) (2 + 5) 3 (4 + 17) + 12"

let f i j k = i + j + k
[<Fact>]
let ``function with curried args on lhs of + op call precedence`` () =
    source <@ f (2 + 5) 3 (4 + 17) + 12 @> =? "f (2 + 5) 3 (4 + 17) + 12"

let a2d = array2D [[1;2];[2;3]]
[<Fact>]
let ``instrinsic calls`` () =
    source <@ "asdf".[1] @> =? "\"asdf\".[1]"
    source <@ [|1;2;3|].[1] @> =? "[|1; 2; 3|].[1]"
    source <@ a2d.[0, 1] @> =? "a2d.[0, 1]"

[<Fact>]
let ``new array with arg sub expressions`` () =
    source <@ [|1+1;2+(3-1);3|] @> =? "[|1 + 1; 2 + (3 - 1); 3|]"

[<Fact>]
let ``simple seq ranges`` () =
    source <@ {1..3} @> =? "{1..3}"
    source <@ {1..-3..-9} @> =? "{1..-3..-9}"

[<Fact>]
let ``precedence of range expression args`` () =
    source <@ {1+1..3-5+6} @> =? "{1 + 1..3 - 5 + 6}" //hmm, precedence isn't right...
    source <@ {1+4..-3+9..-9+1} @> =? "{1 + 4..-3 + 9..-9 + 1}"

module Test = let f (i:string) (j:string) = i + j;;
[<Fact>]
let ``call precedence within function application`` () =
    source <@ Test.f ("hello".Substring(0,2)) "world" @> =? "Test.f (\"hello\".Substring(0, 2)) \"world\""

let add x y = x + y
[<Fact>]
let ``call precedence nested function applications`` () =
    source <@ add (add 1 2) (add 3 4) @> =? "add (add 1 2) (add 3 4)"

let addToString a b = a.ToString() + b.ToString()
[<Fact>]
let ``precedence of intrinsic get within function application`` () =
    source <@ addToString "asdf".[1] "asdf".[2] @> =? "addToString \"asdf\".[1] \"asdf\".[2]"

[<Fact>]
let ``mutable let binding`` () =
    source <@ let mutable x = 3 in x + 2 @> =? "let mutable x = 3 in x + 2"

[<Fact>]
let ``if then else`` () =
    source <@ if true then false else true @> =? "if true then false else true"

[<Fact>]
let ``precedence: if then else in lambda body`` () =
    source <@ fun x -> if x then false else true @> =? "fun x -> if x then false else true"

[<Fact>]
let ``and also`` () =
    source <@ true && false @> =? "true && false"

[<Fact>]
let ``or else`` () =
    source <@ false || true @> =? "false || true"

let x = 4
[<Fact>]
let ``and also, or else precedence`` () =
    source <@ x = 4 || x = 3 && x >= 4 @> =? "x = 4 || x = 3 && x >= 4"
    source <@ (x = 4 || x = 3) && x >= 4 @> =? "(x = 4 || x = 3) && x >= 4"


//need to think up some multi-arg item and named getter scenarios

//Future features:

[<Fact(Skip="Future feature")>]
let ``partial application`` () =
    source <@  List.mapi (fun i j -> i + j) @> =? "List.mapi (fun i j -> i + j)"

[<Fact(Skip="Future feature")>]
let ``pattern match let binding`` () =
    source <@  let (x,y) = 2,3 in () @> =? "let (x, y) = (2, 3)"

