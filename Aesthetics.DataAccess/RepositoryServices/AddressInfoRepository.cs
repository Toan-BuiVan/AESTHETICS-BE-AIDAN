using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryServices
{
    public class AddressInfoRepository : CommonRepository<AddressInfoEntity>, IAddressInfoRepository
    {
        public AddressInfoRepository(ILogger<CommonRepository<AddressInfoEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) 
            : base(logger, dbContext)
        {
        }

    }
}