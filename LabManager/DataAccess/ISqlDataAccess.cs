using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.DataAccess
{
    public interface ISqlDataAcess
    {
        Task<IEnumerable<T>> GetData<T, P>(String spName, P parameters, string connectionId = "conn");
        Task SaveData<T>(string spName, T parameters,string connectionId="conn");

    }
}
    