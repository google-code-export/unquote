﻿module Test.Swensen.Unquote.ReduceTests
open Xunit
open Swensen.Unquote

//we use this since expr don't support structural comparison
let sprintedReduceSteps expr =
    Reduce.reduceSteps expr |> List.map Sprint.sprint

[<Fact>]
let ``already reduced`` () =
    sprintedReduceSteps <@ -18 @> =? ["-18"]
    sprintedReduceSteps <@ (2, 3) @> =? ["(2, 3)"]
    sprintedReduceSteps <@ [1;2;3;4] @> =? ["[1; 2; 3; 4]"]
    sprintedReduceSteps <@ [|1;2;3;4|] @> =? ["[|1; 2; 3; 4|]"]

[<Fact>]
let ``coerce reduces right`` () =
    sprintedReduceSteps <@ Set.ofSeq [1;1;2;4] @> =? [
        "Set.ofSeq [1; 1; 2; 4]"
        "set [1; 2; 4]"
    ]

[<Fact>]
let ``arithmetic expressions`` () = 
    sprintedReduceSteps <@ (2 + (3 - 7)) * 9 @> =? [
        "(2 + (3 - 7)) * 9"
        "(2 + -4) * 9"
        "-2 * 9"
        "-18"
    ]

[<Fact>]
let ``simple lambda with application`` () =
    sprintedReduceSteps <@ (fun i j -> i + j) 1 2 @> =? [
        "(fun i j -> i + j) 1 2"
        "3"
    ]

[<Fact>]
let ``lambda with non-reduced applications`` () =
    sprintedReduceSteps <@ (fun i j -> i + j) (1+2) 2 @> =? [
        "(fun i j -> i + j) (1 + 2) 2"
        "(fun i j -> i + j) 3 2"
        "5"
    ]

[<Fact>]
let ``lambda with application on lhs of + op call`` () =
    sprintedReduceSteps <@ (fun i j k -> i + j + k) (2 + 5) 3 (4 + 17) + 12 @> =? [
        "(fun i j k -> i + j + k) (2 + 5) 3 (4 + 17) + 12"
        "(fun i j k -> i + j + k) 7 3 21 + 12"
        "31 + 12" //failing to evaluate this
        "43"
    ]

let f i j k = i + j + k
[<Fact>]
let ``function with application on lhs of + op call`` () =
    sprintedReduceSteps <@ f (2 + 5) 3 (4 + 17) + 12 @> =? [
        "ReduceTests.f (2 + 5) 3 (4 + 17) + 12"
        "ReduceTests.f 7 3 21 + 12"
        "31 + 12"
        "43"
    ]

let ftuple i j = (i,j)
[<Fact>]
let ``function with application returns tuple`` () =
    sprintedReduceSteps <@ ftuple 1 2 @> =? [
        "ReduceTests.ftuple 1 2"
        "(1, 2)"
    ]

[<Fact>]
let ``function with application compared to tuple`` () =
    sprintedReduceSteps <@ ftuple 1 2 = (1,2) @> =? [
        "ReduceTests.ftuple 1 2 = (1, 2)"
        "(1, 2) = (1, 2)"
        "true"
    ]

[<Fact>]
let ``lambdas are reduced`` () =
    sprintedReduceSteps <@ List.map (fun i -> i + 1) [1;2;3;4] = [2;3;4;5] @> =? [
        "List.map (fun i -> i + 1) [1; 2; 3; 4] = [2; 3; 4; 5]"
        "[2; 3; 4; 5] = [2; 3; 4; 5]"
        "true"
    ]