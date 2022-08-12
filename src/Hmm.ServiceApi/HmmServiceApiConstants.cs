namespace Hmm.ServiceApi
{
    public static class HmmServiceApiConstants
    {
        // ToDo: Add fields parameter for all single entities
        // ToDo: Add fields parameter for all collection entities
        // ToDo: Add user management via IDP
        public static class Policy
        {
            public const string MustOwnGasLog = "MusOwnGasLog";

            public const string SubjectMustMatchUser = "SubjectMusMatchUser";
        }

        public static class HttpClient
        {
            public const string Idp = "Idpclient";
        }

        private const string CollectionParameterName = "resourceParameters";

        private const string FieldsParameterName = "fields";

        public static bool IsCollectionParameter(this string parameterName) => !string.IsNullOrEmpty(parameterName) &&
                                                                               parameterName.Trim().ToLower()
                                                                                   .Equals(CollectionParameterName
                                                                                       .ToLower());
        public static bool IsFieldsParameter(this string parameterName) => !string.IsNullOrEmpty(parameterName) &&
                                                                               parameterName.Trim().ToLower()
                                                                                   .Equals(FieldsParameterName
                                                                                       .ToLower());
    }
}