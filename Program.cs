﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalBatchV2
{
    public static class OptimalBatchStatic
    {
        public static Batch[] GetOptimalBatches(Requirement[] requirements, int pieceCost, int adjustCost, int pieceSeconds, int adjustSeconds, double bankDayRate, int maxBatchQuantity, int mustFrequency = 1, int recomendedFrequency = 1, int avgQnt = 0)
        {
            if (requirements.Length == 0) return Array.Empty<Batch>();
            int optQuantity = pieceSeconds > 0 ? (5 * adjustSeconds) / pieceSeconds : 0;
            int requiredSumQuantity = requirements.Sum(x => x.quantity);
            if (optQuantity > requiredSumQuantity / 2)
                optQuantity = requiredSumQuantity / 2;
            var linkedList = new LinkedList<Requirement>(requirements.OrderBy(r => r.Deadline));
            List<Batch> batches = new List<Batch>();
            Batch batch = null;
            recomendedFrequency = LCM(mustFrequency, recomendedFrequency);
            if (maxBatchQuantity <= 0) maxBatchQuantity = int.MaxValue;
            var recomendedMaxQuantity = QuantityByFrequencyDown(recomendedFrequency, maxBatchQuantity);
            if (recomendedMaxQuantity > 0) maxBatchQuantity = recomendedMaxQuantity;
            var reqNode = linkedList.First;
            do
            {
                var req = reqNode.Value;
                if (batch != null && batch.FreeQuantity > 0)
                {
                    int qnt = Math.Min(req.Netto, batch.FreeQuantity);
                    double reqPieceCost = pieceCost * (1 + bankDayRate * (req.Deadline - batch.deadline).TotalDays);
                    req.reserved += qnt;
                    batch.Reserved += qnt;
                    batch.quantity = Math.Max(batch.Reserved, batch.quantity);
                    batch.Cost += qnt * reqPieceCost;
                    batch.reqs.Add(Tuple.Create(req, qnt));
                }
                else
                {
                    int dateQnt = requirements.Where(x => x.Deadline <= req.Deadline.AddDays(2)).Sum(x => x.Netto);
                    int reserveQnt = Math.Min(req.Netto, maxBatchQuantity);
                    req.reserved += reserveQnt;
                    int minQnt = Math.Max(optQuantity, dateQnt);
                    minQnt = Math.Max(minQnt, avgQnt);
                    batch = new Batch(maxBatchQuantity, reserveQnt, req, mustFrequency, pieceCost, adjustCost, minQnt);
                    batch.reqs.Add(Tuple.Create(req, reserveQnt));
                    batches.Add(batch);
                }
                if (req.Netto <= 0)
                    reqNode = reqNode.Next;
            } while (reqNode != null);
            var last = batches.Last();
            last.quantity = last.Reserved;
            return batches.ToArray();
        }
        public static List<Batch> GetOptimalBatches(Requirement[] requirements, int pieceCost, int adjustCost, double bankDayRate, int maxBatchQuantity, int mustFrequency = 1, int recomendedFrequency = 1, int avgQnt = 0)
        {
            if (requirements.Length == 0) return new List<Batch>();
            var linkedList = new LinkedList<Requirement>(requirements.OrderBy(r => r.Deadline));
            List<Batch> batches = new List<Batch>();
            Batch batch = null;
            recomendedFrequency = LCM(mustFrequency, recomendedFrequency);
            if (maxBatchQuantity <= 0) maxBatchQuantity = int.MaxValue;
            var recomendedMaxQuantity = QuantityByFrequencyDown(recomendedFrequency, maxBatchQuantity);
            if (recomendedMaxQuantity > 0) maxBatchQuantity = recomendedMaxQuantity;
            var reqNode = linkedList.First;
            do
            {
                var req = reqNode.Value;
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
                    double reqPieceCost = pieceCost * (1 + bankDayRate * (req.Deadline - batch.deadline).TotalDays);
                    int qnt = Math.Min(req.Netto, batch.FreeQuantity);
                    if (batch.FreeQuantity > 0)
                    {
                        req.reserved += qnt;
                        batch.Reserved += qnt;
                        batch.quantity = batch.Reserved;
                        batch.Cost += qnt * reqPieceCost;
                        batch.reqs.Add(Tuple.Create(req, qnt));
                    }
                    qnt = Math.Min(req.Netto, batch.FreeLimit);
                    if (qnt > 0)
                    {
                        double newCost = batch.Cost + qnt * reqPieceCost;
                        var newReserved = batch.Reserved + qnt;
                        double price = newCost / newReserved;
                        if (price < batch.Price)
                        {
                            req.reserved += qnt;
                            batch.Reserved = newReserved;
                            batch.quantity = newReserved;
                            batch.Cost = newCost;
                            batch.reqs.Add(Tuple.Create(req, qnt));
                        }
                        else
                            batch = null;
                    }
                }
                if (req.Netto <= 0)
                    reqNode = reqNode.Next;
            } while (reqNode != null);
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
        public DateTime Deadline;
        public int quantity;
        public int reserved = 0;
        public int Netto => quantity - reserved;
        public object req;

        public Requirement(int quantity, DateTime deadline, object req = null)
        {
            this.quantity = quantity;
            Deadline = deadline.Date;
            this.req = req;
        }
        public override string ToString()
        {
            return string.Format("deadline={0:d}\tqnt={1}", Deadline, quantity);
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
            deadline = requirement.Deadline;
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
            deadline = requirement.Deadline;
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