using NUnit.Framework;
using Psns.Common.Functional;
using System;
using System.Linq;
using System.Threading;
using static NUnit.StaticExpect.Expectations;
using static Psns.Common.Analysis.Lib;
using static Psns.Common.Functional.Prelude;

namespace Analysis.CSharp.UnitTests.MemoUnitTests
{
    [TestFixture]
    public class MemoTests
    {
        [Test]
        public void MemoWeak_ShouldOnlyExecuteOnceUntilDurationExpires()
        {
            var exeCount = 0;
            var addOne = fun((int i) => { exeCount++ ; return i + 1; });
            var memoAddOne = memoWeak(addOne, TimeSpan.FromMilliseconds(100));

            memoAddOne(1);
            memoAddOne(1);
            memoAddOne(2);

            Thread.Sleep(150);

            memoAddOne(1);
            memoAddOne(1);

            Thread.Sleep(150);

            memoAddOne(1);

            Expect(exeCount, EqualTo(4));
        }

        [Test]
        public void MemoWeakKeyed_ShouldOnlyExecuteOnceUntilDurationExpires()
        {
            var exeCount = 0;
            var addOne = fun((int i) => { exeCount++; return i + 1; });
            var keyGen = fun((int key) => key.ToString());
            var memoAddOne = memoWeakKeyed(addOne, keyGen, TimeSpan.FromMilliseconds(100));

            var results = Empty<Tuple<int, int>>();
            var add = fun((int i) => results.Append(memoAddOne(i)));

            results = add(1);
            results = add(1);
            results = add(2);

            Thread.Sleep(150);

            results = add(1);
            results = add(1);

            Thread.Sleep(150);

            results = add(1);

            Expect(exeCount, EqualTo(4));

            var lookup = results.ToLookup(t => t.Item1, t => t.Item2);
            Expect(lookup[2].Sum(), EqualTo(7));
            Expect(lookup[3].Sum(), EqualTo(1));
        }
    }
}