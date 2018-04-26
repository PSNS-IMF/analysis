namespace Psns.Common.Analysis

module Lib =
    open System

    /// <summary>Composes a function that is called with the result of the previous call.</summary>
    /// <param name="func"></param>
    /// <param name="starting">The first value to return</param>
    let memoizePrev (func: Func<'a, 'a>) starting =
        let cached = ref starting
        Func<'a>(
            fun () ->
                let r = func.Invoke !cached
                cached := r
                r
        )

    /// <summary>Composes a function that is called with the result of the previous
    /// call and an additional parameter. The return value of <paramref name="func" /> 
    /// will be the previous result rather than the result of the current call.</summary>
    /// <param name="func"></param>
    /// <param name="starting">The first value to return</param>
    let memoizePrevPrev (func: Func<'a, 'b, 'a>) starting =
        let cached = ref starting
        Func<'b, 'a>(
            fun x ->
                let next = func.Invoke (!cached, x)
                let res = !cached
                cached := next
                res
        )