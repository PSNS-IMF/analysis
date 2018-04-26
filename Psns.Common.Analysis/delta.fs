namespace Psns.Common.Analysis

    open System

    /// <summary>Represents a change in time.</summary>
    [<Struct>]
    type Delta =
        struct
            /// <summary>Time elapsed since last Delta</summary>
            val Elapsed: TimeSpan;
            /// <summary>Timestamp of the last Delta</summary>
            val Since: DateTime;

            private new(elapsed, since) = { Elapsed = elapsed; Since = since }

            static member op_Implicit(elapsed, since) = new Delta(elapsed, since)

            override this.ToString() =
                sprintf "{Elapsed: %s, Since: %s}" (this.Elapsed.ToString()) (this.Since.ToString())
        end

    module Delta =
        let ofValues since elapsed = Delta.op_Implicit(elapsed, since)