using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimalBatch
{
    class Program
    {
        static void Main()
        {
            Requirement[] reqs = SeedSmallLastBatch();
            Console.WriteLine("reqs.Sum={0}", reqs.Sum(r => r.quantity));
            var optimalBatches = OptimalBatchStatic.GetOptimalBatches(reqs, 26, 30, 1);
            foreach (var batch in optimalBatches)
            {
                Console.WriteLine("limit={0}\treserv={1}\tbalance={2}\tqnt={3}\tdeadline={4}",
                    batch.limit, batch.reserved, batch.Balance, batch.Quantity, batch.deadline);
                foreach (var kv in batch.reqs)
                {
                    Console.WriteLine("\t{0}\t{1}", kv.Item2, kv.Item1.deadline);
                }
            }
            Console.WriteLine("optimalBatches.Sum(b => b.Quantity)={0}", optimalBatches.Sum(b => b.Quantity));
            Console.Read();
        }

        private static Requirement[] SeedSmallLastBatch()
        {
            Requirement[] reqs = new Requirement[] {

            //new Requirement(15, new DateTime(2021, 5, 26) ),
            //new Requirement(1, new DateTime(2021, 6, 21) ),

            new Requirement(8, new DateTime(2021, 7, 30) ),
            new Requirement(18, new DateTime(2021, 8, 20) ),
            new Requirement(2, new DateTime(2021, 8, 20) ),
            new Requirement(20, new DateTime(2021, 8, 28) ),
            //new Requirement(12, new DateTime(2021, 6,14) ),
            //new Requirement(12, new DateTime(2021, 6,14) ),
            //new Requirement(12, new DateTime(2021, 6,15) ),
            //new Requirement(12, new DateTime(2021, 6,18) ),
            //new Requirement(12, new DateTime(2021, 6,18) ),
            //new Requirement(12, new DateTime(2021, 6,22) ),
            //new Requirement(12, new DateTime(2021, 6,22) ),
            //new Requirement(12, new DateTime(2021, 6,22) ),
            };
            //var optimalBatches = OptimalBatchStatic.GetOptimalBatches(reqs, 75, 30, 5);
            return reqs;
        }
    }

    public static class OptimalBatchStatic
    {
        public static List<Batch> GetOptimalBatches(Requirement[] requirements, uint maxQuantity, uint days = 30, uint frequency = 1)
        {
            var firstReq = requirements.First();
            List<Batch> batches = new List<Batch>();
            uint firstBatchQnt = (uint)requirements.Where(r => r.deadline < firstReq.deadline.AddDays(days))
                .Sum(r => r.quantity);
            if (firstBatchQnt > maxQuantity * 2)
                firstBatchQnt = maxQuantity;
            var batch = new Batch(firstBatchQnt, firstReq, frequency);
            batches.Add(batch);
            for (int i = 0; i < requirements.Length; i++)
            {
                var req = requirements[i];
                if (batch.Balance == 0)
                {
                    uint batchQnt = (uint)requirements.Skip(i).Where(r => r.deadline < req.deadline.AddDays(days))
                        .Sum(r => r.quantity);
                    if (batchQnt > maxQuantity * 2)
                        batchQnt = maxQuantity;
                    batch = new Batch(batchQnt, req, frequency);
                    batches.Add(batch);
                }
                uint reqQnt = req.quantity;
                while (true)
                    if (req.deadline < batch.deadline.AddDays(days))
                    {
                        while (true)
                        {
                            uint reserveQnt = Math.Min(batch.Balance, reqQnt);
                            batch.reserved += reserveQnt;
                            reqQnt -= reserveQnt;
                            batch.reqs.Add(Tuple.Create(req, reserveQnt));

                            if (reqQnt > 0)
                            {
                                uint batchQnt = reqQnt +
                                    (uint)requirements.Skip(i + 1).Where(r => r.deadline < req.deadline.AddDays(days))
                                    .Sum(r => r.quantity);
                                if (batchQnt > maxQuantity * 2)
                                    batchQnt = maxQuantity;
                                batch = new Batch(batchQnt, req, frequency);
                                batches.Add(batch);
                            }
                            else break;
                        }
                        break;
                    }
                    else if (batch.Quantity > batch.reserved)
                    {
                        uint reserveQnt = Math.Min(batch.Quantity - batch.reserved, reqQnt);
                        batch.reserved += reserveQnt;
                        reqQnt -= reserveQnt;
                        batch.reqs.Add(Tuple.Create(req, reserveQnt));
                        if (reqQnt == 0) break;
                    }
                    else
                    {
                        batch = new Batch(maxQuantity, req, frequency);
                        batches.Add(batch);
                    }
            }
            return batches;
        }
    }

    public struct Requirement
    {
        public DateTime deadline;
        public uint quantity;
        public object req;

        public Requirement(uint quantity, DateTime deadline, object req = null)
        {
            this.quantity = quantity;
            this.deadline = deadline;
            this.req = req;
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

        public uint Balance => limit - reserved;
        public uint Quantity => QuantityByFrequency(frequency, reserved);

        public static uint QuantityByFrequency(uint frequency, uint quantity)
        {
            if (frequency > 1 && quantity % frequency > 0)
            {
                uint billetQnt = (quantity / frequency) + 1;
                return billetQnt * frequency;
            }
            return quantity;
        }
    }
}
