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

let getDelta = Anomaly.getDeltaWithRatePerSecond Maybe<DateTime>.None

let getDeltaDelayed (delay: int) =
    Thread.Sleep delay
    getDelta.Invoke

let mapDelta =
    Lib.memoizePrevPrev (Func<float bound, Delta * float, float bound>(fun _ b -> 
        Boundary.ofPair (round ((snd b) * 0.001), round ((snd b) * 0.999)))) (Boundary.ofPair (0.0, 0.0))

let classify mapDelta getDelta =
    let delta = getDelta ()
    delta
    |> mapDelta
    |> fun mapped -> 
        let cls = Anomaly.classify mapped (snd delta)
        (printfn "bounds: %s, rate: %s, classification: %s" (string mapped) (string (snd delta)) (string cls)) |> ignore; cls

let getAndClassifyDelta = getDeltaDelayed >> classify mapDelta.Invoke

List.map getAndClassifyDelta [
    100;    // High
    700;    // Norm
    2100;   // Norm
    200;    // High
    100;    // High
    5000;   // Norm
    5000;   // High
    4000;   // High
]