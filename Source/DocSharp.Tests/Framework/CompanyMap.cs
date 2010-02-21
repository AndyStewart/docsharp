using DocSharp.Framework;
using DocSharp.Tests.TestFixtures;

namespace DocSharp.Tests.Framework
{
    public class CompanyMap : IMap<Company>
    {
        public void Map(DocumentMap<Company> map)
        {
            map.Id(q => q.Id);
        }
    }
}