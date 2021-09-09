using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalBatch
{
    class Program
    {
        static void Main()
        {
            const int Days = 30;
            Requirement[] reqs = SeedSmallLastBatch();
            const int OptimalQuantity = 0;
            const int MaxQuantity = 1000;
            const int Frequency = 8;
            Console.WriteLine("reqs.Sum={0} OptimalQuantity={1} MaxQuantity={2}", reqs.Sum(r => r.quantity), OptimalQuantity, MaxQuantity);
            var optimalBatches = OptimalBatchStatic.GetOptimalBatches(reqs, OptimalQuantity, MaxQuantity, Days, Frequency);
            foreach (var req in reqs.OrderBy(r => r.deadline))
                Console.WriteLine(req);
            foreach (var batch in optimalBatches)
            {
                Console.WriteLine(batch);
                foreach (var kv in batch.reqs)
                    Console.WriteLine("\t{0}\t{1:d}", kv.Item2, kv.Item1.deadline);
            }
            Console.WriteLine("optimalBatches.Sum(b => b.Quantity)={0}", optimalBatches.Sum(b => b.Quantity));
            Console.Read();
        }

        private static Requirement[] SeedSmallLastBatch()
        {
            Requirement[] reqs = new Requirement[] {

            //new Requirement(15, new DateTime(2021, 5, 26) ),
            //new Requirement(1, new DateTime(2021, 6, 21) ),

            new Requirement(15, new DateTime(2021, 8, 1) ),
            new Requirement(8, new DateTime(2021, 9, 1) ),
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
    }

    public static class OptimalBatchStatic
    {
        public static List<Batch> GetOptimalBatches(Requirement[] requirements, uint optimalQuantity, uint maxQuantity, uint days = 30, uint frequency = 1)
        {
            var orderedReqs = requirements.OrderBy(r => r.deadline).ToArray();
            List<Batch> batches = new List<Batch>();
            Batch batch = null;
            int i = 0;
            while (i < orderedReqs.Length)
            {
                var req = orderedReqs[i];
                if (batch == null || batch.FreeLimit <= 0 || (batch.FreeQuantity <= 0 && req.deadline > batch.deadline.AddDays(days)))
                {
                    uint reqNetto = (uint)orderedReqs.Skip(i).TakeWhile(r => r.deadline < req.deadline.AddDays(days))
                            .Sum(r => r.Netto);
                    Console.WriteLine("req.Netto={0} reqNetto={1}", req.Netto, reqNetto);

                    var batchLimit = GetBatchLimit(optimalQuantity, maxQuantity, reqNetto);
                    batch = new Batch(batchLimit, req, frequency);
                    batches.Add(batch);
                }
                uint reserveQnt = Math.Min(batch.FreeLimit, req.Netto);
                req.reserved += reserveQnt;
                batch.reserved += reserveQnt;
                batch.reqs.Add(Tuple.Create(req, reserveQnt));

                if (req.Netto <= 0) i++;
            }
            return batches;
        }

        //public static List<Batch> GetOptimalBatches1(Requirement[] requirements, uint optimalQuantity, uint maxQuantity, uint days = 30, uint frequency = 1)
        //{
        //    var orderedReqs = requirements.OrderBy(r => r.deadline).ToArray();
        //    var firstReq = orderedReqs.First();
        //    List<Batch> batches = new List<Batch>();
        //    uint firstBatchQnt = (uint)orderedReqs.Where(r => r.deadline < firstReq.deadline.AddDays(days))
        //        .Sum(r => r.quantity);
        //    var batchLimit = GetBatchLimit(optimalQuantity, maxQuantity, firstBatchQnt);
        //    var batch = new Batch(batchLimit, firstReq, frequency);
        //    batches.Add(batch);
        //    for (int i = 0; i < orderedReqs.Length; i++)
        //    {
        //        var req = orderedReqs[i];
        //        if (batch.FreeLimit == 0)
        //        {
        //            uint batchQnt = (uint)orderedReqs.Skip(i).Where(r => r.deadline < req.deadline.AddDays(days))
        //                .Sum(r => r.quantity);
        //            batchQnt = GetBatchLimit(optimalQuantity, maxQuantity, batchQnt);
        //            batch = new Batch(batchQnt, req, frequency);
        //            batches.Add(batch);
        //        }
        //        uint reqQnt = req.quantity;
        //        while (true)
        //            if (req.deadline < batch.deadline.AddDays(days))
        //            {
        //                while (true)
        //                {
        //                    uint reserveQnt = Math.Min(batch.FreeLimit, reqQnt);
        //                    //if (reserveQnt <= 0) break;
        //                    batch.reserved += reserveQnt;
        //                    reqQnt -= reserveQnt;
        //                    batch.reqs.Add(Tuple.Create(req, reserveQnt));
        //                    if (reqQnt > 0)
        //                    {
        //                        uint periodQnt = reqQnt +
        //                            (uint)orderedReqs.Skip(i + 1).Where(r => r.deadline < req.deadline.AddDays(days))
        //                            .Sum(r => r.quantity);
        //                        var batchQnt = GetBatchLimit(optimalQuantity, maxQuantity, periodQnt);
        //                        batch = new Batch(batchQnt, req, frequency);
        //                        batches.Add(batch);
        //                    }
        //                    else break;
        //                }
        //                break;
        //            }
        //            else if (batch.FreeQuantity > 0)
        //            {
        //                uint reserveQnt = Math.Min(batch.FreeQuantity, reqQnt);
        //                batch.reserved += reserveQnt;
        //                reqQnt -= reserveQnt;
        //                batch.reqs.Add(Tuple.Create(req, reserveQnt));
        //                if (reqQnt == 0) break;
        //            }
        //            else
        //            {
        //                uint periodQnt = reqQnt +
        //                    (uint)orderedReqs.Skip(i + 1).Where(r => r.deadline < req.deadline.AddDays(days))
        //                    .Sum(r => r.quantity);
        //                var batchQnt = GetBatchLimit(optimalQuantity, maxQuantity, periodQnt);
        //                batch = new Batch(batchQnt, req, frequency);
        //                batches.Add(batch);
        //            }
        //    }
        //    return batches;
        //}

        private static uint GetBatchLimit(uint optimalQuantity, uint maxQuantity, uint periodQnt)
        {
            if (optimalQuantity > 0 && periodQnt > optimalQuantity * 2)
                periodQnt = optimalQuantity;
            if (maxQuantity > optimalQuantity && periodQnt > maxQuantity)
                periodQnt = maxQuantity;
            return periodQnt > 0 ? periodQnt : 1;
        }
    }

    public class Requirement
    {
        public DateTime deadline;
        public uint quantity;
        public uint reserved = 0;
        public uint Netto => quantity - reserved;
        public object req;

        public Requirement(uint quantity, DateTime deadline, object req = null)
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
        public uint limit;
        public object requirement;
        public List<Tuple<Requirement, uint>> reqs;
        public uint reserved = 0;
        public uint frequency;

        public Batch(uint limit, Requirement requirement, uint frequency)
        {
            this.limit = QuantityByFrequency(frequency, limit);
            this.deadline = requirement.deadline;
            this.requirement = requirement.req;
            this.frequency = frequency;
            this.reqs = new List<Tuple<Requirement, uint>>();
        }

        public uint FreeLimit => limit - reserved;
        public uint Quantity => QuantityByFrequency(frequency, reserved);
        public uint FreeQuantity => Quantity - reserved;

        public static uint QuantityByFrequency(uint frequency, uint quantity)
        {
            if (frequency > 1 && quantity % frequency > 0)
            {
                uint billetQnt = (quantity / frequency) + 1;
                return billetQnt * frequency;
            }
            return quantity;
        }
        public override string ToString()
        {
            return string.Format("Batch deadline={0:d}\treserved={1}\tlimit={2}\tFreeLimit={3}\tQuantity={4}", deadline, reserved, limit, FreeLimit, Quantity);
        }
    }
}