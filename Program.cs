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
            var optimalBatches = OptimalBatchStatic.GetOptimalBatches(reqs, 75, 30, 5);
            foreach (var batch in optimalBatches)
            {
                Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", batch.limit, batch.reserved, batch.Balance, batch.Quantity, batch.deadline);
                foreach (var kv in batch.reqs)
                {
                    Console.WriteLine("\t{0}\t{1}", kv.Value, kv.Key.deadline);
                }
            }
            Console.WriteLine("optimalBatches.Sum(b => b.Quantity)={0}", optimalBatches.Sum(b => b.Quantity));
            Console.Read();
        }

        private static Requirement[] SeedSmallLastBatch()
        {
            Requirement[] reqs = new Requirement[] {
            new Requirement(74, new DateTime(2021, 2, 22) ),
            new Requirement(76, new DateTime(2021, 2, 22) ),
            new Requirement(10, new DateTime(2021, 3, 22) ),
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
            var batch = new Batch(maxQuantity, firstReq, frequency);
            batches.Add(batch);
            for (int i = 0; i < requirements.Length; i++)
            {
                var req = requirements[i];
                if (batch.Balance == 0)
                {
                    uint batchQnt = (uint)requirements.Skip(i).Where(r => r.deadline < req.deadline.AddDays(days))
                        .Sum(r => r.quantity);
                    if (batchQnt > maxQuantity * 1.5)
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
                            batch.reqs.Add(req, reserveQnt);

                            if (reqQnt > 0)
                            {
                                uint batchQnt = reqQnt +
                                    (uint)requirements.Skip(i + 1).Where(r => r.deadline < req.deadline.AddDays(days))
                                    .Sum(r => r.quantity);
                                if (batchQnt > maxQuantity * 1.5)
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
                        batch.reqs.Add(req, reserveQnt);
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
        public Dictionary<Requirement, uint> reqs;
        public uint reserved = 0;
        public uint frequency;

        public Batch(uint limit, Requirement requirement, uint frequency)
        {
            this.limit = QuantityByFrequency(frequency, limit);
            this.deadline = requirement.deadline;
            this.requirement = requirement.req;
            this.frequency = frequency;
            this.reqs = new Dictionary<Requirement, uint>();
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
