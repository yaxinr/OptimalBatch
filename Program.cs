﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalBatchV2
{
    public static class OptimalBatchStatic
    {
        public static List<Batch> GetOptimalBatches(Requirement[] requirements, int pieceCost, int adjustCost, double bankDayRate, int maxBatchQuantity, int mustFrequency = 1, int recomendedFrequency = 1, int avgQnt = 0)
        {
            if (requirements.Length == 0) return new List<Batch>();
            requirements = requirements.GroupBy(r => r.deadline.Date).Select(g => new Requirement(g.Sum(r => r.quantity), g.Key, g.First().req)).ToArray();
            var orderedReqs = requirements.OrderBy(r => r.deadline).ToList();
            List<Batch> batches = new List<Batch>();
            Batch batch = null;
            recomendedFrequency = LCM(mustFrequency, recomendedFrequency);
            if (maxBatchQuantity <= 0) maxBatchQuantity = int.MaxValue;
            var recomendedMaxQuantity = QuantityByFrequencyDown(recomendedFrequency, maxBatchQuantity);
            if (recomendedMaxQuantity > 0) maxBatchQuantity = recomendedMaxQuantity;
            while (true)
            {
                var req = orderedReqs.FirstOrDefault(r => r.Netto > 0);
                if (req == null) break;

                if (batch == null || batch.FreeLimit <= 0)
                {
                    int reserveQnt = Math.Min(req.Netto, maxBatchQuantity);
                    req.reserved += reserveQnt;
                    batch = new Batch(maxBatchQuantity, reserveQnt, req, mustFrequency, pieceCost, adjustCost, avgQnt);
                    batch.reqs.Add(Tuple.Create(req, reserveQnt));
                    batches.Add(batch);
                }
                else
                {
                    double reqPieceCost = pieceCost * (1 + bankDayRate * (req.deadline - batch.deadline).TotalDays);
                    if (batch.FreeQuantity > 0)
                    {
                        int qnt = Math.Min(batch.FreeQuantity, req.Netto);
                        req.reserved += qnt;
                        batch.Reserved += qnt;
                        batch.quantity = batch.Reserved;
                        batch.Cost += qnt * reqPieceCost;
                        batch.reqs.Add(Tuple.Create(req, qnt));
                    }
                    int limitedQnt = Math.Min(req.Netto, batch.FreeLimit);
                    if (limitedQnt > 0)
                    {
                        int qnt = 0;
                        int initQnt = 1;
                        if (recomendedFrequency > 1)
                        {
                            initQnt = recomendedFrequency - batch.Reserved % recomendedFrequency;
                            if (initQnt > limitedQnt)
                                initQnt = 1;
                        }
                        double prevPrice = batch.Price;
                        for (var reqQuantity = initQnt; reqQuantity <= limitedQnt; reqQuantity++)
                        {
                            int newQuantity = batch.Reserved + reqQuantity;
                            double price = (batch.Cost + reqQuantity * reqPieceCost) / newQuantity;
                            if (price < prevPrice * 0.995 || (recomendedFrequency > 1 && price < prevPrice && newQuantity % recomendedFrequency == 0))
                            {
                                qnt = reqQuantity;
                                prevPrice = price;
                            }
                            else break;
                        }
                        if (qnt > 0)
                        {
                            req.reserved += qnt;
                            batch.Reserved += qnt;
                            batch.quantity = batch.Reserved;
                            batch.Cost += qnt * reqPieceCost;
                            batch.reqs.Add(Tuple.Create(req, qnt));
                        }
                        else
                            batch = null;
                    }
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
        public double Cost = 0;
        public double Price => Cost / Reserved;
        public Batch(int quantity, Requirement requirement, int frequency)
        {
            this.quantity = quantity;
            deadline = requirement.deadline;
            this.requirement = requirement.req;
            this.frequency = frequency;
            reqs = new List<Tuple<Requirement, int>>();
        }
        public Batch(int limit, int reserved, Requirement requirement, int frequency, int pieceCost, int adjustCost, int minQuantity = 0)
        {
            Limit = OptimalBatchStatic.QuantityByFrequency(frequency, limit);
            quantity = Math.Max(minQuantity, reserved);
            quantity = Math.Min(Limit, quantity);
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