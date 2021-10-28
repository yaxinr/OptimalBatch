using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalBatchV2
{
    class Program
    {
        static void Main()
        {
            const int Days = 30;
            Requirement[] reqs = SeedManyReqs();
            Console.WriteLine("reqs.Sum(x => x.quantity)={0}", reqs.Sum(x => x.quantity));
            const int OptimalQuantity = 66;
            const int MaxQuantity = 272;
            const int avgQnt = 130;
            const int MustFrequency = 1;
            const int RecomendedFrequency = 10;
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
            Console.WriteLine("optimalBatches.Sum(b => b.FreeQuantity)={0}", optimalBatches.Sum(b => b.FreeQuantity));
            Console.Read();
        }

        private static Requirement[] SeedOneReq()
        {
            Requirement[] reqs = new Requirement[] {
                new Requirement(10, new DateTime(2021, 10, 6) ),
            };
            return reqs;
        }
        private static Requirement[] SeedSmallLastBatch()
        {
            Requirement[] reqs = new Requirement[] {
            new Requirement(22, new DateTime(2021, 11, 14) ),
            new Requirement(200, new DateTime(2022, 1, 15) ),
            };
            return reqs;
        }
        private static Requirement[] SeedManyReqs()
        {
            Requirement[] reqs = new Requirement[] {

            //new Requirement(15, new DateTime(2021, 5, 26) ),
            //new Requirement(1, new DateTime(2021, 6, 21) ),

            new Requirement(22, new DateTime(2021, 1, 1) ),
            //new Requirement(8, new DateTime(2021, 1, 1) ),
            new Requirement(200, new DateTime(2021, 2, 1) ),
            new Requirement(300, new DateTime(2021, 3, 1) ),
            new Requirement(300, new DateTime(2021, 5, 1) ),
            new Requirement(300, new DateTime(2021, 12, 1) ),
            //new Requirement(4, new DateTime(2021, 8, 27) ),
            //new Requirement(56, new DateTime(2021, 8, 29) ),
            //new Requirement(16, new DateTime(2021, 8, 29) ),
            //new Requirement(16, new DateTime(2021, 8, 29) ),
            //new Requirement(2, new DateTime(2021, 6,6) ),
            //new Requirement(2, new DateTime(2021, 6,8) ),
            //new Requirement(2, new DateTime(2021, 6,9) ),
            //new Requirement(1, new DateTime(2021, 6,11) ),
            //new Requirement(3, new DateTime(2021, 6,12) ),
            //new Requirement(10, new DateTime(2021, 6,29) ),
            //new Requirement(12, new DateTime(2021, 6,22) ),
            //new Requirement(12, new DateTime(2021, 6,22) ),
            };
            //var optimalBatches = OptimalBatchStatic.GetOptimalBatches(reqs, 75, 30, 5);
            return reqs;
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
                        var limitByFrequency = QuantityByFrequency(recomendedFrequency, batchLimit);
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
            var lastBatch = batches.LastOrDefault();
            if (lastBatch != null)
                lastBatch.quantity = lastBatch.reserved;
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
        public int FreeQuantity => quantity - reserved;

        public override string ToString()
        {
            return string.Format("Batch deadline={0:d}\treserved={1}\tquantity={2}\tFreeQuantity={3}\tQuantity={4}", deadline, reserved, quantity, FreeQuantity, Quantity);
        }
    }
}