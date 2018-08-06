namespace Psns.Common.Analysis

module Lib =
    open System
    open System.Collections.Concurrent

    /// <summary>Composes a function whose result is stored so that it is only executed once.</summary>
    /// <param name="f">A function whose result will be stored after the first execution.</param>
    /// <remarks>Threadsafe</remarks>
    let memo (f: Func<'a, 'b>) =
        let store = new ConcurrentDictionary<_, _>()
        Func<'a, 'b>(fun x -> store.GetOrAdd(Some x, lazy (f.Invoke x)).Force())

    let private fst (a, _, _) = a
    let private snd (_, b, _) = b
    let private thd (_, _, c) = c
    let private map (a, b, c) f = a, b, f c 

    /// <summary>Composes a function whose result is stored so that it is only executed once
    /// or until <c>duration</c> has elapsed.</summary>
    /// <param name="f">A function whose result will be stored after the first execution.</param>
    /// <param name="keyGen">A function to be called to create the key that identifies 
    /// the stored result for <c>f</c>.</param>
    /// <param name="duration">A <seealso cref="System.TimeSpan" /> that will determine how long
    /// the stored value of <c>f</c> will be cached until it is re-evaluated.</param>
    /// <returns>A <c>tuple</c> of the result of <c>f</c> and an <see cref="int" /> 
    /// indicating how many times <c>f</c> has been called.</returns>
    /// <remarks>Threadsafe</remarks>
    let memoWeakKeyed (f: Func<'a, 'b>) (keyGen: Func<'a, 'c>) duration =
        let store = new ConcurrentDictionary<_, _>()
        let update x (current: Lazy<'b> * DateTime * int) =
            let now = DateTime.Now
            let diff = now.Subtract (snd current)
            (diff > duration) |> function | true -> lazy (f.Invoke x), now, 1 | _ -> map current (fun i -> i + 1)
        Func<'a, 'b * int>(fun x ->
            let key = Some (keyGen.Invoke x)
            let newVal = (lazy (f.Invoke x), DateTime.Now, 1)
            let res = store.AddOrUpdate(key, newVal, (fun _ v -> update x v))
            ((fst res).Force(), thd res))

    /// <summary>Composes a function whose result is stored so that it is only executed once
    /// or until <c>duration</c> has elapsed.</summary>
    /// <param name="f">A function whose result will be stored after the first execution.</param>
    /// <param name="duration">A <seealso cref="System.TimeSpan" /> that will determine how long
    /// the stored value of <c>f</c> will be cached until it is re-evaluated.</param>
    /// <returns>A <c>tuple</c> of the result of <c>f</c> and an <see cref="int" /> 
    /// indicating how many times <c>f</c> has been called.</returns>
    /// <remarks>Threadsafe</remarks>
    let memoWeak (f: Func<'a, 'b>) duration =
        memoWeakKeyed f (Func<'a, _>(fun a -> a)) duration

    /// <summary>Composes a function that is called with the result of the previous call.</summary>
    /// <param name="f"></param>
    /// <param name="starting"></param>
    /// <returns>Item one of the returned tuple calls <paramref name="f" />
    /// and stores its result for the next call. Item two is just a reader function that returns 
    /// the previous result without calling <paramref name="f" />.</returns>
    let memoPrevWithReader (f: Func<'a, 'a>) starting =
        let store = MailboxProcessor.Start(fun inbox ->
            let rec messageLoop oldState = async {
                let! (msg, channel: AsyncReplyChannel<'a>) = inbox.Receive()
                let state = msg(oldState)

                channel.Reply(state)

                return! messageLoop state
                }
            messageLoop(starting))
        
        let composeGetNext getF =
            Func<_>(fun () -> store.PostAndReply(fun channel -> getF, channel))

        Func<_>(
            fun () -> composeGetNext f.Invoke, composeGetNext (fun a -> a))

    /// <summary>Composes a function that is called with the result of the previous call.</summary>
    /// <param name="func"></param>
    /// <remarks>Threadsafe</remarks>
    let memoizePrev (func: Func<'a, 'a>) starting =
        memoPrevWithReader func starting
        |> fun func -> func.Invoke()
        |> Operators.fst

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