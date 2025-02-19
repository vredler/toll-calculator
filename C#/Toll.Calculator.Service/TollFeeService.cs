﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toll.Calculator.DAL.Repositories.Interface;
using Toll.Calculator.Domain;
using Toll.Calculator.Service.Interface;

namespace Toll.Calculator.Service
{
    public class TollFeeService : ITollFeeService
    {
        private readonly ITollFeeRepository _tollFeeRepository;
        private readonly IVehicleRepository _vehicleRepository;

        public TollFeeService(
            IVehicleRepository vehicleRepository,
            ITollFeeRepository tollFeeRepository)
        {
            _vehicleRepository = vehicleRepository;
            _tollFeeRepository = tollFeeRepository;
        }

        public async Task<decimal> GetTotalFee(Vehicle vehicleType, List<DateTime> passageDates)
        {
            var tollFreeVehicles = await _vehicleRepository.GetTollFreeVehiclesAsync();

            if (tollFreeVehicles.Contains(vehicleType) ||
                !passageDates.Any())
                return 0;

            decimal totalFee = 0;

            var distinctDates = passageDates.GroupBy(x => x.ToString("yyyyMMdd")).Select(y => y.First()).ToList();

            foreach (var distinctDate in distinctDates)
            {
                if (await _tollFeeRepository.IsTollFreeDateAsync(distinctDate))
                    continue;

                totalFee += await GetTotalFeeForDay(passageDates.Where(p => p.Date == distinctDate.Date).ToList());
            }

            return totalFee;
        }

        private async Task<decimal> GetTotalFeeForDay(List<DateTime> passageDates)
        {
            passageDates.Sort((a, b) => a.CompareTo(b));
            var leewayInterval = await _tollFeeRepository.GetPassageLeewayInterval();

            var intervalStart = passageDates.First();
            var intervalHighestFee = await _tollFeeRepository.GetPassageFeeByTimeAsync(intervalStart);
            decimal totalFee = 0;

            foreach (var passageDate in passageDates)
            {
                var passageFee = await _tollFeeRepository.GetPassageFeeByTimeAsync(passageDate);

                var diff = passageDate - intervalStart;

                if (diff <= leewayInterval)
                {
                    if (totalFee > 0) totalFee -= intervalHighestFee;
                    if (passageFee >= intervalHighestFee) intervalHighestFee = passageFee;
                    totalFee += intervalHighestFee;
                }
                else
                {
                    totalFee += passageFee;
                    intervalStart = passageDate;
                    intervalHighestFee = passageFee;
                }
            }

            var maximumDailyFee = await _tollFeeRepository.GetMaximumDailyFeeAsync();
            if (totalFee > maximumDailyFee) totalFee = maximumDailyFee;
            return totalFee;
        }
    }
}