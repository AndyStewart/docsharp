using System;
using System.Collections.Generic;

namespace DocSharp.Tests.TestFixtures
{
    public class Company
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public List<Contact> Contacts { get; set; }
    }
}