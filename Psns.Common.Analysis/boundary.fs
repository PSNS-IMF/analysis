namespace Psns.Common.Analysis

    /// <summary>A range between two extremes.</summary>
    type Boundary<'T> =
        struct
            val Min: 'T
            val Max: 'T

            private new(min, max) = { Min = min ; Max = max }

            static member private def(t: 'T option) = Option.defaultWith (fun () -> Unchecked.defaultof<'T>) t
            static member op_Implicit(minMax: 'T * 'T): 'T bound = new Boundary<'T>(fst minMax, snd minMax)
            static member op_Implicit(min: 'T option, max: 'T option): 'T bound = new Boundary<'T>(Boundary.def min, Boundary.def max)
            static member op_Explicit(boundary: 'T bound) = (boundary.Min, boundary.Max)
            static member op_Equality(a: 'a bound, b: 'a bound) = a.Min = b.Min && a.Max = b.Max

            /// <summary>Create with new <c>Min</c> while keeping same <c>Max</c>.</summary>
            /// <param name="min"></param>
            member this.WithMin(min) = new Boundary<'T>(min, this.Max)

            /// <summary>Create with new <c>Max</c> while keeping same <c>Min</c>.</summary>
            /// <param name="min"></param>
            member this.WithMax(max) = new Boundary<'T>(this.Min, max)

            override this.ToString() =
                sprintf "{Min: %s, Max: %s}" (this.Min.ToString()) (this.Max.ToString())
        end
    and 'T bound = Boundary<'T>

    module Boundary =
        open System

        let toPair boundary = Boundary.op_Explicit(boundary)
        let ofPair minMax = Boundary.op_Implicit(minMax)
        let ofValues (min, max) = Boundary.op_Implicit(Some min, Some max)
        let ofMin min = Boundary.op_Implicit(Some min, None)
        let ofMax max = Boundary.op_Implicit(None, Some max)
        let withMin min (boundary: 'a bound) = boundary.WithMin min
        let withMax max (boundary: 'a bound) = boundary.WithMax max

        let apply (func: Func<'T, 'T, 'R * 'R>) (boundary: 'T bound): 'R bound =
            func.Invoke (boundary.Min, boundary.Max) |> fun p -> Boundary.op_Implicit(p)
        let applyAll (func: Func<'T, 'R>) (boundary: 'T bound): 'R bound =
            (func.Invoke boundary.Min, func.Invoke boundary.Max) |> fun p -> Boundary.op_Implicit(p)

        let map (mapping: Func<'T, 'T, 'a>) (boundary: 'T bound) = mapping.Invoke (boundary.Min, boundary.Max)
        let bind (binding: Func<'T, 'T, 'R bound>) (boundary: 'T bound): 'R bound = binding.Invoke (boundary.Min, boundary.Max)