using api.Services;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownePark;

namespace api.Data.Impl
{
    public class UserRepository : IUserRepository
    {
        private readonly IDataverseService _dataverseService;

        public UserRepository(IDataverseService dataverseService) 
        {
            _dataverseService = dataverseService;
        }

        public bs_User GetUserRoles(string email)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var query = new QueryExpression(bs_User.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_User.Fields.bs_FirstName, bs_User.Fields.bs_LastName,
                                        bs_User.Fields.bs_SystemUserID, bs_User.Fields.bs_Email,
                                        bs_User.Fields.bs_UserId, bs_User.Fields.bs_Roles,
                                        bs_User.Fields.bs_Name)
            };

            query.Criteria.AddCondition(bs_User.Fields.bs_Email, ConditionOperator.Equal, email);

            var user = serviceClient.RetrieveMultiple(query).Entities.FirstOrDefault();

            return user.ToEntity<bs_User>();
        }
    }
}
