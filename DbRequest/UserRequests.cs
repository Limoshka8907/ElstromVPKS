using ElstromVPKS.Models;
using Microsoft.EntityFrameworkCore;
namespace ElstromVPKS.DbRequest
{
    public class UserRequests
    {

        private ElstromContext _elstromContext;


        public UserRequests(ElstromContext context)
        {
            _elstromContext = context;
        }

        public async Task<Employee> GetEmployeeByLogin(string login)
        {
            var userEntity = await _elstromContext.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Username == login) ?? throw new Exception();

            return userEntity;
        }

    }
}
