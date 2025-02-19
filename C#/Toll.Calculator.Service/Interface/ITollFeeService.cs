﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toll.Calculator.Domain;

namespace Toll.Calculator.Service.Interface
{
    public interface ITollFeeService
    {
        Task<decimal> GetTotalFee(Vehicle vehicleType, List<DateTime> passageDates);
    }
}