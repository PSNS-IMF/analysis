module LibTests

    open System
    open System.Threading
    open FsUnit
    open NUnit.Framework

    open Psns.Common.Analysis
    open Lib

    module MemoizePrevTests =
        let startingValue = 0

        let getAdder() = memoizePrev (Func<int, int>(fun prev -> prev + 1)) startingValue
        let asAsync (f: Func<'a, 'b>) = async { return f.Invoke }
        let asyncController fAsync =
            fAsync
            |> List.replicate 20
            |> Async.Parallel
        let runAndReduce controller =
            controller
            |> Async.RunSynchronously
            |> Seq.reduce (fun i0 i1 -> i0 + i1)
        let run = asyncController >> runAndReduce

        [<Test>]
        let ``it should call a function with the result of the previous function.`` () =
            let adder = getAdder()

            let one = adder.Invoke()
            let two = adder.Invoke()

            [one; two] |> should equal [1; 2]

        [<Test>]
        let ``it should call a function with the result of the previous function when multithreaded.`` () =
            let adder = getAdder()
            let asyncAdder = async { return adder.Invoke() }

            run asyncAdder |> should equal 210

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

            run asyncAdder |> should equal 190

    module MemoTests =
        
        let adderFactory calls = memo (Func<int, int>(fun i ->
            incr calls
            i + 1))

        let asyncAdder (f: Func<'a, 'b>) arg = async { return f.Invoke arg }
        let asyncController adder mapper =
            (fun i -> asyncAdder adder i)
            |> List.replicate 20
            |> List.mapi mapper
            |> Async.Parallel
        let runAndReduce controller =
            controller
            |> Async.RunSynchronously
            |> Seq.reduce (fun i0 i1 -> i0 + i1)
        let run adder = asyncController adder >> runAndReduce

        [<Test>]
        let ``it should only call a cached function once.`` () =
            let calls = ref 0
            let adder = adderFactory calls

            adder.Invoke 1 |> ignore
            adder.Invoke 1 |> ignore

            !calls |> should equal 1

        [<Test>]
        let ``it should only call a cached function once when multithreaded.`` () =
            let calls = ref 0
            let adder = adderFactory calls
            let result = run adder (fun index toAsync -> toAsync ((index % 2 = 0) |> function | true -> index | _ -> 0))

            !calls |> should equal 10
            result |> should equal 110

        [<Test>]
        let ``it should only call a cached function once or until duration expires when multithreaded.`` () =
            let calls = ref 0
            let add1 = Func<int, int>(fun index ->
                Interlocked.Increment(calls) |> ignore
                index + 1)
            let adder = memoWeak add1 (TimeSpan.FromMilliseconds(5.0))

            let asyncController =
                [1..20]
                |> Seq.map (fun index ->
                    async {
                        let delay = (index % 2 = 0) |> function | true -> 0 | _ -> 20
                        do! Async.Sleep delay
                        return adder.Invoke 0
                    })
                |> Async.Parallel

            let result = runAndReduce asyncController

            !calls |> should be (greaterThan 1)
            result |> should equal 20