namespace Lockdown.Build.RawEntities
{
    using System.Collections.Generic;
    using System.Dynamic;

    public class DynamicDictionary : DynamicObject
    {
        private readonly Dictionary<string, string> dictionary;

        public DynamicDictionary(Dictionary<string, string> dictionary)
        {
            this.dictionary = dictionary;
        }

        public override bool TryGetMember(
            GetMemberBinder binder, out object result)
        {
            string outValue;
            this.dictionary.TryGetValue(binder.Name, out outValue);
            result = outValue;

            return true;
        }

        public override bool TrySetMember(
            SetMemberBinder binder, object value)
        {
            this.dictionary[binder.Name] = (string)value;

            return true;
        }
    }
}
