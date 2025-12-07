#define CONTRACTS_FULL
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests;

public class MyDictionaryContractTests
{

}

[ContractClass(typeof(MyDictionaryContracts))]
interface IMyDictionary : IDictionary
{

    bool TryGetValue(string key, out object value);

    bool ContainsKey(string key);
}

class MyDictionary : Hashtable, IMyDictionary
{
    public bool ContainsKey(string key)
    {
        return this.Contains(key);
    }

    public bool TryGetValue(string key, out object value)
    {
        if (this.ContainsKey(key))
        {
            value = this[key]!;
            return true;
        }
        else
        {
            value = null!;
            return false;
        }
    }
}

[ContractClassFor(typeof(IMyDictionary))]
class MyDictionaryContracts
{
    public MyDictionaryContracts()
    {
        Dbc.Def(static (IMyDictionary dict, string key) => dict.ContainsKey(key)
                // static (IMyDictionary dict, string key, out object value) => Contract.Ensures(Contract.Result<bool>() == false || Contract.ValueAtReturn<object>(out value) != null, "Out parameter 'value' must be non-null when key exists"),
                // static (IMyDictionary dict, string key, out object value) => Contract.Requires(key != null, "Key cannot be null"),
                // static (IMyDictionary dict, string key, out object value) => Contract.Ensures(value == (Contract.OldValue<IMyDictionary>(dict).ContainsKey(key) ? Contract.OldValue<object>(dict[key]) : default), "Out parameter 'value' must be correct"),
                // static (IMyDictionary dict, string key, out object value) => Contract.Ensures(Contract.Result<bool>() == (Contract.OldValue<IMyDictionary>(dict).ContainsKey(key)), "Return value must indicate if key exists")
                );
    }
}