// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

#r "bin/Debug/Psns.Common.SystemExtensions.dll"

#load "lib.fs"
#load "delta.fs"
#load "boundary.fs"
#load "anomaly.fs"

open System
open System.Threading
open Psns.Common.Analysis
open Psns.Common.Functional

// Define your library scripting code here

let getDelta = Anomaly.memoizedGetDelta Maybe<DateTime>.None
let applyDelta = Anomaly.memoizedToBoundary Maybe<float>.None

let getDeltaDelayed (delay: int) =
    Thread.Sleep delay
    getDelta.Invoke

let mapDelta (delta: Delta) = delta.Elapsed.TotalSeconds

let classify mapDelta getDelta =
    let delta = mapDelta (getDelta ())
    delta
    |> fun d -> applyDelta.Invoke d, d
    |> fun boundDelta -> 
        let cls = Anomaly.classify (fst boundDelta) (snd boundDelta)
        printfn "bounds: %s, classification: %s" (string boundDelta) (string cls)
        cls

let getAndClassifyDelta = getDeltaDelayed >> classify mapDelta

List.map getAndClassifyDelta [
    0;      // High
    1000;   // High
    0;      // Low
    2000;   // High
    1000;   // Low
    500;    // Low
    1000;   // High
]