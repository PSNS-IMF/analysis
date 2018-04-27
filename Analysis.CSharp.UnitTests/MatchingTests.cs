using NUnit.Framework;
using Psns.Common.Analysis;
using Psns.Common.Functional;
using System;
using System.Linq;
using System.Threading;
using static NUnit.StaticExpect.Expectations;
using static Psns.Common.Analysis.Anomaly;
using static Psns.Common.Analysis.Anomaly.Classification;
using static Psns.Common.Functional.Prelude;

namespace Analysis.CSharp.UnitTests
{
    [TestFixture]
    public class MatchingTests
    {
        [Test]
        public void Should_Match_Bool() =>
            Expect(Boundary.applyAll(v => v * 2, Boundary.ofValues(1, 2)).ToString(), EqualTo("{Min: 2, Max: 4}"));

        [Test]
        public void Should_Cache_PreviousDelta()
        {
            var func = memoizedGetDelta(Maybe<DateTime>.None);

            var delta = func.Invoke();
            Thread.Sleep(500);
            var deltaNext = func.Invoke();

            Expect(delta.Elapsed.Milliseconds, GreaterThanOrEqualTo(0));
            Expect(deltaNext.Elapsed.Milliseconds, GreaterThanOrEqualTo(500));
        }

        [Test]
        public void Should_Calculate_CorrectDeltaRatePerSec()
        {
            var getDeltaRate = getDeltaWithRatePerSecond(Maybe<DateTime>.None);

            Thread.Sleep(500);

            var deltaRate = getDeltaRate();

            Expect(deltaRate.Item2, GreaterThanOrEqualTo(1.9));
        }

        [Test]
        public void Should_BeAbleToClassifyARate()
        {
            var round = fun((double d) => Math.Round(d, 4, MidpointRounding.ToEven));
            var min = fun((double d) => d * 0.001).Compose(round);
            var max = fun((double d) => d * 0.999).Compose(round);

            var getDeltaRate = getDeltaWithRatePerSecond(Maybe<DateTime>.None);

            var mapRate = Lib.memoizePrevPrev<Boundary<double>, Tuple<Delta, double>>(
                (a, b) => Boundary.ofValues(min(b.Item2), max(b.Item2)), 
                Boundary.ofValues(0d, 0d));

            var expected = Cons(100, 500, 2100, 200)
                .Select(delay =>
                {
                    Thread.Sleep(delay);

                    var deltaRate = getDeltaRate();
                    var rate = mapRate(deltaRate);
                    return classify(rate, deltaRate.Item2);
                });

            var actual = Cons(High, Norm, Norm, High);

            Expect(expected, EqualTo(actual));
        }
    }
}