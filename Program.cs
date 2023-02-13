using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalBatchV2
{
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
                    batch.Reserved += reserveQnt;
                    batch.reqs.Add(Tuple.Create(req, reserveQnt));
                    if (batch.FreeQuantity <= 0) break;
                }
            }
            return batches;
        }
        public static List<Batch> GetOptimalBatches(Requirement[] requirements, int pieceCost, int adjustCost, decimal bankDayRate, int maxQuantity, int mustFrequency = 1, int recomendedFrequency = 1, int avgQnt = 0)
        {
            var orderedReqs = requirements.OrderBy(r => r.deadline).ToList();
            List<Batch> batches = new List<Batch>();
            Batch batch = null;
            if (avgQnt > 0)
                orderedReqs.Insert(0, new Requirement(avgQnt - orderedReqs.Sum(r => r.Netto), requirements.First().deadline));
            recomendedFrequency = LCM(mustFrequency, recomendedFrequency);
            var recomendedMaxQuantity = QuantityByFrequencyDown(recomendedFrequency, maxQuantity);
            if (recomendedMaxQuantity > 0) maxQuantity = recomendedMaxQuantity;
            while (true)
            {
                var allNetto = orderedReqs.Sum(r => r.Netto);
                if (allNetto <= 0)
                    break;
                var req = orderedReqs.Where(r => r.Netto > 0).First();
                {
                    var deadline = req.deadline;
                    if (batch != null)
                    {
                        int reqQuantity = req.Netto;
                        while (reqQuantity > 0)
                        {
                            int directCost = reqQuantity * pieceCost;
                            decimal reqCost = directCost + (decimal)((req.deadline - batch.deadline).TotalDays * directCost) * bankDayRate;
                            int newQuantity = batch.Reserved + reqQuantity;
                            decimal price = (batch.Cost + reqCost) / newQuantity;
                            if (price <= batch.Price)
                            {
                                var reserveQnt = Math.Min(batch.FreeLimit, reqQuantity);
                                reqQuantity -= reserveQnt;
                                req.reserved += reserveQnt;
                                batch.Reserved += reserveQnt;
                                batch.Cost += reqCost;
                                batch.reqs.Add(Tuple.Create(req, reserveQnt));
                                if (batch.FreeLimit <= 0) break;
                            }
                            else
                            {
                                reqQuantity /= 2;
                                if (reqQuantity <= 0)
                                {
                                    int quantity = Math.Min(req.Netto, maxQuantity);
                                    req.reserved += quantity;
                                    batch = new Batch(maxQuantity, quantity, req, mustFrequency, pieceCost, adjustCost);
                                    batches.Add(batch);

                                    reqQuantity = req.Netto;
                                }
                            }
                        }
                    }
                    else
                    {
                        int quantity = Math.Min(req.Netto, maxQuantity);
                        req.reserved += quantity;
                        batch = new Batch(maxQuantity, quantity, req, mustFrequency, pieceCost, adjustCost);
                        batches.Add(batch);
                    }
                }
            }
            batches.ForEach(b => b.quantity = b.Reserved);
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
        public static int QuantityByFrequencyDown(int frequency, int quantity)
        {
            if (frequency > 1 && quantity % frequency > 0)
            {
                int billetQnt = quantity / frequency;
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
        public int Limit;
        public int quantity;
        public object requirement;
        public List<Tuple<Requirement, int>> reqs;
        public int Reserved = 0;
        private int frequency;
        public int PieceCost;
        public int AdjustCost;
        public int Quantity => OptimalBatchStatic.QuantityByFrequency(frequency, quantity);
        public int FreeQuantity => Quantity - Reserved;
        public int FreeLimit => Limit - Reserved;
        public int DirectCost => Reserved * PieceCost + AdjustCost;
        public decimal Cost = 0;
        public decimal Price => Cost / Reserved;
        public Batch(int quantity, Requirement requirement, int frequency)
        {
            this.quantity = quantity;
            deadline = requirement.deadline;
            this.requirement = requirement.req;
            this.frequency = frequency;
            reqs = new List<Tuple<Requirement, int>>();
        }
        public Batch(int limit, int reserved, Requirement requirement, int frequency, int pieceCost, int adjustCost)
        {
            Limit = OptimalBatchStatic.QuantityByFrequency(frequency, limit);
            quantity = 0;
            Reserved = reserved;
            deadline = requirement.deadline;
            this.requirement = requirement.req;
            this.frequency = frequency;
            reqs = new List<Tuple<Requirement, int>>();
            PieceCost = pieceCost;
            AdjustCost = adjustCost;
            Cost = adjustCost + reserved * pieceCost;
        }
        public override string ToString()
        {
            return $"Batch deadline={deadline:d}\treserved={Reserved}\tquantity={quantity}\tFreeQuantity={FreeQuantity}\tQuantity={Quantity}";
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