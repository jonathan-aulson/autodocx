using api.Adapters.Impl;
using api.Adapters.Mappers;
using api.Models.Dto;
using api.Models.Vo;
using api.Models.Vo.Enum;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;
using static api.Models.Vo.ContractDetailVo;

namespace BackendTests.Adapters
{
    public class ContractServiceAdapterTest
    {
        private readonly IContractService _contractService;
        private readonly ContractServiceAdapter _contractServiceAdapter;

        public ContractServiceAdapterTest()
        {
            _contractService = Substitute.For<IContractService>();
            _contractServiceAdapter = new ContractServiceAdapter(_contractService);
        }

        [Fact]
        public void GetContractDetail_ShouldCallContractServiceAndReturnAdaptedResponse()
        {
            var customerSiteId = Guid.NewGuid();
            var contractVo = new ContractDetailVo
            {
                Id = Guid.NewGuid(),
                PurchaseOrder = "PO-123",
                PaymentTerms = "Due in 30 days",
                BillingType = BillingType.Advanced,
                IncrementAmount = 1.05m,
                IncrementMonth = Month.January,
                ConsumerPriceIndex = true,
                Notes = "Some notes",
                PerOccupiedRoom = new ContractDetailVo.PerOccupiedRoomVo
                {
                    Enabled = true,
                    RoomRate = 10.00m
                },
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    Enabled = true,
                    ServiceRates = new List<ContractDetailVo.ServiceRateVo>
                    {
                        new ContractDetailVo.ServiceRateVo
                        {
                            Id = Guid.NewGuid(),
                            Name = "Valet",
                            DisplayName = "Valet Service",
                            Fee = 1000
                        },
                        new ContractDetailVo.ServiceRateVo
                        {
                            Id = Guid.NewGuid(),
                            Name = "Shuttle",
                            DisplayName = "Shuttle Service",
                            Fee = 3000
                        }
                    }
                },
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    Enabled = true,
                    JobRates = new List<ContractDetailVo.JobRateVo>
                    {
                        new ContractDetailVo.JobRateVo
                        {
                            Id = Guid.NewGuid(),
                            Name = "Valet",
                            DisplayName = "Valet Service",
                            Rate = 100,
                            OvertimeRate = 150,
                            JobCode = "123",
                            InvoiceGroup = 1
                        },
                        new ContractDetailVo.JobRateVo
                        {
                            Id = Guid.NewGuid(),
                            Name = "Shuttle",
                            DisplayName = "Shuttle Service",
                            Rate = 200,
                            OvertimeRate = 300,
                            JobCode = "456",
                            InvoiceGroup = 2
                        }
                    }
                },
                RevenueShare = new RevenueShareVo
                {
                    Enabled = true,
                    ThresholdStructures = new List<ThresholdStructureVo>
                    {
                        new ThresholdStructureVo
                        {
                            Id = Guid.NewGuid(),
                            AccumulationType = AccumulationType.Monthly,
                            RevenueCodes = new List<string> { "123", "456" },
                            Tiers = new List<TierVo>
                            {
                                new TierVo
                                {
                                    SharePercentage = 10,
                                    Amount = null
                                }
                            },
                            ValidationThresholdType = ValidationThresholdType.VehicleCount,
                            ValidationThresholdAmount = 50,
                            InvoiceGroup = 1
                        }
                    }
                },
                BellServiceFee = new BellServiceFeeVo
                {
                    Enabled = true,
                    
                },
                MidMonthAdvance = new MidMonthAdvanceVo
                {
                    Enabled = true,
                    MidMonthAdvances = new List<MidMonthVo>
                    {
                        new MidMonthVo
                        {
                            Amount = 1000,
                            InvoiceGroup = 1,
                            LineTitle = LineTitleType.PreBill
                        }
                    }
                },
                DepositedRevenue = new DepositedRevenueVo
                {
                    Enabled = true,
                    DepositData = new List<DepositDataVo>
                    {
                        new DepositDataVo
                        {
                            TowneParkResponsibleForParkingTax = true,
                            InvoiceGroup = 1
                        }
                    }
                },
                BillableAccount = new BillableAccountVo
                {
                    Enabled = true,
                    BillableAccountsData = new List<BillableAccountDataVo>
                    {
                        new BillableAccountDataVo
                        {
                            PayrollAccountsData = "test data",
                            PayrollAccountsInvoiceGroup = 1,
                            PayrollAccountsLineTitle = "test title"
                        }
                    }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Enabled = false,
                    ManagementFees = new List<ManagementFeeVo>
                    {
                        new ManagementFeeVo
                        {
                            FixedFeeAmount = 1000,
                            RevenuePercentageAmount = 10,
                            ManagementAgreementType = ManagementAgreementType.RevenuePercentage,
                            InvoiceGroup = 1,
                            LaborHourJobCode = "2222",
                            LaborHourOvertimeRate = 20,
                            LaborHourRate = 10,
                            InsuranceType = InsuranceType.BasedOnBillableAccounts
                        }   
                    }
                }
            };
            _contractService.GetContractDetail(customerSiteId).Returns(contractVo);

            var expectedContractDto = new ContractDetailDto
            {
                Id = contractVo.Id,
                PurchaseOrder = contractVo.PurchaseOrder,
                PaymentTerms = contractVo.PaymentTerms,
                BillingType = contractVo.BillingType.ToString(),
                IncrementMonth = contractVo.IncrementMonth.ToString(),
                IncrementAmount = contractVo.IncrementAmount,
                ConsumerPriceIndex = contractVo.ConsumerPriceIndex,
                Notes = contractVo.Notes,
                PerOccupiedRoom = new ContractDetailDto.PerOccupiedRoomDto
                {
                    Enabled = contractVo.PerOccupiedRoom.Enabled,
                    RoomRate = contractVo.PerOccupiedRoom.RoomRate
                },
                FixedFee = new ContractDetailDto.FixedFeeDto
                {
                    Enabled = contractVo.FixedFee.Enabled,
                    ServiceRates = contractVo.FixedFee.ServiceRates.Select(sr => new ContractDetailDto.ServiceRateDto
                    {
                        Id = sr.Id,
                        Name = sr.Name,
                        DisplayName = sr.DisplayName,
                        Fee = sr.Fee
                    }).ToList()
                },
                PerLaborHour = new ContractDetailDto.PerLaborHourDto
                {
                    Enabled = contractVo.PerLaborHour.Enabled,
                    JobRates = contractVo.PerLaborHour.JobRates.Select(jr => new ContractDetailDto.JobRateDto
                    {
                        Id = jr.Id,
                        Name = jr.Name,
                        DisplayName = jr.DisplayName,
                        Rate = jr.Rate,
                        OvertimeRate = jr.OvertimeRate,
                        JobCode = jr.JobCode,
                        InvoiceGroup = jr.InvoiceGroup
                    }).ToList()
                },
                RevenueShare = new ContractDetailDto.RevenueShareDto
                {
                    Enabled = contractVo.RevenueShare.Enabled,
                    ThresholdStructures = contractVo.RevenueShare.ThresholdStructures.Select(ts => new ContractDetailDto.ThresholdStructureDto
                    {
                        Id = ts.Id,
                        AccumulationType = ts.AccumulationType.ToString(),
                        RevenueCodes = ts.RevenueCodes,
                        Tiers = ts.Tiers.Select(t => new ContractDetailDto.TierDto
                        {
                            SharePercentage = t.SharePercentage,
                            Amount = t.Amount
                        }).ToList(),
                        ValidationThresholdType = ts.ValidationThresholdType.ToString(),
                        ValidationThresholdAmount = ts.ValidationThresholdAmount,
                        InvoiceGroup = ts.InvoiceGroup
                    }).ToList()
                },
                BellServiceFee = new ContractDetailDto.BellServiceFeeDto
                {
                    Enabled = true,
                },
                MidMonthAdvance = new ContractDetailDto.MidMonthAdvanceDto
                {
                    Enabled = true,
                    MidMonthAdvances = contractVo.MidMonthAdvance.MidMonthAdvances.Select(m => new ContractDetailDto.MidMonthDto
                    {
                        Amount = m.Amount,
                        InvoiceGroup = m.InvoiceGroup,
                        LineTitle = m.LineTitle.ToString()
                    }).ToList()
                },
                DepositedRevenue = new ContractDetailDto.DepositedRevenueDto
                {
                    Enabled = true,
                    DepositData = contractVo.DepositedRevenue.DepositData.Select(d => new ContractDetailDto.DepositDataDto
                    {
                        TowneParkResponsibleForParkingTax = d.TowneParkResponsibleForParkingTax,
                        InvoiceGroup = d.InvoiceGroup
                    }).ToList()
                },
                BillableAccount = new ContractDetailDto.BillableAccountDto
                {
                    Enabled = true,
                    BillableAccountsData = contractVo.BillableAccount.BillableAccountsData.Select(b => new ContractDetailDto.BillableAccountDataDto
                    {
                        PayrollAccountsData = b.PayrollAccountsData,
                        PayrollAccountsInvoiceGroup = b.PayrollAccountsInvoiceGroup,
                        PayrollAccountsLineTitle = b.PayrollAccountsLineTitle
                    }).ToList()
                },
                ManagementAgreement = new ContractDetailDto.ManagementAgreementDto
                {
                    Enabled = false,
                    ManagementFees = contractVo.ManagementAgreement.ManagementFees.Select(m => new ContractDetailDto.ManagementFeeDto
                    {
                        FixedFeeAmount = m.FixedFeeAmount,
                        RevenuePercentageAmount = m.RevenuePercentageAmount,
                        ManagementAgreementType = m.ManagementAgreementType.ToString(),
                        InvoiceGroup = m.InvoiceGroup,
                        LaborHourJobCode = m.LaborHourJobCode,
                        LaborHourOvertimeRate = m.LaborHourOvertimeRate,
                        LaborHourRate = m.LaborHourRate,
                        InsuranceType = m.InsuranceType.ToString()
                    }).ToList()
                }
            };

            var result = _contractServiceAdapter.GetContractDetail(customerSiteId);

            result.Should().BeEquivalentTo(expectedContractDto);
        }

        [Fact]
        public void UpdateContract_ShouldCallContractServiceWithMappedVo()
        {
            var contractId = Guid.NewGuid();

            var updateContractDto = new ContractDetailDto()
            { 
                Id = Guid.NewGuid(),
                PurchaseOrder = "PO-123",
                PaymentTerms = "Due in 30 days",
                BillingType = "Advanced",
                IncrementAmount = 1.05m,
                IncrementMonth = "January",
                ConsumerPriceIndex = true,
                Notes = "Some notes",
                PerOccupiedRoom = new ContractDetailDto.PerOccupiedRoomDto
                {
                    Enabled = true,
                    RoomRate = 10.00m
                },
                FixedFee = new ContractDetailDto.FixedFeeDto
                {
                    Enabled = true,
                    ServiceRates = new List<ContractDetailDto.ServiceRateDto>
                    {
                        new ContractDetailDto.ServiceRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Valet",
                            DisplayName = "Valet Service",
                            Fee = 1000
                        },
                        new ContractDetailDto.ServiceRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Shuttle",
                            DisplayName = "Shuttle Service",
                            Fee = 3000
                        }
                    }
                },
                PerLaborHour = new ContractDetailDto.PerLaborHourDto
                {
                    Enabled = true,
                    JobRates = new List<ContractDetailDto.JobRateDto>
                    {
                        new ContractDetailDto.JobRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Valet",
                            DisplayName = "Valet Service",
                            Rate = 100,
                            OvertimeRate = 150,
                            JobCode = "123",
                            InvoiceGroup = 1
                        },
                        new ContractDetailDto.JobRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Shuttle",
                            DisplayName = "Shuttle Service",
                            Rate = 200,
                            OvertimeRate = 300,
                            JobCode = "456",
                            InvoiceGroup = 2
                        }
                    }
                },
                RevenueShare = new ContractDetailDto.RevenueShareDto
                {
                    Enabled = true,
                    ThresholdStructures = new List<ContractDetailDto.ThresholdStructureDto>
                    {
                        new ContractDetailDto.ThresholdStructureDto
                        {
                            Id = Guid.NewGuid(),
                            AccumulationType = "Monthly",
                            RevenueCodes = new List<string> { "123", "456" },
                            Tiers = new List<ContractDetailDto.TierDto>
                            {
                                new ContractDetailDto.TierDto
                                {
                                    SharePercentage = 10,
                                    Amount = null
                                }
                            }
                        }
                    }
                },
                BellServiceFee = new ContractDetailDto.BellServiceFeeDto
                {
                    Enabled = true,
                }
            };
                
            var expectedVo = new ContractDetailVo
            {
                Id = updateContractDto.Id,
                PurchaseOrder = updateContractDto.PurchaseOrder,
                PaymentTerms = updateContractDto.PaymentTerms,
                BillingType = BillingType.Advanced,
                IncrementAmount = updateContractDto.IncrementAmount,
                IncrementMonth = Month.January,
                ConsumerPriceIndex = true,
                Notes = updateContractDto.Notes,
                PerOccupiedRoom = new ContractDetailVo.PerOccupiedRoomVo
                {
                    Enabled = true,
                    RoomRate = 10.00m
                },
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    Enabled = true,
                    ServiceRates = updateContractDto.FixedFee.ServiceRates.Select(sr => new ContractDetailVo.ServiceRateVo
                    {
                        Id = sr.Id,
                        Name = sr.Name,
                        DisplayName = sr.DisplayName,
                        Fee = sr.Fee
                    }).ToList()
                },
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    Enabled = true,
                    JobRates = updateContractDto.PerLaborHour.JobRates.Select(jr => new ContractDetailVo.JobRateVo
                    {
                        Id = jr.Id,
                        Name = jr.Name,
                        DisplayName = jr.DisplayName,
                        Rate = jr.Rate,
                        OvertimeRate = jr.OvertimeRate,
                        JobCode = jr.JobCode,
                        InvoiceGroup = jr.InvoiceGroup
                    }).ToList()
                },
                RevenueShare = new RevenueShareVo
                {
                    Enabled = true,
                    ThresholdStructures = updateContractDto.RevenueShare.ThresholdStructures.Select(ts => new ThresholdStructureVo
                    {
                        Id = ts.Id,
                        AccumulationType = AccumulationType.Monthly,
                        RevenueCodes = ts.RevenueCodes,
                        Tiers = ts.Tiers.Select(t => new TierVo
                        {
                            SharePercentage = t.SharePercentage,
                            Amount = t.Amount
                        }).ToList()
                    }).ToList()
                },
                BellServiceFee = new BellServiceFeeVo
                {
                    Enabled = true,
                }
            };

            _contractServiceAdapter.UpdateContract(contractId, updateContractDto);

            _contractService.Received(1).UpdateContract(contractId, Arg.Is<ContractDetailVo>(
                vo => expectedVo.IncrementAmount.Equals(vo.IncrementAmount)  &&
                      vo.IncrementMonth == expectedVo.IncrementMonth
            ));
        }

        [Fact]
        public void GetDeviations_ShouldReturnListOfDeviationDtos()
        {
            var deviationVos = new List<DeviationVo>
            {
                new DeviationVo
                {
                    ContractId = Guid.NewGuid(),
                    DeviationAmount = 1000.00m,
                    DeviationPercentage = 10.00m,
                    CustomerSiteId = Guid.NewGuid(),
                    SiteName = "Site 1",
                    SiteNumber = "1"
                },
                new DeviationVo
                {
                    ContractId = Guid.NewGuid(),
                    DeviationAmount = 2000.00m,
                    DeviationPercentage = 20.00m,
                    CustomerSiteId = Guid.NewGuid(),
                    SiteName = "Site 2",
                    SiteNumber = "2"
                }
            };

            _contractService.GetDeviations().Returns(deviationVos);

            var expectedDeviationDtos = deviationVos.Select(d => new DeviationDto
            {
                ContractId = d.ContractId,
                DeviationAmount = d.DeviationAmount,
                DeviationPercentage = d.DeviationPercentage,
                CustomerSiteId = d.CustomerSiteId,
                SiteName = d.SiteName,
                SiteNumber = d.SiteNumber
            });

            var result = _contractServiceAdapter.GetDeviations();

            result.Should().BeEquivalentTo(expectedDeviationDtos);
        }

        [Fact]
        public void UpdateDeviationThreshold_ShouldCallContractServiceWithMappedVo()
        {
            var updateDeviationDtos = new List<DeviationDto>
            {
                new DeviationDto
                {
                    ContractId = Guid.NewGuid(),
                    DeviationAmount = 1000.00m,
                    DeviationPercentage = 10.00m,
                },
                new DeviationDto
                {
                    ContractId = Guid.NewGuid(),
                    DeviationAmount = 2000.00m,
                    DeviationPercentage = 20.00m,
                }
            };

            var expectedVo = updateDeviationDtos.Select(d => new DeviationVo
            {
                ContractId = d.ContractId,
                DeviationAmount = d.DeviationAmount,
                DeviationPercentage = d.DeviationPercentage
            });

            _contractServiceAdapter.UpdateDeviationThreshold(updateDeviationDtos);

            _contractService.Received(1).UpdateDeviationThreshold(Arg.Is<IEnumerable<DeviationVo>>(
                vo => vo.Count() == expectedVo.Count() &&
                      vo.All(v => expectedVo.Any(e => e.ContractId == v.ContractId &&
                                                      e.DeviationAmount == v.DeviationAmount &&
                                                      e.DeviationPercentage == v.DeviationPercentage)
                      )
            ));
        }
    }
}
