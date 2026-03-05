namespace SUI.Find.Application.Constants;

public static class ApplicationConstants
{
    public static class Auth
    {
        public const string AuthContextKey = "AuthContext";
    }

    public static class Audit
    {
        public const string AccessQueueName = "audit-queue";

        public static class HttpRequest
        {
            public const string EventName = "HTTP_REQUEST";
        }

        public static class PolicyEnforcementPoint
        {
            public const string FindEventName = "PEP_FIND";
            public const string FetchRequestEventName = "PEP_FETCH_REQUEST";
        }
    }

    public static class Providers
    {
        public const string LoggingName = "providers";
    }

    public static class SystemIds
    {
        public const string Default = "DefaultSystem";
    }
}
