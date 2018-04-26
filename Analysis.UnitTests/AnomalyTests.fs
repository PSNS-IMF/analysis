module AnomalyTests

open NUnit.Framework
open FsUnit
open System

open Psns.Common.Analysis
open Anomaly
open System.Threading
open Psns.Common.Functional

type ex = Psns.Common.Functional.Prelude

let None = Psns.Common.Functional.Maybe<System.DateTime>.None
let getDeltaFactory start = fun () -> getDelta start
let getDeltaMemoFactory start = fun () -> memoizedGetDelta start
let getBoundaryMemoFactory start = fun () -> memoizedToBoundary start
let getDeltaWithRateFactory start = fun () -> getDeltaWithRatePerSecond start

[<Test>]
let ``it should return zero when getting a delta with no previous time.`` () =
    getDeltaFactory None ()
    |> fun delta -> delta.Elapsed.Ticks
    |> should equal 0

[<Test>]
let ``it should get the correct delta from a previous time.`` () =
    15
    |> float
    |> TimeSpan.FromMinutes
    |> DateTime.Now.Subtract
    |> ex.Some
    |> fun s -> getDeltaFactory s ()
    |> fun delta -> delta.Elapsed.TotalMinutes
    |> should equal 15

[<Test>]
let ``it should get the correct delta from a previous delta time.`` () =
    15
    |> float
    |> TimeSpan.FromMinutes
    |> DateTime.Now.Subtract
    |> ex.Some
    |> fun start -> getDeltaMemoFactory start ()
    |> fun getDelta -> getDelta.Invoke() |> ignore |> fun () -> getDelta
    |> fun getDelta ->
        Thread.Sleep 500
        getDelta.Invoke().Elapsed.TotalMilliseconds
    |> should be (equalWithin 500.0 600.0)

[<Test>]
let ``it should get the correct event rate from a previous delta time with no start time.`` () =
    getDeltaWithRateFactory None ()
    |> fun getDelta ->
        Thread.Sleep 500
        let res = getDelta.Invoke()
        res |> snd |> should greaterThanOrEqualTo 1.9
        getDelta
    |> fun getDelta ->
        Thread.Sleep 100
        getDelta.Invoke() |> snd
    |> should be (equalWithin 8.0 9.9)

[<Test>]
let ``it should return the previously stored boundary.`` () =
    let composed = getBoundaryMemoFactory Maybe<int>.None ()

    composed.Invoke 1 |> should equal (Boundary.ofPair (0, 0))
    composed.Invoke 3 |> should equal (Boundary.ofPair (1, 1))

[<Test>]
let ``it should classify a delta below the min boundary as a Low Anomaly.`` () =
    10 |> classify (Boundary.ofMin 11) |> should equal Low

[<Test>]
let ``it should classify a delta between Min and Max to be Normal.`` () =
    12 |> classify (Boundary.ofValues (10, 13)) |> should equal Norm

[<Test>]
let ``it should classify a delta above Max as a High Anomaly.`` () =
    14 |> classify (Boundary.ofValues (10, 13)) |> should equal High