﻿using System;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.PaymentSystems
{
    public interface IPaymentTransaction
    {
        string Id { get; }

        string ClientId { get; }

        double Amount { get; }

        string AssetId { get; }


        /// <summary>
        /// Amount of asset we deposit account
        /// </summary>
        double? DepositedAmount { get; }

        string DepositedAssetId { get; }


        double? Rate { get; }


        string AggregatorTransactionId { get; }

        DateTime Created { get; }

        PaymentStatus Status { get; }

        CashInPaymentSystem PaymentSystem { get; }

        string Info { get; }

    }


    public enum PaymentStatus
    {
        Created,
        NotifyProcessed,
        NotifyDeclined,
        Processing
    }

    

    public interface IPaymentTransactionsRepository
    {
        Task<IPaymentTransaction> SetStatus(string id, PaymentStatus status);
    }
}