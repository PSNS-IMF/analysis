namespace Psns.Common.Analysis

    module Anomaly =

        open System
        open Lib
        open Delta
        open Psns.Common.Functional

        type ex = Psns.Common.Functional.Prelude

        [<Struct>]
        type Classification =
            | Low
            | High
            | Norm

        /// <summary>Calculates the difference in time between
        /// <paramref name="starting" /> and <c>Now</c>.</summary>
        /// <param name="starting">The optional initial <see cref="Option&lt;System.DateTime&gt;" />
        /// to be subtracted from <c>Now</c></param>
        /// <returns><c>Delta</c></returns>
        let getDelta (starting: Maybe<DateTime>) =
            let now = DateTime.Now
            starting.Match ((fun prev -> now.Subtract prev), (fun () -> TimeSpan.Zero))
            |> ofValues now

        /// <summary>Composes a function of <c>getDelta</c> and <c>Lib.memoizePrev</c>.</summary>
        /// <param name="start"></param>
        /// <returns>The <c>Delta</c> of the previous event</returns>
        let memoizedGetDelta start =
            let first = getDelta start
            first
            |> memoizePrev (Func<Delta, Delta>(fun prev -> getDelta (ex.Some prev.Since)))

        let internal round (num: float) = Math.Round(num, 4, MidpointRounding.ToEven)

        let private ratePerSecond (delta: Delta) =
            (delta, round (1000.0 / (delta.Elapsed.TotalMilliseconds |> float)))

        /// <summary>Composes a function of <c>getDelta</c> and <c>Lib.memoizePrev</c> that 
        /// calculates the current <c>Delta</c> rate in seconds.</summary>
        /// <param name="start"></param>
        /// <returns>A function that returns a <c>Delta * float</c> of the previous event</returns>
        let getDeltaWithRatePerSecond start =
            let first = getDelta start, 0.0
            first
            |> memoizePrev (Func<Delta * float, Delta * float>((fun prev -> 
                ex.Some (fst prev).Since
                |> getDelta
                |> ratePerSecond)))
        
        /// <summary>Composes a function that creates and remembers
        /// a previously created <c>Boundary</c>.</summary>
        /// <param name="initial">Optional value to use as the first <c>Boundary</c></param>
        /// <returns>A function that accepts a value to be made into a <c>Boundary</c> and returns
        /// the previously created <c>Boundary</c>.</returns>
        /// <remarks>If <paramref name="initial" /> is <c>None</c>,
        /// then the default value of 'a is used.</remarks>
        let memoizedToBoundary (initial: Maybe<'a>) =
            let start =
                initial.Match ((fun s -> s), (fun () -> Unchecked.defaultof<'a>))
            memoizePrevPrev (Func<'a bound, _, 'a bound>(fun _ v -> Boundary.ofPair (v, v))) (Boundary.ofPair (start, start))
            
        /// <summary>Determines if the <paramref name="delta" /> is outside of
        /// or within the <paramref name="boundary" />.</summary>
        /// <param name="boundary"></param>
        /// <param name="delta"></param>
        /// <returns><see cref="Anomaly.Classification" /></returns>
        let classify (boundary: 'T bound) delta =
            match delta < boundary.Min with
            | true -> Low
            | false ->
                match delta > boundary.Max with
                | true -> High
                | false -> Norm