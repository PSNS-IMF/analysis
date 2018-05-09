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
let ``it should call a function with the result of the previous function when multithreaded.`` () =
    let adder = memoizePrev (Func<int, int>(fun prev -> prev + 1)) startingValue
    let asyncAdder = async { return adder.Invoke() }

    let asyncController =
        asyncAdder
        |> List.replicate 20
        |> Async.Parallel

    let result =
        asyncController
        |> Async.RunSynchronously
        |> Seq.reduce (fun i0 i1 -> i0 + i1)

    result |> should equal 210

[<Test>]
let ``it should return the result of the previous function call.`` () =
    let adder = memoizePrevPrev (Func<int, int, int>(fun prev _ -> prev + 1)) startingValue

    let one = adder.Invoke 0
    let two = adder.Invoke 0

    [one; two] |> should equal [0; 1]

[<Test>]
let ``it should return the result of the previous function call when multithreaded.`` () =
    let adder = memoizePrevPrev (Func<int, int, int>(fun prev _ -> prev + 1)) startingValue
    let asyncAdder = async { return adder.Invoke 0 }

    let asyncController =
        asyncAdder
        |> List.replicate 20
        |> Async.Parallel

    let result =
        asyncController
        |> Async.RunSynchronously
        |> Seq.reduce (fun i0 i1 -> i0 + i1)

    result |> should equal 190

[<Test>]
let ``it should only call a cached function once.`` () =
    let calls = ref 0
    let adder = memo (Func<int, int>(fun i ->
        incr calls
        i + 1))

    adder.Invoke 1 |> ignore
    adder.Invoke 1 |> ignore

    !calls |> should equal 1

[<Test>]
let ``it should only call a cached function once when multithreaded.`` () =
    let calls = ref 0
    let adder = memo (Func<int, int>(fun i ->
        incr calls
        i + 1))

    let asyncAdder arg = async { return adder.Invoke arg }
    let asyncController =
        asyncAdder
        |> List.replicate 20
        |> List.mapi (fun index f -> f (match index % 2 with | 0 -> index | _ -> 0))
        |> Async.Parallel

    let result =
        asyncController
        |> Async.RunSynchronously
        |> Seq.reduce (fun i0 i1 -> i0 + i1)

    !calls |> should equal 10
    result |> should equal 110