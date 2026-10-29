using System.Collections.Immutable;
using System.Linq;

namespace DK.Gov.Oio.Saml.Constants
{
    public static class OioSaml4AttributeProfiles {
        public static readonly string DkPerson = "https://data.gov.dk/eid/Person/DK";
        
        public static readonly string DkPersonWithoutCpr = "https://data.gov.dk/eid/Person/DK/WithoutCPR";
        
        public static readonly string DkPersonAnonymous = "https://data.gov.dk/eid/Person/DK/Anonymous";
        
        public static readonly string DkProfessional = "https://data.gov.dk/eid/Professional/DK";
        
        public static readonly string DkProfessionalAnonymous = "https://data.gov.dk/eid/Professional/DK/Anonymous";
        
        public static readonly string EidasPerson = "https://data.gov.dk/eid/Person/EU";
        
        public static readonly string EidasPersonAnonymous = "https://data.gov.dk/eid/Person/EU/Anonymous";
        
        public static readonly string EidasLegalPerson = "https://data.gov.dk/eid/LegalPerson/EU";
        
        public static readonly string EidasProfessional = "https://data.gov.dk/eid/Professional/EU";

        public static readonly ImmutableArray<string> DkProfiles = new[] { 
            DkPerson, DkPersonAnonymous, DkPersonWithoutCpr,
            DkProfessional, DkProfessionalAnonymous
        }.ToImmutableArray();
        
        public static readonly ImmutableArray<string> EidasProfiles = new[] { 
            EidasPerson, EidasPersonAnonymous,
            EidasProfessional, EidasLegalPerson
        }.ToImmutableArray();
        
        public static readonly ImmutableArray<string> AllProfiles = DkProfiles.Concat(EidasProfiles).ToImmutableArray();
    }
}