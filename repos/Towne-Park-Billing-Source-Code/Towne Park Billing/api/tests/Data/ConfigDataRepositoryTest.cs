using System;
using System.Collections.Generic;
using System.Linq;
using api.Data;
using api.Data.Impl;
using api.Models.Vo.Enum;
using api.Services;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class ConfigDataRepositoryTest
    {
        private readonly IDataverseService _dataverseServiceMock;
        private readonly IOrganizationService _serviceClientMock;
        private readonly ConfigDataRepository _configDataRepository;

        public ConfigDataRepositoryTest()
        {
            _dataverseServiceMock = Substitute.For<IDataverseService>();
            _serviceClientMock = Substitute.For<IOrganizationService>();
            _dataverseServiceMock.GetServiceClient().Returns(_serviceClientMock);
            _configDataRepository = new ConfigDataRepository(_dataverseServiceMock);
        }

        [Fact]
        public void GetGlCodes_ShouldRetrieveCorrectData()
        {
            // Arrange
            var codeTypes = new List<bs_glcodetypechoices>
            {
                bs_glcodetypechoices.RevenueShare,
                bs_glcodetypechoices.ManagementAgreement
            };
            var codeTypeInts = codeTypes.Select(ct => (int)ct).ToArray();

            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_GLCodeConfig
                {
                    [bs_GLCodeConfig.Fields.bs_Code] = "4790",
                    [bs_GLCodeConfig.Fields.bs_Name] = "Revenue Share"
                },
                new bs_GLCodeConfig
                {
                    [bs_GLCodeConfig.Fields.bs_Code] = "4791",
                    [bs_GLCodeConfig.Fields.bs_Name] = "Management Agreement"
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Is<QueryBase>(query =>
                    ValidateQuery(query, bs_GLCodeConfig.EntityLogicalName, bs_GLCodeConfig.Fields.bs_Type, codeTypeInts)))
                .Returns(expectedEntities);

            // Act
            var result = _configDataRepository.GetGlCodes(codeTypes);

            // Assert
            result.Should().BeEquivalentTo(expectedEntities.Entities.Cast<bs_GLCodeConfig>());
        }

        private bool ValidateQuery(QueryBase queryBase, string expectedEntityName, string expectedFieldName, int[] expectedValues)
        {
            if (queryBase is QueryExpression query)
            {
                var condition = query.Criteria.Conditions.FirstOrDefault(c => c.AttributeName == expectedFieldName);
                return query.EntityName == expectedEntityName &&
                       condition != null &&
                       condition.Operator == ConditionOperator.In &&
                       condition.Values.Cast<int>().SequenceEqual(expectedValues);
            }
            return false;
        }

        [Fact]
        public void GetInvoiceConfigData_ShouldRetrieveCorrectData()
        {
            // Arrange
            var configGroup = bs_generalconfiggroupchoices.InvoiceHeaderFooter;

            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_GeneralConfig
                {
                    [bs_GeneralConfig.Fields.bs_Key] = bs_generalconfigchoices.TowneParksLegalName,
                    [bs_GeneralConfig.Fields.bs_Value] = "Towne Park, LLC"
                },
                new bs_GeneralConfig
                {
                    [bs_GeneralConfig.Fields.bs_Key] = bs_generalconfigchoices.TowneParksEmail,
                    [bs_GeneralConfig.Fields.bs_Value] = "towne.park@townepark.com"
                }
            });

            _serviceClientMock.RetrieveMultiple(Arg.Any<QueryExpression>()).Returns(expectedEntities);

            // Act
            var result = _configDataRepository.GetInvoiceConfigData(configGroup);

            // Assert
            result.Should().BeEquivalentTo(expectedEntities.Entities.Cast<bs_GeneralConfig>());
        }
    }
}
