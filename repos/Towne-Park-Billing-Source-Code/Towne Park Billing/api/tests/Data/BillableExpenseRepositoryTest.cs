using System;
using System.Collections.Generic;
using System.Linq;
using api.Data.Impl;
using api.Services;
using api.Data; // For ExpenseActualsData
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class BillableExpenseRepositoryTest
    {
        private readonly IDataverseService _dataverseServiceMock;
        private readonly IOrganizationService _serviceClientMock;
        private readonly BillableExpenseRepository _billableExpenseRepository;

        public BillableExpenseRepositoryTest()
        {
            _dataverseServiceMock = Substitute.For<IDataverseService>();
            _serviceClientMock = Substitute.For<IOrganizationService>();
            _dataverseServiceMock.GetServiceClient().Returns(_serviceClientMock);
            _billableExpenseRepository = new BillableExpenseRepository(_dataverseServiceMock);
        }

        [Fact]
        public void GetEnabledExpenseAccounts_WithValidData_ShouldReturnEnabledAccounts()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var expenseAccountsJson = @"[
                {""code"": ""7045"", ""title"": ""Employee Relations Expense"", ""isEnabled"": true},
                {""code"": ""7075"", ""title"": ""Fuel - Vehicles Expense"", ""isEnabled"": false},
                {""code"": ""7100"", ""title"": ""Loss & Damage Expense"", ""isEnabled"": true}
            ]";

            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_BillableAccount
                {
                    [bs_BillableAccount.Fields.bs_ExpenseAccountsData] = expenseAccountsJson
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Is<QueryExpression>(query =>
                    ValidateBillableAccountQuery(query, siteId)))
                .Returns(expectedEntities);

            // Act
            var result = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain("7045");
            result.Should().Contain("7100");
            result.Should().NotContain("7075");
        }

        [Fact]
        public void GetEnabledExpenseAccounts_WithEmptyJson_ShouldReturnEmptyList()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_BillableAccount
                {
                    [bs_BillableAccount.Fields.bs_ExpenseAccountsData] = "[]"
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(expectedEntities);

            // Act
            var result = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetEnabledExpenseAccounts_WithNullJson_ShouldReturnEmptyList()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_BillableAccount
                {
                    [bs_BillableAccount.Fields.bs_ExpenseAccountsData] = null
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(expectedEntities);

            // Act
            var result = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetEnabledExpenseAccounts_WithMalformedJson_ShouldReturnEmptyList()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_BillableAccount
                {
                    [bs_BillableAccount.Fields.bs_ExpenseAccountsData] = "invalid json"
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(expectedEntities);

            // Act
            var result = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetEnabledExpenseAccounts_WithNoRecordsFound_ShouldReturnEmptyList()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var emptyEntities = new EntityCollection();

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(emptyEntities);

            // Act
            var result = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetEnabledExpenseAccounts_WithAllDisabledAccounts_ShouldReturnEmptyList()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var expenseAccountsJson = @"[
                {""code"": ""7045"", ""title"": ""Employee Relations Expense"", ""isEnabled"": false},
                {""code"": ""7075"", ""title"": ""Fuel - Vehicles Expense"", ""isEnabled"": false}
            ]";

            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_BillableAccount
                {
                    [bs_BillableAccount.Fields.bs_ExpenseAccountsData] = expenseAccountsJson
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(expectedEntities);

            // Act
            var result = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetEnabledExpenseAccounts_WithException_ShouldReturnEmptyList()
        {
            // Arrange
            var siteId = Guid.NewGuid();

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Throws(new Exception("Database error"));

            // Act
            var result = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetExpenseActualsForSites_WithValidData_ShouldReturnExpenseActuals()
        {
            // Arrange
            var siteIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var year = 2024;
            var monthOneBased = 7;

            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_BillableExpense
                {
                    [bs_BillableExpense.Fields.bs_SiteId] = new EntityReference("bs_customersite", siteIds[0]),
                    [bs_BillableExpense.Fields.bs_BillableExpenseActuals] = 1500.00m,
                    [bs_BillableExpense.Fields.bs_OtherExpenseActuals] = 750.00m
                },
                new bs_BillableExpense
                {
                    [bs_BillableExpense.Fields.bs_SiteId] = new EntityReference("bs_customersite", siteIds[1]),
                    [bs_BillableExpense.Fields.bs_BillableExpenseActuals] = 2000.00m,
                    [bs_BillableExpense.Fields.bs_OtherExpenseActuals] = 1000.00m
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(expectedEntities);

            // Act
            var result = _billableExpenseRepository.GetExpenseActualsForSites(siteIds, year, monthOneBased);

            // Assert
            result.Should().HaveCount(2);
            result[0].SiteId.Should().Be(siteIds[0]);
            result[0].BillableExpenseActuals.Should().Be(1500.00m);
            result[0].OtherExpenseActuals.Should().Be(750.00m);
            result[1].SiteId.Should().Be(siteIds[1]);
            result[1].BillableExpenseActuals.Should().Be(2000.00m);
            result[1].OtherExpenseActuals.Should().Be(1000.00m);
        }

        [Fact]
        public void GetExpenseActualsForSites_WithEmptySiteList_ShouldReturnEmptyArray()
        {
            // Arrange
            var siteIds = new List<Guid>();
            var year = 2024;
            var monthOneBased = 7;

            // Act
            var result = _billableExpenseRepository.GetExpenseActualsForSites(siteIds, year, monthOneBased);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetExpenseActualsForSites_WithNoDataFound_ShouldReturnSitesWithZeroValues()
        {
            // Arrange
            var siteIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var year = 2024;
            var monthOneBased = 7;

            var expectedEntities = new EntityCollection(new List<Entity>());

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(expectedEntities);

            // Act
            var result = _billableExpenseRepository.GetExpenseActualsForSites(siteIds, year, monthOneBased);

            // Assert
            result.Should().HaveCount(2);
            result[0].SiteId.Should().Be(siteIds[0]);
            result[0].BillableExpenseActuals.Should().Be(0m);
            result[0].OtherExpenseActuals.Should().Be(0m);
            result[1].SiteId.Should().Be(siteIds[1]);
            result[1].BillableExpenseActuals.Should().Be(0m);
            result[1].OtherExpenseActuals.Should().Be(0m);
        }

        [Fact]
        public void GetExpenseActualsForSites_WithException_ShouldReturnEmptyArray()
        {
            // Arrange
            var siteIds = new List<Guid> { Guid.NewGuid() };
            var year = 2024;
            var monthOneBased = 7;

            _serviceClientMock
                .RetrieveMultiple(Arg.Any<QueryExpression>())
                .Throws(new Exception("Database error"));

            // Act
            var result = _billableExpenseRepository.GetExpenseActualsForSites(siteIds, year, monthOneBased);

            // Assert
            result.Should().BeEmpty();
        }

        private bool ValidateBillableAccountQuery(QueryExpression query, Guid expectedSiteId)
        {
            if (query.EntityName != bs_BillableAccount.EntityLogicalName)
                return false;

            if (!query.ColumnSet.Columns.Contains(bs_BillableAccount.Fields.bs_ExpenseAccountsData))
                return false;

            if (query.LinkEntities.Count != 1)
                return false;

            var linkEntity = query.LinkEntities[0];
            if (linkEntity.LinkFromEntityName != bs_BillableAccount.EntityLogicalName ||
                linkEntity.LinkFromAttributeName != bs_BillableAccount.Fields.bs_ContractFK ||
                linkEntity.LinkToEntityName != bs_Contract.EntityLogicalName ||
                linkEntity.LinkToAttributeName != bs_Contract.Fields.bs_ContractId)
                return false;

            var siteCondition = linkEntity.LinkCriteria.Conditions
                .FirstOrDefault(c => c.AttributeName == bs_Contract.Fields.bs_CustomerSiteFK);

            return siteCondition != null &&
                   siteCondition.Operator == ConditionOperator.Equal &&
                   siteCondition.Values.Contains(expectedSiteId);
        }

        private bool ValidateBillableExpenseQuery(QueryExpression query, Guid expectedSiteId)
        {
            if (query.EntityName != bs_BillableExpense.EntityLogicalName)
                return false;

            if (!query.ColumnSet.Columns.Contains(bs_BillableExpense.Fields.bs_SiteId))
                return false;

            if (query.LinkEntities.Count != 1)
                return false;

            var linkEntity = query.LinkEntities[0];
            if (linkEntity.LinkFromEntityName != bs_BillableExpense.EntityLogicalName ||
                linkEntity.LinkFromAttributeName != bs_BillableExpense.Fields.bs_SiteId ||
                linkEntity.LinkToEntityName != bs_CustomerSite.EntityLogicalName ||
                linkEntity.LinkToAttributeName != bs_CustomerSite.Fields.bs_CustomerSiteId)
                return false;

            var siteCondition = linkEntity.LinkCriteria.Conditions
                .FirstOrDefault(c => c.AttributeName == bs_CustomerSite.Fields.bs_CustomerSiteId);

            return siteCondition != null &&
                   siteCondition.Operator == ConditionOperator.Equal &&
                   siteCondition.Values.Contains(expectedSiteId);
        }
    }
}