using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalBatchV2
{
    class Program
    {
        static void Main()
        {
            TestMustFrequency();
        }
        static void Test(CalcParams calcParams, Requirement[] reqs)
        {
            const int Days = 30;
            Console.WriteLine("reqs.Sum(x => x.quantity)={0}", reqs.Sum(x => x.quantity));
            int OptimalQuantity = calcParams.OptimalQuantity;
            int MaxQuantity = calcParams.MaxQuantity;
            int avgQnt = calcParams.avgQnt;
            int MustFrequency = calcParams.MustFrequency;
            int RecomendedFrequency = calcParams.RecomendedFrequency;
            Console.WriteLine("reqs.Sum={0} OptimalQuantity={1} MaxQuantity={2}", reqs.Sum(r => r.quantity), OptimalQuantity, MaxQuantity);
            var optimalBatches = OptimalBatchStatic.GetOptimalBatches(reqs, OptimalQuantity, MaxQuantity, Days, MustFrequency, RecomendedFrequency, avgQnt);
            foreach (var req in reqs.OrderBy(r => r.deadline))
                Console.WriteLine(req);
            foreach (var batch in optimalBatches)
            {
                Console.WriteLine(batch);
                foreach (var kv in batch.reqs)
                    Console.WriteLine("\t{0}\t{1:d}", kv.Item2, kv.Item1.deadline);
            }
            Console.WriteLine("optimalBatches.Sum(b => b.Quantity)={0}", optimalBatches.Sum(b => b.Quantity));
            Console.WriteLine("optimalBatches.Sum(b => b.reserved)={0}", optimalBatches.Sum(b => b.reserved));
            Console.WriteLine("optimalBatches.Sum(b => b.FreeQuantity)={0}", optimalBatches.Sum(b => b.FreeQuantity));
            Console.Read();
        }

        private static void TestOneReq()
        {
            var calcParams = new CalcParams()
            {
                OptimalQuantity = 11,
                MaxQuantity = 120,
                avgQnt = 130,
                MustFrequency = 4,
                RecomendedFrequency = 20
            };
            Requirement[] reqs = new Requirement[] {
                new Requirement(10, new DateTime(2021, 10, 6) ),
            };
            Test(calcParams, reqs);
        }
        private static void TestSmallLastBatch()
        {
            Requirement[] reqs = new Requirement[] {
                new Requirement(22, new DateTime(2021, 11, 14) ),
                new Requirement(200, new DateTime(2022, 1, 15) ),
            };
            var calcParams = new CalcParams()
            {
                OptimalQuantity = 50,
                MaxQuantity = 999,
                avgQnt = 0,
                MustFrequency = 1,
                RecomendedFrequency = 1
            };
            Test(calcParams, reqs);
        }
        private static void TestRecomendedFrequency()
        {
            Requirement[] reqs = new Requirement[] {
                new Requirement(10, new DateTime(2021, 1, 1) ),
                new Requirement(10, new DateTime(2021, 2, 1) ),
                new Requirement(10, new DateTime(2021, 3, 1) ),
                new Requirement(10, new DateTime(2021, 5, 1) ),
                new Requirement(11, new DateTime(2021, 12, 1) ),
            };
            var calcParams = new CalcParams()
            {
                OptimalQuantity = 5,
                MaxQuantity = 999,
                avgQnt = 0,
                MustFrequency = 2,
                RecomendedFrequency = 3
            };
            Test(calcParams, reqs);
        }
        private static void TestMustFrequency()
        {
            Requirement[] reqs = new Requirement[] {
                new Requirement(100, new DateTime(2021, 1, 1) ),
                new Requirement(100, new DateTime(2021, 1, 2) ),
                new Requirement(100, new DateTime(2021, 2, 1) ),
                new Requirement(100, new DateTime(2021, 3, 1) ),
                new Requirement(10, new DateTime(2021, 5, 1) ),
                new Requirement(11, new DateTime(2021, 12, 1) ),
            };
            var calcParams = new CalcParams()
            {
                OptimalQuantity = 69,
                MaxQuantity = 160,
                avgQnt = 0,
                MustFrequency = 5,
                RecomendedFrequency = 1
            };
            Test(calcParams, reqs);
        }
        private static Requirement[] SeedPerevodnikReqs()
        {
            Requirement[] reqs = new Requirement[] {
            new Requirement(6, new DateTime(2021, 10, 20) ),
            new Requirement(1, new DateTime(2021, 10, 21) ),
            };
            return reqs;
        }
        private static Requirement[] SeedPlashkiReqs()
        {
            return new Requirement[] {
                new Requirement(1, new DateTime(2021, 10, 18) ),
                new Requirement(20, new DateTime(2021, 11, 20) ),
            };
        }
        private static Requirement[] SeedReqsPlashki366_399_06()
        {
            Requirement[] reqs = new Requirement[] {
                new Requirement(272, new DateTime(2021, 11, 11) ),
                new Requirement(272, new DateTime(2021, 11, 20) ),
                new Requirement(272, new DateTime(2021, 11, 21) ),
                new Requirement(245, new DateTime(2021, 12, 4) ),
            };
            return reqs;
        }
    }

    public static class OptimalBatchStatic
    {
        public static List<Batch> GetOptimalBatches(Requirement[] requirements, int optimalQuantity, int maxQuantity, uint days = 30, int mustFrequency = 1, int recomendedFrequency = 1, int avgQnt = 0)
        {
            var orderedReqs = requirements.OrderBy(r => r.deadline).ToArray();
            List<Batch> batches = new List<Batch>();
            Batch batch = null;
            recomendedFrequency = LCM(mustFrequency, recomendedFrequency);
            while (true)
            {
                var allNetto = orderedReqs.Sum(r => r.Netto);
                if (allNetto <= 0)
                    break;
                foreach (var req in orderedReqs.Where(r => r.Netto > 0))
                {
                    if (batch == null || batch.FreeQuantity <= 0)
                    {
                        var periodNetto = orderedReqs
                            .TakeWhile(r => r.deadline < req.deadline.AddDays(days))
                            .Sum(r => r.Netto);
                        var batchLimit = GetBatchLimit(optimalQuantity, maxQuantity, periodNetto, allNetto, avgQnt);
                        var quantityRecomendedFrequency = QuantityByFrequency(recomendedFrequency, batchLimit);
                        var limitByFrequency = quantityRecomendedFrequency < allNetto ? quantityRecomendedFrequency : batchLimit;
                        if (maxQuantity > 0 && limitByFrequency > maxQuantity)
                            limitByFrequency = QuantityByFrequency(mustFrequency, batchLimit);
                        batch = new Batch(limitByFrequency, req, mustFrequency);
                        batches.Add(batch);
                    }
                    var reserveQnt = Math.Min(batch.FreeQuantity, req.Netto);
                    req.reserved += reserveQnt;
                    batch.reserved += reserveQnt;
                    batch.reqs.Add(Tuple.Create(req, reserveQnt));
                    if (batch.FreeQuantity <= 0) break;
                }
            }
            return batches;
        }

        //greatest common divisor
        static int GCD(int n1, int n2)
        {
            int div;
            if (n1 == n2) return n1;
            int d = n1 - n2;
            if (d < 0)
            {
                d = -d; div = GCD(n1, d);
            }
            else
                div = GCD(n2, d);
            return div;
        }

        //least common multiple
        public static int LCM(int n1, int n2)
        {
            return
                n1 < 2
                    ? n2 < 2
                        ? 1
                        : n2
                    : n2 < 2
                        ? n1
                        : n1 * n2 / GCD(n1, n2);
        }

        private static int GetBatchLimit(int optimalQuantity, int maxQuantity, int periodReq, int allReq, int avgQnt)
        {
            int qnt = periodReq;
            if (optimalQuantity > 1 && qnt < optimalQuantity)
                qnt = allReq < optimalQuantity
                    ? allReq
                    : Math.Min(optimalQuantity, allReq / 2);
            if (avgQnt > 0 && qnt < optimalQuantity)
                qnt = Math.Min(optimalQuantity, avgQnt);
            if (maxQuantity > 0 && qnt > maxQuantity)
                qnt = maxQuantity;
            return qnt > 0 ? qnt : 1;
        }

        public static int QuantityByFrequency(int frequency, int quantity)
        {
            if (frequency > 1 && quantity % frequency > 0)
            {
                int billetQnt = (quantity / frequency) + 1;
                return billetQnt * frequency;
            }
            return quantity;
        }
    }

    public class Requirement
    {
        public DateTime deadline;
        public int quantity;
        public int reserved = 0;
        public int Netto => quantity - reserved;
        public object req;

        public Requirement(int quantity, DateTime deadline, object req = null)
        {
            this.quantity = quantity;
            this.deadline = deadline;
            this.req = req;
        }
        public override string ToString()
        {
            return string.Format("deadline={0:d}\tqnt={1}", deadline, quantity);
        }
    }

    public class Batch
    {
        public DateTime deadline;
        public int quantity;
        public object requirement;
        public List<Tuple<Requirement, int>> reqs;
        public int reserved = 0;
        private int frequency;

        public Batch(int quantity, Requirement requirement, int frequency)
        {
            this.quantity = quantity;
            this.deadline = requirement.deadline;
            this.requirement = requirement.req;
            this.frequency = frequency;
            this.reqs = new List<Tuple<Requirement, int>>();
        }

        public int Quantity => OptimalBatchStatic.QuantityByFrequency(frequency, quantity);
        public int FreeQuantity => Quantity - reserved;

        public override string ToString()
        {
            return string.Format("Batch deadline={0:d}\treserved={1}\tquantity={2}\tFreeQuantity={3}\tQuantity={4}", deadline, reserved, quantity, FreeQuantity, Quantity);
        }
    }
    public struct CalcParams
    {
        public int OptimalQuantity;
        public int MaxQuantity;
        public int avgQnt;
        public int MustFrequency;
        public int RecomendedFrequency;
    }
}