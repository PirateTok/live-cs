using System;

namespace TikTokLive.Errors
{
    public class TikTokLiveException : Exception
    {
        public TikTokLiveException(string message) : base(message) { }
        public TikTokLiveException(string message, Exception inner) : base(message, inner) { }
    }

    public class UserNotFoundException : TikTokLiveException
    {
        public string Username { get; }
        public UserNotFoundException(string username)
            : base($"user not found: {username}") => Username = username;
    }

    public class HostNotOnlineException : TikTokLiveException
    {
        public string Reason { get; }
        public HostNotOnlineException(string reason)
            : base($"host not online: {reason}") => Reason = reason;
    }

    public class RoomIdMissingException : TikTokLiveException
    {
        public RoomIdMissingException()
            : base("room id missing from response") { }
    }

    public class AgeRestrictedException : TikTokLiveException
    {
        public AgeRestrictedException(string message)
            : base($"age-restricted stream: {message}") { }
    }

    public class DeviceBlockedException : TikTokLiveException
    {
        public DeviceBlockedException()
            : base("device blocked — ttwid was flagged, fetch a fresh one") { }
        public DeviceBlockedException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class TikTokApiException : TikTokLiveException
    {
        public long StatusCode { get; }
        public TikTokApiException(long statusCode)
            : base($"tiktok api error: statusCode={statusCode}") => StatusCode = statusCode;
    }

    public class TikTokBlockedException : TikTokLiveException
    {
        public int HttpStatus { get; }
        public TikTokBlockedException(int httpStatus, string detail)
            : base($"tiktok blocked (HTTP {httpStatus}): {detail}") => HttpStatus = httpStatus;
    }

    public class ConnectionClosedException : TikTokLiveException
    {
        public ConnectionClosedException()
            : base("connection closed") { }
    }

    public class ProtocolException : TikTokLiveException
    {
        public ProtocolException(string message)
            : base($"protocol error: {message}") { }
        public ProtocolException(string message, Exception inner)
            : base($"protocol error: {message}", inner) { }
    }

    public class ProfilePrivateException : TikTokLiveException
    {
        public string Username { get; }
        public ProfilePrivateException(string username)
            : base($"profile is private: @{username}") => Username = username;
    }

    public class ProfileNotFoundException : TikTokLiveException
    {
        public string Username { get; }
        public ProfileNotFoundException(string username)
            : base($"profile not found: @{username}") => Username = username;
    }

    public class ProfileScrapeException : TikTokLiveException
    {
        public ProfileScrapeException(string reason)
            : base($"failed to scrape profile: {reason}") { }
    }

    public class ProfileErrorException : TikTokLiveException
    {
        public long Code { get; }
        public ProfileErrorException(long code)
            : base($"profile fetch error: statusCode={code}") => Code = code;
    }
}
