﻿namespace Psns.Common.Analysis

module Lib =
    open System
    open System.Collections.Concurrent

    /// <summary>Composes a function whose result is stored so that it is only executed once.</summary>
    /// <param name="f">A function whose result will be stored after the first execution.</param>
    /// <remarks>Threadsafe</remarks>
    let memo (f: Func<'a, 'b>) =
        let store = new ConcurrentDictionary<_, _>()
        Func<'a, 'b>(fun x -> store.GetOrAdd(Some x, lazy (f.Invoke x)).Force())

    /// <summary>Composes a function whose result is stored so that it is only executed once
    /// or until <c>duration</c> has elapsed.</summary>
    /// <param name="f">A function whose result will be stored after the first execution.</param>
    /// <param name="duration">A <seealso cref="System.TimeSpan" /> that will determine how long
    /// the stored value of <c>f</c> will be cached until it is re-evaluated.</param>
    /// <remarks>Threadsafe</remarks>
    let memoWeak (f: Func<'a, 'b>) duration =
        let store = new ConcurrentDictionary<_, _>()
        let update x (current: Lazy<'b> * DateTime) =
            let now = DateTime.Now
            let diff = now.Subtract (snd current)
            match x with
            | Some x when diff > duration -> lazy (f.Invoke x), now
            | _ -> current
        Func<'a, 'b>(fun x -> (fst (store.AddOrUpdate(Some x, (lazy (f.Invoke x), DateTime.Now), update))).Force())

    /// <summary>Composes a function that is called with the result of the previous call.</summary>
    /// <param name="func"></param>
    /// <remarks>Threadsafe</remarks>
    let memoizePrev (func: Func<'a, 'a>) starting =
        let cached = ref starting
        let guard = new obj()
        Func<'a>(
            fun () ->
                lock(guard)(fun () ->
                    let r = func.Invoke !cached
                    cached := r
                    r
            )
        )

    /// <summary>Composes a function that is called with the result of the previous
    /// call and an additional parameter. The return value of <paramref name="func" /> 
    /// will be the previous result rather than the result of the current call.</summary>
    /// <param name="func"></param>
    /// <param name="starting">The first value to return</param>
    /// <remarks>Threadsafe</remarks>
    let memoizePrevPrev (func: Func<'a, 'b, 'a>) starting =
        let cached = ref starting
        let guard = new obj()
        Func<'b, 'a>(
            fun x ->
                lock(guard)(fun () ->
                    let next = func.Invoke (!cached, x)
                    let res = !cached
                    cached := next
                    res
                )
        )