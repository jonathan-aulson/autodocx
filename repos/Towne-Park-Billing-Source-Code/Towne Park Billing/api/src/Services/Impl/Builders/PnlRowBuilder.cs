using System;
using System.Collections.Generic;
using System.Linq;
using api.Models.Dto;

namespace api.Services.Impl.Builders
{
    public class PnlRowBuilder
    {
        private string _columnName;
        private List<MonthValueDto> _monthlyValues = new List<MonthValueDto>();

        public PnlRowBuilder WithColumnName(string columnName)
        {
            _columnName = columnName;
            return this;
        }

        public PnlRowBuilder WithMonthlyValues(int numberOfMonths, Func<int, MonthValueDto> monthValueFactory)
        {
            _monthlyValues = Enumerable.Range(0, numberOfMonths)
                .Select(monthIndex => monthValueFactory(monthIndex))
                .ToList();
            return this;
        }
        
        public PnlRowBuilder WithInitializedMonthlyValues(int numberOfMonths)
        {
            _monthlyValues = Enumerable.Range(0, numberOfMonths)
                .Select(monthIndex => new MonthValueDto
                {
                    Month = monthIndex,
                    Value = 0m,
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto(), // Ensure this is initialized
                    //add additional breakdowns here
                    SiteDetails = new List<SiteMonthlyRevenueDetailDto>() // Ensure this is initialized
                })
                .ToList();
            return this;
        }


        public PnlRowBuilder SetMonthlyValue(int monthZeroBased, MonthValueDto value)
        {
            if (monthZeroBased >= 0 && monthZeroBased < _monthlyValues.Count)
            {
                _monthlyValues[monthZeroBased] = value;
            }
            // Optionally, handle error or resize if out of bounds
            return this;
        }
        
        public PnlRowBuilder UpdateMonthlyValue(int monthZeroBased, Action<MonthValueDto> updateAction)
        {
            if (monthZeroBased >= 0 && monthZeroBased < _monthlyValues.Count)
            {
                updateAction(_monthlyValues[monthZeroBased]);
            }
            return this;
        }


        public PnlRowDto Build()
        {
            if (string.IsNullOrEmpty(_columnName))
            {
                throw new InvalidOperationException("ColumnName must be set to build a PnlRowDto.");
            }

            return new PnlRowDto
            {
                ColumnName = _columnName,
                MonthlyValues = _monthlyValues
            };
        }
    }
}
