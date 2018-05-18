using NUnit.Framework;
using System;
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
    }
}