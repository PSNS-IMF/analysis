module LibTests

open NUnit.Framework
open FsUnit

open Psns.Common.Analysis
open Lib
open System

let startingValue = 0

[<Test>]
let ``it should call a function with the result of the previous function.`` () =
    let adder = memoizePrev (Func<int, int>(fun prev -> prev + 1)) startingValue

    let one = adder.Invoke()
    let two = adder.Invoke()

    [one; two] |> should equal [1; 2]

[<Test>]
let ``it should return the result of the previous function call.`` () =
    let adder = memoizePrevPrev (Func<int, int, int>(fun prev _ -> prev + 1)) startingValue

    let one = adder.Invoke 0
    let two = adder.Invoke 0

    [one; two] |> should equal [0; 1]