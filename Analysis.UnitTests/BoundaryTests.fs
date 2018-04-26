module BoundaryTests

open NUnit.Framework
open FsUnit
open Psns.Common.Analysis
open System

let boundary = Boundary.ofValues (2, 4)

[<Test>]
let ``it should apply the mapping function.`` () =
    let mapping = Func<int, int, int> (fun min max -> min * max)

    Boundary.map mapping boundary |> should equal 8

[<Test>]
let ``it should apply the binding function.`` () =
    let binding = Func<int, int, int bound> (fun min max -> Boundary.ofPair(min + 2, max + 2))

    Boundary.bind binding boundary |> should equal (Boundary.ofPair (4, 6))

[<Test>]
let ``it should apply a value to both Mix and Max.`` () =
    let applier = Func<int, int> (fun v -> v * 2)

    Boundary.applyAll applier boundary |> should equal (Boundary.ofPair (4, 8))

[<Test>]
let ``it should apply separate values to Min and Max.`` () =
    let applier = Func<int, int, int * int> (fun min max -> min * 2, max * 4)

    Boundary.apply applier boundary |> should equal (Boundary.ofPair (4, 16))

[<Test>]
let ``it should unelevate to a tuple.`` () =
    Boundary.toPair (Boundary.ofPair(1, 1)) |> should equal (1, 1)

[<Test>]
let ``it could be equal to another.`` () =
    Boundary.ofPair (1, 2) = Boundary.ofPair (1, 2) |> should be True

[<Test>]
let ``it could not equal another.`` () =
    Boundary.ofPair (1, 1) = Boundary.ofPair (1, 2) |> should be False