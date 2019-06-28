using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TaskAll.Tests
{
    public class IntegrationTest
    {
        private const int TimeSlotMs = 1000;
        private const int LagMs = 100;

        [Test]
        public async Task Test_OldWay()
        {
            var sw = Stopwatch.StartNew();

            Task<int> get1 = Get1();
            Task<int> get2 = Get2();
            Task<string> get3Str = Get3Str();
            Task<int> get4 = Get4();

            await Task.WhenAll(get1, get2, get3Str, get4);

            var result = get1.Result + get2.Result + int.Parse(get3Str.Result) + get4.Result;


            sw.Stop();

            Assert.AreEqual(9, result);
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, TimeSlotMs);
            Assert.Less(sw.ElapsedMilliseconds, TimeSlotMs + LagMs);
        }

        [Test]
        public async Task Test_ParallelOnly()
        {
            var sw = Stopwatch.StartNew();

            var result = await
                from val1  in Get1().AsParallel()
                from val2  in Get2().AsParallel()
                from val3S in Get3Str().AsParallel()
                from val4  in Get4().AsParallel()
                select val1 + val2 + int.Parse(val3S) + val4;


            sw.Stop();

            Assert.AreEqual(9, result);
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, TimeSlotMs);
            Assert.Less(sw.ElapsedMilliseconds, TimeSlotMs + LagMs);
        }

        [Test]
        public async Task Test_SequentialAndLet()
        {
            var sw = Stopwatch.StartNew();

            var result = await
                from one in Get1().AsParallel()

                let oneA = one

                from two in Get2().AsParallel()
                from freeStr in Get3Str().AsParallel()

                let free = int.Parse(freeStr)//Intermediate expr 

                from eight in  Add5(free).AsSequential()//Here all the previous results can be used

                from oneB in Get1().AsParallel()
                from twoA in Get2().AsParallel()
                
                from six in Add5(oneB).AsSequential()//Here all the previous results can be used 
                
                select one + oneA + two + int.Parse(freeStr) + free + eight + oneB + twoA + six;


            sw.Stop();

            Assert.AreEqual(27, result);
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, TimeSlotMs*3);
            Assert.Less(sw.ElapsedMilliseconds, TimeSlotMs*3 + LagMs);
        }


        [Test]
        public async Task Test_ParallelAndLet()
        {
            var sw = Stopwatch.StartNew();

            var result = await
                from one in Get1().AsParallel()

                let oneA = one

                from two in Get2().AsParallel()
                from freeStr in Get3Str().AsParallel()

                let free = int.Parse(freeStr)//Intermediate expr 

                from oneB in Get1().AsParallel()
                from twoA in Get2().AsParallel()
                
                select one + oneA + two + int.Parse(freeStr) + free + oneB + twoA;


            sw.Stop();

            Assert.AreEqual(13, result);
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, TimeSlotMs);
            Assert.Less(sw.ElapsedMilliseconds, TimeSlotMs + LagMs);
        }

        [Test]
        public async Task Test_Error()
        {
            var sw = Stopwatch.StartNew();

            var task =
                from val1 in Get1().AsParallel()
                from val2 in Get2().AsParallel()
                from err in Error(1).AsParallel()
                from val4 in Get4().AsParallel()
                from error3 in Error(3).AsSequential() 
                from err2 in Error(2).AsParallel()
                select val1 + val2  + val4;

            Exception exception = null;
            try
            {
                await task;
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.NotNull(exception);
            Assert.AreEqual("This is a test error #1", exception.Message);


            sw.Stop();

            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, TimeSlotMs);
            Assert.Less(sw.ElapsedMilliseconds, TimeSlotMs + LagMs);
        }

        [Test]
        public async Task Test_ErrorInSelect()
        {
            var sw = Stopwatch.StartNew();

            var task =
                from val1 in Get1().AsParallel()
                from val2 in Get2().AsParallel()
                from val4 in Get4().AsParallel()

                select val1 == 1 ? throw new Exception("This is a test error #2") : val1 + val2  + val4;

            Exception exception = null;
            try
            {
                await task;
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.NotNull(exception);
            Assert.AreEqual("This is a test error #2", exception.Message);


            sw.Stop();

            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, TimeSlotMs);
            Assert.Less(sw.ElapsedMilliseconds, TimeSlotMs + LagMs);
        }

        [Test]
        public async Task Test_NotResolvedParamError()
        {
            Exception exception = null;
            try
            {
                var task = 
                from val1 in Get1().AsParallel()
                from val4 in Get4().AsParallel()
                from val2 in Add5(val1).AsParallel()
                select val1 + val2 + val4;

                await task;
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.NotNull(exception);
            Assert.AreEqual("Object reference not set to an instance of an object. Check that not yet resolved parameter is not passed to a parallel task.", exception.Message);
        }

        public static async Task<int> Get1()
        {
            await Task.Delay(TimeSlotMs);
            return 1;
        }

        public static async Task<string> Get3Str()
        {
            await Task.Delay(TimeSlotMs);
            return 3.ToString();
        }

        public static async Task<int> Get4()
        {
            await Task.Delay(TimeSlotMs);
            return 3;
        }

        public static async Task<int> Get2()
        {
            await Task.Delay(TimeSlotMs);
            return 2;
        }

        public static async Task<int> Error(int id)
        {
            await Task.Yield();
            throw new Exception("This is a test error #" + id);
        }

        public static async Task<int> Add5(int value)
        {
            await Task.Delay(TimeSlotMs);
            return value + 5;
        }
    }
}